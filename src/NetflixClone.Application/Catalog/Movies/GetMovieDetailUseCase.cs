using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Application.Common.Results;
namespace NetflixClone.Application.Catalog.Movies;
public sealed class GetMovieDetailUseCase(IMovieCatalogQueries movies) : IGetMovieDetailUseCase
{
    public async Task<Result<MovieDetail>> ExecuteAsync(int movieId, CancellationToken cancellationToken = default)
    {
        if (movieId <= 0) return Result<MovieDetail>.Failure(MovieErrors.NotFound);
        var movie = await movies.GetDetailAsync(movieId, cancellationToken);
        return movie is null ? Result<MovieDetail>.Failure(MovieErrors.NotFound) : Result<MovieDetail>.Success(movie);
    }
}
