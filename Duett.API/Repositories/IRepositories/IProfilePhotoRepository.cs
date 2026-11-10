using Duett.API.Models;

namespace Duett.API.Repositories.IRepositories;

public interface IProfilePhotoRepository
{
    Task<ProfilePhoto?> GetByUserIdAsync(
        string userId,
        CancellationToken cancellationToken = default);

    // Returnerar den gamla bildens lagringsnyckel
    // och den nya bildens sparade uppladdningstid.
    Task<(
        string? PreviousStorageKey,
        DateTime UploadedAtUtc)> ReplaceAsync(
        string userId,
        string storageKey,
        string contentType,
        long sizeBytes,
        CancellationToken cancellationToken = default);
}