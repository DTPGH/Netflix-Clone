using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Application.Common.Abstractions.Time;
using NetflixClone.Application.Common.Exceptions;
using NetflixClone.Application.Common.Results;
namespace NetflixClone.Application.Profiles;
public sealed class DeleteProfileUseCase(IProfileRepository profiles, IUnitOfWork unitOfWork, IClock clock,
    IAuthenticationMutationScopeFactory scopes, ProfileAccountPasswordVerifier passwords) : IDeleteProfileUseCase
{
    public async Task<Result<DeleteProfileResult>> ExecuteAsync(DeleteProfileCommand command, CancellationToken cancellationToken = default)
    {
        await using var scope = await scopes.BeginAsync(command.UserAccountId, cancellationToken);
        if (scope is null) return Result<DeleteProfileResult>.Failure(ProfileErrors.AccountNotFound);
        var profile = await profiles.GetByIdForAccountAsync(command.UserAccountId, command.ProfileId, cancellationToken);
        if (profile is null) return Result<DeleteProfileResult>.Failure(ProfileErrors.NotFound);
        var password = await passwords.VerifyAsync(command.UserAccountId, command.AccountPassword, cancellationToken);
        if (password.IsFailure) return Result<DeleteProfileResult>.Failure(password.Error!);
        if (profile.IsDeleted) return Result<DeleteProfileResult>.Success(new());
        var now = clock.UtcNow;
        profile.IsDeleted = true;
        profile.DeletedAt = now;
        profile.UpdatedAt = ProfileRules.NextUpdatedAt(profile.UpdatedAt, now);
        try { await unitOfWork.SaveChangesAsync(cancellationToken); await scope.CommitAsync(cancellationToken); }
        catch (PersistenceConcurrencyException) { return Result<DeleteProfileResult>.Failure(ProfileErrors.ConcurrentChange); }
        return Result<DeleteProfileResult>.Success(new());
    }
}
