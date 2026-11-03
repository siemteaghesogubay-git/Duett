namespace Duett.API.Models;

public class UserMatchDetails
{
    public Guid MatchId { get; set; }

    public string OtherUserId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public DateOnly DateOfBirth { get; set; }

    public string City { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
}