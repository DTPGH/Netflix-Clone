using NetflixClone.Application.Common.Results;
namespace NetflixClone.Application.Catalog.Genres;
public interface IListGenresUseCase
{
    Task<Result<IReadOnlyList<GenreSummary>>> ExecuteAsync(CancellationToken cancellationToken = default);
}
