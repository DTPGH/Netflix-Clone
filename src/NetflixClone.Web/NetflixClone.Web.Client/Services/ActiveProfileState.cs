using System.Security.Claims;
using NetflixClone.Web.Client.Models;
namespace NetflixClone.Web.Client.Services;

// Client-only, tab-local UI context. API ownership checks remain authoritative.
// Intentionally not persisted: a reload requires an explicit profile selection.
public sealed class ActiveProfileState : IDisposable
{
    private readonly AuthSession session;
    private readonly ProfilesApiClient profiles;
    private readonly ProfileAccessState access;
    private string? accountId;
    public ProfileReply? Selected { get; private set; }
    public long Version { get; private set; }
    public event Action? Changed;
    public ActiveProfileState(AuthSession session, ProfilesApiClient profiles, ProfileAccessState access)
    {
        this.session = session; this.profiles = profiles;
        this.access = access;
        session.Changed += SessionChanged;
        access.UnlockRequired += Clear;
    }
    public async Task InitializeAsync()
    {
        await session.InitializeAsync();
        await SynchronizeAsync();
    }
    private void SessionChanged() => _ = SynchronizeAsync();
    private async Task SynchronizeAsync()
    {
        var auth = await session.GetAuthenticationStateAsync();
        var current = session.IsAuthenticated && !session.IsLoggingOut
            ? auth.User.FindFirst(ClaimTypes.NameIdentifier)?.Value : null;
        if (current == accountId) return; // Token renewal must not discard the selection.
        accountId = current;
        Clear();
    }
    public async Task<string?> SelectAsync(int profileId, string? pin = null)
    {
        await InitializeAsync();
        var version = Version;
        if (accountId is null) return "Please sign in to choose a profile.";
        // Revalidate even if the selection page was opened before a profile was deleted.
        var result = await profiles.ListAsync();
        if (version != Version || !session.IsAuthenticated) return "Your session changed. Please try again.";
        if (!result.Success) return result.Error;
        var selected = result.Value!.Profiles.SingleOrDefault(p => p.ProfileId == profileId);
        if (selected is null) return "This profile is no longer available. Reload the list.";
        var unlock = await profiles.UnlockAsync(profileId, pin);
        if (version != Version || !session.IsAuthenticated || session.IsLoggingOut) return "Your session changed. Please try again.";
        if (!unlock.Success) return unlock.Error;
        access.Set(profileId, unlock.Value!.UnlockToken, unlock.Value.ExpiresAtUtc);
        Selected = selected; Version++; Changed?.Invoke();
        return null;
    }
    public void Clear()
    {
        access.Clear();
        Selected = null; Version++; Changed?.Invoke();
    }
    public void Update(ProfileReply profile)
    {
        if (Selected?.ProfileId != profile.ProfileId) return;
        Selected = profile; Version++; Changed?.Invoke();
    }
    public void Reconcile(IReadOnlyList<ProfileReply> available)
    {
        if (Selected is not { } current) return;
        var latest = available.SingleOrDefault(p => p.ProfileId == current.ProfileId);
        if (latest is null) Clear();
        else if (latest != current) Update(latest);
    }
    public void InvalidateIfCurrent(int profileId, long version)
    {
        if (Version == version && Selected?.ProfileId == profileId) Clear();
    }
    public void Dispose() { session.Changed -= SessionChanged; access.UnlockRequired -= Clear; access.Clear(); }
}
