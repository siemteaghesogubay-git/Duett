using System.ComponentModel.DataAnnotations;
using Duett.API.Dtos;
using Duett.API.Models;
using Duett.API.Repositories.IRepositories;
using Duett.API.Services.IServices;

namespace Duett.API.Services;

public class AdminService : IAdminService
{
    private readonly IAdminRepository _repository;

    public AdminService(IAdminRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<AdminUserResponseDto>> GetUsersAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var skip = CalculateSkip(pageNumber, pageSize);

        var users = await _repository.GetUsersAsync(
            skip,
            pageSize,
            cancellationToken);

        return users.Select(user => new AdminUserResponseDto
        {
            UserId = user.UserId,
            Email = user.Email,
            EmailConfirmed = user.EmailConfirmed,
            DisplayName = user.DisplayName,
            CreatedAtUtc = user.CreatedAtUtc,
            LockoutEnd = user.LockoutEnd
        }).ToList();
    }

    public async Task<List<AdminReportResponseDto>> GetReportsAsync(
        string? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var skip = CalculateSkip(pageNumber, pageSize);
        UserReportStatus? reportStatus = null;

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<UserReportStatus>(
                    status.Trim(),
                    ignoreCase: true,
                    out var parsedStatus) ||
                !Enum.IsDefined(typeof(UserReportStatus), parsedStatus))
            {
                throw new ValidationException(
                    "Status måste vara Open, UnderReview, Resolved eller Dismissed.");
            }

            reportStatus = parsedStatus;
        }

        var reports = await _repository.GetReportsAsync(
            reportStatus,
            skip,
            pageSize,
            cancellationToken);

        return reports.Select(report => new AdminReportResponseDto
        {
            Id = report.Id,

            ReporterUserId = report.ReporterUserId,
            ReporterEmail = report.ReporterEmail,
            ReporterDisplayName = report.ReporterDisplayName,

            ReportedUserId = report.ReportedUserId,
            ReportedEmail = report.ReportedEmail,
            ReportedDisplayName = report.ReportedDisplayName,

            MatchId = report.MatchId,
            Reason = report.Reason.ToString(),
            Details = report.Details,
            Status = report.Status.ToString(),
            ReviewedByUserId = report.ReviewedByUserId,
            ReviewNote = report.ReviewNote,
            ReviewedAtUtc = report.ReviewedAtUtc,
            CreatedAtUtc = report.CreatedAtUtc
        }).ToList();
    }

    public async Task<bool> ReviewReportAsync(
        Guid reportId,
        string adminUserId,
        ReviewUserReportDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(adminUserId);
        ArgumentNullException.ThrowIfNull(dto);

        if (!Enum.TryParse<UserReportStatus>(
                dto.Status.Trim(),
                ignoreCase: true,
                out var status) ||
            !Enum.IsDefined(typeof(UserReportStatus), status))
        {
            throw new ValidationException(
                "Status måste vara UnderReview, Resolved eller Dismissed.");
        }

        if (status == UserReportStatus.Open)
        {
            throw new ValidationException(
                "En rapport kan inte ändras tillbaka till Open.");
        }

        var reviewNote = string.IsNullOrWhiteSpace(dto.ReviewNote)
            ? null
            : dto.ReviewNote.Trim();

        if (reviewNote?.Length > 2000)
        {
            throw new ValidationException(
                "Administratörens anteckning får vara högst 2 000 tecken.");
        }

        var report = await _repository.GetReportForUpdateAsync(
            reportId,
            cancellationToken);

        if (report is null)
        {
            return false;
        }

        report.Status = status;
        report.ReviewedByUserId = adminUserId;
        report.ReviewNote = reviewNote;
        report.ReviewedAtUtc = DateTime.UtcNow;

        await _repository.SaveChangesAsync(cancellationToken);

        return true;
    }

    private static int CalculateSkip(
        int pageNumber,
        int pageSize)
    {
        if (pageNumber < 1)
        {
            throw new ValidationException(
                "Sidnumret måste vara minst 1.");
        }

        if (pageSize < 1 || pageSize > 50)
        {
            throw new ValidationException(
                "Antalet resultat per sida måste vara mellan 1 och 50.");
        }

        var skip = ((long)pageNumber - 1) * pageSize;

        if (skip > int.MaxValue)
        {
            throw new ValidationException(
                "Sidnumret är för stort.");
        }

        return (int)skip;
    }
}