namespace Duett.API.Models;

public class GameProgress
{
    public Guid MatchId { get; set; }

    public int MyCorrectGuessCount { get; set; }

    public int PartnerCorrectGuessCount { get; set; }
}