using Duett.API.Data;
using Duett.API.Models;
using Duett.API.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace Duett.API.Repositories;

public class GameRepository : IGameRepository
{
    private readonly ApplicationDbContext _context;

    public GameRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UserMatch?> GetMatchForUserAsync(
        Guid matchId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.UserMatches
            .AsNoTracking()
            .SingleOrDefaultAsync(
                match => match.Id == matchId
                    && (match.FirstUserId == userId
                        || match.SecondUserId == userId),
                cancellationToken);
    }

    public async Task<GameRound?> GetRoundForUserAsync(
        Guid roundId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.GameRounds
            .AsNoTracking()
            .Include(round => round.Question)
                .ThenInclude(question => question.Options)
            .SingleOrDefaultAsync(
                round => round.Id == roundId
                    && (round.Match.FirstUserId == userId
                        || round.Match.SecondUserId == userId),
                cancellationToken);
    }

    public async Task<List<GameRoundSubmission>> GetSubmissionsForUserAsync(
        Guid roundId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.GameRoundSubmissions
            .AsNoTracking()
            .Where(submission =>
                submission.RoundId == roundId
                && (submission.Round.Match.FirstUserId == userId
                    || submission.Round.Match.SecondUserId == userId))
            .ToListAsync(cancellationToken);
    }

    public async Task<StartGameRoundResult> GetOrCreateCurrentRoundAsync(
        Guid matchId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                cancellationToken);

