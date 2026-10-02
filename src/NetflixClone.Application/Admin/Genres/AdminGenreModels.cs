namespace NetflixClone.Application.Admin.Genres;
public sealed record AdminGenre(int GenreId, string Name, int MovieCount);
public sealed record SaveAdminGenre(int ActorId, int? GenreId, string? Name, string? ExpectedName);
