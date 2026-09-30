using NetflixClone.Application.Admin.Movies;

namespace NetflixClone.Api.Contracts.Admin.Movies;

public sealed record AdminMovieSummaryResponse(int MovieId, string Title, DateOnly? ReleaseDate,
    string? ThumbnailUrl, string MaturityRating, bool IsFeatured, bool IsAvailable, bool IsDeleted, DateTime UpdatedAtUtc)
{
    public static AdminMovieSummaryResponse From(AdminMovieSummary movie) => new(movie.MovieId, movie.Title,
        movie.ReleaseDate, movie.ThumbnailUrl, movie.MaturityRating, movie.IsFeatured, movie.IsAvailable,
        movie.IsDeleted, movie.UpdatedAtUtc);
}

public sealed record AdminMovieGenreResponse(int GenreId, string Name);

public sealed record AdminMovieDetailResponse(int MovieId, string Title, string? Description, DateOnly? ReleaseDate,
    int DurationSeconds, string? ThumbnailUrl, string? BackdropUrl, string? TrailerUrl, string? VideoUrl,
    string MaturityRating, byte MinAge, bool IsFeatured, bool IsAvailable, bool IsDeleted, DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc, IReadOnlyList<AdminMovieGenreResponse> Genres)
{
    public static AdminMovieDetailResponse From(AdminMovieDetail movie) => new(movie.MovieId, movie.Title,
        movie.Description, movie.ReleaseDate, movie.DurationSeconds, movie.ThumbnailUrl, movie.BackdropUrl,
        movie.TrailerUrl, movie.VideoUrl, movie.MaturityRating, movie.MinAge, movie.IsFeatured, movie.IsAvailable,
        movie.IsDeleted,
        movie.CreatedAtUtc, movie.UpdatedAtUtc,
        movie.Genres.Select(genre => new AdminMovieGenreResponse(genre.GenreId, genre.Name)).ToArray());
}

public sealed record AdminMoviePageResponse(IReadOnlyList<AdminMovieSummaryResponse> Items,
    int Page, int PageSize, int TotalCount);
