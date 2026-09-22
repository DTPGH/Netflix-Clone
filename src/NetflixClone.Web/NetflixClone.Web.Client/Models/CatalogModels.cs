namespace NetflixClone.Web.Client.Models;
public sealed record MovieCard(int MovieId, string Title, DateOnly? ReleaseDate, int DurationSeconds,
    string? ThumbnailUrl, string MaturityRating, bool IsFeatured, bool IsAvailable);
public sealed record MoviesReply(MovieCard[] Items, int Page, int PageSize, int TotalCount);
public sealed record CatalogGenre(int GenreId, string Name);
public sealed record GenresReply(CatalogGenre[] Genres);
public sealed record CatalogCredit(int PersonId, string FullName, string? PhotoUrl, string CreditType, string? CharacterName);
public sealed record MovieReply(int MovieId, string Title, DateOnly? ReleaseDate, int DurationSeconds,
    string? ThumbnailUrl, string MaturityRating, bool IsFeatured, bool IsAvailable,
    string? Description, string? BackdropUrl, string? TrailerUrl, byte MinAge,
    CatalogGenre[] Genres, CatalogCredit[] Credits);
public sealed record PlaybackReply(int MovieId, string Title, string VideoUrl, string ContentType, bool IsDemo);
