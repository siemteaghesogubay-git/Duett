using Duett.API.Models;

namespace Duett.API.Repositories.IRepositories;

public interface IUserProfileRepository
{
    Task<UserProfile?> GetByUserIdAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        UserProfile profile,
        CancellationToken cancellationToken = default);

    Task<UserProfile?> GetTrackedByUserIdAsync(
    string userId,
    CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}