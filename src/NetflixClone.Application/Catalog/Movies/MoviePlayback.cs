namespace NetflixClone.Application.Catalog.Movies;
public sealed record MoviePlayback(int MovieId, string Title, bool IsAvailable, string? VideoUrl, byte MinAge = 0);
public sealed record PlaybackResult(int MovieId, string Title, string MediaKey, string ContentType, bool IsDemo);
