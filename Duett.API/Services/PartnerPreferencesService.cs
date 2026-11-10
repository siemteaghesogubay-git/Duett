using System.ComponentModel.DataAnnotations;
using Duett.API.Dtos;
using Duett.API.Models;
using Duett.API.Repositories.IRepositories;
using Duett.API.Services.IServices;

namespace Duett.API.Services;

public class PartnerPreferencesService : IPartnerPreferencesService
{
    private readonly IPartnerPreferencesRepository _repository;

    private static readonly string[] AllowedGenders =
    {
        "Man",
        "Kvinna",
        "Icke-binär",
        "Annat"
    };

    private static readonly string[] AllowedRelationshipGoals =
    {
        "Seriöst förhållande",
        "Dejting"
    };

    private static readonly string[] AllowedRelationshipModes =
    {
        "Distans",
        "Träffas fysiskt",
        "Öppen för båda"
    };

    public PartnerPreferencesService(
        IPartnerPreferencesRepository repository)
    {
        _repository = repository;
    }

    public async Task<PartnerPreferencesResponseDto?> GetMyPreferencesAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var preferences = await _repository.GetByUserIdAsync(
            userId,
            cancellationToken);

        return preferences is null ? null : MapToDto(preferences);
    }

    public async Task SaveAsync(
        string userId,
        SavePartnerPreferencesDto dto,
        CancellationToken cancellationToken = default)
    {
        if (dto.MinAge is not int minAge ||
            dto.MaxAge is not int maxAge)
        {
            throw new ValidationException(
                "Både lägsta och högsta ålder krävs.");
        }

        if (minAge < 18 || minAge > 120 ||
            maxAge < 18 || maxAge > 120)
        {
            throw new ValidationException(
                "Åldrarna måste vara mellan 18 och 120 år.");
        }

        if (maxAge < minAge)
        {
            throw new ValidationException(
                "Högsta åldern får inte vara lägre än lägsta åldern.");
        }

        if (dto.PreferredGenders is null ||
            dto.PreferredGenders.Count == 0)
        {
            throw new ValidationException("Välj minst ett kön.");
        }

        var preferredGenders = new List<string>();

        foreach (var value in dto.PreferredGenders)
        {
            var gender = AllowedGenders.FirstOrDefault(
                allowed => string.Equals(
                    allowed,
                    value?.Trim(),
                    StringComparison.OrdinalIgnoreCase));

            if (gender is null)
            {
                throw new ValidationException(
                    "Tillåtna könsalternativ är Man, Kvinna, " +
                    "Icke-binär och Annat.");
            }

            if (!preferredGenders.Contains(gender))
            {
                preferredGenders.Add(gender);
            }
        }

        var relationshipGoal = AllowedRelationshipGoals.FirstOrDefault(
            allowed => string.Equals(
                allowed,
                dto.RelationshipGoal?.Trim(),
                StringComparison.OrdinalIgnoreCase));

        if (relationshipGoal is null)
        {
            throw new ValidationException(
                "Välj Seriöst förhållande eller Dejting som relationsmål.");
        }

        // Trimma och normalisera till ett av de tillåtna värdena.
        var relationshipMode = AllowedRelationshipModes.FirstOrDefault(
            allowed => string.Equals(
                allowed,
                dto.RelationshipMode?.Trim(),
                StringComparison.OrdinalIgnoreCase));

        if (relationshipMode is null)
        {
            throw new ValidationException(
                "Välj Distans, Träffas fysiskt eller " +
                "Öppen för båda som relationsform.");
        }

        var preferredCity = string.IsNullOrWhiteSpace(dto.PreferredCity)
            ? null
            : dto.PreferredCity.Trim();

        if (preferredCity is not null && preferredCity.Length > 100)
        {
            throw new ValidationException(
                "Staden får innehålla högst 100 tecken.");
        }

        var preferences = await _repository.GetTrackedByUserIdAsync(
            userId,
            cancellationToken);

        if (preferences is null)
        {
            preferences = new PartnerPreferences
            {
                UserId = userId,
                MinAge = minAge,
                MaxAge = maxAge,
                PreferredGenders = preferredGenders,
                PreferredCity = preferredCity,
                RelationshipGoal = relationshipGoal,
                RelationshipMode = relationshipMode
            };

            await _repository.AddAsync(
                preferences,
                cancellationToken);

            return;
        }

        preferences.MinAge = minAge;
        preferences.MaxAge = maxAge;
        preferences.PreferredGenders = preferredGenders;
        preferences.PreferredCity = preferredCity;
        preferences.RelationshipGoal = relationshipGoal;
        preferences.RelationshipMode = relationshipMode;

        await _repository.SaveChangesAsync(cancellationToken);
    }

    private static PartnerPreferencesResponseDto MapToDto(
        PartnerPreferences preferences)
    {
        return new PartnerPreferencesResponseDto
        {
            MinAge = preferences.MinAge,
            MaxAge = preferences.MaxAge,
            PreferredGenders = preferences.PreferredGenders.ToList(),
            PreferredCity = preferences.PreferredCity,
            RelationshipGoal = preferences.RelationshipGoal,
            RelationshipMode = preferences.RelationshipMode
        };
    }
}