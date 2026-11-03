namespace Duett.API.Dtos;

public class GameRoundResponseDto
{
    public Guid RoundId { get; set; }

    public Guid MatchId { get; set; }

    public int RoundNumber { get; set; }

    public string QuestionText { get; set; } = string.Empty;

    public List<GameQuestionOptionDto> Options { get; set; } = new();

    public bool HasSubmitted { get; set; }

    public bool PartnerHasSubmitted { get; set; }

    public bool IsCompleted { get; set; }
}