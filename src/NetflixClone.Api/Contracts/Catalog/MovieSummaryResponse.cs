using NetflixClone.Application.Catalog.Movies;
namespace NetflixClone.Api.Contracts.Catalog;
public sealed record MovieSummaryResponse(int MovieId, string Title, DateOnly? ReleaseDate, int DurationSeconds,
    string? ThumbnailUrl, string MaturityRating, bool IsFeatured, bool IsAvailable)
{
    public static MovieSummaryResponse From(MovieSummary m) => new(m.MovieId, m.Title, m.ReleaseDate,
        m.DurationSeconds, m.ThumbnailUrl, m.MaturityRating, m.IsFeatured, m.IsAvailable);
}
