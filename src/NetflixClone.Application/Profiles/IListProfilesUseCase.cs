using NetflixClone.Application.Common.Results;
namespace NetflixClone.Application.Profiles;
public interface IListProfilesUseCase
{
    Task<Result<ListProfilesResult>> ExecuteAsync(ListProfilesQuery command, CancellationToken cancellationToken = default);
}
