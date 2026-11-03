namespace Duett.API.Models;

public class UserMatch
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string FirstUserId { get; set; } = string.Empty;

    public ApplicationUser FirstUser { get; set; } = null!;

    public string SecondUserId { get; set; } = string.Empty;

    public ApplicationUser SecondUser { get; set; } = null!;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}