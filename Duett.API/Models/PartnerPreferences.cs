using System.ComponentModel.DataAnnotations;

namespace Duett.API.Models;

public class PartnerPreferences
{
    [Key]
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;

    public int MinAge { get; set; } = 18;

    public int MaxAge { get; set; } = 99;

    public List<string> PreferredGenders { get; set; } = new();

    [MaxLength(100)]
    public string? PreferredCity { get; set; }

    [MaxLength(50)]
    public string RelationshipGoal { get; set; } = string.Empty;
}