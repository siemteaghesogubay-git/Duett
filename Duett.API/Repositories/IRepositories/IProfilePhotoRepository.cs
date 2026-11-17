using Duett.API.Models;

namespace Duett.API.Repositories.IRepositories;

public interface IProfilePhotoRepository
{
    Task<List<ProfilePhoto>> GetByUserIdAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<ProfilePhoto?> GetByIdAsync(
        string userId,
        Guid photoId,
        CancellationToken cancellationToken = default);

    Task<ProfilePhoto?> AddAsync(
        string userId,
        string storageKey,
        string contentType,
        long sizeBytes,
        int maxPhotos,
        CancellationToken cancellationToken = default);

    Task<ProfilePhoto?> DeleteAsync(
        string userId,
        Guid photoId,
        CancellationToken cancellationToken = default);


    Task<bool> SetPrimaryAsync(
       string userId,
       Guid photoId,
       CancellationToken cancellationToken = default);
}