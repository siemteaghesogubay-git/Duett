using System.ComponentModel.DataAnnotations;
using Duett.API.Dtos;
using Duett.API.Exceptions;
using Duett.API.Models;
using Duett.API.Repositories.IRepositories;
using Duett.API.Services.IServices;

namespace Duett.API.Services;

public class UserProfileService : IUserProfileService
{
    private readonly IUserProfileRepository _repository;

    public UserProfileService(IUserProfileRepository repository)
    {
        _repository = repository;
    }

    public async Task<UserProfileResponseDto?> GetMyProfileAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var profile = await _repository.GetByUserIdAsync(
            userId,
            cancellationToken);

        return profile is null ? null : MapToDto(profile);
    }

    public async Task<UserProfileResponseDto> CreateAsync(
        string userId,
        CreateUserProfileDto dto,
        CancellationToken cancellationToken = default)
    {
        var displayName = dto.DisplayName?.Trim() ?? string.Empty;
        var gender = NormalizeGender(dto.Gender);
        var city = dto.City?.Trim() ?? string.Empty;
        var bio = dto.Bio?.Trim() ?? string.Empty;

        if (displayName.Length < 2 || displayName.Length > 50)
        {
            throw new ValidationException(
                "Visningsnamnet ska vara mellan 2 och 50 tecken.");
        }

        

        if (city.Length == 0 || city.Length > 100)
        {
            throw new ValidationException(
                "Ange stad med högst 100 tecken.");
        }

        if (bio.Length > 1000)
        {
            throw new ValidationException(
                "Presentationen får innehålla högst 1000 tecken.");
        }

        if (dto.DateOfBirth is not DateOnly dateOfBirth)
        {
            throw new ValidationException("Födelsedatum krävs.");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        if (dateOfBirth == DateOnly.MinValue || dateOfBirth > today)
        {
            throw new ValidationException("Ange ett giltigt födelsedatum.");
        }

        if (dateOfBirth > today.AddYears(-18))
        {
            throw new ValidationException(
                "Du måste vara minst 18 år för att skapa en profil.");
        }

        if (await _repository.ExistsAsync(userId, cancellationToken))
        {
            throw new ProfileAlreadyExistsException();
        }

        var profile = new UserProfile
        {
            UserId = userId,
            DisplayName = displayName,
            DateOfBirth = dateOfBirth,
            Gender = gender,
            City = city,
            Bio = bio,
            CreatedAtUtc = DateTime.UtcNow
        };


        await _repository.AddAsync(profile, cancellationToken);

        return MapToDto(profile);
    }
    public async Task<bool> UpdateAsync(
    string userId,
    UpdateUserProfileDto dto,
    CancellationToken cancellationToken = default)
    {
        var profile = await _repository.GetTrackedByUserIdAsync(
            userId,
            cancellationToken);

        if (profile is null)
        {
            return false;
        }

        var displayName = dto.DisplayName?.Trim() ?? string.Empty;
        var gender = NormalizeGender(dto.Gender); 
        var city = dto.City?.Trim() ?? string.Empty;
        var bio = dto.Bio?.Trim() ?? string.Empty;

        if (displayName.Length < 2 || displayName.Length > 50)
        {
            throw new ValidationException(
                "Visningsnamnet ska vara mellan 2 och 50 tecken.");
        }

      

        if (city.Length == 0 || city.Length > 100)
        {
            throw new ValidationException(
                "Ange stad med högst 100 tecken.");
        }

        if (bio.Length > 1000)
        {
            throw new ValidationException(
                "Presentationen får innehålla högst 1000 tecken.");
        }

        var hasChanges =
            profile.DisplayName != displayName ||
            profile.Gender != gender ||
            profile.City != city ||
            profile.Bio != bio;

        if (hasChanges)
        {
            profile.DisplayName = displayName;
            profile.Gender = gender;
            profile.City = city;
            profile.Bio = bio;
            profile.UpdatedAtUtc = DateTime.UtcNow;

            await _repository.SaveChangesAsync(cancellationToken);
        }

        return true;
    }
    private static string NormalizeGender(string? value)
    {
        string[] allowedGenders =
        {
        "Man",
        "Kvinna",
        "Icke-binär",
        "Annat"
    };

        var gender = allowedGenders.FirstOrDefault(
            allowed => string.Equals(
                allowed,
                value?.Trim(),
                StringComparison.OrdinalIgnoreCase));

        if (gender is null)
        {
            throw new ValidationException(
                "Tillåtna könsalternativ är Man, Kvinna, Icke-binär och Annat.");
        }

        return gender;
    }
    private static UserProfileResponseDto MapToDto(UserProfile profile)
    {
        return new UserProfileResponseDto
        {
            UserId = profile.UserId,
            DisplayName = profile.DisplayName,
            DateOfBirth = profile.DateOfBirth,
            Gender = profile.Gender,
            City = profile.City,
            Bio = profile.Bio,
            CreatedAtUtc = profile.CreatedAtUtc,
            UpdatedAtUtc = profile.UpdatedAtUtc
        };
    }
}

