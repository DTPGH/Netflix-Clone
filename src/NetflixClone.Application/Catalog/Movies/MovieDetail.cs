namespace NetflixClone.Application.Catalog.Movies;
public sealed record MovieDetail(int MovieId, string Title, DateOnly? ReleaseDate, int DurationSeconds,
    string? ThumbnailUrl, string MaturityRating, bool IsFeatured, bool IsAvailable,
    string? Description, string? BackdropUrl, string? TrailerUrl, byte MinAge,
    IReadOnlyList<NetflixClone.Application.Catalog.Genres.GenreSummary> Genres, IReadOnlyList<MovieCreditSummary> Credits);

