using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Application.Common.Results;
namespace NetflixClone.Application.Profiles;
public sealed class ListProfilesUseCase(IProfileRepository profiles) : IListProfilesUseCase
{
    public async Task<Result<ListProfilesResult>> ExecuteAsync(ListProfilesQuery command, CancellationToken cancellationToken = default)
        => Result<ListProfilesResult>.Success(new(await profiles.ListActiveByAccountAsync(command.UserAccountId, cancellationToken)));
}
