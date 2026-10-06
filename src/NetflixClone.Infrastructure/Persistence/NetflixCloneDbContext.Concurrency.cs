using Microsoft.EntityFrameworkCore;
using NetflixClone.Domain.Entities;

namespace NetflixClone.Infrastructure.Persistence;

public partial class NetflixCloneDbContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserAccount>(entity =>
        {
            entity.Property(a => a.PasswordHash).IsConcurrencyToken();
            entity.Property(a => a.PasswordResetTokenHash).IsConcurrencyToken();
            entity.Property(a => a.PasswordResetTokenExpiresAt).IsConcurrencyToken();
        });
        modelBuilder.Entity<ViewingSession>(entity =>
        {
            entity.Property<Guid?>("ClientSessionId");
            entity.Property<long>("CheckpointSequence").IsConcurrencyToken();
            entity.Property<long>("WatchedMilliseconds");
            entity.Property<DateTime>("LastCheckpointAtUtc");
            entity.Property(s => s.EndedAt).IsConcurrencyToken();
            entity.HasIndex("DeviceId", "ClientSessionId").IsUnique()
                .HasDatabaseName("UX_ViewingSessions_Device_ClientSession")
                .HasFilter("[ClientSessionId] IS NOT NULL");
        });
        modelBuilder.Entity<Plan>(entity =>
        {
            entity.Property(p => p.UpdatedAt).IsConcurrencyToken();
            // The DB default is true; explicitly persist false for newly-created draft plans.
            entity.Property(p => p.IsActive).HasSentinel(true);
        });
        modelBuilder.Entity<MovieCollection>().Property(c => c.UpdatedAt).IsConcurrencyToken();
        modelBuilder.Entity<Movie>(entity =>
        {
            entity.Property(movie => movie.IsDeleted).IsConcurrencyToken();
            entity.Property(movie => movie.UpdatedAt).IsConcurrencyToken();
        });
        modelBuilder.Entity<WatchHistory>().Property(h => h.UpdatedAt).IsConcurrencyToken();
        modelBuilder.Entity<Rating>().Property(rating => rating.UpdatedAt).IsConcurrencyToken();
        modelBuilder.Entity<Profile>(entity =>
        {
            entity.Property(profile => profile.PinHash).IsConcurrencyToken();
            entity.Property(profile => profile.PinFailedAttempts).IsConcurrencyToken();
            entity.Property(profile => profile.PinLockoutEnd).IsConcurrencyToken();
            entity.Property(profile => profile.IsDeleted).IsConcurrencyToken();
            entity.Property(profile => profile.UpdatedAt).IsConcurrencyToken();
        });
        // Every issuance updates LastActiveAt, so revocation detects newly issued sessions too.
        modelBuilder.Entity<Device>(entity =>
        {
            entity.Property(device => device.RevokedAt).IsConcurrencyToken();
            entity.Property(device => device.LastActiveAt).IsConcurrencyToken();
        });
        // A refresh token may be consumed only if its persisted state still matches
        // the state read by this context. Keep this configuration outside scaffolded files.
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.Property(token => token.RevokedAt).IsConcurrencyToken();
            entity.Property(token => token.ReplacedByTokenId).IsConcurrencyToken();
        });
    }
}
