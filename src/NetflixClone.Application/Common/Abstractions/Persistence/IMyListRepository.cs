using NetflixClone.Application.MyList;
using NetflixClone.Domain.Entities;
namespace NetflixClone.Application.Common.Abstractions.Persistence;
public interface IMyListRepository
{
    Task<MyListResult> ListAsync(int profileId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<bool> MovieExistsAsync(int movieId, CancellationToken cancellationToken = default);
    Task<MyListItem?> GetAsync(int profileId, int movieId, CancellationToken cancellationToken = default);
    Task AddAsync(MyListItem item, CancellationToken cancellationToken = default);
    void Remove(MyListItem item);
}
