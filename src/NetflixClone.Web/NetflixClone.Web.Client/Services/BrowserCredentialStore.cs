using Microsoft.JSInterop;
using NetflixClone.Web.Client.Models;

namespace NetflixClone.Web.Client.Services;

public sealed class BrowserCredentialStore(IJSRuntime js) : ICredentialStore, IAsyncDisposable
{
    private IJSObjectReference? _module;
    private DotNetObjectReference<BrowserCredentialStore>? _reference;
    public event Action? SessionChanged;
    public event Func<Task>? Resumed;
    public async Task InitializeAsync()
    {
        if (_module is not null) return;
        _module = await js.InvokeAsync<IJSObjectReference>("import", "./js/auth-storage.js");
        _reference = DotNetObjectReference.Create(this);
        await _module.InvokeVoidAsync("listen", _reference);
    }
    [JSInvokable] public void OnSessionChanged() => SessionChanged?.Invoke();
    [JSInvokable] public Task OnResumed() => Resumed?.Invoke() ?? Task.CompletedTask;
    public async Task<IAsyncDisposable> AcquireAsync()
    {
        await InitializeAsync();
        var id = await _module!.InvokeAsync<int>("acquire");
        return new Lease(_module!, id);
    }
    public async Task<StoredCredential?> ReadAsync() => await _module!.InvokeAsync<StoredCredential?>("read");
    public async Task WriteAsync(StoredCredential credential) => await _module!.InvokeVoidAsync("write", credential);
    public async Task ClearAsync() => await _module!.InvokeVoidAsync("clear");
    public async Task<string?> GetDeviceAsync() => await _module!.InvokeAsync<string?>("getDevice");
    public async Task SetDeviceAsync(string identifier) => await _module!.InvokeVoidAsync("setDevice", identifier);
    public async Task<string> GetEpochAsync() => await _module!.InvokeAsync<string>("getEpoch");
    public async Task ChangeEpochAsync() => await _module!.InvokeVoidAsync("changeEpoch");
    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
        {
            await _module.InvokeVoidAsync("unlisten");
            await _module.DisposeAsync();
        }
        _reference?.Dispose();
    }
    private sealed class Lease(IJSObjectReference module, int id) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync() => await module.InvokeVoidAsync("release", id);
    }
}
