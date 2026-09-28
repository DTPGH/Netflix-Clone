using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Application.Common.Results;
namespace NetflixClone.Application.Catalog.Movies;
public sealed class GetMovieDetailUseCase(IMovieCatalogQueries movies, IProfileRepository profiles) : IGetMovieDetailUseCase
{
    public async Task<Result<MovieDetail>> ExecuteAsync(ProfileMovieQuery query, CancellationToken cancellationToken = default)
    {
        var profile = await profiles.GetByIdForAccountAsync(query.UserAccountId, query.ProfileId, cancellationToken);
        if (profile is null || profile.IsDeleted || query.MovieId <= 0) return Result<MovieDetail>.Failure(MovieErrors.NotFound);
        var movie = await movies.GetDetailAsync(query.MovieId, cancellationToken);
        return movie is null || movie.MinAge > profile.MaturityLevel
            ? Result<MovieDetail>.Failure(MovieErrors.NotFound) : Result<MovieDetail>.Success(movie);
    }
}
