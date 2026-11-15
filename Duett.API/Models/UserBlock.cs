namespace Duett.API.Models;

public class UserBlock
{
    public string BlockerUserId { get; set; } = string.Empty;

    public ApplicationUser BlockerUser { get; set; } = null!;

    public string BlockedUserId { get; set; } = string.Empty;

    public ApplicationUser BlockedUser { get; set; } = null!;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}