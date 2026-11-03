using Duett.API.Data;
using Duett.API.Models;
using Duett.API.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace Duett.API.Repositories;

public class PartnerPreferencesRepository
    : IPartnerPreferencesRepository
{
    private readonly ApplicationDbContext _context;

    public PartnerPreferencesRepository(
        ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PartnerPreferences?> GetByUserIdAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.PartnerPreferences
            .AsNoTracking()
            .SingleOrDefaultAsync(
                preferences => preferences.UserId == userId,
                cancellationToken);
    }

    public async Task<PartnerPreferences?> GetTrackedByUserIdAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.PartnerPreferences
            .SingleOrDefaultAsync(
                preferences => preferences.UserId == userId,
                cancellationToken);
    }

    public async Task AddAsync(
        PartnerPreferences preferences,
        CancellationToken cancellationToken = default)
    {
        _context.PartnerPreferences.Add(preferences);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}