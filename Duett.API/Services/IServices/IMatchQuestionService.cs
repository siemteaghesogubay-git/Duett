using Duett.API.Dtos;
using Duett.API.Models;

namespace Duett.API.Services.IServices;

public interface IMatchQuestionService
{
    Task<(
        MatchQuestionStatus Status,
        List<MatchQuestionResponseDto>? Questions)> GetQuestionsAsync(
        Guid matchId,
        string userId,
        CancellationToken cancellationToken = default);

    Task<(
        MatchQuestionStatus Status,
        MatchQuestionResponseDto? Question)> CreateAsync(
        Guid matchId,
        string userId,
        CreateMatchQuestionDto dto,
        CancellationToken cancellationToken = default);

    Task<(
        MatchQuestionStatus Status,

        MatchQuestionResponseDto? Question)> AnswerAsync(
        Guid matchId,
        Guid questionId,
        string userId,
        AnswerMatchQuestionDto dto,
        CancellationToken cancellationToken = default);
    

    Task<MatchQuestionProgressResponseDto?> GetProgressAsync(
    Guid matchId,
    string userId,
    CancellationToken cancellationToken = default);

}