using System.ComponentModel.DataAnnotations;
using Azure.Communication;
using Azure.Communication.Identity;
using Duett.API.Dtos;
using Duett.API.Models;
using Duett.API.Repositories.IRepositories;
using Duett.API.Services.IServices;
using Duett.API.Storage.IStorage;
using Microsoft.AspNetCore.Identity;

namespace Duett.API.Services;

public class AdminService : IAdminService
{
    private readonly IAdminRepository _repository;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IPrivatePhotoStorage _photoStorage;
    private readonly CommunicationIdentityClient _identityClient;
    private readonly ILogger<AdminService> _logger;

    public AdminService(
        IAdminRepository repository,
        UserManager<ApplicationUser> userManager,
        IPrivatePhotoStorage photoStorage,
        CommunicationIdentityClient identityClient,
        ILogger<AdminService> logger)
    {
        _repository = repository;
        _userManager = userManager;
        _photoStorage = photoStorage;
        _identityClient = identityClient;
        _logger = logger;
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

    public async Task<bool> DeactivateUserAsync(
        string userId,
        string adminUserId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(adminUserId);

        var user = await _userManager.FindByIdAsync(userId);

        if (user is null)
        {
            return false;
        }

        await EnsureUserCanBeDisabledOrDeletedAsync(
            user,
            adminUserId);

        if (!user.LockoutEnabled)
        {
            user.LockoutEnabled = true;

            EnsureIdentitySucceeded(
                await _userManager.UpdateAsync(user),
                "Kunde inte aktivera kontolåsning.");
        }

        EnsureIdentitySucceeded(
            await _userManager.SetLockoutEndDateAsync(
                user,
                DateTimeOffset.MaxValue),
            "Kunde inte avaktivera kontot.");

        EnsureIdentitySucceeded(
            await _userManager.UpdateSecurityStampAsync(user),
            "Kunde inte uppdatera kontots säkerhetsinformation.");

        return true;
    }

    public async Task<bool> ReactivateUserAsync(
        string userId,
        string adminUserId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(adminUserId);

        var user = await _userManager.FindByIdAsync(userId);

        if (user is null)
        {
            return false;
        }

        if (user.Id == adminUserId)
        {
            throw new ValidationException(
                "Du kan inte ändra status på ditt eget konto.");
        }

        EnsureIdentitySucceeded(
            await _userManager.SetLockoutEndDateAsync(user, null),
            "Kunde inte återaktivera kontot.");

        EnsureIdentitySucceeded(
            await _userManager.ResetAccessFailedCountAsync(user),
            "Kunde inte återställa inloggningsförsök.");

        EnsureIdentitySucceeded(
            await _userManager.UpdateSecurityStampAsync(user),
            "Kunde inte uppdatera kontots säkerhetsinformation.");

        return true;
    }

    public async Task<bool> DeleteUserAsync(
        string userId,
        string adminUserId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(adminUserId);

        var user = await _userManager.FindByIdAsync(userId);

        if (user is null)
        {
            return false;
        }

        await EnsureUserCanBeDisabledOrDeletedAsync(
            user,
            adminUserId);

        var profilePhoto = await _repository.GetProfilePhotoAsync(
            userId,
            cancellationToken);

        var acsUserId = user.AcsUserId;

        var wasDeleted = await _repository.DeleteUserAsync(
            userId,
            cancellationToken);

        if (!wasDeleted)
        {
            return false;
        }

        await DeleteProfilePhotoAsync(
            profilePhoto,
            cancellationToken);

        await DeleteCallingIdentityAsync(
            acsUserId,
            userId,
            cancellationToken);

        return true;
    }

    private async Task EnsureUserCanBeDisabledOrDeletedAsync(
        ApplicationUser user,
        string adminUserId)
    {
        if (user.Id == adminUserId)
        {
            throw new ValidationException(
                "Du kan inte ändra eller radera ditt eget konto.");
        }

        if (!await _userManager.IsInRoleAsync(user, "ADMIN"))
        {
            return;
        }

        var administrators = await _userManager
            .GetUsersInRoleAsync("ADMIN");

        if (administrators.Count <= 1)
        {
            throw new ValidationException(
                "Det går inte att ändra eller radera den sista administratören.");
        }
    }

    private async Task DeleteProfilePhotoAsync(
        ProfilePhoto? profilePhoto,
        CancellationToken cancellationToken)
    {
        if (profilePhoto is null)
        {
            return;
        }

        try
        {
            await _photoStorage.DeleteAsync(
                profilePhoto.StorageKey,
                cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Kunde inte radera profilbilden {StorageKey} efter " +
                "att användaren har raderats.",
                profilePhoto.StorageKey);
        }
    }

    private async Task DeleteCallingIdentityAsync(
        string? acsUserId,
        string userId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(acsUserId))
        {
            return;
        }

        try
        {
            var communicationUser =
                new CommunicationUserIdentifier(acsUserId);

            await _identityClient.DeleteUserAsync(
                communicationUser,
                cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Kunde inte radera samtalsidentiteten för användare {UserId}.",
                userId);
        }
    }

    private static void EnsureIdentitySucceeded(
        IdentityResult result,
        string errorMessage)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(errorMessage);
        }
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