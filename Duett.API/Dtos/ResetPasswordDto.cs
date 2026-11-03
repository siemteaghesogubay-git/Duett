using System.ComponentModel.DataAnnotations;

namespace Duett.API.Dtos;

public class ResetPasswordDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Återställningstoken krävs.")]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nytt lösenord krävs.")]
    [StringLength(128, MinimumLength = 8,
        ErrorMessage = "Lösenordet ska vara mellan 8 och 128 tecken.")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Bekräfta lösenordet.")]
    [Compare(nameof(NewPassword),
        ErrorMessage = "Lösenorden matchar inte.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}