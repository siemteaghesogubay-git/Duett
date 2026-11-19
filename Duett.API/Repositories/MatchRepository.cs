using Duett.API.Data;
using Duett.API.Models;
using Duett.API.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace Duett.API.Repositories;

public class MatchRepository : IMatchRepository
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public MatchRepository(
        ApplicationDbContext context,
        IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    private bool ShowAllUsers =>
        _configuration.GetValue<bool>(
            "Matching:ShowAllUsers");

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

        if (ShowAllUsers)
        {
            return await _context.UserProfiles
                .AsNoTracking()
                .Where(profile =>
                    profile.UserId != userId &&
                    !_context.UserBlocks.Any(block =>
                        (block.BlockerUserId == userId &&
                         block.BlockedUserId == profile.UserId) ||
                        (block.BlockerUserId == profile.UserId &&
                         block.BlockedUserId == userId)))
                .OrderBy(profile => profile.UserId)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);
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
            return [];
        }

        var myAge = CalculateAge(
            myProfile.DateOfBirth,
            today);

        if (myAge < 18)
        {
            return [];
        }

        var youngestBirthDate =
            today.AddYears(-myPreferences.MinAge);

        var oldestBirthDateExclusive =
            today.AddYears(-(myPreferences.MaxAge + 1));

        var query =
            from profile in _context.UserProfiles.AsNoTracking()
            join preferences in _context.PartnerPreferences.AsNoTracking()
                on profile.UserId equals preferences.UserId
            where profile.UserId != userId
                && !_context.UserBlocks.Any(block =>
                    (block.BlockerUserId == userId &&
                     block.BlockedUserId == profile.UserId) ||
                    (block.BlockerUserId == profile.UserId &&
                     block.BlockedUserId == userId))
                && profile.DateOfBirth > oldestBirthDateExclusive
                && profile.DateOfBirth <= youngestBirthDate
                && myPreferences.PreferredGenders
                    .Contains(profile.Gender)
                && (myPreferences.PreferredCity == null ||
                    profile.City == myPreferences.PreferredCity)
                && preferences.MinAge <= myAge
                && preferences.MaxAge >= myAge
                && preferences.PreferredGenders
                    .Contains(myProfile.Gender)
                && (preferences.PreferredCity == null ||
                    preferences.PreferredCity == myProfile.City)
                && preferences.RelationshipGoal ==
                    myPreferences.RelationshipGoal
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

        if (ShowAllUsers)
        {
            return await _context.UserProfiles
                .AsNoTracking()
                .Where(profile =>
                    profile.UserId == otherUserId &&
                    !_context.UserBlocks.Any(block =>
                        (block.BlockerUserId == userId &&
                         block.BlockedUserId == otherUserId) ||
                        (block.BlockerUserId == otherUserId &&
                         block.BlockedUserId == userId)))
                .AnyAsync(cancellationToken);
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

        var myAge = CalculateAge(
            myProfile.DateOfBirth,
            today);

        if (myAge < 18)
        {
            return false;
        }

        var youngestBirthDate =
            today.AddYears(-myPreferences.MinAge);

        var oldestBirthDateExclusive =
            today.AddYears(-(myPreferences.MaxAge + 1));

        var query =
            from profile in _context.UserProfiles.AsNoTracking()
            join preferences in _context.PartnerPreferences.AsNoTracking()
                on profile.UserId equals preferences.UserId
            where profile.UserId == otherUserId
                && !_context.UserBlocks.Any(block =>
                    (block.BlockerUserId == userId &&
                     block.BlockedUserId == profile.UserId) ||
                    (block.BlockerUserId == profile.UserId &&
                     block.BlockedUserId == userId))
                && profile.DateOfBirth > oldestBirthDateExclusive
                && profile.DateOfBirth <= youngestBirthDate
                && myPreferences.PreferredGenders
                    .Contains(profile.Gender)
                && (myPreferences.PreferredCity == null ||
                    profile.City == myPreferences.PreferredCity)
                && preferences.MinAge <= myAge
                && preferences.MaxAge >= myAge
                && preferences.PreferredGenders
                    .Contains(myProfile.Gender)
                && (preferences.PreferredCity == null ||
                    preferences.PreferredCity == myProfile.City)
                && preferences.RelationshipGoal ==
                    myPreferences.RelationshipGoal
            select profile.UserId;

        return await query.AnyAsync(cancellationToken);
    }

    private static int CalculateAge(
        DateOnly dateOfBirth,
        DateOnly today)
    {
        var age = today.Year - dateOfBirth.Year;

        if (dateOfBirth.Month > today.Month ||
            (dateOfBirth.Month == today.Month &&
             dateOfBirth.Day > today.Day))
        {
            age--;
        }

        return age;
    }
}