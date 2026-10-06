using NetflixClone.Application.Common.Abstractions.Security;
using NetflixClone.Application.Common.Results;

namespace NetflixClone.Application.Profiles;

public interface IProfilePinUseCase
{
    Task<Result<ProfileSummary>> SetAsync(int accountId, int profileId, string accountPassword, string? pin, CancellationToken ct);
    Task<Result<GeneratedProfileUnlockToken>> UnlockAsync(int accountId, int profileId, string? pin, CancellationToken ct);
}
