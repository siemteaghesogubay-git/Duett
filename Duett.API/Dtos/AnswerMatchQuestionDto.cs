using System.ComponentModel.DataAnnotations;

namespace Duett.API.Dtos;

public class AnswerMatchQuestionDto
{
    [Required(ErrorMessage = "Svaret får inte vara tomt.")]
    [StringLength(2000,
        ErrorMessage = "Svaret får innehålla högst 2000 tecken.")]
    public string AnswerText { get; set; } = string.Empty;
}