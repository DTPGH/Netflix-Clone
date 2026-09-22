using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Application.Common.Results;
namespace NetflixClone.Application.Catalog.Movies;
public interface IGetMoviePlaybackUseCase
{
    Task<Result<PlaybackResult>> ExecuteAsync(int movieId, CancellationToken cancellationToken = default);
}
public sealed class GetMoviePlaybackUseCase(IMovieCatalogQueries movies) : IGetMoviePlaybackUseCase
{
    public async Task<Result<PlaybackResult>> ExecuteAsync(int movieId, CancellationToken cancellationToken = default)
    {
        var movie = await movies.GetPlaybackAsync(movieId, cancellationToken);
        if (movie is null) return Result<PlaybackResult>.Failure(MovieErrors.NotFound);
        // Only local public demo assets are supported. No remote URLs, traversal or query strings.
        var url = movie.VideoUrl;
        if (!movie.IsAvailable || string.IsNullOrWhiteSpace(url) ||
            !System.Text.RegularExpressions.Regex.IsMatch(url, @"\A/videos/[A-Za-z0-9_-]+\.mp4\z"))
            return Result<PlaybackResult>.Failure(new Error("Movies.PlaybackUnavailable", "Video is not available.", ErrorType.Conflict));
        return Result<PlaybackResult>.Success(new(movie.MovieId, movie.Title, url, "video/mp4", true));
    }
}
