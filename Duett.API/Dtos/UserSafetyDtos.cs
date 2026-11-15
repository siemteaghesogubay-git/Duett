using System;
using System.ComponentModel.DataAnnotations;

namespace Duett.API.Dtos;

public class BlockUserDto
{
    [Required(ErrorMessage = "Ange användaren du vill blockera.")]
    [StringLength(450)]
    public string BlockedUserId { get; set; } = string.Empty;
}

public class CreateUserReportDto
{
    [Required(ErrorMessage = "Ange användaren du vill rapportera.")]
    [StringLength(450)]
    public string ReportedUserId { get; set; } = string.Empty;

    // Tomt värde är tillåtet om rapporten gäller en profil
    // och inte en specifik matchning.
    public Guid? MatchId { get; set; }

    [Required(ErrorMessage = "Välj en anledning till rapporten.")]
    [StringLength(50)]
    public string Reason { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Details { get; set; }
}