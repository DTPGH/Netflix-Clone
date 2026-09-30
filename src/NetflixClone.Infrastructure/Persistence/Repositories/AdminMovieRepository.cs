using Microsoft.EntityFrameworkCore;
using NetflixClone.Application.Admin.Movies;
using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Domain.Entities;

namespace NetflixClone.Infrastructure.Persistence.Repositories;

public sealed class AdminMovieRepository(NetflixCloneDbContext db) : IAdminMovieRepository
{
    public async Task<AdminMoviePage> ListAsync(AdminMovieCriteria criteria, CancellationToken cancellationToken = default)
    {
        var query = db.Movies.AsNoTracking();
        if (criteria.Deletion == AdminMovieDeletionFilter.Active) query = query.Where(movie => !movie.IsDeleted);
        else if (criteria.Deletion == AdminMovieDeletionFilter.Deleted) query = query.Where(movie => movie.IsDeleted);
        if (criteria.Search is { } search) query = query.Where(movie => movie.Title.Contains(search));
        if (criteria.GenreId is { } genreId) query = query.Where(movie => movie.Genres.Any(genre => genre.Id == genreId));
        if (criteria.IsAvailable is { } available) query = query.Where(movie => movie.IsAvailable == available);
        var total = await query.CountAsync(cancellationToken);
        var ordered = criteria.Sort == AdminMovieSort.TitleAscending
            ? query.OrderBy(movie => movie.Title).ThenBy(movie => movie.Id)
            : query.OrderByDescending(movie => movie.UpdatedAt).ThenByDescending(movie => movie.Id);
        var items = await ordered.Skip(checked((criteria.Page - 1) * criteria.PageSize)).Take(criteria.PageSize)
            .Select(movie => new AdminMovieSummary(movie.Id, movie.Title, movie.ReleaseDate, movie.ThumbnailUrl,
                movie.MaturityRating, movie.IsFeatured, movie.IsAvailable, movie.IsDeleted, movie.UpdatedAt))
            .ToListAsync(cancellationToken);
        return new(items.Select(item => item with {
            UpdatedAtUtc = DateTime.SpecifyKind(item.UpdatedAtUtc, DateTimeKind.Utc)
        }).ToArray(), criteria.Page, criteria.PageSize, total);
    }

    public async Task<AdminMovieDetail?> GetAsync(int movieId, CancellationToken cancellationToken = default)
    {
        var movie = await db.Movies.AsNoTracking().Where(entity => entity.Id == movieId)
            .Select(entity => new AdminMovieDetail(entity.Id, entity.Title, entity.Description, entity.ReleaseDate,
                entity.DurationSeconds, entity.ThumbnailUrl, entity.BackdropUrl, entity.TrailerUrl, entity.VideoUrl,
                entity.MaturityRating, entity.MinAge, entity.IsFeatured, entity.IsAvailable, entity.IsDeleted, entity.CreatedAt,
                entity.UpdatedAt, entity.Genres.OrderBy(genre => genre.Name).ThenBy(genre => genre.Id)
                    .Select(genre => new AdminMovieGenre(genre.Id, genre.Name)).ToArray()))
            .AsSplitQuery().FirstOrDefaultAsync(cancellationToken);
        return movie is null ? null : movie with {
            CreatedAtUtc = DateTime.SpecifyKind(movie.CreatedAtUtc, DateTimeKind.Utc),
            UpdatedAtUtc = DateTime.SpecifyKind(movie.UpdatedAtUtc, DateTimeKind.Utc)
        };
    }

    public Task<Movie?> GetForUpdateAsync(int movieId, CancellationToken cancellationToken = default)
        => db.Movies.Include(movie => movie.Genres)
            .SingleOrDefaultAsync(movie => movie.Id == movieId, cancellationToken);

    public async Task<IReadOnlyList<Genre>> GetGenresAsync(IReadOnlyCollection<int> genreIds,
        CancellationToken cancellationToken = default)
        => await db.Genres.Where(genre => genreIds.Contains(genre.Id)).ToListAsync(cancellationToken);

    public async Task AddAsync(Movie movie, CancellationToken cancellationToken = default)
        => await db.Movies.AddAsync(movie, cancellationToken);
}
