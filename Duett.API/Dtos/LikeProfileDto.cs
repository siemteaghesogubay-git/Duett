using System.ComponentModel.DataAnnotations;

namespace Duett.API.Dtos;

public class LikeProfileDto
{
    [Required(ErrorMessage = "Ange användaren du vill gilla.")]
    [StringLength(450)]
    public string ToUserId { get; set; } = string.Empty;
}