namespace NetflixClone.Api.Contracts.Admin.Genres;
public sealed record AdminGenreResponse(int GenreId, string Name, int MovieCount);
public sealed record SaveGenreRequest(string? Name, string? ExpectedName);
