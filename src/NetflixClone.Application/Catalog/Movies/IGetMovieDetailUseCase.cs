using NetflixClone.Application.Common.Results;
namespace NetflixClone.Application.Catalog.Movies;
public interface IGetMovieDetailUseCase
{
    Task<Result<MovieDetail>> ExecuteAsync(ProfileMovieQuery query, CancellationToken cancellationToken = default);
}
