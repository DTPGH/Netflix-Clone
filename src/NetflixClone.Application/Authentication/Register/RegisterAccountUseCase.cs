using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Application.Common.Abstractions.Security;
using NetflixClone.Application.Common.Results;
using NetflixClone.Domain.Constants;
using NetflixClone.Domain.Entities;

namespace NetflixClone.Application.Authentication.Register;

public sealed class RegisterAccountUseCase : IRegisterAccountUseCase
{
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork unitOfWork;

    public RegisterAccountUseCase(
        IUserAccountRepository userAccountRepository,
        IRoleRepository roleRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork)
    {
        _userAccountRepository = userAccountRepository;
        _roleRepository = roleRepository;
        _passwordHasher = passwordHasher;
        this.unitOfWork = unitOfWork;
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

        var userAccount = new UserAccount { Email = email, PasswordHash = passwordHash };

        userAccount.UserRoles.Add(new UserRole { RoleId = userRole.Id });

        await _userAccountRepository.AddAsync(userAccount, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        var result = new RegisterAccountResult(userAccount.Id, userAccount.Email, userAccount.EmailConfirmed);
        return Result<RegisterAccountResult>.Success(result);
    }
}