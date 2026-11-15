namespace Duett.API.Models;

public enum UserReportReason
{
    InappropriateProfile = 1,
    Harassment = 2,
    FakeProfile = 3,
    Other = 4
}

public enum UserReportStatus
{
    Open = 1,
    UnderReview = 2,
    Resolved = 3,
    Dismissed = 4
}

public class UserReport
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string ReporterUserId { get; set; } = string.Empty;

    public ApplicationUser ReporterUser { get; set; } = null!;

    public string ReportedUserId { get; set; } = string.Empty;

    public ApplicationUser ReportedUser { get; set; } = null!;

    public Guid? MatchId { get; set; }

    public UserMatch? Match { get; set; }

    public UserReportReason Reason { get; set; }

    public string? Details { get; set; }

    public UserReportStatus Status { get; set; } =
        UserReportStatus.Open;

    public string? ReviewedByUserId { get; set; }

    public ApplicationUser? ReviewedByUser { get; set; }

    public string? ReviewNote { get; set; }

    public DateTime? ReviewedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

}