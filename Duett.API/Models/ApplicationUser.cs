using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Duett.API.Models;

public class ApplicationUser : IdentityUser
{
    public DateTime CreatedAtUtc { get; set; } =
        DateTime.UtcNow;

    [MaxLength(256)]
    public string? AcsUserId { get; set; }

    [MaxLength(2048)]
    public string? ProtectedEmailVerificationCode { get; set; }

    public DateTime? EmailVerificationExpiresAtUtc { get; set; }

    public int EmailVerificationFailedAttempts { get; set; }

    public DateTime? EmailVerificationNextSendAtUtc { get; set; }

    public ICollection<ProfilePhoto> ProfilePhotos { get; set; } =
        new List<ProfilePhoto>();
}