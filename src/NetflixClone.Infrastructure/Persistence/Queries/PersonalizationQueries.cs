using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using NetflixClone.Application.Catalog.Movies;
using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Application.Personalization;
using NetflixClone.Domain.Entities;
namespace NetflixClone.Infrastructure.Persistence.Queries;
public sealed class PersonalizationQueries(NetflixCloneDbContext db) : IPersonalizationQueries
{
    private IQueryable<Movie> Eligible(byte maturityLevel)
        => db.Movies.AsNoTracking().Where(m => !m.IsDeleted && m.IsAvailable && m.MinAge <= maturityLevel);
    private static readonly Expression<Func<Movie, MovieSummary>> Summary = m =>
        new(m.Id, m.Title, m.ReleaseDate, m.DurationSeconds, m.ThumbnailUrl, m.MaturityRating, m.IsFeatured, m.IsAvailable);
    public async Task<bool> AreMoviesEligibleAsync(IReadOnlyList<int> ids, byte maturityLevel, CancellationToken ct = default)
        => await Eligible(maturityLevel).CountAsync(m => ids.Contains(m.Id), ct) == ids.Count;
    public async Task<BrowseMoviesResult> GetOnboardingMoviesAsync(byte maturityLevel, int page, int pageSize, string? search, CancellationToken ct = default)
    {
        var query = Eligible(maturityLevel);
        if (!string.IsNullOrEmpty(search)) query = query.Where(m => m.Title.Contains(search));
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(m => m.IsFeatured).ThenByDescending(m => m.ReleaseDate).ThenBy(m => m.Id)
            .Skip(checked((page - 1) * pageSize)).Take(pageSize).Select(Summary).ToListAsync(ct);
        return new(items, page, pageSize, total);
    }
    public async Task<RecommendationsResult> GetRecommendationsAsync(int profileId, byte maturityLevel, int limit, CancellationToken ct = default)
    {
        // Explicit rating overrides onboarding for the same movie, including NotForMe.
        var ratedGenres = db.Ratings.AsNoTracking()
            .Where(r => r.ProfileId == profileId && (r.Value == "Like" || r.Value == "Love") &&
                !r.Movie.IsDeleted && r.Movie.MinAge <= maturityLevel)
            .SelectMany(r => r.Movie.Genres.Select(g => new {
                GenreId = g.Id, Weight = r.Value == "Love" ? PersonalizationRules.LoveWeight : PersonalizationRules.LikeWeight
            }));
        var preferredGenres = db.ProfilePreferences.AsNoTracking()
            .Where(p => p.ProfileId == profileId && !p.Movie.IsDeleted && p.Movie.MinAge <= maturityLevel &&
                !p.Movie.Ratings.Any(r => r.ProfileId == profileId))
            .SelectMany(p => p.Movie.Genres.Select(g => new { GenreId = g.Id, Weight = PersonalizationRules.PreferenceWeight }));
        var genreWeights = ratedGenres.Concat(preferredGenres)
            .GroupBy(x => x.GenreId).Select(g => new { GenreId = g.Key, Weight = g.Sum(x => x.Weight) });
        var candidates = Eligible(maturityLevel).Where(m =>
            !m.ProfilePreferences.Any(p => p.ProfileId == profileId) && !m.Ratings.Any(r => r.ProfileId == profileId));
        var scores = candidates.SelectMany(m => m.Genres.Select(g => new { MovieId = m.Id, GenreId = g.Id }))
            .Join(genreWeights, m => m.GenreId, w => w.GenreId, (m, w) => new { m.MovieId, w.Weight })
            .GroupBy(x => x.MovieId).Select(g => new { MovieId = g.Key, Score = g.Sum(x => x.Weight) });
        var ranked = candidates.Join(scores, m => m.Id, s => s.MovieId, (m, s) => new { Movie = m, s.Score })
            .OrderByDescending(x => x.Score).ThenByDescending(x => x.Movie.ReleaseDate).ThenBy(x => x.Movie.Id)
            .Select(x => x.Movie).Take(limit);
        var items = await ranked.Select(Summary).ToListAsync(ct);
        var personalizedCount = items.Count;
        if (items.Count < limit)
        {
            var selected = items.Select(m => m.MovieId).ToArray();
            items.AddRange(await candidates.Where(m => !selected.Contains(m.Id))
                .OrderByDescending(m => m.IsFeatured).ThenByDescending(m => m.ReleaseDate).ThenBy(m => m.Id)
                .Take(limit - items.Count).Select(Summary).ToListAsync(ct));
        }
        return new(items, personalizedCount == 0 ? "fallback" : personalizedCount == items.Count ? "personalized" : "mixed");
    }
}
