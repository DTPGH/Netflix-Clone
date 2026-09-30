using NetflixClone.Application.Common.Abstractions.Media;
using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Application.Common.Results;
using NetflixClone.Domain.Constants;

namespace NetflixClone.Application.Admin.Media;

public sealed record UploadAdminMediaCommand(int ActorUserAccountId, AdminMediaKind Kind, string FileName,
    string ContentType, long Length, Stream Content);
public sealed record AdminMediaUploadResult(string Value, string StoredFileName, long SizeBytes);

public static class AdminMediaUploadErrors
{
    public static readonly Error InvalidFile = new("AdminMedia.InvalidFile",
        "Choose a supported image or MP4 file within the size limit.", ErrorType.Validation);
    public static readonly Error AccountUnavailable = new("AdminMedia.AccountUnavailable",
        "The authenticated account is unavailable.", ErrorType.Unauthorized);
    public static readonly Error Forbidden = new("AdminMedia.Forbidden",
        "Current administrator access is required.", ErrorType.Forbidden);
}

public interface IAdminMediaUploadUseCase
{
    Task<Result<AdminMediaUploadResult>> ExecuteAsync(UploadAdminMediaCommand command,
        CancellationToken cancellationToken = default);
}

public sealed class AdminMediaUploadUseCase(IUserAccountRepository accounts, IAdminMediaStorage storage)
    : IAdminMediaUploadUseCase
{
    public const long MaxImageBytes = 10 * 1024 * 1024;
    public const long MaxVideoBytes = 1024L * 1024 * 1024;

    public async Task<Result<AdminMediaUploadResult>> ExecuteAsync(UploadAdminMediaCommand command,
        CancellationToken cancellationToken = default)
    {
        var account = command.ActorUserAccountId > 0
            ? await accounts.GetByIdAsync(command.ActorUserAccountId, cancellationToken) : null;
        if (account is null) return Result<AdminMediaUploadResult>.Failure(AdminMediaUploadErrors.AccountUnavailable);
        if (account.IsLocked || !account.EmailConfirmed ||
            !(await accounts.GetRoleNamesAsync(account.Id, cancellationToken)).Contains(RoleNames.Admin, StringComparer.Ordinal))
            return Result<AdminMediaUploadResult>.Failure(AdminMediaUploadErrors.Forbidden);

        var extension = Path.GetExtension(command.FileName).ToLowerInvariant();
        var validMetadata = command.Length > 0 && command.Length <=
            (command.Kind == AdminMediaKind.Image ? MaxImageBytes : MaxVideoBytes) &&
            (command.Kind == AdminMediaKind.Image
                ? extension is ".jpg" or ".jpeg" or ".png" or ".webp" &&
                    command.ContentType is "image/jpeg" or "image/png" or "image/webp"
                : extension == ".mp4" && command.ContentType == "video/mp4");
        if (!validMetadata) return Result<AdminMediaUploadResult>.Failure(AdminMediaUploadErrors.InvalidFile);

        var stored = await storage.StoreAsync(command.Kind, command.Content, command.Length, cancellationToken);
        return stored is null
            ? Result<AdminMediaUploadResult>.Failure(AdminMediaUploadErrors.InvalidFile)
            : Result<AdminMediaUploadResult>.Success(new(stored.Value, stored.StoredFileName, stored.SizeBytes));
    }
}
