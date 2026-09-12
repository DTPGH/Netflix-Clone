using Microsoft.EntityFrameworkCore;
using NetflixClone.Application.Common.Exceptions;

namespace NetflixClone.Infrastructure.Persistence;

public partial class NetflixCloneDbContext
{
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return SaveChangesAsync(acceptAllChangesOnSuccess: true, cancellationToken);
    }

    public override async Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            // EF has already unwound the failed save; do not retry or accept tracked changes.
            throw new PersistenceConcurrencyException(
                "The data changed after it was read. The changes could not be saved.", exception);
        }
    }
}
