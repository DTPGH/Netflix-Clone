namespace NetflixClone.Application.Catalog.Movies;
public sealed record MovieSummary(int MovieId, string Title, DateOnly? ReleaseDate, int DurationSeconds,
    string? ThumbnailUrl, string MaturityRating, bool IsFeatured, bool IsAvailable);

