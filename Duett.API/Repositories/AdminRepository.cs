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

    public Task<ProfilePhoto?> GetProfilePhotoAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return _context.ProfilePhotos
            .AsNoTracking()
            .SingleOrDefaultAsync(
                photo => photo.UserId == userId,
                cancellationToken);
    }

    public async Task<bool> DeleteUserAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database
            .BeginTransactionAsync(cancellationToken);

        try
        {
            var user = await _context.Users.SingleOrDefaultAsync(
                currentUser => currentUser.Id == userId,
                cancellationToken);

            if (user is null)
            {
                await transaction.RollbackAsync(
                    CancellationToken.None);

                return false;
            }

            var matchIds = _context.UserMatches
                .Where(match =>
                    match.FirstUserId == userId ||
                    match.SecondUserId == userId)
                .Select(match => match.Id);

            var roundIds = _context.GameRounds
                .Where(round => matchIds.Contains(round.MatchId))
                .Select(round => round.Id);

            // Rensa beroenden till spelrundor före rundor och matchningar.
            await _context.GameRoundSubmissions
                .Where(submission =>
                    submission.UserId == userId ||
                    roundIds.Contains(submission.RoundId))
                .ExecuteDeleteAsync(cancellationToken);

            await _context.ChatMessages
                .Where(message =>
                    message.SenderUserId == userId ||
                    matchIds.Contains(message.MatchId))
                .ExecuteDeleteAsync(cancellationToken);

            await _context.MatchQuestions
                .Where(question =>
                    question.AskedByUserId == userId ||
                    question.AnsweredByUserId == userId ||
                    matchIds.Contains(question.MatchId))
                .ExecuteDeleteAsync(cancellationToken);

            await _context.GameRounds
                .Where(round => matchIds.Contains(round.MatchId))
                .ExecuteDeleteAsync(cancellationToken);

            await _context.UserMatches
                .Where(match =>
                    match.FirstUserId == userId ||
                    match.SecondUserId == userId)
                .ExecuteDeleteAsync(cancellationToken);

            // Behåll rapporter som enbart granskats av användaren,
            // men öppna dem igen när granskaren försvinner.
            await _context.UserReports
                .Where(report => report.ReviewedByUserId == userId)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(
                            report => report.ReviewedByUserId,
                            (string?)null)
                        .SetProperty(
                            report => report.ReviewNote,
                            (string?)null)
                        .SetProperty(
                            report => report.ReviewedAtUtc,
                            (DateTime?)null)
                        .SetProperty(
                            report => report.Status,
                            UserReportStatus.Open),
                    cancellationToken);

            // Ta bort rapporter där användaren är anmälare eller anmäld.
            await _context.UserReports
                .Where(report =>
                    report.ReporterUserId == userId ||
                    report.ReportedUserId == userId)
                .ExecuteDeleteAsync(cancellationToken);

            await _context.UserBlocks
                .Where(block =>
                    block.BlockerUserId == userId ||
                    block.BlockedUserId == userId)
                .ExecuteDeleteAsync(cancellationToken);

            await _context.ProfileLikes
                .Where(like =>
                    like.FromUserId == userId ||
                    like.ToUserId == userId)
                .ExecuteDeleteAsync(cancellationToken);

            await _context.ProfilePhotos
                .Where(photo => photo.UserId == userId)
                .ExecuteDeleteAsync(cancellationToken);

            await _context.PartnerPreferences
                .Where(preferences => preferences.UserId == userId)
                .ExecuteDeleteAsync(cancellationToken);

            await _context.UserProfiles
                .Where(profile => profile.UserId == userId)
                .ExecuteDeleteAsync(cancellationToken);

            // Identity-tabeller som roller, claims och tokens rensas
            // genom Identitys befintliga relationer.
            _context.Users.Remove(user);

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return true;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}