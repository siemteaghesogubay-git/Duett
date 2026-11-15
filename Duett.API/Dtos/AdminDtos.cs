using System.ComponentModel.DataAnnotations;

namespace Duett.API.Dtos;

public class AdminUserResponseDto
{
    public string UserId { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public bool EmailConfirmed { get; set; }

    public string? DisplayName { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTimeOffset? LockoutEnd { get; set; }
}

public class AdminReportResponseDto
{
    public Guid Id { get; set; }

    public string ReporterUserId { get; set; } = string.Empty;

    public string ReporterEmail { get; set; } = string.Empty;

    public string? ReporterDisplayName { get; set; }

    public string ReportedUserId { get; set; } = string.Empty;

    public string ReportedEmail { get; set; } = string.Empty;

    public string? ReportedDisplayName { get; set; }

    public Guid? MatchId { get; set; }

    public string Reason { get; set; } = string.Empty;

    public string? Details { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? ReviewedByUserId { get; set; }

    public string? ReviewNote { get; set; }

    public DateTime? ReviewedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}

public class ReviewUserReportDto
{
    [Required(ErrorMessage = "Ange rapportens nya status.")]
    [StringLength(50)]
    public string Status { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? ReviewNote { get; set; }
}