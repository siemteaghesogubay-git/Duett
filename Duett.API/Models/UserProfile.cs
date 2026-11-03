using System.ComponentModel.DataAnnotations;

namespace Duett.API.Models;

public class UserProfile
{
    [Key]
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;

    [MaxLength(50)]
    public string DisplayName { get; set; } = string.Empty;

    public DateOnly DateOfBirth { get; set; }

    [MaxLength(50)]
    public string Gender { get; set; } = string.Empty;

    [MaxLength(100)]
    public string City { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Bio { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }

}