namespace Duett.API.Models;

public class AdminUserDetails
{
    public string UserId { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public bool EmailConfirmed { get; set; }

    public string? DisplayName { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTimeOffset? LockoutEnd { get; set; }
}

public class AdminReportDetails
{
    public Guid Id { get; set; }

    public string ReporterUserId { get; set; } = string.Empty;

    public string ReporterEmail { get; set; } = string.Empty;

    public string? ReporterDisplayName { get; set; }

    public string ReportedUserId { get; set; } = string.Empty;

    public string ReportedEmail { get; set; } = string.Empty;

    public string? ReportedDisplayName { get; set; }

    public Guid? MatchId { get; set; }

    public UserReportReason Reason { get; set; }

    public string? Details { get; set; }

    public UserReportStatus Status { get; set; }

    public string? ReviewedByUserId { get; set; }

    public string? ReviewNote { get; set; }

    public DateTime? ReviewedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}