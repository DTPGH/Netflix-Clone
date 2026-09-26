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
        catch (DbUpdateException exception) when (
            exception.InnerException is Microsoft.Data.SqlClient.SqlException sql &&
            sql.Errors.Cast<Microsoft.Data.SqlClient.SqlError>().Any(error =>
                error.Number is 2601 or 2627 &&
                error.Message.Contains("UQ_MyListItems_ProfileId_MovieId", StringComparison.Ordinal)) &&
            exception.Entries.Count == 1 &&
            exception.Entries[0].Entity is NetflixClone.Domain.Entities.MyListItem &&
            exception.Entries[0].State == EntityState.Added &&
            ChangeTracker.Entries().Count(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted) == 1)
        {
            throw new MyListAlreadyExistsException("The movie is already in this profile's My List.", exception);
        }
    }
}
