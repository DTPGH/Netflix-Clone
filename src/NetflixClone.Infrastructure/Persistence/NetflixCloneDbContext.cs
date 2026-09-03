using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using NetflixClone.Domain.Entities;

namespace NetflixClone.Infrastructure.Persistence;

public partial class NetflixCloneDbContext : DbContext
{
    public NetflixCloneDbContext(DbContextOptions<NetflixCloneDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AdminActionLog> AdminActionLogs { get; set; }

    public virtual DbSet<Device> Devices { get; set; }

    public virtual DbSet<Genre> Genres { get; set; }

    public virtual DbSet<Movie> Movies { get; set; }

    public virtual DbSet<MovieCredit> MovieCredits { get; set; }

    public virtual DbSet<MyListItem> MyListItems { get; set; }

    public virtual DbSet<PaymentTransaction> PaymentTransactions { get; set; }

    public virtual DbSet<Person> People { get; set; }

    public virtual DbSet<Plan> Plans { get; set; }

    public virtual DbSet<Profile> Profiles { get; set; }

    public virtual DbSet<ProfilePreference> ProfilePreferences { get; set; }

    public virtual DbSet<Rating> Ratings { get; set; }

    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Subscription> Subscriptions { get; set; }

    public virtual DbSet<UserAccount> UserAccounts { get; set; }

    public virtual DbSet<UserRole> UserRoles { get; set; }

    public virtual DbSet<ViewingSession> ViewingSessions { get; set; }

    public virtual DbSet<WatchHistory> WatchHistories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AdminActionLog>(entity =>
        {
            entity.HasIndex(e => e.ActorUserAccountId, "IX_AdminActionLogs_ActorUserAccountId");

            entity.HasIndex(e => e.CreatedAt, "IX_AdminActionLogs_CreatedAt");

            entity.HasIndex(e => e.TargetUserAccountId, "IX_AdminActionLogs_TargetUserAccountId");

            entity.HasIndex(e => new { e.TargetUserAccountId, e.CreatedAt }, "IX_AdminActionLogs_TargetUserAccountId_CreatedAt");

            entity.Property(e => e.Action).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Reason).HasMaxLength(500);

            entity.HasOne(d => d.ActorUserAccount).WithMany(p => p.AdminActionLogActorUserAccounts)
                .HasForeignKey(d => d.ActorUserAccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AdminActionLogs_ActorUserAccounts");

            entity.HasOne(d => d.TargetUserAccount).WithMany(p => p.AdminActionLogTargetUserAccounts)
                .HasForeignKey(d => d.TargetUserAccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AdminActionLogs_TargetUserAccounts");
        });

        modelBuilder.Entity<Device>(entity =>
        {
            entity.HasIndex(e => e.LastActiveAt, "IX_Devices_LastActiveAt");

            entity.HasIndex(e => e.UserAccountId, "IX_Devices_UserAccountId");

            entity.HasIndex(e => new { e.UserAccountId, e.RevokedAt }, "IX_Devices_UserAccountId_RevokedAt");

            entity.HasIndex(e => new { e.UserAccountId, e.DeviceIdentifierHash }, "UQ_Devices_UserAccountId_DeviceIdentifierHash").IsUnique();

            entity.Property(e => e.DeviceIdentifierHash).HasMaxLength(500);
            entity.Property(e => e.DeviceName).HasMaxLength(200);
            entity.Property(e => e.DeviceType).HasMaxLength(50);
            entity.Property(e => e.FirstSeenAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.LastActiveAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.UserAgent).HasMaxLength(500);

            entity.HasOne(d => d.UserAccount).WithMany(p => p.Devices)
                .HasForeignKey(d => d.UserAccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Devices_UserAccounts");
        });

        modelBuilder.Entity<Genre>(entity =>
        {
            entity.HasIndex(e => e.Name, "UQ__Genres__737584F65692E934").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<Movie>(entity =>
        {
            entity.HasIndex(e => e.IsAvailable, "IX_Movies_IsAvailable");

            entity.HasIndex(e => e.IsFeatured, "IX_Movies_IsFeatured");

            entity.HasIndex(e => e.ReleaseDate, "IX_Movies_ReleaseDate");

            entity.HasIndex(e => e.Title, "IX_Movies_Title");

            entity.Property(e => e.BackdropUrl).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.IsAvailable).HasDefaultValue(true);
            entity.Property(e => e.MaturityRating)
                .HasMaxLength(10)
                .HasDefaultValue("P");
            entity.Property(e => e.ThumbnailUrl).HasMaxLength(500);
            entity.Property(e => e.Title).HasMaxLength(255);
            entity.Property(e => e.TrailerUrl).HasMaxLength(500);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.VideoUrl).HasMaxLength(500);

            entity.HasMany(d => d.Genres).WithMany(p => p.Movies)
                .UsingEntity<Dictionary<string, object>>(
                    "MovieGenre",
                    r => r.HasOne<Genre>().WithMany()
                        .HasForeignKey("GenreId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_MovieGenres_Genres"),
                    l => l.HasOne<Movie>().WithMany()
                        .HasForeignKey("MovieId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_MovieGenres_Movies"),
                    j =>
                    {
                        j.HasKey("MovieId", "GenreId").HasName("PK__MovieGen__BBEAC44DEC9C51B1");
                        j.ToTable("MovieGenres");
                        j.HasIndex(new[] { "GenreId" }, "IX_MovieGenres_GenreId");
                    });
        });

        modelBuilder.Entity<MovieCredit>(entity =>
        {
            entity.HasIndex(e => e.MovieId, "IX_MovieCredits_MovieId");

            entity.HasIndex(e => new { e.MovieId, e.CreditType }, "IX_MovieCredits_MovieId_CreditType");

            entity.HasIndex(e => e.PersonId, "IX_MovieCredits_PersonId");

            entity.Property(e => e.CharacterName).HasMaxLength(200);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.CreditType).HasMaxLength(20);

            entity.HasOne(d => d.Movie).WithMany(p => p.MovieCredits)
                .HasForeignKey(d => d.MovieId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MovieCredits_Movies");

            entity.HasOne(d => d.Person).WithMany(p => p.MovieCredits)
                .HasForeignKey(d => d.PersonId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MovieCredits_People");
        });

        modelBuilder.Entity<MyListItem>(entity =>
        {
            entity.HasIndex(e => e.MovieId, "IX_MyListItems_MovieId");

            entity.HasIndex(e => new { e.ProfileId, e.MovieId }, "UQ_MyListItems_ProfileId_MovieId").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Movie).WithMany(p => p.MyListItems)
                .HasForeignKey(d => d.MovieId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MyListItems_Movies");

            entity.HasOne(d => d.Profile).WithMany(p => p.MyListItems)
                .HasForeignKey(d => d.ProfileId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MyListItems_Profiles");
        });

        modelBuilder.Entity<PaymentTransaction>(entity =>
        {
            entity.HasIndex(e => e.PaidAt, "IX_PaymentTransactions_PaidAt");

            entity.HasIndex(e => e.Status, "IX_PaymentTransactions_Status");

            entity.HasIndex(e => e.SubscriptionId, "IX_PaymentTransactions_SubscriptionId");

            entity.HasIndex(e => e.TransactionCode, "UQ__PaymentT__D85E7026CB14D93F").IsUnique();

            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Method).HasMaxLength(50);
            entity.Property(e => e.Status).HasMaxLength(20);
            entity.Property(e => e.TransactionCode).HasMaxLength(100);

            entity.HasOne(d => d.Subscription).WithMany(p => p.PaymentTransactions)
                .HasForeignKey(d => d.SubscriptionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PaymentTransactions_Subscriptions");
        });

        modelBuilder.Entity<Person>(entity =>
        {
            entity.HasIndex(e => e.FullName, "IX_People_FullName");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.FullName).HasMaxLength(200);
            entity.Property(e => e.PhotoUrl).HasMaxLength(500);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysdatetime())");
        });

        modelBuilder.Entity<Plan>(entity =>
        {
            entity.HasIndex(e => e.Name, "UQ__Plans__737584F6D9F2247B").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.MaxQuality).HasMaxLength(20);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysdatetime())");
        });

        modelBuilder.Entity<Profile>(entity =>
        {
            entity.HasIndex(e => e.UserAccountId, "IX_Profiles_UserAccountId");

            entity.HasIndex(e => new { e.UserAccountId, e.IsDeleted }, "IX_Profiles_UserAccountId_IsDeleted");

            entity.Property(e => e.AvatarUrl).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.MaturityLevel).HasDefaultValue((byte)18);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.PinHash).HasMaxLength(500);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.UserAccount).WithMany(p => p.Profiles)
                .HasForeignKey(d => d.UserAccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Profiles_UserAccounts");
        });

        modelBuilder.Entity<ProfilePreference>(entity =>
        {
            entity.HasIndex(e => e.MovieId, "IX_ProfilePreferences_MovieId");

            entity.HasIndex(e => new { e.ProfileId, e.MovieId }, "UQ_ProfilePreferences_ProfileId_MovieId").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Movie).WithMany(p => p.ProfilePreferences)
                .HasForeignKey(d => d.MovieId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProfilePreferences_Movies");

            entity.HasOne(d => d.Profile).WithMany(p => p.ProfilePreferences)
                .HasForeignKey(d => d.ProfileId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProfilePreferences_Profiles");
        });

        modelBuilder.Entity<Rating>(entity =>
        {
            entity.HasIndex(e => e.MovieId, "IX_Ratings_MovieId");

            entity.HasIndex(e => new { e.ProfileId, e.MovieId }, "UQ_Ratings_ProfileId_MovieId").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Value).HasMaxLength(20);

            entity.HasOne(d => d.Movie).WithMany(p => p.Ratings)
                .HasForeignKey(d => d.MovieId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Ratings_Movies");

            entity.HasOne(d => d.Profile).WithMany(p => p.Ratings)
                .HasForeignKey(d => d.ProfileId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Ratings_Profiles");
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasIndex(e => e.DeviceId, "IX_RefreshTokens_DeviceId");

            entity.HasIndex(e => new { e.DeviceId, e.RevokedAt, e.ExpiresAt }, "IX_RefreshTokens_Device_Active");

            entity.HasIndex(e => e.ExpiresAt, "IX_RefreshTokens_ExpiresAt");

            entity.HasIndex(e => e.ReplacedByTokenId, "IX_RefreshTokens_ReplacedByTokenId");

            entity.HasIndex(e => e.UserAccountId, "IX_RefreshTokens_UserAccountId");

            entity.HasIndex(e => e.TokenHash, "UQ__RefreshT__BCB33F920206C60D").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.TokenHash).HasMaxLength(500);

            entity.HasOne(d => d.Device).WithMany(p => p.RefreshTokens)
                .HasForeignKey(d => d.DeviceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RefreshTokens_Devices");

            entity.HasOne(d => d.ReplacedByToken).WithMany(p => p.InverseReplacedByToken)
                .HasForeignKey(d => d.ReplacedByTokenId)
                .HasConstraintName("FK_RefreshTokens_RefreshTokens");

            entity.HasOne(d => d.UserAccount).WithMany(p => p.RefreshTokens)
                .HasForeignKey(d => d.UserAccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RefreshTokens_UserAccounts");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasIndex(e => e.Name, "UQ__Roles__737584F6C5E2B826").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.Name).HasMaxLength(50);
        });

        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.HasIndex(e => e.EndDate, "IX_Subscriptions_EndDate");

            entity.HasIndex(e => e.PlanId, "IX_Subscriptions_PlanId");

            entity.HasIndex(e => e.UserAccountId, "IX_Subscriptions_UserAccountId");

            entity.HasIndex(e => new { e.UserAccountId, e.Status }, "IX_Subscriptions_UserAccountId_Status");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Status).HasMaxLength(20);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Plan).WithMany(p => p.Subscriptions)
                .HasForeignKey(d => d.PlanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Subscriptions_Plans");

            entity.HasOne(d => d.UserAccount).WithMany(p => p.Subscriptions)
                .HasForeignKey(d => d.UserAccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Subscriptions_UserAccounts");
        });

        modelBuilder.Entity<UserAccount>(entity =>
        {
            entity.HasIndex(e => e.Email, "UQ__UserAcco__A9D10534D97E4B05").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.EmailConfirmationTokenHash).HasMaxLength(500);
            entity.Property(e => e.PasswordHash).HasMaxLength(500);
            entity.Property(e => e.PasswordResetTokenHash).HasMaxLength(500);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysdatetime())");
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasIndex(e => new { e.UserAccountId, e.RoleId }, "UQ_UserRoles").IsUnique();

            entity.Property(e => e.AssignedAt).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Role).WithMany(p => p.UserRoles)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserRoles_Roles");

            entity.HasOne(d => d.UserAccount).WithMany(p => p.UserRoles)
                .HasForeignKey(d => d.UserAccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserRoles_UserAccounts");
        });

        modelBuilder.Entity<ViewingSession>(entity =>
        {
            entity.HasIndex(e => e.DeviceId, "IX_ViewingSessions_DeviceId");

            entity.HasIndex(e => new { e.DeviceId, e.EndedAt }, "IX_ViewingSessions_Device_Active");

            entity.HasIndex(e => e.MovieId, "IX_ViewingSessions_MovieId");

            entity.HasIndex(e => new { e.MovieId, e.IsQualifiedView }, "IX_ViewingSessions_Movie_QualifiedView");

            entity.HasIndex(e => e.ProfileId, "IX_ViewingSessions_ProfileId");

            entity.HasIndex(e => e.StartedAt, "IX_ViewingSessions_StartedAt");

            entity.Property(e => e.EndReason).HasMaxLength(30);
            entity.Property(e => e.Quality).HasMaxLength(20);
            entity.Property(e => e.StartedAt).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Device).WithMany(p => p.ViewingSessions)
                .HasForeignKey(d => d.DeviceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ViewingSessions_Devices");

            entity.HasOne(d => d.Movie).WithMany(p => p.ViewingSessions)
                .HasForeignKey(d => d.MovieId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ViewingSessions_Movies");

            entity.HasOne(d => d.Profile).WithMany(p => p.ViewingSessions)
                .HasForeignKey(d => d.ProfileId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ViewingSessions_Profiles");
        });

        modelBuilder.Entity<WatchHistory>(entity =>
        {
            entity.HasIndex(e => new { e.ProfileId, e.IsCompleted, e.IsHidden, e.LastWatchedAt }, "IX_WatchHistories_ContinueWatching");

            entity.HasIndex(e => e.MovieId, "IX_WatchHistories_MovieId");

            entity.HasIndex(e => new { e.ProfileId, e.MovieId }, "UQ_WatchHistories_ProfileId_MovieId").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.LastWatchedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Movie).WithMany(p => p.WatchHistories)
                .HasForeignKey(d => d.MovieId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_WatchHistories_Movies");

            entity.HasOne(d => d.Profile).WithMany(p => p.WatchHistories)
                .HasForeignKey(d => d.ProfileId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_WatchHistories_Profiles");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
