using System.Text.RegularExpressions;

namespace NetflixClone.Application.Admin.Movies;

public static partial class AdminMovieRules
{
    public static byte? MinAge(string? rating) => rating switch { "P" => 0, "T13" => 13, "T16" => 16, "T18" => 18, _ => null };
    public static DateTime NextUpdatedAt(DateTime previous, DateTime now)
        => now > previous ? now : previous.AddTicks(1);
    public static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    public static bool Image(string? value) => value is null || LocalImage().IsMatch(value) || UploadedImage().IsMatch(value) || Https(value);
    public static bool Trailer(string? value)
    {
        if (value is null) return true;
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps) return false;
        return uri.Host.Equals("youtu.be", StringComparison.OrdinalIgnoreCase) ||
            uri.Host.Equals("youtube.com", StringComparison.OrdinalIgnoreCase) ||
            uri.Host.Equals("www.youtube.com", StringComparison.OrdinalIgnoreCase);
    }
    public static bool Video(string? value) => value is null || VideoKey().IsMatch(value);
    private static bool Https(string value) => Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps;
    [GeneratedRegex(@"\A/images/movies/[A-Za-z0-9_-]+\.(webp|jpg|jpeg|png)\z", RegexOptions.IgnoreCase)]
    private static partial Regex LocalImage();
    [GeneratedRegex(@"\A/api/catalog-images/[a-f0-9]{32}\.(jpg|png|webp)\z")]
    private static partial Regex UploadedImage();
    [GeneratedRegex(@"\A/videos/[A-Za-z0-9_-]+\.mp4\z", RegexOptions.IgnoreCase)]
    private static partial Regex VideoKey();
}
