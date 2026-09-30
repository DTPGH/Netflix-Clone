using System.Text.RegularExpressions;
namespace NetflixClone.Api.Media;
public sealed class PrivateDemoMedia
{
    private readonly string root;
    public string RootPath => root;
    public PrivateDemoMedia(IConfiguration configuration, IWebHostEnvironment environment)
    {
        // Production should set an absolute external media directory explicitly.
        root = Path.GetFullPath(configuration["DemoMedia:RootPath"] ?? "../../private-media/videos", environment.ContentRootPath);
        var webRoot = Path.GetFullPath(environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot"));
        if (root.Equals(webRoot, StringComparison.OrdinalIgnoreCase) ||
            root.StartsWith(webRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Demo media must be outside wwwroot.");
    }
    public string? Resolve(string key)
    {
        if (!Regex.IsMatch(key, @"\A[A-Za-z0-9_-]+\.mp4\z")) return null;
        var path = Path.Combine(root, key);
        // Do not follow media symlinks/reparse points to files outside this directory.
        return File.Exists(path) && (File.GetAttributes(path) & FileAttributes.ReparsePoint) == 0 ? path : null;
    }
}
