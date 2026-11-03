using System.ComponentModel.DataAnnotations;

namespace Duett.API.Dtos;

public class RegisterDto
{
    [Required(ErrorMessage = "E-postadress krävs.")]
    [EmailAddress(ErrorMessage = "Ange en giltig e-postadress.")]
    [StringLength(256, ErrorMessage = "E-postadressen är för lång.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Lösenord krävs.")]
    [StringLength(
        128,
        MinimumLength = 8,
        ErrorMessage = "Lösenordet måste innehålla mellan 8 och 128 tecken.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Bekräfta lösenordet.")]
    [Compare(
        nameof(Password),
        ErrorMessage = "Lösenorden matchar inte.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}