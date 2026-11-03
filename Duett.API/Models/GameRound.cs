namespace Duett.API.Models;

public class GameRound
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid MatchId { get; set; }

    public UserMatch Match { get; set; } = null!;

    public int RoundNumber { get; set; }

    public Guid QuestionId { get; set; }
    public GameQuestion Question { get; set; } = null!;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAtUtc { get; set; }
}