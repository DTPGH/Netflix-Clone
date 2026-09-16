using NetflixClone.Web.Client.Models;

namespace NetflixClone.Web.Client.Services;

// Browser-specific persistence is hidden here, not inside pages or API DTOs.
public interface ICredentialStore
{
    event Action? SessionChanged;
    event Func<Task>? Resumed;
    Task InitializeAsync();
    Task<IAsyncDisposable> AcquireAsync();
    Task<StoredCredential?> ReadAsync();
    Task WriteAsync(StoredCredential credential);
    Task ClearAsync();
    Task<string?> GetDeviceAsync();
    Task SetDeviceAsync(string identifier);
    Task<string> GetEpochAsync();
    Task ChangeEpochAsync();
}
