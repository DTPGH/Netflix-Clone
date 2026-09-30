using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Application.Common.Abstractions.Time;
using NetflixClone.Application.Common.Exceptions;
using NetflixClone.Application.Common.Results;
using NetflixClone.Domain.Constants;
using NetflixClone.Domain.Entities;

namespace NetflixClone.Application.Admin.Movies;

public interface IAdminMovieManagementUseCase
{
    Task<Result<AdminMoviePage>> ListAsync(ListAdminMoviesQuery query, CancellationToken cancellationToken = default);
    Task<Result<AdminMovieDetail>> GetAsync(GetAdminMovieQuery query, CancellationToken cancellationToken = default);
    Task<Result<AdminMovieDetail>> CreateAsync(CreateAdminMovieCommand command, CancellationToken cancellationToken = default);
    Task<Result<AdminMovieDetail>> UpdateAsync(UpdateAdminMovieCommand command, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteAsync(DeleteAdminMovieCommand command, CancellationToken cancellationToken = default);
    Task<Result<AdminMovieDetail>> RestoreAsync(RestoreAdminMovieCommand command, CancellationToken cancellationToken = default);
}

public sealed class AdminMovieManagementUseCase(IAdminMovieRepository movies, IUserAccountRepository accounts,
    IUnitOfWork unitOfWork, IClock clock) : IAdminMovieManagementUseCase
{
    public async Task<Result<AdminMoviePage>> ListAsync(ListAdminMoviesQuery query, CancellationToken cancellationToken = default)
    {
        var access = await AuthorizeAsync(query.ActorUserAccountId, cancellationToken);
        if (access is not null) return Result<AdminMoviePage>.Failure(access);
        var search = AdminMovieRules.Optional(query.Search);
        if (query.Page < 1 || query.PageSize is < 1 or > 50 || search?.Length > 100 || query.GenreId is <= 0 ||
            (long)(query.Page - 1) * query.PageSize > int.MaxValue || query.Sort is not ("updatedAtDesc" or "titleAsc") ||
            query.Deletion is not ("active" or "deleted" or "all"))
            return Result<AdminMoviePage>.Failure(AdminMovieErrors.InvalidQuery);
        return Result<AdminMoviePage>.Success(await movies.ListAsync(new(query.Page, query.PageSize, search,
            query.GenreId, query.IsAvailable, query.Deletion == "deleted" ? AdminMovieDeletionFilter.Deleted :
                query.Deletion == "all" ? AdminMovieDeletionFilter.All : AdminMovieDeletionFilter.Active,
            query.Sort == "titleAsc" ? AdminMovieSort.TitleAscending :
                AdminMovieSort.UpdatedDescending), cancellationToken));
    }

    public async Task<Result<AdminMovieDetail>> GetAsync(GetAdminMovieQuery query, CancellationToken cancellationToken = default)
    {
        var access = await AuthorizeAsync(query.ActorUserAccountId, cancellationToken);
        if (access is not null) return Result<AdminMovieDetail>.Failure(access);
        if (query.MovieId <= 0) return Result<AdminMovieDetail>.Failure(AdminMovieErrors.NotFound);
        var movie = await movies.GetAsync(query.MovieId, cancellationToken);
        return movie is null ? Result<AdminMovieDetail>.Failure(AdminMovieErrors.NotFound) : Result<AdminMovieDetail>.Success(movie);
    }

    public async Task<Result<AdminMovieDetail>> CreateAsync(CreateAdminMovieCommand command, CancellationToken cancellationToken = default)
    {
        var access = await AuthorizeAsync(command.ActorUserAccountId, cancellationToken);
        if (access is not null) return Result<AdminMovieDetail>.Failure(access);
        var normalized = Normalize(command.Movie);
        if (normalized is null) return Result<AdminMovieDetail>.Failure(AdminMovieErrors.InvalidMovie);
        var genres = await GenresAsync(normalized.GenreIds!, cancellationToken);
        if (genres is null) return Result<AdminMovieDetail>.Failure(AdminMovieErrors.InvalidGenres);
        var now = clock.UtcNow;
        var movie = new Movie { CreatedAt = now, UpdatedAt = now };
        Apply(movie, normalized, genres);
        await movies.AddAsync(movie, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<AdminMovieDetail>.Success(Map(movie));
    }

    public async Task<Result<AdminMovieDetail>> UpdateAsync(UpdateAdminMovieCommand command, CancellationToken cancellationToken = default)
    {
        var access = await AuthorizeAsync(command.ActorUserAccountId, cancellationToken);
        if (access is not null) return Result<AdminMovieDetail>.Failure(access);
        if (command.ExpectedUpdatedAtUtc is not { Kind: DateTimeKind.Utc })
            return Result<AdminMovieDetail>.Failure(AdminMovieErrors.InvalidMovie);
        var normalized = Normalize(command.Movie);
        if (normalized is null) return Result<AdminMovieDetail>.Failure(AdminMovieErrors.InvalidMovie);
        var movie = await movies.GetForUpdateAsync(command.MovieId, cancellationToken);
        if (movie is null || movie.IsDeleted) return Result<AdminMovieDetail>.Failure(AdminMovieErrors.NotFound);
        if (movie.UpdatedAt.Ticks != command.ExpectedUpdatedAtUtc.Value.Ticks)
            return Result<AdminMovieDetail>.Failure(AdminMovieErrors.ConcurrentChange);
        var genres = await GenresAsync(normalized.GenreIds!, cancellationToken);
        if (genres is null) return Result<AdminMovieDetail>.Failure(AdminMovieErrors.InvalidGenres);
        Apply(movie, normalized, genres);
        movie.UpdatedAt = AdminMovieRules.NextUpdatedAt(movie.UpdatedAt, clock.UtcNow);
        try { await unitOfWork.SaveChangesAsync(cancellationToken); }
        catch (PersistenceConcurrencyException) { return Result<AdminMovieDetail>.Failure(AdminMovieErrors.ConcurrentChange); }
        return Result<AdminMovieDetail>.Success(Map(movie));
    }

    public async Task<Result<bool>> DeleteAsync(DeleteAdminMovieCommand command, CancellationToken cancellationToken = default)
    {
        var access = await AuthorizeAsync(command.ActorUserAccountId, cancellationToken);
        if (access is not null) return Result<bool>.Failure(access);
        if (command.ExpectedUpdatedAtUtc is not { Kind: DateTimeKind.Utc })
            return Result<bool>.Failure(AdminMovieErrors.InvalidMovie);
        var movie = await movies.GetForUpdateAsync(command.MovieId, cancellationToken);
        if (movie is null || movie.IsDeleted) return Result<bool>.Failure(AdminMovieErrors.NotFound);
        if (movie.UpdatedAt.Ticks != command.ExpectedUpdatedAtUtc.Value.Ticks)
            return Result<bool>.Failure(AdminMovieErrors.ConcurrentChange);
        movie.IsDeleted = true;
        movie.UpdatedAt = AdminMovieRules.NextUpdatedAt(movie.UpdatedAt, clock.UtcNow);
        try { await unitOfWork.SaveChangesAsync(cancellationToken); }
        catch (PersistenceConcurrencyException) { return Result<bool>.Failure(AdminMovieErrors.ConcurrentChange); }
        return Result<bool>.Success(true);
    }

    public async Task<Result<AdminMovieDetail>> RestoreAsync(RestoreAdminMovieCommand command,
        CancellationToken cancellationToken = default)
    {
        var access = await AuthorizeAsync(command.ActorUserAccountId, cancellationToken);
        if (access is not null) return Result<AdminMovieDetail>.Failure(access);
        if (command.ExpectedUpdatedAtUtc is not { Kind: DateTimeKind.Utc })
            return Result<AdminMovieDetail>.Failure(AdminMovieErrors.InvalidMovie);
        var movie = await movies.GetForUpdateAsync(command.MovieId, cancellationToken);
        if (movie is null) return Result<AdminMovieDetail>.Failure(AdminMovieErrors.NotFound);
        if (!movie.IsDeleted) return Result<AdminMovieDetail>.Failure(AdminMovieErrors.NotDeleted);
        if (movie.UpdatedAt.Ticks != command.ExpectedUpdatedAtUtc.Value.Ticks)
            return Result<AdminMovieDetail>.Failure(AdminMovieErrors.ConcurrentChange);
        movie.IsDeleted = false;
        movie.UpdatedAt = AdminMovieRules.NextUpdatedAt(movie.UpdatedAt, clock.UtcNow);
        try { await unitOfWork.SaveChangesAsync(cancellationToken); }
        catch (PersistenceConcurrencyException) { return Result<AdminMovieDetail>.Failure(AdminMovieErrors.ConcurrentChange); }
        return Result<AdminMovieDetail>.Success(Map(movie));
    }

    private async Task<Error?> AuthorizeAsync(int actorId, CancellationToken cancellationToken)
    {
        var account = actorId > 0 ? await accounts.GetByIdAsync(actorId, cancellationToken) : null;
        if (account is null) return AdminMovieErrors.AccountUnavailable;
        if (account.IsLocked || !account.EmailConfirmed) return AdminMovieErrors.Forbidden;
        var roles = await accounts.GetRoleNamesAsync(actorId, cancellationToken);
        return roles.Contains(RoleNames.Admin, StringComparer.Ordinal) ? null : AdminMovieErrors.Forbidden;
    }

    private async Task<IReadOnlyList<Genre>?> GenresAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken)
    {
        var distinct = ids.Distinct().ToArray();
        if (distinct.Length == 0 || distinct.Any(id => id <= 0)) return null;
        var genres = await movies.GetGenresAsync(distinct, cancellationToken);
        return genres.Count == distinct.Length ? genres : null;
    }

    private static SaveAdminMovieData? Normalize(SaveAdminMovieData source)
    {
        var title = AdminMovieRules.Optional(source.Title);
        var description = AdminMovieRules.Optional(source.Description);
        var thumbnail = AdminMovieRules.Optional(source.ThumbnailUrl);
        var backdrop = AdminMovieRules.Optional(source.BackdropUrl);
        var trailer = AdminMovieRules.Optional(source.TrailerUrl);
        var video = AdminMovieRules.Optional(source.VideoUrl);
        var rating = AdminMovieRules.Optional(source.MaturityRating)?.ToUpperInvariant();
        if (title is null || title.Length > 255 || description?.Length > 2000 || source.DurationSeconds <= 0 ||
            thumbnail?.Length > 500 || backdrop?.Length > 500 || trailer?.Length > 500 || video?.Length > 500 ||
            AdminMovieRules.MinAge(rating) is null || !AdminMovieRules.Image(thumbnail) ||
            !AdminMovieRules.Image(backdrop) || !AdminMovieRules.Trailer(trailer) || !AdminMovieRules.Video(video) ||
            source.IsAvailable && video is null || source.GenreIds is null)
            return null;
        return source with { Title = title, Description = description, ThumbnailUrl = thumbnail, BackdropUrl = backdrop,
            TrailerUrl = trailer, VideoUrl = video, MaturityRating = rating };
    }

    private static void Apply(Movie target, SaveAdminMovieData source, IReadOnlyList<Genre> genres)
    {
        target.Title = source.Title!; target.Description = source.Description; target.ReleaseDate = source.ReleaseDate;
        target.DurationSeconds = source.DurationSeconds; target.ThumbnailUrl = source.ThumbnailUrl;
        target.BackdropUrl = source.BackdropUrl; target.TrailerUrl = source.TrailerUrl; target.VideoUrl = source.VideoUrl;
        target.MaturityRating = source.MaturityRating!; target.MinAge = AdminMovieRules.MinAge(source.MaturityRating)!.Value;
        target.IsFeatured = source.IsFeatured; target.IsAvailable = source.IsAvailable;
        target.Genres.Clear(); foreach (var genre in genres) target.Genres.Add(genre);
    }

    private static AdminMovieDetail Map(Movie movie) => new(movie.Id, movie.Title, movie.Description, movie.ReleaseDate,
        movie.DurationSeconds, movie.ThumbnailUrl, movie.BackdropUrl, movie.TrailerUrl, movie.VideoUrl,
        movie.MaturityRating, movie.MinAge, movie.IsFeatured, movie.IsAvailable, movie.IsDeleted,
        DateTime.SpecifyKind(movie.CreatedAt, DateTimeKind.Utc), DateTime.SpecifyKind(movie.UpdatedAt, DateTimeKind.Utc),
        movie.Genres.OrderBy(g => g.Name).ThenBy(g => g.Id).Select(g => new AdminMovieGenre(g.Id, g.Name)).ToArray());
}
