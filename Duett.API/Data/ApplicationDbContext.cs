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

    public DbSet<UserMatch> UserMatches => Set<UserMatch>();

    public DbSet<GameQuestion> GameQuestions => Set<GameQuestion>();

    public DbSet<GameQuestionOption> GameQuestionOptions
        => Set<GameQuestionOption>();

    public DbSet<GameRound> GameRounds => Set<GameRound>();

    public DbSet<GameRoundSubmission> GameRoundSubmissions
        => Set<GameRoundSubmission>();

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

            profile.Property(p => p.DisplayName).IsRequired();
            profile.Property(p => p.Gender).IsRequired();
            profile.Property(p => p.City).IsRequired();
            profile.Property(p => p.Bio).IsRequired();
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
    }
}