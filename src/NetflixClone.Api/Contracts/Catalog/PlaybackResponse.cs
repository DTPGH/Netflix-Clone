namespace NetflixClone.Api.Contracts.Catalog;
public sealed record PlaybackResponse(int MovieId, string Title, string VideoUrl, string ContentType, bool IsDemo);
