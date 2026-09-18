using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Application.Common.Abstractions.Time;
using NetflixClone.Application.Common.Results;
using NetflixClone.Domain.Entities;
namespace NetflixClone.Application.Profiles;
public sealed class CreateProfileUseCase(IProfileRepository profiles, IProfileCreationScopeFactory scopeFactory,
    IUnitOfWork unitOfWork, IClock clock) : ICreateProfileUseCase
{
    public async Task<Result<CreateProfileResult>> ExecuteAsync(CreateProfileCommand command, CancellationToken cancellationToken = default)
    {
        var name = ProfileRules.NormalizeName(command.Name);
        if (name is null) return Result<CreateProfileResult>.Failure(ProfileErrors.InvalidName);
        await using var scope = await scopeFactory.BeginAsync(command.UserAccountId, cancellationToken);
        if (scope is null) return Result<CreateProfileResult>.Failure(ProfileErrors.AccountNotFound);
        if (await profiles.CountActiveByAccountAsync(command.UserAccountId, cancellationToken) >= ProfileRules.MaxActiveProfiles)
            return Result<CreateProfileResult>.Failure(ProfileErrors.LimitReached);
        var now = clock.UtcNow;
        var profile = new Profile
        {
            UserAccountId = command.UserAccountId, Name = name, IsKids = command.IsKids,
            MaturityLevel = ProfileRules.MaturityLevel(command.IsKids),
            CreatedAt = now, UpdatedAt = now,
            AvatarUrl = null, PinHash = null, OnboardingCompleted = false, IsDeleted = false, DeletedAt = null
        };
        await profiles.AddAsync(profile, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await scope.CommitAsync(cancellationToken);
        return Result<CreateProfileResult>.Success(new(ProfileSummary.From(profile)));
    }
}
