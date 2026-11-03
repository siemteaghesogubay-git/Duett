namespace Duett.API.Models;

public class GameRoundSubmission
{
    public Guid RoundId { get; set; }

    public GameRound Round { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;

    public Guid AnswerOptionId { get; set; }

    public GameQuestionOption AnswerOption { get; set; } = null!;

    public Guid GuessOptionId { get; set; }

    public GameQuestionOption GuessOption { get; set; } = null!;

    public DateTime SubmittedAtUtc { get; set; } = DateTime.UtcNow;
}