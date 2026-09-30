namespace NetflixClone.Application.Common.Abstractions.Media;

public enum AdminMediaKind { Image, Video }
public sealed record StoredAdminMedia(string Value, string StoredFileName, long SizeBytes);

public interface IAdminMediaStorage
{
    Task<StoredAdminMedia?> StoreAsync(AdminMediaKind kind, Stream content, long declaredLength,
        CancellationToken cancellationToken = default);
}
