using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Application.Common.Results;
namespace NetflixClone.Application.Catalog.Genres;
public sealed class ListGenresUseCase(IGenreCatalogQueries genres) : IListGenresUseCase
{
    public async Task<Result<IReadOnlyList<GenreSummary>>> ExecuteAsync(CancellationToken cancellationToken = default)
        => Result<IReadOnlyList<GenreSummary>>.Success(await genres.ListAsync(cancellationToken));
}
