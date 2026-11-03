namespace Duett.API.Dtos;

public class UserMatchResponseDto
{
    public Guid MatchId { get; set; }

    public string OtherUserId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public int Age { get; set; }

    public string City { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
}