using Duett.API.Data;
using Duett.API.Models;
using Duett.API.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace Duett.API.Repositories;

public class ProfilePhotoRepository : IProfilePhotoRepository
{
    private readonly ApplicationDbContext _context;

    public ProfilePhotoRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ProfilePhoto>> GetByUserIdAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        return await _context.ProfilePhotos
            .AsNoTracking()
            .Where(photo => photo.UserId == userId)
            .OrderBy(photo => photo.SortOrder)
            .ThenBy(photo => photo.UploadedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<ProfilePhoto?> GetByIdAsync(
        string userId,
        Guid photoId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        return await _context.ProfilePhotos
            .AsNoTracking()
            .SingleOrDefaultAsync(
                photo =>
                    photo.Id == photoId &&
                    photo.UserId == userId,
                cancellationToken);
    }

    public async Task<ProfilePhoto?> AddAsync(
        string userId,
        string storageKey,
        string contentType,
        long sizeBytes,
        int maxPhotos,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        if (sizeBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sizeBytes));
        }

        if (maxPhotos < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxPhotos));
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                cancellationToken);

        // Hindrar två samtidiga uppladdningar från samma användare.
        var user = await _context.Users
            .FromSqlInterpolated($"""
                SELECT *
                FROM [dbo].[AspNetUsers] WITH (UPDLOCK, HOLDLOCK)
                WHERE [Id] = {userId}
                """)
            .AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            throw new InvalidOperationException(
                "Användaren finns inte längre.");
        }

        var existingPhotos = await _context.ProfilePhotos
            .Where(photo => photo.UserId == userId)
            .OrderBy(photo => photo.SortOrder)
            .ToListAsync(cancellationToken);

        if (existingPhotos.Count >= maxPhotos)
        {
            return null;
        }

        var nextSortOrder = existingPhotos.Count == 0
            ? 0
            : existingPhotos.Max(photo => photo.SortOrder) + 1;

        var photo = new ProfilePhoto
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            StorageKey = storageKey,
            ContentType = contentType,
            SizeBytes = sizeBytes,
            SortOrder = nextSortOrder,
            UploadedAtUtc = DateTime.UtcNow
        };

        _context.ProfilePhotos.Add(photo);

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return photo;
    }

    public async Task<ProfilePhoto?> DeleteAsync(
        string userId,
        Guid photoId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                cancellationToken);

        // Hindrar uppladdning och borttagning från att ändra ordningen
        // samtidigt för samma användare.
        var user = await _context.Users
            .FromSqlInterpolated($"""
                SELECT *
                FROM [dbo].[AspNetUsers] WITH (UPDLOCK, HOLDLOCK)
                WHERE [Id] = {userId}
                """)
            .AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            throw new InvalidOperationException(
                "Användaren finns inte längre.");
        }

        var photo = await _context.ProfilePhotos
            .SingleOrDefaultAsync(
                item =>
                    item.Id == photoId &&
                    item.UserId == userId,
                cancellationToken);

        if (photo is null)
        {
            return null;
        }

        _context.ProfilePhotos.Remove(photo);

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return photo;
    }






    public async Task<bool> SetPrimaryAsync(
    string userId,
    Guid photoId,
    CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                cancellationToken);

        var user = await _context.Users
            .FromSqlInterpolated($"""
            SELECT *
            FROM [dbo].[AspNetUsers] WITH (UPDLOCK, HOLDLOCK)
            WHERE [Id] = {userId}
            """)
            .AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            throw new InvalidOperationException(
                "The user no longer exists.");
        }

        var photos = await _context.ProfilePhotos
            .Where(photo => photo.UserId == userId)
            .OrderBy(photo => photo.SortOrder)
            .ThenBy(photo => photo.UploadedAtUtc)
            .ToListAsync(cancellationToken);

        var selectedPhoto = photos
            .SingleOrDefault(photo => photo.Id == photoId);

        if (selectedPhoto is null)
        {
            return false;
        }

        var orderedPhotos = photos
            .OrderBy(photo => photo.Id == photoId ? 0 : 1)
            .ThenBy(photo => photo.SortOrder)
            .ThenBy(photo => photo.UploadedAtUtc)
            .ToList();

        // Tillfälliga positiva värden undviker konflikter
        // med det unika indexet UserId + SortOrder.
        var offset = photos.Count + 1000;

        foreach (var photo in orderedPhotos)
        {
            photo.SortOrder += offset;
        }

        await _context.SaveChangesAsync(cancellationToken);

        for (var index = 0; index < orderedPhotos.Count; index++)
        {
            orderedPhotos[index].SortOrder = index;
        }

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return true;
    }
}