using NetflixClone.Application.Common.Results;
namespace NetflixClone.Application.Profiles;

public interface IProfileAccessGuard
{
    Task<Result<bool>> CheckAsync(int accountId, int profileId, string? unlockToken, CancellationToken ct);
}
