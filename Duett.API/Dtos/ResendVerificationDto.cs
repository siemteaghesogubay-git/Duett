using System.ComponentModel.DataAnnotations;

namespace Duett.API.Dtos;

public class ResendVerificationDto
{
    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = string.Empty;
}