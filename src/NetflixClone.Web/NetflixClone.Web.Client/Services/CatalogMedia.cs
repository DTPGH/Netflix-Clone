using System.Text.RegularExpressions;
namespace NetflixClone.Web.Client.Services;
public static class CatalogMedia
{
    public static string? Image(string? value, Uri? apiBase = null)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (Regex.IsMatch(value, @"\A/images/movies/[A-Za-z0-9_-]+\.(webp|jpg|jpeg|png)\z")) return value;
        if (apiBase is not null && Regex.IsMatch(value, @"\A/api/catalog-images/[a-f0-9]{32}\.(jpg|png|webp)\z"))
            return new Uri(apiBase, value.TrimStart('/')).AbsoluteUri;
        return Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == "https" ? uri.AbsoluteUri : null;
    }
    public static string? YoutubeId(string? value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != "https") return null;
        string? id = null;
        if (uri.Host == "youtu.be") id = uri.AbsolutePath.Trim('/');
        else if (uri.Host is "youtube.com" or "www.youtube.com")
        {
            if (uri.AbsolutePath == "/watch")
                id = uri.Query.TrimStart('?').Split('&').FirstOrDefault(p => p.StartsWith("v=", StringComparison.Ordinal))?[2..];
            else if (uri.AbsolutePath.StartsWith("/embed/", StringComparison.Ordinal))
                id = uri.AbsolutePath[7..];
        }
        return id is not null && Regex.IsMatch(id, @"\A[A-Za-z0-9_-]{11}\z") ? id : null;
    }
}
