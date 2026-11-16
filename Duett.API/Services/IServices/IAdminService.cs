using Duett.API.Dtos;

namespace Duett.API.Services.IServices;

public interface IAdminService
{
    Task<List<AdminUserResponseDto>> GetUsersAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<List<AdminReportResponseDto>> GetReportsAsync(
        string? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<bool> ReviewReportAsync(
        Guid reportId,
        string adminUserId,
        ReviewUserReportDto dto,
        CancellationToken cancellationToken = default);

    Task<bool> DeactivateUserAsync(
        string userId,
        string adminUserId,
        CancellationToken cancellationToken = default);

    Task<bool> ReactivateUserAsync(
        string userId,
        string adminUserId,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteUserAsync(
        string userId,
        string adminUserId,
        CancellationToken cancellationToken = default);
}