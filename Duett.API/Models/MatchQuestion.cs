using System.ComponentModel.DataAnnotations;

namespace Duett.API.Models;

public class MatchQuestion
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid MatchId { get; set; }

    public UserMatch Match { get; set; } = null!;

    [MaxLength(450)]
    public string AskedByUserId { get; set; } = string.Empty;

    public ApplicationUser AskedByUser { get; set; } = null!;

    // Fråga 1, 2 eller 3 från den här personen.
    public int QuestionNumber { get; set; }

    [MaxLength(300)]
    public string Text { get; set; } = string.Empty;

    // Null tills partnern har besvarat frågan.
    [MaxLength(2000)]
    public string? AnswerText { get; set; }

    [MaxLength(450)]
    public string? AnsweredByUserId { get; set; }

    public ApplicationUser? AnsweredByUser { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? AnsweredAtUtc { get; set; }

}