using Duett.API.Data;
using Duett.API.Models;
using Duett.API.Repositories.IRepositories;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Duett.API.Repositories;

public class UserMatchRepository : IUserMatchRepository
{
    private readonly ApplicationDbContext _context;

    public UserMatchRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UserMatch> GetOrCreateAsync(
        string firstUserId,
        string secondUserId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(firstUserId);
        ArgumentException.ThrowIfNullOrWhiteSpace(secondUserId);

        if (string.Equals(
            firstUserId,
            secondUserId,
            StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "En matchning kräver två olika användare.");
        }

        if (string.CompareOrdinal(firstUserId, secondUserId) > 0)
        {
            (firstUserId, secondUserId) =
                (secondUserId, firstUserId);
        }

        var existing = await _context.UserMatches
            .AsNoTracking()
            .SingleOrDefaultAsync(
                match => match.FirstUserId == firstUserId
                    && match.SecondUserId == secondUserId,
                cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        var newMatch = new UserMatch
        {
            Id = Guid.NewGuid(),
            FirstUserId = firstUserId,
            SecondUserId = secondUserId,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.UserMatches.Add(newMatch);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);

            return newMatch;
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is SqlException sqlException &&
            (sqlException.Number == 2601 ||
             sqlException.Number == 2627))
        {
            _context.Entry(newMatch).State = EntityState.Detached;

            var savedMatch = await _context.UserMatches
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    match => match.FirstUserId == firstUserId
                        && match.SecondUserId == secondUserId,
                    cancellationToken);

            if (savedMatch is null)
            {
                throw;
            }

            return savedMatch;
        }
    }

    public async Task<List<UserMatchDetails>> GetForUserAsync(
        string userId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        if (skip < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(skip));
        }

        if (take < 1 || take > 50)
        {
            throw new ArgumentOutOfRangeException(nameof(take));
        }

        var query =
            from match in _context.UserMatches.AsNoTracking()
            where match.FirstUserId == userId
                || match.SecondUserId == userId
            let otherUserId = match.FirstUserId == userId
                ? match.SecondUserId
                : match.FirstUserId
            where !_context.UserBlocks.Any(block =>
                (block.BlockerUserId == userId &&
                 block.BlockedUserId == otherUserId) ||
                (block.BlockerUserId == otherUserId &&
                 block.BlockedUserId == userId))
            join profile in _context.UserProfiles.AsNoTracking()
                on otherUserId equals profile.UserId
            select new UserMatchDetails
            {
                MatchId = match.Id,
                OtherUserId = profile.UserId,
                DisplayName = profile.DisplayName,
                DateOfBirth = profile.DateOfBirth,
                City = profile.City,
                CreatedAtUtc = match.CreatedAtUtc,

                RelationshipMode = _context.PartnerPreferences
                    .Where(preferences =>
                        preferences.UserId == otherUserId)
                    .Select(preferences =>
                        preferences.RelationshipMode)
                    .FirstOrDefault(),

                MyCorrectGuessCount = _context.GameRoundSubmissions
                    .Count(submission =>
                        submission.UserId == userId
                        && submission.Round.MatchId == match.Id
                        && submission.Round.CompletedAtUtc != null
                        && _context.GameRoundSubmissions.Any(
                            partnerSubmission =>
                                partnerSubmission.RoundId
                                    == submission.RoundId
                                && partnerSubmission.UserId
                                    == otherUserId
                                && partnerSubmission.AnswerOptionId
                                    == submission.GuessOptionId))
            };

        return await query
            .OrderByDescending(match => match.CreatedAtUtc)
            .ThenBy(match => match.MatchId)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }
}