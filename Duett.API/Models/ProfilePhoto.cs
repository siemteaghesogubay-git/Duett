using System.ComponentModel.DataAnnotations;

namespace Duett.API.Models;

public class ProfilePhoto
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(450)]
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;

    [Required]
    [StringLength(200)]
    public string StorageKey { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string ContentType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    // 0 är användarens första/huvudbild.
    public int SortOrder { get; set; }

    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;
}