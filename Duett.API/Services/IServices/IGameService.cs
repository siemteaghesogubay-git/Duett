using Duett.API.Dtos;
using Duett.API.Models;

namespace Duett.API.Services.IServices;

public interface IGameService
{
    Task<(StartGameRoundStatus Status, GameRoundResponseDto? Round)>
        StartRoundAsync(
            Guid matchId,
            string userId,
            CancellationToken cancellationToken = default);

    Task<GameRoundResponseDto?> GetRoundAsync(
        Guid roundId,
        string userId,
        CancellationToken cancellationToken = default);

    Task<SubmitGameRoundStatus> SubmitAsync(
    Guid roundId,
    string userId,
    SubmitGameRoundDto dto,
    CancellationToken cancellationToken = default);



    Task<(GameRoundResultStatus Status, GameRoundResultDto? Result)>
    GetResultAsync(
        Guid roundId,
        string userId,
        CancellationToken cancellationToken = default);


    Task<GameProgressResponseDto?> GetProgressAsync(
    Guid matchId,
    string userId,
    CancellationToken cancellationToken = default);


}


