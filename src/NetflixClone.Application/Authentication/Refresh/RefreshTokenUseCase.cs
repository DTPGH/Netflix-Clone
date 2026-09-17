using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Application.Common.Abstractions.Security;
using NetflixClone.Application.Common.Abstractions.Time;
using NetflixClone.Application.Common.Exceptions;
using NetflixClone.Application.Common.Results;
using NetflixClone.Domain.Entities;

namespace NetflixClone.Application.Authentication.Refresh;

public sealed class RefreshTokenUseCase : IRefreshTokenUseCase
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IAccessTokenGenerator _accessTokenGenerator;
    private readonly IClock _clock;
    private readonly IUnitOfWork _unitOfWork;

    public RefreshTokenUseCase(
        IRefreshTokenRepository refreshTokenRepository,
        IRefreshTokenService refreshTokenService,
        IUserAccountRepository userAccountRepository,
        IAccessTokenGenerator accessTokenGenerator,
        IClock clock,
        IUnitOfWork unitOfWork)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _refreshTokenService = refreshTokenService;
        _userAccountRepository = userAccountRepository;
        _accessTokenGenerator = accessTokenGenerator;
        _clock = clock;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<RefreshTokenResult>> ExecuteAsync(RefreshTokenCommand command, CancellationToken cancellationToken = default)
    {
        var hash = _refreshTokenService.HashIfValid(command.RefreshToken);
        if (hash is null)
        {
            return Result<RefreshTokenResult>.Failure(RefreshTokenErrors.InvalidRefreshToken);
        }

        var oldToken = await _refreshTokenRepository.GetByHashAsync(hash, cancellationToken);
        var utcNow = _clock.UtcNow;
        if (oldToken is null || oldToken.ReplacedByTokenId.HasValue ||
            oldToken.RevokedAt.HasValue || oldToken.ExpiresAt <= utcNow)
        {
            return Result<RefreshTokenResult>.Failure(RefreshTokenErrors.InvalidRefreshToken);
        }

        var device = oldToken.Device;
        var account = oldToken.UserAccount;
        if (device is null || device.UserAccountId != oldToken.UserAccountId ||
            device.RevokedAt.HasValue || account is null ||
            account.Id != oldToken.UserAccountId || account.IsLocked || !account.EmailConfirmed)
        {
            return Result<RefreshTokenResult>.Failure(RefreshTokenErrors.InvalidRefreshToken);
        }

        var roles = await _userAccountRepository.GetRoleNamesAsync(account.Id, cancellationToken);
        if (roles.Count == 0)
        {
            return Result<RefreshTokenResult>.Failure(RefreshTokenErrors.RolesNotConfigured);
        }

        var generated = _refreshTokenService.Generate();
        var replacement = new RefreshToken
        {
            UserAccountId = oldToken.UserAccountId,
            DeviceId = oldToken.DeviceId,
            TokenHash = generated.TokenHash,
            CreatedAt = utcNow,
            // Rotation preserves the original session's absolute expiration.
            ExpiresAt = oldToken.ExpiresAt,
            RevokedAt = null,
            ReplacedByTokenId = null
        };
        await _refreshTokenRepository.AddAsync(replacement, cancellationToken);
        oldToken.RevokedAt = utcNow;
        oldToken.ReplacedByToken = replacement;
        // Participate in device revocation concurrency in the same rotation transaction.
        device.LastActiveAt = utcNow > device.LastActiveAt ? utcNow : device.LastActiveAt.AddTicks(1);

        var accessToken = _accessTokenGenerator.Generate(account.Id, roles);
        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (PersistenceConcurrencyException)
        {
            // A competing request changed the token or device. Never retry this rotation.
            return Result<RefreshTokenResult>.Failure(RefreshTokenErrors.InvalidRefreshToken);
        }

        return Result<RefreshTokenResult>.Success(new RefreshTokenResult(
            accessToken.AccessToken, accessToken.ExpiresAtUtc,
            generated.RawToken, replacement.ExpiresAt));
    }
}
