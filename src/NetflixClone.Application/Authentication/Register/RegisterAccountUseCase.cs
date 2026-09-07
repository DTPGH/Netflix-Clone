using NetflixClone.Application.Authentication.EmailConfirmation;
using NetflixClone.Application.Common.Abstractions.Messaging;
using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Application.Common.Abstractions.Security;
using NetflixClone.Application.Common.Abstractions.Time;
using NetflixClone.Application.Common.Results;
using NetflixClone.Domain.Constants;
using NetflixClone.Domain.Entities;

namespace NetflixClone.Application.Authentication.Register;

public sealed class RegisterAccountUseCase : IRegisterAccountUseCase
{
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailConfirmationTokenService _emailConfirmationTokenService;
    private readonly IEmailConfirmationSender _emailConfirmationSender;
    private readonly IClock _clock;

    public RegisterAccountUseCase(
        IUserAccountRepository userAccountRepository,
        IRoleRepository roleRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork,
        IEmailConfirmationTokenService emailConfirmationTokenService,
        IEmailConfirmationSender emailConfirmationSender,
        IClock clock)
    {
        _userAccountRepository = userAccountRepository;
        _roleRepository = roleRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
        _emailConfirmationTokenService = emailConfirmationTokenService;
        _emailConfirmationSender = emailConfirmationSender;
        _clock = clock;
    }

    public async Task<Result<RegisterAccountResult>> ExecuteAsync(RegisterAccountCommand command, CancellationToken cancellationToken = default)
    {
        var email = command.Email.Trim().ToLowerInvariant();

        var emailExists = await _userAccountRepository.ExistsByEmailAsync(email, cancellationToken);

        if (emailExists)
        {
            return Result<RegisterAccountResult>.Failure(RegisterAccountErrors.EmailAlreadyExists);
        }

        var userRole = await _roleRepository.GetByNameAsync(RoleNames.User, cancellationToken);

        if (userRole is null)
        {
            return Result<RegisterAccountResult>.Failure(RegisterAccountErrors.UserRoleNotFound);
        }

        var passwordHash = _passwordHasher.Hash(command.Password);
        var confirmationToken = _emailConfirmationTokenService.Generate();
        var utcNow = _clock.UtcNow;
        var userAccount = new UserAccount { 
            Email = email, 
            PasswordHash = passwordHash,

            EmailConfirmed = false,
            EmailConfirmationTokenHash = confirmationToken.TokenHash,
            EmailConfirmationTokenExpiresAt = utcNow.Add(EmailConfirmationRules.TokenLifetime), 
            EmailConfirmationLastSentAt = utcNow
        
        };

        userAccount.UserRoles.Add(new UserRole { RoleId = userRole.Id });

        await _userAccountRepository.AddAsync(userAccount, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _emailConfirmationSender.SendAsync(userAccount.Id, userAccount.Email, confirmationToken.RawToken, cancellationToken);
        var result = new RegisterAccountResult(userAccount.Id, userAccount.Email, userAccount.EmailConfirmed);
        return Result<RegisterAccountResult>.Success(result);
    }
}