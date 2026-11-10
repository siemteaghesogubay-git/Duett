using System.ComponentModel.DataAnnotations;

namespace Duett.API.Dtos;

public class SavePartnerPreferencesDto
{
    [Required(ErrorMessage = "Lägsta ålder krävs.")]
    [Range(18, 120,
        ErrorMessage = "Lägsta ålder måste vara mellan 18 och 120 år.")]
    public int? MinAge { get; set; }

    [Required(ErrorMessage = "Högsta ålder krävs.")]
    [Range(18, 120,
        ErrorMessage = "Högsta ålder måste vara mellan 18 och 120 år.")]
    public int? MaxAge { get; set; }

    [Required(ErrorMessage = "Välj minst ett kön.")]
    [MinLength(1, ErrorMessage = "Välj minst ett kön.")]
    public List<string> PreferredGenders { get; set; } = new();

    [StringLength(100,
        ErrorMessage = "Staden får innehålla högst 100 tecken.")]
    public string? PreferredCity { get; set; }

    [Required(ErrorMessage = "Relationsmål krävs.")]
    [StringLength(50,
        ErrorMessage = "Relationsmålet får innehålla högst 50 tecken.")]
    public string RelationshipGoal { get; set; } = string.Empty;

    [Required(ErrorMessage = "Relationsform krävs.")]
    [StringLength(50,
        ErrorMessage = "Relationsformen får innehålla högst 50 tecken.")]
    public string RelationshipMode { get; set; } = string.Empty;
}

public class PartnerPreferencesResponseDto
{
    public int MinAge { get; set; }

    public int MaxAge { get; set; }

    public List<string> PreferredGenders { get; set; } = new();

    public string? PreferredCity { get; set; }

    public string RelationshipGoal { get; set; } = string.Empty;

    public string? RelationshipMode { get; set; }


}