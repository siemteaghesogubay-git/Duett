using Duett.API.Models;

namespace Duett.API.Repositories.IRepositories;

public interface IMatchQuestionRepository
{
    Task<List<MatchQuestion>> GetQuestionsAsync(
        Guid matchId,
        string userId,
        CancellationToken cancellationToken = default);

    Task<(
        MatchQuestionStatus Status,
        MatchQuestion? Question)> CreateAsync(
        Guid matchId,
        string userId,
        string text,
        CancellationToken cancellationToken = default);

    Task<(
        MatchQuestionStatus Status,
        MatchQuestion? Question)> AnswerAsync(
        Guid matchId,
        Guid questionId,
        string userId,
        string answerText,
        CancellationToken cancellationToken = default);
}