using Duett.API.Models;

namespace Duett.API.Repositories.IRepositories;

public interface IUserSafetyRepository
{
    Task<bool> UserExistsAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<bool> IsBlockedBetweenUsersAsync(
        string firstUserId,
        string secondUserId,
        CancellationToken cancellationToken = default);

    Task AddBlockAsync(
        UserBlock block,
        CancellationToken cancellationToken = default);

    Task<bool> RemoveBlockAsync(
        string blockerUserId,
        string blockedUserId,
        CancellationToken cancellationToken = default);

    Task<bool> IsMatchBetweenUsersAsync(
        Guid matchId,
        string firstUserId,
        string secondUserId,
        CancellationToken cancellationToken = default);

    Task AddReportAsync(
        UserReport report,
        CancellationToken cancellationToken = default);
}