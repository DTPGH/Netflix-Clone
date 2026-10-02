using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Application.Common.Abstractions.Time;
using NetflixClone.Application.Common.Exceptions;
using NetflixClone.Application.Common.Results;
using NetflixClone.Application.Admin.Movies;
using NetflixClone.Application.Profiles;
using NetflixClone.Domain.Constants;
using NetflixClone.Domain.Entities;
namespace NetflixClone.Application.Collections;
public interface IMovieCollectionUseCase
{
    Task<Result<CollectionPage>> ListAsync(int actorId, int page, int pageSize, CancellationToken ct);
    Task<Result<CollectionDetail>> GetAsync(int actorId, int id, CancellationToken ct);
    Task<Result<CollectionDetail>> SaveAsync(SaveCollectionCommand command, CancellationToken ct);
    Task<Result<IReadOnlyList<BrowseCollection>>> BrowseAsync(int accountId, int profileId, CancellationToken ct);
}
public sealed class MovieCollectionUseCase(IMovieCollectionRepository collections, IMovieCollectionQueries queries,
    IUserAccountRepository accounts, IProfileRepository profiles, IUnitOfWork unitOfWork, IClock clock) : IMovieCollectionUseCase
{
    private static readonly Error Invalid = new("Collections.InvalidData", "Enter a title (maximum 200 characters), a non-negative display order and at most 50 distinct existing movies. Publishing requires an active movie.", ErrorType.Validation);
    private static readonly Error Missing = new("Collections.NotFound", "The collection was not found.", ErrorType.NotFound);
    private static readonly Error Conflict = new("Collections.ConcurrentChange", "The collection changed. Reload before saving.", ErrorType.Conflict);
    private async Task<Error?> Access(int id, CancellationToken ct)
    {
        var account = id > 0 ? await accounts.GetByIdAsync(id, ct) : null;
        if (account is null) return AdminMovieErrors.AccountUnavailable;
        if (account.IsLocked || !account.EmailConfirmed) return AdminMovieErrors.Forbidden;
        return (await accounts.GetRoleNamesAsync(id, ct)).Contains(RoleNames.Admin, StringComparer.Ordinal) ? null : AdminMovieErrors.Forbidden;
    }
    public async Task<Result<CollectionPage>> ListAsync(int actorId, int page, int pageSize, CancellationToken ct)
    {
        var access = await Access(actorId, ct);
        if (access is not null) return Result<CollectionPage>.Failure(access);
        if (page < 1 || pageSize is < 1 or > 50 || (long)(page - 1) * pageSize > int.MaxValue)
            return Result<CollectionPage>.Failure(Invalid);
        return Result<CollectionPage>.Success(await collections.ListAsync(page, pageSize, ct));
    }
    public async Task<Result<CollectionDetail>> GetAsync(int actorId, int id, CancellationToken ct)
    {
        var access = await Access(actorId, ct);
        if (access is not null) return Result<CollectionDetail>.Failure(access);
        var entity = await collections.GetAsync(id, ct);
        return entity is null ? Result<CollectionDetail>.Failure(Missing) : Result<CollectionDetail>.Success(Map(entity));
    }
    public async Task<Result<CollectionDetail>> SaveAsync(SaveCollectionCommand command, CancellationToken ct)
    {
        var access = await Access(command.ActorUserAccountId, ct);
        if (access is not null) return Result<CollectionDetail>.Failure(access);
        var title = command.Title?.Trim();
        var ids = command.MovieIds;
        if (string.IsNullOrEmpty(title) || title.Length > 200 || command.DisplayOrder < 0 || ids is null ||
            ids.Count > 50 || ids.Any(id => id <= 0) || ids.Distinct().Count() != ids.Count)
            return Result<CollectionDetail>.Failure(Invalid);
        MovieCollection entity;
        var now = clock.UtcNow;
        if (command.CollectionId is { } id)
        {
            if (command.ExpectedUpdatedAtUtc is not { Kind: DateTimeKind.Utc }) return Result<CollectionDetail>.Failure(Invalid);
            var existing = await collections.GetAsync(id, ct);
            if (existing is null) return Result<CollectionDetail>.Failure(Missing);
            if (existing.UpdatedAt.Ticks != command.ExpectedUpdatedAtUtc.Value.Ticks) return Result<CollectionDetail>.Failure(Conflict);
            entity = existing;
        }
        else entity = new() { CreatedAt = now, UpdatedAt = now };
        var movies = await collections.GetMoviesAsync(ids, ct);
        if (movies.Count != ids.Count || movies.Any(m => m.IsDeleted && !entity.MovieCollectionItems.Any(i => i.MovieId == m.Id)) ||
            command.IsPublished && !movies.Any(m => !m.IsDeleted))
            return Result<CollectionDetail>.Failure(Invalid);
        foreach (var item in entity.MovieCollectionItems.ToArray())
            if (!ids.Contains(item.MovieId)) { collections.RemoveItem(item); entity.MovieCollectionItems.Remove(item); }
        for (var position = 0; position < ids.Count; position++)
        {
            var movieId = ids[position];
            var item = entity.MovieCollectionItems.SingleOrDefault(i => i.MovieId == movieId);
            if (item is null)
            {
                item = new() { MovieId = movieId, Movie = movies.Single(m => m.Id == movieId) };
                entity.MovieCollectionItems.Add(item);
            }
            item.Position = position;
        }
        entity.Title = title; entity.IsPublished = command.IsPublished; entity.DisplayOrder = command.DisplayOrder;
        if (command.CollectionId.HasValue) entity.UpdatedAt = AdminMovieRules.NextUpdatedAt(entity.UpdatedAt, now);
        else await collections.AddAsync(entity, ct);
        try { await unitOfWork.SaveChangesAsync(ct); }
        catch (PersistenceConcurrencyException) { return Result<CollectionDetail>.Failure(Conflict); }
        return Result<CollectionDetail>.Success(Map(entity));
    }
    public async Task<Result<IReadOnlyList<BrowseCollection>>> BrowseAsync(int accountId, int profileId, CancellationToken ct)
    {
        var profile = await profiles.GetByIdForAccountAsync(accountId, profileId, ct);
        if (profile is null || profile.IsDeleted) return Result<IReadOnlyList<BrowseCollection>>.Failure(ProfileErrors.NotFound);
        return Result<IReadOnlyList<BrowseCollection>>.Success(await queries.BrowseAsync(profile.MaturityLevel, ct));
    }
    private static CollectionDetail Map(MovieCollection c) => new(c.Id, c.Title, c.IsPublished, c.DisplayOrder,
        DateTime.SpecifyKind(c.UpdatedAt, DateTimeKind.Utc),
        c.MovieCollectionItems.OrderBy(i => i.Position).ThenBy(i => i.MovieId)
            .Select(i => new CollectionMovie(i.MovieId, i.Movie.Title, i.Movie.ThumbnailUrl, i.Movie.IsDeleted)).ToArray());
}
