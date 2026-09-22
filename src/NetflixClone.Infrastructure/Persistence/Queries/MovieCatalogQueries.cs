using Microsoft.EntityFrameworkCore;
using NetflixClone.Application.Catalog.Movies;
using NetflixClone.Application.Catalog.Genres;
using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Domain.Entities;
namespace NetflixClone.Infrastructure.Persistence.Queries;
public sealed class MovieCatalogQueries(NetflixCloneDbContext dbContext) : IMovieCatalogQueries
{
    // Availability is playback metadata, not catalog visibility.
    private IQueryable<Movie> PublicMovies => dbContext.Movies.AsNoTracking().Where(m => !m.IsDeleted);
    public Task<MoviePlayback?> GetPlaybackAsync(int movieId, CancellationToken cancellationToken = default)
        => PublicMovies.Where(m => m.Id == movieId)
            .Select(m => new MoviePlayback(m.Id, m.Title, m.IsAvailable, m.VideoUrl))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<BrowseMoviesResult> BrowseAsync(MovieCatalogCriteria criteria, CancellationToken cancellationToken = default)
    {
        var query = PublicMovies;
        if (criteria.Search is { } search) query = query.Where(m => m.Title.Contains(search));
        if (criteria.GenreId is { } genreId) query = query.Where(m => m.Genres.Any(g => g.Id == genreId));
        var total = await query.CountAsync(cancellationToken);
        var ordered = criteria.Sort == MovieCatalogSort.TitleAscending
            ? query.OrderBy(m => m.Title).ThenBy(m => m.Id)
            : query.OrderByDescending(m => m.ReleaseDate.HasValue).ThenByDescending(m => m.ReleaseDate).ThenBy(m => m.Id);
        var items = await ordered.Skip(checked((criteria.Page - 1) * criteria.PageSize)).Take(criteria.PageSize)
            .Select(m => new MovieSummary(m.Id, m.Title, m.ReleaseDate, m.DurationSeconds,
                m.ThumbnailUrl, m.MaturityRating, m.IsFeatured, m.IsAvailable))
            .ToListAsync(cancellationToken);
        return new(items, criteria.Page, criteria.PageSize, total);
    }

    public Task<MovieDetail?> GetDetailAsync(int movieId, CancellationToken cancellationToken = default)
        => PublicMovies.Where(m => m.Id == movieId)
            .Select(m => new MovieDetail(m.Id, m.Title, m.ReleaseDate, m.DurationSeconds,
                m.ThumbnailUrl, m.MaturityRating, m.IsFeatured, m.IsAvailable,
                m.Description, m.BackdropUrl, m.TrailerUrl, m.MinAge,
                m.Genres.OrderBy(g => g.Name).ThenBy(g => g.Id)
                    .Select(g => new GenreSummary(g.Id, g.Name)).ToList(),
                m.MovieCredits.OrderBy(c => c.CreditType).ThenBy(c => c.Person.FullName).ThenBy(c => c.Id)
                    .Select(c => new MovieCreditSummary(c.PersonId, c.Person.FullName, c.Person.PhotoUrl,
                        c.CreditType, c.CharacterName)).ToList()))
            .AsSplitQuery()
            .FirstOrDefaultAsync(cancellationToken);
}
