using Duett.API.Models;

namespace Duett.API.Repositories.IRepositories;

public interface IAdminRepository
{
    Task<List<AdminUserDetails>> GetUsersAsync(
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<List<AdminReportDetails>> GetReportsAsync(
        UserReportStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<UserReport?> GetReportForUpdateAsync(
        Guid reportId,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);

}