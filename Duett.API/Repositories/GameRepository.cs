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
        var match = await _context.UserMatches
            .AsNoTracking()
            .SingleOrDefaultAsync(
                savedMatch =>
                    savedMatch.Id == matchId &&
                    (savedMatch.FirstUserId == userId ||
                     savedMatch.SecondUserId == userId),
                cancellationToken);

        if (match is null)
        {
            return null;
        }

        var isBlocked = await IsBlockedBetweenUsersAsync(
            match.FirstUserId,
            match.SecondUserId,
            cancellationToken);

        return isBlocked ? null : match;
    }

    public async Task<GameRound?> GetRoundForUserAsync(
        Guid roundId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var round = await _context.GameRounds
            .AsNoTracking()
            .Include(savedRound => savedRound.Question)
                .ThenInclude(question => question.Options)
            .SingleOrDefaultAsync(
                savedRound =>
                    savedRound.Id == roundId &&
                    (savedRound.Match.FirstUserId == userId ||
                     savedRound.Match.SecondUserId == userId),
                cancellationToken);

        if (round is null)
        {
            return null;
        }

        var match = await GetMatchForUserAsync(
            round.MatchId,
            userId,
            cancellationToken);

        return match is null ? null : round;
    }

    public async Task<List<GameRoundSubmission>> GetSubmissionsForUserAsync(
        Guid roundId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var matchId = await _context.GameRounds
            .AsNoTracking()
            .Where(round =>
                round.Id == roundId &&
                (round.Match.FirstUserId == userId ||
                 round.Match.SecondUserId == userId))
            .Select(round => (Guid?)round.MatchId)
            .SingleOrDefaultAsync(cancellationToken);

        if (matchId is null)
        {
            return new List<GameRoundSubmission>();
        }

        var match = await GetMatchForUserAsync(
            matchId.Value,
            userId,
            cancellationToken);

        if (match is null)
        {
            return new List<GameRoundSubmission>();
        }

        return await _context.GameRoundSubmissions
            .AsNoTracking()
            .Where(submission => submission.RoundId == roundId)
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

        var isBlocked = await IsBlockedBetweenUsersAsync(
            match.FirstUserId,
            match.SecondUserId,
            cancellationToken);

        if (isBlocked)
        {
            return new StartGameRoundResult
            {
                Status = StartGameRoundStatus.MatchNotFound
            };
        }

        var currentRound = await _context.GameRounds
            .AsNoTracking()
            .Include(round => round.Question)
                .ThenInclude(question => question.Options)
            .Where(round =>
                round.MatchId == matchId &&
                round.CompletedAtUtc == null)
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

        var correctGuesses =
            from submission in _context.GameRoundSubmissions
            join partnerSubmission in _context.GameRoundSubmissions
                on submission.RoundId equals partnerSubmission.RoundId
            where submission.Round.MatchId == matchId
                && submission.Round.CompletedAtUtc != null
                && (
                    (submission.UserId == match.FirstUserId &&
                     partnerSubmission.UserId == match.SecondUserId) ||
                    (submission.UserId == match.SecondUserId &&
                     partnerSubmission.UserId == match.FirstUserId)
                )
                && submission.GuessOptionId ==
                    partnerSubmission.AnswerOptionId
            select submission;

        var firstUserCorrectCount = await correctGuesses.CountAsync(
            submission => submission.UserId == match.FirstUserId,
            cancellationToken);

        var secondUserCorrectCount = await correctGuesses.CountAsync(
            submission => submission.UserId == match.SecondUserId,
            cancellationToken);

        if (firstUserCorrectCount >= 3 &&
            secondUserCorrectCount >= 3)
        {
            return new StartGameRoundResult
            {
                Status = StartGameRoundStatus.GameCompleted
            };
        }

        var question = await _context.GameQuestions
            .Include(savedQuestion => savedQuestion.Options)
            .Where(savedQuestion =>
                savedQuestion.IsActive &&
                savedQuestion.Options.Count >= 2 &&
                !_context.GameRounds.Any(round =>
                    round.MatchId == matchId &&
                    round.QuestionId == savedQuestion.Id))
            .OrderBy(savedQuestion => savedQuestion.Id)
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

        var matchId = await _context.GameRounds
            .AsNoTracking()
            .Where(round =>
                round.Id == roundId &&
                (round.Match.FirstUserId == userId ||
                 round.Match.SecondUserId == userId))
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

        var isBlocked = await IsBlockedBetweenUsersAsync(
            match.FirstUserId,
            match.SecondUserId,
            cancellationToken);

        if (isBlocked)
        {
            return SubmitGameRoundStatus.RoundNotFound;
        }

        var round = await _context.GameRounds
            .SingleOrDefaultAsync(
                savedRound =>
                    savedRound.Id == roundId &&
                    savedRound.MatchId == matchIdValue,
                cancellationToken);

        if (round is null)
        {
            return SubmitGameRoundStatus.RoundNotFound;
        }

        var alreadySubmitted = await _context.GameRoundSubmissions
            .AnyAsync(
                submission =>
                    submission.RoundId == roundId &&
                    submission.UserId == userId,
                cancellationToken);

        if (alreadySubmitted)
        {
            return SubmitGameRoundStatus.AlreadySubmitted;
        }

        if (round.CompletedAtUtc is not null)
        {
            return SubmitGameRoundStatus.RoundCompleted;
        }

        if (answerOptionId == Guid.Empty ||
            guessOptionId == Guid.Empty)
        {
            return SubmitGameRoundStatus.InvalidOption;
        }

        var answerIsValid = await _context.GameQuestionOptions
            .AnyAsync(
                option =>
                    option.Id == answerOptionId &&
                    option.QuestionId == round.QuestionId,
                cancellationToken);

        var guessIsValid = await _context.GameQuestionOptions
            .AnyAsync(
                option =>
                    option.Id == guessOptionId &&
                    option.QuestionId == round.QuestionId,
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
                submission =>
                    submission.RoundId == roundId &&
                    submission.UserId == partnerUserId,
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

        var correctGuesses =
            from submission in _context.GameRoundSubmissions
            join partnerSubmission in _context.GameRoundSubmissions
                on submission.RoundId equals partnerSubmission.RoundId
            where submission.Round.MatchId == matchId
                && submission.Round.CompletedAtUtc != null
                && (
                    (submission.UserId == match.FirstUserId &&
                     partnerSubmission.UserId == match.SecondUserId) ||
                    (submission.UserId == match.SecondUserId &&
                     partnerSubmission.UserId == match.FirstUserId)
                )
                && submission.GuessOptionId ==
                    partnerSubmission.AnswerOptionId
            select submission;

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

    private Task<bool> IsBlockedBetweenUsersAsync(
        string firstUserId,
        string secondUserId,
        CancellationToken cancellationToken)
    {
        return _context.UserBlocks
            .AsNoTracking()
            .AnyAsync(
                block =>
                    (block.BlockerUserId == firstUserId &&
                     block.BlockedUserId == secondUserId) ||
                    (block.BlockerUserId == secondUserId &&
                     block.BlockedUserId == firstUserId),
                cancellationToken);
    }
}