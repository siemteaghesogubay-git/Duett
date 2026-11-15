using System.ComponentModel.DataAnnotations;
using Duett.API.Dtos;
using Duett.API.Models;
using Duett.API.Repositories.IRepositories;
using Duett.API.Services.IServices;

namespace Duett.API.Services;

public class UserSafetyService : IUserSafetyService
{
    private readonly IUserSafetyRepository _repository;

    public UserSafetyService(IUserSafetyRepository repository)
    {
        _repository = repository;
    }

    public async Task BlockAsync(
        string blockerUserId,
        BlockUserDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var blockedUserId = ValidateUserId(dto.BlockedUserId);

        if (string.Equals(
                blockerUserId,
                blockedUserId,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException(
                "Du kan inte blockera dig själv.");
        }

        var userExists = await _repository.UserExistsAsync(
            blockedUserId,
            cancellationToken);

        if (!userExists)
        {
            throw new ValidationException(
                "Användaren kunde inte hittas.");
        }

        var block = new UserBlock
        {
            BlockerUserId = blockerUserId,
            BlockedUserId = blockedUserId,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _repository.AddBlockAsync(
            block,
            cancellationToken);
    }

    public Task<bool> UnblockAsync(
        string blockerUserId,
        string blockedUserId,
        CancellationToken cancellationToken = default)
    {
        var validBlockedUserId = ValidateUserId(blockedUserId);

        return _repository.RemoveBlockAsync(
            blockerUserId,
            validBlockedUserId,
            cancellationToken);
    }

    public async Task<Guid> CreateReportAsync(
        string reporterUserId,
        CreateUserReportDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var reportedUserId = ValidateUserId(dto.ReportedUserId);

        if (string.Equals(
                reporterUserId,
                reportedUserId,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException(
                "Du kan inte rapportera dig själv.");
        }

        var reportedUserExists = await _repository.UserExistsAsync(
            reportedUserId,
            cancellationToken);

        if (!reportedUserExists)
        {
            throw new ValidationException(
                "Användaren kunde inte hittas.");
        }

        var reasonText = dto.Reason?.Trim() ?? string.Empty;

        if (!Enum.GetNames<UserReportReason>()
                .Contains(
                    reasonText,
                    StringComparer.OrdinalIgnoreCase) ||
            !Enum.TryParse<UserReportReason>(
                reasonText,
                ignoreCase: true,
                out var reason))
        {
            throw new ValidationException(
                "Välj en giltig anledning till rapporten.");
        }

        var details = dto.Details?.Trim();

        if (reason == UserReportReason.Other &&
            string.IsNullOrWhiteSpace(details))
        {
            throw new ValidationException(
                "Beskriv rapporten när du väljer Other.");
        }

        if (dto.MatchId == Guid.Empty)
        {
            throw new ValidationException(
                "Matchnings-ID:t är ogiltigt.");
        }

        if (dto.MatchId.HasValue)
        {
            var isMatchBetweenUsers =
                await _repository.IsMatchBetweenUsersAsync(
                    dto.MatchId.Value,
                    reporterUserId,
                    reportedUserId,
                    cancellationToken);

            if (!isMatchBetweenUsers)
            {
                throw new ValidationException(
                    "Rapporten måste gälla en matchning där ni båda deltar.");
            }
        }

        var report = new UserReport
        {
            Id = Guid.NewGuid(),
            ReporterUserId = reporterUserId,
            ReportedUserId = reportedUserId,
            MatchId = dto.MatchId,
            Reason = reason,
            Details = details,
            Status = UserReportStatus.Open,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _repository.AddReportAsync(
            report,
            cancellationToken);

        return report.Id;
    }

    public Task<bool> IsBlockedBetweenUsersAsync(
        string firstUserId,
        string secondUserId,
        CancellationToken cancellationToken = default)
    {
        if (string.Equals(
                firstUserId,
                secondUserId,
                StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(false);
        }

        return _repository.IsBlockedBetweenUsersAsync(
            firstUserId,
            secondUserId,
            cancellationToken);
    }

    private static string ValidateUserId(string? userId)
    {
        var value = userId?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(value) ||
            value.Length > 450)
        {
            throw new ValidationException(
                "Ange ett giltigt användar-ID.");
        }

        return value;
    }
}