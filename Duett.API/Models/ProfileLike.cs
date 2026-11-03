namespace Duett.API.Models;

public class ProfileLike
{
    public string FromUserId { get; set; } = string.Empty;

    public ApplicationUser FromUser { get; set; } = null!;

    public string ToUserId { get; set; } = string.Empty;

    public ApplicationUser ToUser { get; set; } = null!;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}