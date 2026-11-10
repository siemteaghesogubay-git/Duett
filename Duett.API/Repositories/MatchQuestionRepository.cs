using Duett.API.Data;
using Duett.API.Models;
using Duett.API.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace Duett.API.Repositories;

public class MatchQuestionRepository : IMatchQuestionRepository
{
    private readonly ApplicationDbContext _context;

    public MatchQuestionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<MatchQuestion>> GetQuestionsAsync(
        Guid matchId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        return await _context.MatchQuestions
            .AsNoTracking()
            .Where(question =>
                question.MatchId == matchId
                && (question.Match.FirstUserId == userId
                    || question.Match.SecondUserId == userId))
            .OrderBy(question => question.AskedByUserId)
            .ThenBy(question => question.QuestionNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<(
        MatchQuestionStatus Status,
        MatchQuestion? Question)> CreateAsync(
        Guid matchId,
        string userId,
        string text,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                cancellationToken);

        var match = await LockMatchForUserAsync(
            matchId,
            userId,
            cancellationToken);

        if (match is null)
        {
            return (MatchQuestionStatus.MatchNotFound, null);
        }

        // Hämta redan använda nummer efter att låset tagits.
        var usedNumbers = await _context.MatchQuestions
            .Where(question =>
                question.MatchId == matchId
                && question.AskedByUserId == userId)
            .Select(question => question.QuestionNumber)
            .ToListAsync(cancellationToken);

        var nextNumber = Enumerable.Range(1, 3)
            .FirstOrDefault(number => !usedNumbers.Contains(number));

        if (nextNumber == 0)
        {
            return (MatchQuestionStatus.QuestionLimitReached, null);
        }

        var question = new MatchQuestion
        {
            Id = Guid.NewGuid(),
            MatchId = matchId,
            AskedByUserId = userId,
            QuestionNumber = nextNumber,
            Text = text,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.MatchQuestions.Add(question);

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return (MatchQuestionStatus.Success, question);
    }

    public async Task<(
        MatchQuestionStatus Status,
        MatchQuestion? Question)> AnswerAsync(
        Guid matchId,
        Guid questionId,
        string userId,
        string answerText,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                cancellationToken);

        var match = await LockMatchForUserAsync(
            matchId,
            userId,
            cancellationToken);

        if (match is null)
        {
            return (MatchQuestionStatus.MatchNotFound, null);
        }

        // Frågan måste tillhöra den angivna matchningen.
        var question = await _context.MatchQuestions
            .SingleOrDefaultAsync(
                question => question.Id == questionId
                    && question.MatchId == matchId,
                cancellationToken);

        if (question is null)
        {
            return (MatchQuestionStatus.QuestionNotFound, null);
        }

        if (string.Equals(
            question.AskedByUserId,
            userId,
            StringComparison.OrdinalIgnoreCase))
        {
            return (MatchQuestionStatus.CannotAnswerOwnQuestion, null);
        }

        var partnerUserId = string.Equals(
            match.FirstUserId,
            userId,
            StringComparison.OrdinalIgnoreCase)
                ? match.SecondUserId
                : match.FirstUserId;

        // Endast frågor från den andra deltagaren får besvaras.
        if (!string.Equals(
            question.AskedByUserId,
            partnerUserId,
            StringComparison.OrdinalIgnoreCase))
        {
            return (MatchQuestionStatus.QuestionNotFound, null);
        }

        if (question.AnsweredAtUtc is not null)
        {
            return (MatchQuestionStatus.AlreadyAnswered, null);
        }

        question.AnswerText = answerText;
        question.AnsweredByUserId = userId;
        question.AnsweredAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return (MatchQuestionStatus.Success, question);
    }

    // Anropas endast inifrån en pågående transaktion.
    private async Task<UserMatch?> LockMatchForUserAsync(
        Guid matchId,
        string userId,
        CancellationToken cancellationToken)
    {
        return await _context.UserMatches
            .FromSqlInterpolated($"""
                SELECT *
                FROM [dbo].[UserMatches] WITH (UPDLOCK, HOLDLOCK)
                WHERE [Id] = {matchId}
                  AND ([FirstUserId] = {userId}
                       OR [SecondUserId] = {userId})
                """)
            .AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken);
    }
}