using System.ComponentModel.DataAnnotations;

namespace Duett.API.Dtos;

public class LoginDto
{
    [Required(ErrorMessage = "E-postadress krävs.")]
    [EmailAddress(ErrorMessage = "Ange en giltig e-postadress.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Lösenord krävs.")]
    public string Password { get; set; } = string.Empty;
}

public class LoginResponseDto
{
    public string AccessToken { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }
}