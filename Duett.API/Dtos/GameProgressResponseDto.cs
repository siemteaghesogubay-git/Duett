namespace Duett.API.Dtos;

public class GameProgressResponseDto
{
    public Guid MatchId { get; set; }

    public int MyLevel { get; set; }

    public int PartnerLevel { get; set; }

    public bool IsPartnerAgeUnlocked => MyLevel >= 1;

    public bool IsPartnerCityUnlocked => MyLevel >= 2;

    public bool IsPartnerRelationshipModeUnlocked => MyLevel >= 3;

    public bool IsChatUnlocked =>
        MyLevel >= 3 && PartnerLevel >= 3;
}