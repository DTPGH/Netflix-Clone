using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Application.Common.Abstractions.Time;
using NetflixClone.Application.Common.Exceptions;
using NetflixClone.Application.Common.Results;
namespace NetflixClone.Application.Profiles;
public sealed class UpdateProfileUseCase(IProfileRepository profiles, IUnitOfWork unitOfWork, IClock clock,
    IAuthenticationMutationScopeFactory scopes, ProfileAccountPasswordVerifier passwords) : IUpdateProfileUseCase
{
    public async Task<Result<UpdateProfileResult>> ExecuteAsync(UpdateProfileCommand command, CancellationToken cancellationToken = default)
    {
        await using var scope = await scopes.BeginAsync(command.UserAccountId, cancellationToken);
        if (scope is null) return Result<UpdateProfileResult>.Failure(ProfileErrors.AccountNotFound);
        var profile = await profiles.GetByIdForAccountAsync(command.UserAccountId, command.ProfileId, cancellationToken);
        if (profile is null || profile.IsDeleted) return Result<UpdateProfileResult>.Failure(ProfileErrors.NotFound);
        var password = await passwords.VerifyAsync(command.UserAccountId, command.AccountPassword, cancellationToken);
        if (password.IsFailure) return Result<UpdateProfileResult>.Failure(password.Error!);
        var name = ProfileRules.NormalizeName(command.Name);
        if (name is null) return Result<UpdateProfileResult>.Failure(ProfileErrors.InvalidName);
        profile.Name = name;
        profile.IsKids = command.IsKids;
        profile.MaturityLevel = ProfileRules.MaturityLevel(command.IsKids);
        profile.UpdatedAt = ProfileRules.NextUpdatedAt(profile.UpdatedAt, clock.UtcNow);
        try { await unitOfWork.SaveChangesAsync(cancellationToken); await scope.CommitAsync(cancellationToken); }
        catch (PersistenceConcurrencyException) { return Result<UpdateProfileResult>.Failure(ProfileErrors.ConcurrentChange); }
        return Result<UpdateProfileResult>.Success(new(ProfileSummary.From(profile)));
    }
}
