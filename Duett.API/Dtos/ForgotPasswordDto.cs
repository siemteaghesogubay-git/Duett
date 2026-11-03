using System.ComponentModel.DataAnnotations;

public class ForgotPasswordDto
{
    [Required(ErrorMessage = "E-postadress krävs.")]
    [EmailAddress(ErrorMessage = "Ange en giltig e-postadress.")]
    public string Email { get; set; } = string.Empty;
}