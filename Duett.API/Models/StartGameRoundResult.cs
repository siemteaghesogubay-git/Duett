namespace Duett.API.Models;

public enum StartGameRoundStatus
{
    Success,
    MatchNotFound,
    NoQuestionsAvailable,
    GameCompleted
}

public class StartGameRoundResult
{
    public StartGameRoundStatus Status { get; init; }

    public GameRound? Round { get; init; }
}