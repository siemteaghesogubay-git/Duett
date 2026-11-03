using System.ComponentModel.DataAnnotations;

namespace Duett.API.Dtos;

public class VerifyEmailDto
{
    [Required(ErrorMessage = "E-postadress krävs.")]
    [EmailAddress(ErrorMessage = "Ange en giltig e-postadress.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Verifieringstoken krävs.")]
    public string Token { get; set; } = string.Empty;
}