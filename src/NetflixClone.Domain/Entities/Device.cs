using System;
using System.Collections.Generic;

namespace NetflixClone.Domain.Entities;

public partial class Device
{
    public int Id { get; set; }

    public int UserAccountId { get; set; }

    public string DeviceIdentifierHash { get; set; } = null!;

    public string DeviceName { get; set; } = null!;

    public string DeviceType { get; set; } = null!;

    public string? UserAgent { get; set; }

    public string? IpAddress { get; set; }

    public DateTime FirstSeenAt { get; set; }

    public DateTime LastActiveAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    public virtual UserAccount UserAccount { get; set; } = null!;

    public virtual ICollection<ViewingSession> ViewingSessions { get; set; } = new List<ViewingSession>();
}
