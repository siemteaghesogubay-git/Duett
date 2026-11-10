using System.ComponentModel.DataAnnotations;

namespace Duett.API.Models;

public class ProfilePhoto
{
    [Key]
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;

    // Slumpmässig intern lagringsnyckel, inte en publik URL.
    [MaxLength(200)]
    public string StorageKey { get; set; } = string.Empty;

    // Bestäms av backend efter kontroll av bildfilen.
    [MaxLength(50)]
    public string ContentType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;
}