namespace Duett.API.Dtos;

public class GameProgressResponseDto
{
    public Guid MatchId { get; set; }

    // Nivå 0–3 baserat på rätta gissningar.
    public int MyLevel { get; set; }

    public int PartnerLevel { get; set; }

    // Vad jag har låst upp om partnern.
    public bool IsPartnerAgeUnlocked => MyLevel >= 1;

    public bool IsPartnerCityUnlocked => MyLevel >= 2;

    public bool IsPartnerRelationshipModeUnlocked => MyLevel >= 3;

    // Båda måste nå nivå 3.
    public bool IsChatUnlocked =>
        MyLevel >= 3 && PartnerLevel >= 3;
}