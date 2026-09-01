using System;
using System.Collections.Generic;

namespace NetflixClone.Domain.Entities;

public partial class Plan
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public decimal Price { get; set; }

    public int MaxProfiles { get; set; }

    public int MaxConcurrentStreams { get; set; }

    public string MaxQuality { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}
