using System.ComponentModel.DataAnnotations;

namespace Duett.API.Dtos;

public class CreateMatchQuestionDto
{
    [Required(ErrorMessage = "Frågan får inte vara tom.")]
    [StringLength(300,
        ErrorMessage = "Frågan får innehålla högst 300 tecken.")]
    public string Text { get; set; } = string.Empty;
}