namespace NetflixClone.Web.Client.Services;

// Browser/tab runtime only. Never persist or render this bearer proof.
public sealed class ProfileAccessState
{
    public int? ProfileId { get; private set; }
    public long Version { get; private set; }
    private string? token;
    private DateTime expiresAtUtc;
    public event Action? UnlockRequired;

    public void Set(int profileId, string unlockToken, DateTime expires)
    { ProfileId = profileId; token = unlockToken; expiresAtUtc = expires; Version++; }
    public string? GetToken(int profileId)
        => ProfileId == profileId && expiresAtUtc > DateTime.UtcNow ? token : null;
    public void Clear() { ProfileId = null; token = null; expiresAtUtc = default; Version++; }
    public bool RequireUnlock(int profileId, long version)
    {
        if (ProfileId != profileId || Version != version) return false;
        Clear(); UnlockRequired?.Invoke(); return true;
    }
}
