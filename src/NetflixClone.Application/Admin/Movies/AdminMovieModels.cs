namespace NetflixClone.Application.Admin.Movies;

public sealed record AdminMovieSummary(int MovieId, string Title, DateOnly? ReleaseDate,
    string? ThumbnailUrl, string MaturityRating, bool IsFeatured, bool IsAvailable, bool IsDeleted,
    DateTime UpdatedAtUtc);

public sealed record AdminMoviePage(IReadOnlyList<AdminMovieSummary> Items, int Page, int PageSize, int TotalCount);

public sealed record AdminMovieGenre(int GenreId, string Name);

public sealed record AdminMovieDetail(int MovieId, string Title, string? Description, DateOnly? ReleaseDate,
    int DurationSeconds, string? ThumbnailUrl, string? BackdropUrl, string? TrailerUrl, string? VideoUrl,
    string MaturityRating, byte MinAge, bool IsFeatured, bool IsAvailable, bool IsDeleted, DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc, IReadOnlyList<AdminMovieGenre> Genres);

public sealed record AdminMovieCriteria(int Page, int PageSize, string? Search, int? GenreId,
    bool? IsAvailable, AdminMovieDeletionFilter Deletion, AdminMovieSort Sort);

public enum AdminMovieSort { UpdatedDescending, TitleAscending }
public enum AdminMovieDeletionFilter { Active, Deleted, All }

public sealed record ListAdminMoviesQuery(int ActorUserAccountId, int Page, int PageSize, string? Search,
    int? GenreId, bool? IsAvailable, string? Deletion, string? Sort);
public sealed record GetAdminMovieQuery(int ActorUserAccountId, int MovieId);
public sealed record SaveAdminMovieData(string? Title, string? Description, DateOnly? ReleaseDate,
    int DurationSeconds, string? ThumbnailUrl, string? BackdropUrl, string? TrailerUrl, string? VideoUrl,
    string? MaturityRating, bool IsFeatured, bool IsAvailable, IReadOnlyCollection<int>? GenreIds);
public sealed record CreateAdminMovieCommand(int ActorUserAccountId, SaveAdminMovieData Movie);
public sealed record UpdateAdminMovieCommand(int ActorUserAccountId, int MovieId, SaveAdminMovieData Movie,
    DateTime? ExpectedUpdatedAtUtc);
public sealed record DeleteAdminMovieCommand(int ActorUserAccountId, int MovieId, DateTime? ExpectedUpdatedAtUtc);
public sealed record RestoreAdminMovieCommand(int ActorUserAccountId, int MovieId, DateTime? ExpectedUpdatedAtUtc);
