using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Application.Common.Abstractions.Time;
using NetflixClone.Application.Common.Results;
using NetflixClone.Domain.Constants;
using NetflixClone.Domain.Entities;
namespace NetflixClone.Application.Admin.Genres;
public interface IAdminGenreUseCase
{
    Task<Result<IReadOnlyList<AdminGenre>>> ListAsync(int actorId, CancellationToken ct);
    Task<Result<AdminGenre>> SaveAsync(SaveAdminGenre command, CancellationToken ct);
    Task<Result<bool>> DeleteAsync(int actorId, int genreId, string? expectedName, CancellationToken ct);
}
public sealed class AdminGenreUseCase(IAdminGenreRepository genres, IAdminGenreMutationScopeFactory scopes,
    IUserAccountRepository accounts, IUnitOfWork unitOfWork, IClock clock) : IAdminGenreUseCase
{
    private static readonly Error Invalid = new("AdminGenres.InvalidName", "Enter a genre name of at most 100 characters.", ErrorType.Validation);
    private static readonly Error Duplicate = new("AdminGenres.DuplicateName", "A genre with this name already exists.", ErrorType.Conflict);
    private static readonly Error Missing = new("AdminGenres.NotFound", "The genre was not found.", ErrorType.NotFound);
    private static readonly Error Changed = new("AdminGenres.ConcurrentChange", "The genre changed. Reload before trying again.", ErrorType.Conflict);
    private static readonly Error InUse = new("AdminGenres.InUse", "This genre is linked to movies, including soft-deleted movies. Remove those links before deleting.", ErrorType.Conflict);
    private async Task<Error?> Access(int actorId, CancellationToken ct)
    {
        var account = await accounts.GetByIdAsync(actorId, ct);
        if (account is null || account.IsLocked || !account.EmailConfirmed ||
            !(await accounts.GetRoleNamesAsync(actorId, ct)).Contains(RoleNames.Admin, StringComparer.Ordinal))
            return new Error("AdminGenres.Forbidden", "Current administrator access is required.", ErrorType.Forbidden);
        return null;
    }
    public async Task<Result<IReadOnlyList<AdminGenre>>> ListAsync(int actorId, CancellationToken ct)
    {
        var access = await Access(actorId, ct);
        return access is not null ? Result<IReadOnlyList<AdminGenre>>.Failure(access)
            : Result<IReadOnlyList<AdminGenre>>.Success(await genres.ListAsync(ct));
    }
    public async Task<Result<AdminGenre>> SaveAsync(SaveAdminGenre command, CancellationToken ct)
    {
        var access = await Access(command.ActorId, ct);
        if (access is not null) return Result<AdminGenre>.Failure(access);
        var name = command.Name?.Trim();
        if (string.IsNullOrEmpty(name) || name.Length > 100) return Result<AdminGenre>.Failure(Invalid);
        await using var scope = await scopes.BeginAsync(ct);
        Genre entity;
        if (command.GenreId is { } id)
        {
            var existing = await genres.GetAsync(id, ct);
            if (existing is null) return Result<AdminGenre>.Failure(Missing);
            if (command.ExpectedName is null || existing.Name != command.ExpectedName) return Result<AdminGenre>.Failure(Changed);
            entity = existing;
        }
        else entity = new() { CreatedAt = clock.UtcNow };
        if (await genres.NameExistsAsync(name, command.GenreId, ct)) return Result<AdminGenre>.Failure(Duplicate);
        var movieCount = command.GenreId is { } existingId ? await genres.MovieCountAsync(existingId, ct) : 0;
        entity.Name = name;
        if (!command.GenreId.HasValue) await genres.AddAsync(entity, ct);
        await unitOfWork.SaveChangesAsync(ct);
        await scope.CommitAsync(ct);
        return Result<AdminGenre>.Success(new(entity.Id, entity.Name, movieCount));
    }
    public async Task<Result<bool>> DeleteAsync(int actorId, int genreId, string? expectedName, CancellationToken ct)
    {
        var access = await Access(actorId, ct);
        if (access is not null) return Result<bool>.Failure(access);
        await using var scope = await scopes.BeginAsync(ct);
        var genre = await genres.GetAsync(genreId, ct);
        if (genre is null) return Result<bool>.Failure(Missing);
        if (expectedName is null || genre.Name != expectedName) return Result<bool>.Failure(Changed);
        if (await genres.IsUsedAsync(genreId, ct)) return Result<bool>.Failure(InUse);
        genres.Remove(genre);
        await unitOfWork.SaveChangesAsync(ct);
        await scope.CommitAsync(ct);
        return Result<bool>.Success(true);
    }
}
