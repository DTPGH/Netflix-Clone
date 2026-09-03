using System;
using System.Collections.Generic;

namespace NetflixClone.Domain.Entities;

public partial class Profile
{
    public int Id { get; set; }

    public int UserAccountId { get; set; }

    public string Name { get; set; } = null!;

    public string? AvatarUrl { get; set; }

    public bool IsKids { get; set; }

    public byte MaturityLevel { get; set; }

    public string? PinHash { get; set; }

    public bool OnboardingCompleted { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<MyListItem> MyListItems { get; set; } = new List<MyListItem>();

    public virtual ICollection<ProfilePreference> ProfilePreferences { get; set; } = new List<ProfilePreference>();

    public virtual ICollection<Rating> Ratings { get; set; } = new List<Rating>();

    public virtual UserAccount UserAccount { get; set; } = null!;

    public virtual ICollection<ViewingSession> ViewingSessions { get; set; } = new List<ViewingSession>();

    public virtual ICollection<WatchHistory> WatchHistories { get; set; } = new List<WatchHistory>();
}
