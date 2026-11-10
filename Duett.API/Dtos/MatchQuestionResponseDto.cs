namespace Duett.API.Dtos;

public class MatchQuestionResponseDto
{
    public Guid Id { get; set; }

    public Guid MatchId { get; set; }

    public string AskedByUserId { get; set; } = string.Empty;

    public int QuestionNumber { get; set; }

    public string Text { get; set; } = string.Empty;

    public string? AnswerText { get; set; }

    public string? AnsweredByUserId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? AnsweredAtUtc { get; set; }

    public bool IsAnswered => AnsweredAtUtc.HasValue;
}