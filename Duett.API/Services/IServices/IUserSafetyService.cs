using Duett.API.Dtos;

namespace Duett.API.Services.IServices;

public interface IUserSafetyService
{
    Task BlockAsync(
        string blockerUserId,
        BlockUserDto dto,
        CancellationToken cancellationToken = default);

    Task<bool> UnblockAsync(
        string blockerUserId,
        string blockedUserId,
        CancellationToken cancellationToken = default);

    Task<Guid> CreateReportAsync(
        string reporterUserId,
        CreateUserReportDto dto,
        CancellationToken cancellationToken = default);

    Task<bool> IsBlockedBetweenUsersAsync(
        string firstUserId,
        string secondUserId,
        CancellationToken cancellationToken = default);
}