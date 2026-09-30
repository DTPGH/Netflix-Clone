using System.Security.Cryptography;
using NetflixClone.Application.Common.Abstractions.Media;

namespace NetflixClone.Infrastructure.Media;

public sealed class LocalAdminMediaStorage(string imageRoot, string videoRoot) : IAdminMediaStorage
{
    public async Task<StoredAdminMedia?> StoreAsync(AdminMediaKind kind, Stream content, long declaredLength,
        CancellationToken cancellationToken = default)
    {
        var root = kind == AdminMediaKind.Image ? imageRoot : videoRoot;
        Directory.CreateDirectory(root);
        var temporary = Path.Combine(root, $".{Guid.NewGuid():N}.uploading");
        try
        {
            await using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                128 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                await content.CopyToAsync(output, 128 * 1024, cancellationToken);
            }
            var info = new FileInfo(temporary);
            if (info.Length != declaredLength) return null;
            var extension = await DetectExtensionAsync(temporary, kind, cancellationToken);
            if (extension is null) return null;
            var name = $"{Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant()}{extension}";
            File.Move(temporary, Path.Combine(root, name));
            var value = kind == AdminMediaKind.Image ? $"/api/catalog-images/{name}" : $"/videos/{name}";
            return new(value, name, info.Length);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    private static async Task<string?> DetectExtensionAsync(string path, AdminMediaKind kind,
        CancellationToken cancellationToken)
    {
        var header = new byte[12];
        await using var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        if (await input.ReadAsync(header, cancellationToken) < header.Length) return null;
        if (kind == AdminMediaKind.Video)
            return header.AsSpan(4, 4).SequenceEqual("ftyp"u8) ? ".mp4" : null;
        if (header.AsSpan(0, 3).SequenceEqual(new byte[] { 0xff, 0xd8, 0xff })) return ".jpg";
        if (header.AsSpan(0, 8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a })) return ".png";
        if (header.AsSpan(0, 4).SequenceEqual("RIFF"u8) && header.AsSpan(8, 4).SequenceEqual("WEBP"u8)) return ".webp";
        return null;
    }
}
