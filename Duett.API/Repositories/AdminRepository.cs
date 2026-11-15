using Duett.API.Data;
using Duett.API.Models;
using Duett.API.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace Duett.API.Repositories;

public class AdminRepository : IAdminRepository
{
    private readonly ApplicationDbContext _context;

    public AdminRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<AdminUserDetails>> GetUsersAsync(
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        return await (
            from user in _context.Users.AsNoTracking()

            join profile in _context.UserProfiles.AsNoTracking()
                on user.Id equals profile.UserId into profiles

            from profile in profiles.DefaultIfEmpty()

            orderby user.CreatedAtUtc descending, user.Email

            select new AdminUserDetails
            {
                UserId = user.Id,
                Email = user.Email ?? string.Empty,
                EmailConfirmed = user.EmailConfirmed,
                DisplayName = profile == null
                    ? null
                    : profile.DisplayName,
                CreatedAtUtc = user.CreatedAtUtc,
                LockoutEnd = user.LockoutEnd
            })
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<AdminReportDetails>> GetReportsAsync(
        UserReportStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        IQueryable<AdminReportDetails> query =
            from report in _context.UserReports.AsNoTracking()

            join reporter in _context.Users.AsNoTracking()
                on report.ReporterUserId equals reporter.Id

            join reported in _context.Users.AsNoTracking()
                on report.ReportedUserId equals reported.Id

            join reporterProfile in _context.UserProfiles.AsNoTracking()
                on reporter.Id equals reporterProfile.UserId
                into reporterProfiles

            from reporterProfile in reporterProfiles.DefaultIfEmpty()

            join reportedProfile in _context.UserProfiles.AsNoTracking()
                on reported.Id equals reportedProfile.UserId
                into reportedProfiles

            from reportedProfile in reportedProfiles.DefaultIfEmpty()

            select new AdminReportDetails
            {
                Id = report.Id,

                ReporterUserId = reporter.Id,
                ReporterEmail = reporter.Email ?? string.Empty,
                ReporterDisplayName = reporterProfile == null
                    ? null
                    : reporterProfile.DisplayName,

                ReportedUserId = reported.Id,
                ReportedEmail = reported.Email ?? string.Empty,
                ReportedDisplayName = reportedProfile == null
                    ? null
                    : reportedProfile.DisplayName,

                MatchId = report.MatchId,
                Reason = report.Reason,
                Details = report.Details,
                Status = report.Status,
                ReviewedByUserId = report.ReviewedByUserId,
                ReviewNote = report.ReviewNote,
                ReviewedAtUtc = report.ReviewedAtUtc,
                CreatedAtUtc = report.CreatedAtUtc
            };

        if (status.HasValue)
        {
            query = query.Where(report => report.Status == status.Value);
        }

        return await query
            .OrderBy(report => report.Status)
            .ThenByDescending(report => report.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public Task<UserReport?> GetReportForUpdateAsync(
        Guid reportId,
        CancellationToken cancellationToken = default)
    {
        return _context.UserReports.SingleOrDefaultAsync(
            report => report.Id == reportId,
            cancellationToken);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}