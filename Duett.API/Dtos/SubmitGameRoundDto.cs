using System.ComponentModel.DataAnnotations;

namespace Duett.API.Dtos;

public class SubmitGameRoundDto
{
    [Required(ErrorMessage = "Välj ditt eget svar.")]
    public Guid? AnswerOptionId { get; set; }

    [Required(ErrorMessage = "Välj din gissning på partnerns svar.")]
    public Guid? GuessOptionId { get; set; }
}