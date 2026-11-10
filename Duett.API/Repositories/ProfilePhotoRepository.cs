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

    public async Task<ProfilePhoto?> GetByUserIdAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        return await _context.ProfilePhotos
            .AsNoTracking()
            .SingleOrDefaultAsync(
                photo => photo.UserId == userId,
                cancellationToken);
    }

    public async Task<(
        string? PreviousStorageKey,
        DateTime UploadedAtUtc)> ReplaceAsync(
        string userId,
        string storageKey,
        string contentType,
        long sizeBytes,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        if (sizeBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sizeBytes));
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                cancellationToken);

        // Lås användarraden under uppdateringen.
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
                photo => photo.UserId == userId,
                cancellationToken);

        var previousStorageKey = photo?.StorageKey;

        if (photo is null)
        {
            photo = new ProfilePhoto
            {
                UserId = userId
            };

            _context.ProfilePhotos.Add(photo);
        }

        photo.StorageKey = storageKey;
        photo.ContentType = contentType;
        photo.SizeBytes = sizeBytes;
        photo.UploadedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return (previousStorageKey, photo.UploadedAtUtc);
    }
}