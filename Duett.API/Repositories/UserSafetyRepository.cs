using Duett.API.Data;
using Duett.API.Models;
using Duett.API.Repositories.IRepositories;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Duett.API.Repositories;

public class UserSafetyRepository : IUserSafetyRepository
{
    private readonly ApplicationDbContext _context;

    public UserSafetyRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<bool> UserExistsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return _context.Users
            .AsNoTracking()
            .AnyAsync(user => user.Id == userId, cancellationToken);
    }

    public Task<bool> IsBlockedBetweenUsersAsync(
        string firstUserId,
        string secondUserId,
        CancellationToken cancellationToken = default)
    {
        return _context.UserBlocks
            .AsNoTracking()
            .AnyAsync(
                block =>
                    (block.BlockerUserId == firstUserId &&
                     block.BlockedUserId == secondUserId) ||
                    (block.BlockerUserId == secondUserId &&
                     block.BlockedUserId == firstUserId),
                cancellationToken);
    }

    public async Task AddBlockAsync(
        UserBlock block,
        CancellationToken cancellationToken = default)
    {
        _context.UserBlocks.Add(block);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is SqlException sqlException &&
            (sqlException.Number == 2601 ||
             sqlException.Number == 2627))
        {
            // Samtidiga anrop kan försöka skapa samma blockering.
            _context.Entry(block).State = EntityState.Detached;

            var alreadyBlocked = await _context.UserBlocks
                .AsNoTracking()
                .AnyAsync(
                    savedBlock =>
                        savedBlock.BlockerUserId ==
                            block.BlockerUserId &&
                        savedBlock.BlockedUserId ==
                            block.BlockedUserId,
                    cancellationToken);

            if (!alreadyBlocked)
            {
                throw;
            }
        }
    }

    public async Task<bool> RemoveBlockAsync(
        string blockerUserId,
        string blockedUserId,
        CancellationToken cancellationToken = default)
    {
        var block = await _context.UserBlocks
            .SingleOrDefaultAsync(
                savedBlock =>
                    savedBlock.BlockerUserId == blockerUserId &&
                    savedBlock.BlockedUserId == blockedUserId,
                cancellationToken);

        if (block is null)
        {
            return false;
        }

        _context.UserBlocks.Remove(block);

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public Task<bool> IsMatchBetweenUsersAsync(
        Guid matchId,
        string firstUserId,
        string secondUserId,
        CancellationToken cancellationToken = default)
    {
        return _context.UserMatches
            .AsNoTracking()
            .AnyAsync(
                match =>
                    match.Id == matchId &&
                    ((match.FirstUserId == firstUserId &&
                      match.SecondUserId == secondUserId) ||
                     (match.FirstUserId == secondUserId &&
                      match.SecondUserId == firstUserId)),
                cancellationToken);
    }

    public async Task AddReportAsync(
        UserReport report,
        CancellationToken cancellationToken = default)
    {
        _context.UserReports.Add(report);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
