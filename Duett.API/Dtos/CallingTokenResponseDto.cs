namespace Duett.API.Dtos;

public class CallingTokenResponseDto
{
    public Guid MatchId { get; set; }

    // Kortlivat ACS-token för den inloggade användaren.
    public string Token { get; set; } = string.Empty;

    public DateTimeOffset ExpiresOn { get; set; }

    // ACS-identitet för den inloggade användaren.
    public string CommunicationUserId { get; set; } = string.Empty;

    // ACS-identitet för den andra personen i matchningen.
    public string PeerCommunicationUserId { get; set; } = string.Empty;
}