        // Lås matchningen medan vi kontrollerar och skapar rundan.
        // Parametrarna skickas separat från SQL-texten.
        var match = await _context.UserMatches
            .FromSqlInterpolated($"""
                SELECT *
                FROM [dbo].[UserMatches] WITH (UPDLOCK, HOLDLOCK)
                WHERE [Id] = {matchId}
                  AND ([FirstUserId] = {userId}
                       OR [SecondUserId] = {userId})
                """)
            .AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken);

        if (match is null)
        {
            return new StartGameRoundResult
            {
                Status = StartGameRoundStatus.MatchNotFound
            };
        }

        // Återanvänd en pågående runda.
        var currentRound = await _context.GameRounds
            .AsNoTracking()
            .Include(round => round.Question)
                .ThenInclude(question => question.Options)
            .Where(round =>
                round.MatchId == matchId
                && round.CompletedAtUtc == null)
            .OrderBy(round => round.RoundNumber)
            .FirstOrDefaultAsync(cancellationToken);

        if (currentRound is not null)
        {
            await transaction.CommitAsync(cancellationToken);

            return new StartGameRoundResult
            {
                Status = StartGameRoundStatus.Success,
                Round = currentRound
            };
        }

        // Räkna rätta gissningar i avslutade rundor.
        // Varje bidrag jämförs med den andra deltagarens svar.
        var correctGuesses =
            from submission in _context.GameRoundSubmissions
            join partnerSubmission in _context.GameRoundSubmissions
                on submission.RoundId equals partnerSubmission.RoundId
            where submission.Round.MatchId == matchId
                && submission.Round.CompletedAtUtc != null
                && (
                    (submission.UserId == match.FirstUserId
                        && partnerSubmission.UserId == match.SecondUserId)
                    ||
                    (submission.UserId == match.SecondUserId
                        && partnerSubmission.UserId == match.FirstUserId)
                )
                && submission.GuessOptionId
                    == partnerSubmission.AnswerOptionId
            select submission;

        var firstUserCorrectCount = await correctGuesses.CountAsync(
            submission => submission.UserId == match.FirstUserId,
            cancellationToken);

        var secondUserCorrectCount = await correctGuesses.CountAsync(
            submission => submission.UserId == match.SecondUserId,
            cancellationToken);

        // Spelet är klart först när båda har minst tre rätt.
        if (firstUserCorrectCount >= 3 && secondUserCorrectCount >= 3)
        {
            return new StartGameRoundResult
            {
                Status = StartGameRoundStatus.GameCompleted
            };
        }

        // Välj en aktiv fråga med minst två alternativ
        // som paret inte redan har spelat.
        var question = await _context.GameQuestions
            .Include(question => question.Options)
            .Where(question =>
                question.IsActive
                && question.Options.Count >= 2
                && !_context.GameRounds.Any(round =>
                    round.MatchId == matchId
                    && round.QuestionId == question.Id))
            .OrderBy(question => question.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (question is null)
        {
            return new StartGameRoundResult
            {
                Status = StartGameRoundStatus.NoQuestionsAvailable
            };
        }

        var lastRoundNumber = await _context.GameRounds
            .Where(round => round.MatchId == matchId)
            .Select(round => (int?)round.RoundNumber)
            .MaxAsync(cancellationToken) ?? 0;

        var newRound = new GameRound
        {
            Id = Guid.NewGuid(),
            MatchId = matchId,
            RoundNumber = lastRoundNumber + 1,
            QuestionId = question.Id,
            Question = question,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.GameRounds.Add(newRound);

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new StartGameRoundResult
        {
            Status = StartGameRoundStatus.Success,
            Round = newRound
        };
    }

    public async Task<SubmitGameRoundStatus> SubmitAsync(
        Guid roundId,
        string userId,
        Guid answerOptionId,
        Guid guessOptionId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        // Hitta matchningen och kontrollera deltagaren.
        var matchId = await _context.GameRounds
            .AsNoTracking()
            .Where(round =>
                round.Id == roundId
                && (round.Match.FirstUserId == userId
                    || round.Match.SecondUserId == userId))
            .Select(round => (Guid?)round.MatchId)
            .SingleOrDefaultAsync(cancellationToken);

        if (matchId is null)
        {
            return SubmitGameRoundStatus.RoundNotFound;
        }

        var matchIdValue = matchId.Value;

        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                cancellationToken);

        // Samma lås som vid spelstart.
        var match = await _context.UserMatches
            .FromSqlInterpolated($"""
                SELECT *
                FROM [dbo].[UserMatches] WITH (UPDLOCK, HOLDLOCK)
                WHERE [Id] = {matchIdValue}
                  AND ([FirstUserId] = {userId}
                       OR [SecondUserId] = {userId})
                """)
            .AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken);

        if (match is null)
        {
            return SubmitGameRoundStatus.RoundNotFound;
        }

        // Hämta rundans tillstånd efter att låset tagits.
        // Spårning behövs för att uppdatera CompletedAtUtc.
        var round = await _context.GameRounds
            .SingleOrDefaultAsync(
                round => round.Id == roundId
                    && round.MatchId == matchIdValue,
                cancellationToken);

        if (round is null)
        {
            return SubmitGameRoundStatus.RoundNotFound;
        }

        var alreadySubmitted = await _context.GameRoundSubmissions
            .AnyAsync(
                submission => submission.RoundId == roundId
                    && submission.UserId == userId,
                cancellationToken);

        if (alreadySubmitted)
        {
            return SubmitGameRoundStatus.AlreadySubmitted;
        }

        if (round.CompletedAtUtc is not null)
        {
            return SubmitGameRoundStatus.RoundCompleted;
        }

        if (answerOptionId == Guid.Empty || guessOptionId == Guid.Empty)
        {
            return SubmitGameRoundStatus.InvalidOption;
        }

        // Båda alternativen måste tillhöra rundans fråga.
        var answerIsValid = await _context.GameQuestionOptions
            .AnyAsync(
                option => option.Id == answerOptionId
                    && option.QuestionId == round.QuestionId,
                cancellationToken);

        var guessIsValid = await _context.GameQuestionOptions
            .AnyAsync(
                option => option.Id == guessOptionId
                    && option.QuestionId == round.QuestionId,
                cancellationToken);

        if (!answerIsValid || !guessIsValid)
        {
            return SubmitGameRoundStatus.InvalidOption;
        }

        var partnerUserId = string.Equals(
            match.FirstUserId,
            userId,
            StringComparison.OrdinalIgnoreCase)
                ? match.SecondUserId
                : match.FirstUserId;

        var partnerHasSubmitted = await _context.GameRoundSubmissions
            .AnyAsync(
                submission => submission.RoundId == roundId
                    && submission.UserId == partnerUserId,
                cancellationToken);

        var now = DateTime.UtcNow;

        _context.GameRoundSubmissions.Add(new GameRoundSubmission
        {
            RoundId = roundId,
            UserId = userId,
            AnswerOptionId = answerOptionId,
            GuessOptionId = guessOptionId,
            SubmittedAtUtc = now
        });

        if (partnerHasSubmitted)
        {
            round.CompletedAtUtc = now;
        }

        // Bidraget och eventuell avslutning sparas tillsammans.
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return SubmitGameRoundStatus.Success;
    }





    public async Task<GameProgress?> GetProgressForUserAsync(
    Guid matchId,
    string userId,
    CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        // Bara deltagarna får se matchningens framsteg.
        var match = await GetMatchForUserAsync(
            matchId,
            userId,
            cancellationToken);

        if (match is null)
        {
            return null;
        }

        var partnerUserId = string.Equals(
            match.FirstUserId,
            userId,
            StringComparison.OrdinalIgnoreCase)
                ? match.SecondUserId
                : match.FirstUserId;

        // Jämför varje deltagares gissning med partnerns svar.
        // Endast avslutade rundor räknas.
        var correctGuesses =
            from submission in _context.GameRoundSubmissions
            join partnerSubmission in _context.GameRoundSubmissions
                on submission.RoundId equals partnerSubmission.RoundId
            where submission.Round.MatchId == matchId
                && submission.Round.CompletedAtUtc != null
                && (
                    (submission.UserId == match.FirstUserId
                        && partnerSubmission.UserId == match.SecondUserId)
                    ||
                    (submission.UserId == match.SecondUserId
                        && partnerSubmission.UserId == match.FirstUserId)
                )
                && submission.GuessOptionId
                    == partnerSubmission.AnswerOptionId
            select submission;

        // Hämta bådas antal i samma databasfråga.
        var counts = await correctGuesses
            .GroupBy(submission => submission.UserId)
            .Select(group => new
            {
                UserId = group.Key,
                CorrectCount = group.Count()
            })
            .ToListAsync(cancellationToken);

        var myCount = counts
            .FirstOrDefault(item => string.Equals(
                item.UserId,
                userId,
                StringComparison.OrdinalIgnoreCase))
            ?.CorrectCount ?? 0;

        var partnerCount = counts
            .FirstOrDefault(item => string.Equals(
                item.UserId,
                partnerUserId,
                StringComparison.OrdinalIgnoreCase))
            ?.CorrectCount ?? 0;

        return new GameProgress
        {
            MatchId = matchId,
            MyCorrectGuessCount = myCount,
            PartnerCorrectGuessCount = partnerCount
        };
    }
}