using Duett.API.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Duett.API.Data;

public class ApplicationDbContext
    : IdentityDbContext<ApplicationUser, ApplicationRole, string>
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    public DbSet<PartnerPreferences> PartnerPreferences
        => Set<PartnerPreferences>();

    public DbSet<ProfileLike> ProfileLikes => Set<ProfileLike>();

    public DbSet<UserBlock> UserBlocks => Set<UserBlock>();

    public DbSet<UserReport> UserReports => Set<UserReport>();

    public DbSet<UserMatch> UserMatches => Set<UserMatch>();

    public DbSet<GameQuestion> GameQuestions => Set<GameQuestion>();

    public DbSet<GameQuestionOption> GameQuestionOptions
        => Set<GameQuestionOption>();

    public DbSet<GameRound> GameRounds => Set<GameRound>();

    public DbSet<GameRoundSubmission> GameRoundSubmissions
        => Set<GameRoundSubmission>();

    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

    public DbSet<MatchQuestion> MatchQuestions
        => Set<MatchQuestion>();

    public DbSet<ProfilePhoto> ProfilePhotos
        => Set<ProfilePhoto>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Unika e-postadresser.
        builder.Entity<ApplicationUser>()
            .HasIndex(user => user.NormalizedEmail)
            .IsUnique()
            .HasFilter("[NormalizedEmail] IS NOT NULL");

        // Användarprofil.
        builder.Entity<UserProfile>(profile =>
        {
            profile.HasKey(p => p.UserId);

            profile.HasOne(p => p.User)
                .WithOne()
                .HasForeignKey<UserProfile>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            profile.Property(p => p.DisplayName)
                .IsRequired();

            profile.Property(p => p.Gender)
                .IsRequired();

            profile.Property(p => p.City)
                .IsRequired();

            profile.Property(p => p.Bio)
                .IsRequired();
        });

        // Partnerönskemål.
        builder.Entity<PartnerPreferences>(preferences =>
        {
            preferences.HasKey(p => p.UserId);

            preferences.HasOne(p => p.User)
                .WithOne()
                .HasForeignKey<PartnerPreferences>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            preferences.Property(p => p.RelationshipGoal)
                .IsRequired();

            preferences.ToTable("PartnerPreferences", table =>
            {
                table.HasCheckConstraint(
                    "CK_PartnerPreferences_AgeRange",
                    "[MinAge] >= 18 AND [MaxAge] >= [MinAge]");
            });
        });

        // Gillningar.
        builder.Entity<ProfileLike>(like =>
        {
            like.Property(l => l.FromUserId)
                .HasMaxLength(450)
                .IsRequired();

            like.Property(l => l.ToUserId)
                .HasMaxLength(450)
                .IsRequired();

            like.HasKey(l => new
            {
                l.FromUserId,
                l.ToUserId
            });

            like.HasOne(l => l.FromUser)
                .WithMany()
                .HasForeignKey(l => l.FromUserId)
                .OnDelete(DeleteBehavior.NoAction);

            like.HasOne(l => l.ToUser)
                .WithMany()
                .HasForeignKey(l => l.ToUserId)
                .OnDelete(DeleteBehavior.NoAction);

            like.HasIndex(l => l.ToUserId);

            like.ToTable("ProfileLikes", table =>
            {
                table.HasCheckConstraint(
                    "CK_ProfileLikes_NoSelfLike",
                    "[FromUserId] <> [ToUserId]");
            });
        });

        // Blockeringar.
        builder.Entity<UserBlock>(block =>
        {
            block.HasKey(b => new
            {
                b.BlockerUserId,
                b.BlockedUserId
            });

            block.Property(b => b.BlockerUserId)
                .HasMaxLength(450)
                .IsRequired();

            block.Property(b => b.BlockedUserId)
                .HasMaxLength(450)
                .IsRequired();

            block.HasOne(b => b.BlockerUser)
                .WithMany()
                .HasForeignKey(b => b.BlockerUserId)
                .OnDelete(DeleteBehavior.NoAction);

            block.HasOne(b => b.BlockedUser)
                .WithMany()
                .HasForeignKey(b => b.BlockedUserId)
                .OnDelete(DeleteBehavior.NoAction);

            block.HasIndex(b => b.BlockedUserId);

            block.ToTable("UserBlocks", table =>
            {
                table.HasCheckConstraint(
                    "CK_UserBlocks_NoSelfBlock",
                    "[BlockerUserId] <> [BlockedUserId]");
            });
        });

        // Anmälningar.
        builder.Entity<UserReport>(report =>
        {
            report.HasKey(r => r.Id);

            report.Property(r => r.ReporterUserId)
                .HasMaxLength(450)
                .IsRequired();

            report.Property(r => r.ReportedUserId)
                .HasMaxLength(450)
                .IsRequired();

            report.Property(r => r.Reason)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            report.Property(r => r.Status)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            report.Property(r => r.Details)
                .HasMaxLength(1000);

            report.Property(r => r.ReviewedByUserId)
                .HasMaxLength(450);

            report.Property(r => r.ReviewNote)
                .HasMaxLength(2000);

            report.HasOne(r => r.ReporterUser)
                .WithMany()
                .HasForeignKey(r => r.ReporterUserId)
                .OnDelete(DeleteBehavior.NoAction);

            report.HasOne(r => r.ReportedUser)
                .WithMany()
                .HasForeignKey(r => r.ReportedUserId)
                .OnDelete(DeleteBehavior.NoAction);

            report.HasOne(r => r.ReviewedByUser)
                .WithMany()
                .HasForeignKey(r => r.ReviewedByUserId)
                .OnDelete(DeleteBehavior.NoAction);

            report.HasOne(r => r.Match)
                .WithMany()
                .HasForeignKey(r => r.MatchId)
                .OnDelete(DeleteBehavior.SetNull);

            report.HasIndex(r => r.ReportedUserId);

            report.HasIndex(r => new
            {
                r.Status,
                r.CreatedAtUtc
            });

            report.ToTable("UserReports", table =>
            {
                table.HasCheckConstraint(
                    "CK_UserReports_NoSelfReport",
                    "[ReporterUserId] <> [ReportedUserId]");
            });
        });

        // Sparade matchningar.
        builder.Entity<UserMatch>(match =>
        {
            match.HasKey(m => m.Id);

            match.Property(m => m.FirstUserId)
                .HasMaxLength(450)
                .IsRequired();

            match.Property(m => m.SecondUserId)
                .HasMaxLength(450)
                .IsRequired();

            match.HasOne(m => m.FirstUser)
                .WithMany()
                .HasForeignKey(m => m.FirstUserId)
                .OnDelete(DeleteBehavior.NoAction);

            match.HasOne(m => m.SecondUser)
                .WithMany()
                .HasForeignKey(m => m.SecondUserId)
                .OnDelete(DeleteBehavior.NoAction);

            match.HasIndex(m => new
            {
                m.FirstUserId,
                m.SecondUserId
            }).IsUnique();

            match.HasIndex(m => m.SecondUserId);

            match.ToTable("UserMatches", table =>
            {
                table.HasCheckConstraint(
                    "CK_UserMatches_UserOrder",
                    "[FirstUserId] COLLATE Latin1_General_100_BIN2 " +
                    "< [SecondUserId] COLLATE Latin1_General_100_BIN2");
            });
        });

        // Spelfrågor.
        builder.Entity<GameQuestion>(question =>
        {
            question.HasKey(q => q.Id);

            question.Property(q => q.Text)
                .HasMaxLength(300)
                .IsRequired();
        });

        // Svarsalternativ.
        builder.Entity<GameQuestionOption>(option =>
        {
            option.HasKey(o => o.Id);

            option.Property(o => o.Text)
                .HasMaxLength(150)
                .IsRequired();

            option.HasOne(o => o.Question)
                .WithMany(q => q.Options)
                .HasForeignKey(o => o.QuestionId)
                .OnDelete(DeleteBehavior.NoAction);

            option.HasIndex(o => new
            {
                o.QuestionId,
                o.SortOrder
            }).IsUnique();

            option.ToTable("GameQuestionOptions", table =>
            {
                table.HasCheckConstraint(
                    "CK_GameQuestionOptions_SortOrder",
                    "[SortOrder] >= 1");
            });
        });

        // Spelrundor.
        builder.Entity<GameRound>(round =>
        {
            round.HasKey(r => r.Id);

            round.HasOne(r => r.Match)
                .WithMany()
                .HasForeignKey(r => r.MatchId)
                .OnDelete(DeleteBehavior.NoAction);

            round.HasOne(r => r.Question)
                .WithMany()
                .HasForeignKey(r => r.QuestionId)
                .OnDelete(DeleteBehavior.NoAction);

            round.HasIndex(r => new
            {
                r.MatchId,
                r.RoundNumber
            }).IsUnique();

            round.HasIndex(r => new
            {
                r.MatchId,
                r.QuestionId
            }).IsUnique();

            round.ToTable("GameRounds", table =>
            {
                table.HasCheckConstraint(
                    "CK_GameRounds_RoundNumber",
                    "[RoundNumber] >= 1");
            });
        });

        // Deltagarnas svar och gissningar.
        builder.Entity<GameRoundSubmission>(submission =>
        {
            submission.HasKey(s => new
            {
                s.RoundId,
                s.UserId
            });

            submission.Property(s => s.UserId)
                .HasMaxLength(450)
                .IsRequired();

            submission.HasOne(s => s.Round)
                .WithMany()
                .HasForeignKey(s => s.RoundId)
                .OnDelete(DeleteBehavior.NoAction);

            submission.HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            submission.HasOne(s => s.AnswerOption)
                .WithMany()
                .HasForeignKey(s => s.AnswerOptionId)
                .OnDelete(DeleteBehavior.NoAction);

            submission.HasOne(s => s.GuessOption)
                .WithMany()
                .HasForeignKey(s => s.GuessOptionId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // Chattmeddelanden.
        builder.Entity<ChatMessage>(message =>
        {
            message.HasKey(m => m.Id);

            message.Property(m => m.SenderUserId)
                .HasMaxLength(450)
                .IsRequired();

            message.Property(m => m.Text)
                .HasMaxLength(2000)
                .IsRequired();

            message.HasOne(m => m.Match)
                .WithMany()
                .HasForeignKey(m => m.MatchId)
                .OnDelete(DeleteBehavior.NoAction);

            message.HasOne(m => m.SenderUser)
                .WithMany()
                .HasForeignKey(m => m.SenderUserId)
                .OnDelete(DeleteBehavior.NoAction);

            message.HasIndex(m => new
            {
                m.MatchId,
                m.CreatedAtUtc,
                m.Id
            });
        });

        // Frågor mellan matchade användare.
        builder.Entity<MatchQuestion>(question =>
        {
            question.HasKey(q => q.Id);

            question.Property(q => q.AskedByUserId)
                .HasMaxLength(450)
                .IsRequired();

            question.Property(q => q.AnsweredByUserId)
                .HasMaxLength(450);

            question.Property(q => q.Text)
                .HasMaxLength(300)
                .IsRequired();

            question.Property(q => q.AnswerText)
                .HasMaxLength(2000);

            question.HasOne(q => q.Match)
                .WithMany()
                .HasForeignKey(q => q.MatchId)
                .OnDelete(DeleteBehavior.NoAction);

            question.HasOne(q => q.AskedByUser)
                .WithMany()
                .HasForeignKey(q => q.AskedByUserId)
                .OnDelete(DeleteBehavior.NoAction);

            question.HasOne(q => q.AnsweredByUser)
                .WithMany()
                .HasForeignKey(q => q.AnsweredByUserId)
                .OnDelete(DeleteBehavior.NoAction);

            question.HasIndex(q => new
            {
                q.MatchId,
                q.AskedByUserId,
                q.QuestionNumber
            }).IsUnique();

            question.ToTable("MatchQuestions", table =>
            {
                table.HasCheckConstraint(
                    "CK_MatchQuestions_QuestionNumber",
                    "[QuestionNumber] BETWEEN 1 AND 3");

                table.HasCheckConstraint(
                    "CK_MatchQuestions_NoSelfAnswer",
                    "[AnsweredByUserId] IS NULL " +
                    "OR [AnsweredByUserId] <> [AskedByUserId]");

                table.HasCheckConstraint(
                    "CK_MatchQuestions_AnswerState",
                    "([AnswerText] IS NULL " +
                    "AND [AnsweredByUserId] IS NULL " +
                    "AND [AnsweredAtUtc] IS NULL) " +
                    "OR " +
                    "([AnswerText] IS NOT NULL " +
                    "AND [AnsweredByUserId] IS NOT NULL " +
                    "AND [AnsweredAtUtc] IS NOT NULL)");
            });
        });

        // Flera profilbilder per användare.
        builder.Entity<ProfilePhoto>(photo =>
        {
            photo.HasKey(p => p.Id);

            photo.Property(p => p.UserId)
                .HasMaxLength(450)
                .IsRequired();

            photo.Property(p => p.StorageKey)
                .HasMaxLength(200)
                .IsRequired();

            photo.Property(p => p.ContentType)
                .HasMaxLength(50)
                .IsRequired();

            photo.Property(p => p.SizeBytes)
                .IsRequired();

            photo.Property(p => p.SortOrder)
                .IsRequired();

            photo.HasOne(p => p.User)
                .WithMany(user => user.ProfilePhotos)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            photo.HasIndex(p => new
            {
                p.UserId,
                p.SortOrder
            }).IsUnique();
        });
    }
}