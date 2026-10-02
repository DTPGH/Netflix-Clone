using NetflixClone.Application.Admin.Genres;
using NetflixClone.Domain.Entities;
namespace NetflixClone.Application.Common.Abstractions.Persistence;
public interface IAdminGenreRepository
{
    Task<IReadOnlyList<AdminGenre>> ListAsync(CancellationToken ct);
    Task<Genre?> GetAsync(int id, CancellationToken ct);
    Task<bool> NameExistsAsync(string name, int? exceptId, CancellationToken ct);
    Task<bool> IsUsedAsync(int id, CancellationToken ct);
    Task<int> MovieCountAsync(int id, CancellationToken ct);
    Task AddAsync(Genre genre, CancellationToken ct);
    void Remove(Genre genre);
}
public interface IAdminGenreMutationScopeFactory
{
    Task<IAdminGenreMutationScope> BeginAsync(CancellationToken ct);
}
public interface IAdminGenreMutationScope : IAsyncDisposable
{
    Task CommitAsync(CancellationToken ct);
}
