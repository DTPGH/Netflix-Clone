using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Application.Common.Results;
namespace NetflixClone.Application.Catalog.Movies;
public interface IGetMoviePlaybackUseCase
{
    Task<Result<PlaybackResult>> ExecuteAsync(ProfileMovieQuery query, CancellationToken cancellationToken = default);
}
public sealed class GetMoviePlaybackUseCase(IMovieCatalogQueries movies, IProfileRepository profiles, IUserAccountRepository accounts) : IGetMoviePlaybackUseCase
{
    public async Task<Result<PlaybackResult>> ExecuteAsync(ProfileMovieQuery query, CancellationToken cancellationToken = default)
    {
        var account = await accounts.GetByIdAsync(query.UserAccountId, cancellationToken);
        if (account is null || account.IsLocked || !account.EmailConfirmed)
            return Result<PlaybackResult>.Failure(MovieErrors.NotFound);
        var profile = await profiles.GetByIdForAccountAsync(query.UserAccountId, query.ProfileId, cancellationToken);
        if (profile is null || profile.IsDeleted) return Result<PlaybackResult>.Failure(MovieErrors.NotFound);
        var movie = await movies.GetPlaybackAsync(query.MovieId, cancellationToken);
        if (movie is null || movie.MinAge > profile.MaturityLevel) return Result<PlaybackResult>.Failure(MovieErrors.NotFound);
        // Legacy database value is only a media identifier, never a public URL or filesystem path.
        var url = movie.VideoUrl;
        if (!movie.IsAvailable || string.IsNullOrWhiteSpace(url) ||
            !System.Text.RegularExpressions.Regex.IsMatch(url, @"\A/videos/[A-Za-z0-9_-]+\.mp4\z"))
            return Result<PlaybackResult>.Failure(new Error("Movies.PlaybackUnavailable", "Video is not available.", ErrorType.Conflict));
        return Result<PlaybackResult>.Success(new(movie.MovieId, movie.Title, url["/videos/".Length..], "video/mp4", true));
    }
}
