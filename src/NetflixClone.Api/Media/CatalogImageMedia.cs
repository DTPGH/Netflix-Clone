using System.Text.RegularExpressions;

namespace NetflixClone.Api.Media;

public sealed class CatalogImageMedia
{
    public string RootPath { get; }
    public CatalogImageMedia(IConfiguration configuration, IWebHostEnvironment environment)
    {
        RootPath = Path.GetFullPath(configuration["CatalogMedia:ImageRootPath"] ?? "../../private-media/images",
            environment.ContentRootPath);
        var webRoot = Path.GetFullPath(environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot"));
        if (RootPath.Equals(webRoot, StringComparison.OrdinalIgnoreCase) ||
            RootPath.StartsWith(webRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Uploaded catalog images must be stored outside wwwroot.");
    }
    public (string Path, string ContentType)? Resolve(string key)
    {
        if (!Regex.IsMatch(key, @"\A[a-f0-9]{32}\.(jpg|png|webp)\z")) return null;
        var path = Path.Combine(RootPath, key);
        if (!File.Exists(path) || (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0) return null;
        var contentType = Path.GetExtension(key) switch
        {
            ".jpg" => "image/jpeg", ".png" => "image/png", ".webp" => "image/webp", _ => null
        };
        return contentType is null ? null : (path, contentType);
    }
}
