using System.ComponentModel.DataAnnotations;

namespace Duett.API.Dtos;

public class CreateUserProfileDto
{
    [Required(ErrorMessage = "Visningsnamn krävs.")]
    [StringLength(50, MinimumLength = 2,
        ErrorMessage = "Visningsnamnet ska vara mellan 2 och 50 tecken.")]
    public string DisplayName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Födelsedatum krävs.")]
    public DateOnly? DateOfBirth { get; set; }

    [Required(ErrorMessage = "Kön krävs.")]
    [StringLength(50)]
    public string Gender { get; set; } = string.Empty;

    [Required(ErrorMessage = "Stad krävs.")]
    [StringLength(100)]
    public string City { get; set; } = string.Empty;

    [StringLength(1000,
        ErrorMessage = "Presentationen får innehålla högst 1000 tecken.")]
    public string Bio { get; set; } = string.Empty;
}

// response DTO for returning user profile data

public class UserProfileResponseDto
{
    public string UserId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public DateOnly DateOfBirth { get; set; }

    public string Gender { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string Bio { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }
}

// Upate userprofile DTO


public class UpdateUserProfileDto
{
    [Required(ErrorMessage = "Visningsnamn krävs.")]
    [StringLength(50, MinimumLength = 2,
        ErrorMessage = "Visningsnamnet ska vara mellan 2 och 50 tecken.")]
    public string DisplayName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Kön krävs.")]
    [StringLength(50)]
    public string Gender { get; set; } = string.Empty;

    [Required(ErrorMessage = "Stad krävs.")]
    [StringLength(100)]
    public string City { get; set; } = string.Empty;

    [StringLength(1000,
        ErrorMessage = "Presentationen får innehålla högst 1000 tecken.")]
    public string Bio { get; set; } = string.Empty;
}