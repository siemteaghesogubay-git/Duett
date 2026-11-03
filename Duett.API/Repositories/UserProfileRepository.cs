using Duett.API.Data;
using Duett.API.Models;
using Duett.API.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace Duett.API.Repositories;

public class UserProfileRepository : IUserProfileRepository
{
    private readonly ApplicationDbContext _context;

    public UserProfileRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UserProfile?> GetByUserIdAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.UserProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(
                profile => profile.UserId == userId,
                cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.UserProfiles
            .AnyAsync(
                profile => profile.UserId == userId,
                cancellationToken);
    }

    public async Task AddAsync(
        UserProfile profile,
        CancellationToken cancellationToken = default)
    {
        _context.UserProfiles.Add(profile);

        await _context.SaveChangesAsync(cancellationToken);
    }
    public async Task<UserProfile?> GetTrackedByUserIdAsync(
    string userId,
    CancellationToken cancellationToken = default)
    {
        return await _context.UserProfiles
            .SingleOrDefaultAsync(
                profile => profile.UserId == userId,
                cancellationToken);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}


