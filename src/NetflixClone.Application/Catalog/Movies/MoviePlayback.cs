namespace NetflixClone.Application.Catalog.Movies;
// This slice serves public demonstration MP4 assets, not licensed movie streams.
public sealed record MoviePlayback(int MovieId, string Title, bool IsAvailable, string? VideoUrl);
public sealed record PlaybackResult(int MovieId, string Title, string VideoUrl, string ContentType, bool IsDemo);
