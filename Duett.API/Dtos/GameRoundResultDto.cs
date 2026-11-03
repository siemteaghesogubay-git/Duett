namespace Duett.API.Dtos;

public class GameRoundResultDto
{
    public Guid RoundId { get; set; }

    public int RoundNumber { get; set; }

    public string QuestionText { get; set; } = string.Empty;

    public string MyAnswer { get; set; } = string.Empty;

    public string MyGuess { get; set; } = string.Empty;

    public string PartnerAnswer { get; set; } = string.Empty;

    public string PartnerGuess { get; set; } = string.Empty;

    public bool MyGuessWasCorrect { get; set; }

    public bool PartnerGuessWasCorrect { get; set; }
}