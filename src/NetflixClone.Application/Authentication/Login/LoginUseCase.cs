using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Application.Common.Abstractions.Security;
using NetflixClone.Application.Common.Abstractions.Time;
using NetflixClone.Application.Common.Results;
using NetflixClone.Domain.Entities;

namespace NetflixClone.Application.Authentication.Login;

public sealed class LoginUseCase : ILoginUseCase
{
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IClock _clock;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAccessTokenGenerator _accessTokenGenerator;
    private readonly IDeviceRepository _deviceRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IDeviceIdentifierService _deviceIdentifierService;
    private readonly IRefreshTokenService _refreshTokenService;

    public LoginUseCase(
        IUserAccountRepository userAccountRepository,
        IPasswordHasher passwordHasher,
        IClock clock,
        IUnitOfWork unitOfWork,
        IAccessTokenGenerator accessTokenGenerator,
        IDeviceRepository deviceRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IDeviceIdentifierService deviceIdentifierService,
        IRefreshTokenService refreshTokenService)
    {
        _userAccountRepository = userAccountRepository;
        _passwordHasher = passwordHasher;
        _clock = clock;
        _unitOfWork = unitOfWork;
        _accessTokenGenerator = accessTokenGenerator;
        _deviceRepository = deviceRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _deviceIdentifierService = deviceIdentifierService;
        _refreshTokenService = refreshTokenService;
    }

    public async Task<Result<LoginResult>> ExecuteAsync(LoginCommand command, CancellationToken cancellationToken = default)
    {
        var email = command.Email.Trim().ToLowerInvariant();
        var account = await _userAccountRepository.GetByEmailAsync(email, cancellationToken);

        if (account is null)
        {
            return Result<LoginResult>.Failure(LoginErrors.InvalidCredentials);
        }

        var utcNow = _clock.UtcNow;

        if (account.LockoutEnd > utcNow)
        {
            return Result<LoginResult>.Failure(LoginErrors.TemporarilyLocked);
        }

        var failedLoginStateChanged = false;

        if (account.LockoutEnd.HasValue)
        {
            account.FailedLoginCount = 0;
            account.LockoutEnd = null;
            failedLoginStateChanged = true;
        }

        if (!_passwordHasher.Verify(command.Password, account.PasswordHash))
        {
            account.FailedLoginCount++;

            if (account.FailedLoginCount >= LoginRules.MaxFailedAttempts)
            {
                account.LockoutEnd = utcNow.Add(LoginRules.LockoutDuration);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<LoginResult>.Failure(account.LockoutEnd.HasValue
                ? LoginErrors.TemporarilyLocked
                : LoginErrors.InvalidCredentials);
        }

        if (account.FailedLoginCount != 0)
        {
            account.FailedLoginCount = 0;
            failedLoginStateChanged = true;
        }

        if (account.IsLocked)
        {
            if (failedLoginStateChanged)
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<LoginResult>.Failure(LoginErrors.AccountLocked);
        }

        if (!account.EmailConfirmed)
        {
            if (failedLoginStateChanged)
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<LoginResult>.Failure(LoginErrors.EmailNotConfirmed);
        }

        var roles = await _userAccountRepository.GetRoleNamesAsync(account.Id, cancellationToken);
        if (roles.Count == 0)
        {
            if (failedLoginStateChanged)
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<LoginResult>.Failure(LoginErrors.RolesNotConfigured);
        }

        var identifierHash = _deviceIdentifierService.HashIfValid(command.DeviceIdentifier);
        var device = identifierHash is null ? null
            : await _deviceRepository.GetByIdentifierHashAsync(account.Id, identifierHash, cancellationToken);
        var rawIdentifier = command.DeviceIdentifier;

        if (device is null || device.RevokedAt.HasValue)
        {
            var identifier = _deviceIdentifierService.Generate();
            rawIdentifier = identifier.RawIdentifier;
            device = new Device
            {
                UserAccountId = account.Id,
                DeviceIdentifierHash = identifier.IdentifierHash,
                FirstSeenAt = utcNow
            };
            await _deviceRepository.AddAsync(device, cancellationToken);
        }

        device.DeviceName = command.DeviceName;
        device.DeviceType = command.DeviceType;
        device.UserAgent = command.UserAgent;
        device.IpAddress = command.RemoteIpAddress;
        device.LastActiveAt = utcNow;

        var refreshToken = _refreshTokenService.Generate();
        var refreshTokenExpiresAt = utcNow.Add(RefreshTokenRules.Lifetime);
        await _refreshTokenRepository.AddAsync(new RefreshToken
        {
            UserAccountId = account.Id,
            Device = device,
            TokenHash = refreshToken.TokenHash,
            CreatedAt = utcNow,
            ExpiresAt = refreshTokenExpiresAt,
            RevokedAt = null,
            ReplacedByTokenId = null
        }, cancellationToken);

        var token = _accessTokenGenerator.Generate(account.Id, roles);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<LoginResult>.Success(new LoginResult(
            account.Id, account.Email, token.AccessToken, token.ExpiresAtUtc,
            refreshToken.RawToken, refreshTokenExpiresAt, rawIdentifier!));
    }
}
