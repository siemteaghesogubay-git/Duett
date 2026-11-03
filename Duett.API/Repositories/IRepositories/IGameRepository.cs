using Duett.API.Models;

namespace Duett.API.Repositories.IRepositories;

public interface IGameRepository
{
    Task<UserMatch?> GetMatchForUserAsync(
        Guid matchId,
        string userId,
        CancellationToken cancellationToken = default);

    Task<GameRound?> GetRoundForUserAsync(
        Guid roundId,
        string userId,
        CancellationToken cancellationToken = default);

    Task<List<GameRoundSubmission>> GetSubmissionsForUserAsync(
        Guid roundId,
        string userId,
        CancellationToken cancellationToken = default);



    Task<StartGameRoundResult> GetOrCreateCurrentRoundAsync(
    Guid matchId,
    string userId,
    CancellationToken cancellationToken = default);



    Task<SubmitGameRoundStatus> SubmitAsync(
    Guid roundId,
    string userId,
    Guid answerOptionId,
    Guid guessOptionId,
    CancellationToken cancellationToken = default);



    Task<GameProgress?> GetProgressForUserAsync(
    Guid matchId,
    string userId,
    CancellationToken cancellationToken = default);


}