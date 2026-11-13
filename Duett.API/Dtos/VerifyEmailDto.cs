using System.ComponentModel.DataAnnotations;

namespace Duett.API.Dtos;

public class VerifyEmailDto
{
    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Verification code is required.")]
    [RegularExpression(
        @"^[0-9]{6}$",
        ErrorMessage = "Enter the six-digit verification code.")]
    public string Token { get; set; } = string.Empty;
}