using Duett.API.Models;

namespace Duett.API.Repositories.IRepositories;

public interface IPartnerPreferencesRepository
{
    Task<PartnerPreferences?> GetByUserIdAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<PartnerPreferences?> GetTrackedByUserIdAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        PartnerPreferences preferences,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}