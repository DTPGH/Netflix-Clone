using Microsoft.EntityFrameworkCore;
using NetflixClone.Domain.Entities;

namespace NetflixClone.Infrastructure.Persistence;

public partial class NetflixCloneDbContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        // A refresh token may be consumed only if its persisted state still matches
        // the state read by this context. Keep this configuration outside scaffolded files.
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.Property(token => token.RevokedAt).IsConcurrencyToken();
            entity.Property(token => token.ReplacedByTokenId).IsConcurrencyToken();
        });
    }
}
