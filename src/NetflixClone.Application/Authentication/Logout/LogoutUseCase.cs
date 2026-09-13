using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Application.Common.Abstractions.Security;
using NetflixClone.Application.Common.Abstractions.Time;
using NetflixClone.Application.Common.Exceptions;
using NetflixClone.Application.Common.Results;

namespace NetflixClone.Application.Authentication.Logout;

public sealed class LogoutUseCase : ILogoutUseCase
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IClock _clock;
    private readonly IUnitOfWork _unitOfWork;

    public LogoutUseCase(
        IRefreshTokenRepository refreshTokenRepository,
        IRefreshTokenService refreshTokenService,
        IClock clock,
        IUnitOfWork unitOfWork)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _refreshTokenService = refreshTokenService;
        _clock = clock;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<LogoutResult>> ExecuteAsync(LogoutCommand command, CancellationToken cancellationToken = default)
    {
        var hash = _refreshTokenService.HashIfValid(command.RefreshToken);
        if (hash is null)
        {
            return Result<LogoutResult>.Success(new());
        }

        var token = await _refreshTokenRepository.GetByHashAsync(hash, cancellationToken);
        var utcNow = _clock.UtcNow;
        if (token is null || token.RevokedAt.HasValue ||
            token.ReplacedByTokenId.HasValue || token.ExpiresAt <= utcNow)
        {
            return Result<LogoutResult>.Success(new());
        }

        token.RevokedAt = utcNow;
        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (PersistenceConcurrencyException)
        {
            // A competing revoke/rotation consumed this token. Logout applies only
            // to the submitted token: do not retry or follow its replacement.
            return Result<LogoutResult>.Success(new());
        }

        return Result<LogoutResult>.Success(new());
    }
}
