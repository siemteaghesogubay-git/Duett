using Duett.API.Data;
using Duett.API.Models;
using Duett.API.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace Duett.API.Repositories;

public class MatchRepository : IMatchRepository
{
    private readonly ApplicationDbContext _context;

    public MatchRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<UserProfile>> GetSuggestionsAsync(
        string userId,
        DateOnly today,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        if (skip < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(skip));
        }

        if (take < 1 || take > 50)
        {
            throw new ArgumentOutOfRangeException(nameof(take));
        }

        var myProfile = await _context.UserProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(
                profile => profile.UserId == userId,
                cancellationToken);

        var myPreferences = await _context.PartnerPreferences
            .AsNoTracking()
            .SingleOrDefaultAsync(
                preferences => preferences.UserId == userId,
                cancellationToken);

        if (myProfile is null || myPreferences is null)
        {
            return new List<UserProfile>();
        }

        var myAge = today.Year - myProfile.DateOfBirth.Year;

        if (myProfile.DateOfBirth.Month > today.Month ||
            (myProfile.DateOfBirth.Month == today.Month &&
             myProfile.DateOfBirth.Day > today.Day))
        {
            myAge--;
        }

        if (myAge < 18)
        {
            return new List<UserProfile>();
        }

        // Födelsedatum som motsvarar det önskade åldersintervallet.
        var youngestBirthDate = today.AddYears(-myPreferences.MinAge);
        var oldestBirthDateExclusive =
            today.AddYears(-(myPreferences.MaxAge + 1));

        var preferredGenders = myPreferences.PreferredGenders;
        var preferredCity = myPreferences.PreferredCity;
        var relationshipGoal = myPreferences.RelationshipGoal;

        var myGender = myProfile.Gender;
        var myCity = myProfile.City;

        var query =
            from profile in _context.UserProfiles.AsNoTracking()
            join preferences in _context.PartnerPreferences.AsNoTracking()
                on profile.UserId equals preferences.UserId
            where profile.UserId != userId

                // Personen uppfyller mina önskemål.
                && profile.DateOfBirth > oldestBirthDateExclusive
                && profile.DateOfBirth <= youngestBirthDate
                && preferredGenders.Contains(profile.Gender)
                && (preferredCity == null ||
                    profile.City == preferredCity)

                // Jag uppfyller personens önskemål.
                && preferences.MinAge <= myAge
                && preferences.MaxAge >= myAge
                && preferences.PreferredGenders.Contains(myGender)
                && (preferences.PreferredCity == null ||
                    preferences.PreferredCity == myCity)

                // Båda söker samma typ av relation.
                && preferences.RelationshipGoal == relationshipGoal
            select profile;

        return await query
            .OrderBy(profile => profile.UserId)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }




    public async Task<bool> CanMatchAsync(
    string userId,
    string otherUserId,
    DateOnly today,
    CancellationToken cancellationToken = default)
    {
        if (userId == otherUserId)
        {
            return false;
        }

        var myProfile = await _context.UserProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(
                profile => profile.UserId == userId,
                cancellationToken);

        var myPreferences = await _context.PartnerPreferences
            .AsNoTracking()
            .SingleOrDefaultAsync(
                preferences => preferences.UserId == userId,
                cancellationToken);

        if (myProfile is null || myPreferences is null)
        {
            return false;
        }

        var myAge = today.Year - myProfile.DateOfBirth.Year;

        if (myProfile.DateOfBirth.Month > today.Month ||
            (myProfile.DateOfBirth.Month == today.Month &&
             myProfile.DateOfBirth.Day > today.Day))
        {
            myAge--;
        }

        if (myAge < 18)
        {
            return false;
        }

        var youngestBirthDate = today.AddYears(-myPreferences.MinAge);
        var oldestBirthDateExclusive =
            today.AddYears(-(myPreferences.MaxAge + 1));

        var preferredGenders = myPreferences.PreferredGenders;
        var preferredCity = myPreferences.PreferredCity;
        var relationshipGoal = myPreferences.RelationshipGoal;

        var myGender = myProfile.Gender;
        var myCity = myProfile.City;

        var query =
            from profile in _context.UserProfiles.AsNoTracking()
            join preferences in _context.PartnerPreferences.AsNoTracking()
                on profile.UserId equals preferences.UserId
            where profile.UserId == otherUserId

                // Den andra personen uppfyller mina önskemål.
                && profile.DateOfBirth > oldestBirthDateExclusive
                && profile.DateOfBirth <= youngestBirthDate
                && preferredGenders.Contains(profile.Gender)
                && (preferredCity == null ||
                    profile.City == preferredCity)

                // Jag uppfyller den andra personens önskemål.
                && preferences.MinAge <= myAge
                && preferences.MaxAge >= myAge
                && preferences.PreferredGenders.Contains(myGender)
                && (preferences.PreferredCity == null ||
                    preferences.PreferredCity == myCity)

                && preferences.RelationshipGoal == relationshipGoal
            select profile.UserId;

        return await query.AnyAsync(cancellationToken);
    }
}