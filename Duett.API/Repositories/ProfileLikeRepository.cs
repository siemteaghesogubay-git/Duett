using Duett.API.Data;
using Duett.API.Models;
using Duett.API.Repositories.IRepositories;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Duett.API.Repositories;

public class ProfileLikeRepository : IProfileLikeRepository
{
    private readonly ApplicationDbContext _context;

    public ProfileLikeRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ExistsAsync(
        string fromUserId,
        string toUserId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ProfileLikes
            .AnyAsync(
                like => like.FromUserId == fromUserId
                    && like.ToUserId == toUserId,
                cancellationToken);
    }

    public async Task AddAsync(
        ProfileLike like,
        CancellationToken cancellationToken = default)
    {
        _context.ProfileLikes.Add(like);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is SqlException sqlException &&
            (sqlException.Number == 2601 ||
             sqlException.Number == 2627))
        {
            // Ett samtidigt anrop kan redan ha sparat samma gillning.
            // Ta bort det misslyckade tillägget från EF:s spårning.
            _context.Entry(like).State = EntityState.Detached;

            var alreadyExists = await ExistsAsync(
                like.FromUserId,
                like.ToUserId,
                cancellationToken);

            // Ignorera bara felet om just denna gillning finns.
            if (!alreadyExists)
            {
                throw;
            }
        }
    }
}