using Microsoft.JSInterop;
using NetflixClone.Web.Client.Models;
namespace NetflixClone.Web.Client.Services;
// Only non-secret purchase metadata is persisted; tokens/passwords are never stored here.
public sealed class SubscriptionPurchaseDraftStore(IJSRuntime js) : IAsyncDisposable
{
    private IJSObjectReference? module;
    private async Task<IJSObjectReference> Module() => module ??= await js.InvokeAsync<IJSObjectReference>("import", "./js/subscription-purchase.js");
    public async Task<SubscriptionPurchaseDraft?> ReadAsync(string account) => await (await Module()).InvokeAsync<SubscriptionPurchaseDraft?>("read", account);
    public async Task WriteAsync(string account, SubscriptionPurchaseDraft draft) => await (await Module()).InvokeVoidAsync("write", account, draft);
    public async Task ClearAsync(string account) => await (await Module()).InvokeVoidAsync("clear", account);
    public async ValueTask DisposeAsync() { if (module is not null) await module.DisposeAsync(); }
}
