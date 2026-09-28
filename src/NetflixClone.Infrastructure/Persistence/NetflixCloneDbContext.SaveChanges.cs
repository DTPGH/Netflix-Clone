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
            exception.InnerException is Microsoft.Data.SqlClient.SqlException preferenceSql &&
            preferenceSql.Errors.Cast<Microsoft.Data.SqlClient.SqlError>().Any(error =>
                error.Number is 2601 or 2627 &&
                error.Message.Contains("UQ_ProfilePreferences_ProfileId_MovieId", StringComparison.Ordinal)) &&
            IsOnboardingWrite(exception))
        {
            throw new PersistenceConcurrencyException("Onboarding was changed by another request. Reload and try again.", exception);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is Microsoft.Data.SqlClient.SqlException ratingSql &&
            ratingSql.Errors.Cast<Microsoft.Data.SqlClient.SqlError>().Any(error =>
                error.Number is 2601 or 2627 &&
                error.Message.Contains("UQ_Ratings_ProfileId_MovieId", StringComparison.Ordinal)) &&
            exception.Entries.Count == 1 &&
            exception.Entries[0].Entity is NetflixClone.Domain.Entities.Rating &&
            exception.Entries[0].State == EntityState.Added &&
            ChangeTracker.Entries().Count(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted) == 1)
        {
            // Competing inserts may express different ratings. Never report silent success.
            throw new PersistenceConcurrencyException("The rating was created by another request. Reload and try again.", exception);
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
    private bool IsOnboardingWrite(DbUpdateException exception)
    {
        var writes = ChangeTracker.Entries().Where(e =>
            e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).ToArray();
        var profiles = writes.Where(e => e.Entity is NetflixClone.Domain.Entities.Profile).ToArray();
        if (profiles.Length != 1 || profiles[0].State != EntityState.Modified) return false;
        var profile = (NetflixClone.Domain.Entities.Profile)profiles[0].Entity;
        if (!profile.OnboardingCompleted || (bool)profiles[0].Property(nameof(profile.OnboardingCompleted)).OriginalValue!) return false;
        if (profiles[0].Properties.Any(p => p.IsModified &&
            p.Metadata.Name is not (nameof(profile.OnboardingCompleted) or nameof(profile.UpdatedAt)))) return false;
        // A SQL batch exception can include both the profile update and preference inserts.
        return exception.Entries.Any(e => e.Entity is NetflixClone.Domain.Entities.ProfilePreference) &&
            exception.Entries.All(e => writes.Any(w => ReferenceEquals(w.Entity, e.Entity)))
            && writes.All(e => e == profiles[0] ||
                e.State == EntityState.Added && e.Entity is NetflixClone.Domain.Entities.ProfilePreference p && p.ProfileId == profile.Id);
    }
}
