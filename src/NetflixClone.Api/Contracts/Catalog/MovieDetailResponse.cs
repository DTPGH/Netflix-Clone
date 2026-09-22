using NetflixClone.Application.Catalog.Movies;
namespace NetflixClone.Api.Contracts.Catalog;
public sealed record MovieDetailResponse(int MovieId, string Title, DateOnly? ReleaseDate, int DurationSeconds,
    string? ThumbnailUrl, string MaturityRating, bool IsFeatured, bool IsAvailable,
    string? Description, string? BackdropUrl, string? TrailerUrl, byte MinAge,
    IReadOnlyList<GenreResponse> Genres, IReadOnlyList<MovieCreditResponse> Credits)
{
    public static MovieDetailResponse From(MovieDetail m) => new(m.MovieId, m.Title, m.ReleaseDate,
        m.DurationSeconds, m.ThumbnailUrl, m.MaturityRating, m.IsFeatured, m.IsAvailable,
        m.Description, m.BackdropUrl, m.TrailerUrl, m.MinAge,
        m.Genres.Select(g => new GenreResponse(g.GenreId, g.Name)).ToArray(),
        m.Credits.Select(c => new MovieCreditResponse(c.PersonId, c.FullName, c.PhotoUrl, c.CreditType, c.CharacterName)).ToArray());
}
