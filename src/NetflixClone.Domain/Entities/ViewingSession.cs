using System;
using System.Collections.Generic;

namespace NetflixClone.Domain.Entities;

public partial class ViewingSession
{
    public int Id { get; set; }

    public int ProfileId { get; set; }

    public int MovieId { get; set; }

    public int DeviceId { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime? EndedAt { get; set; }

    public int WatchedSeconds { get; set; }

    public bool IsQualifiedView { get; set; }

    public string? Quality { get; set; }

    public string? EndReason { get; set; }

    public virtual Device Device { get; set; } = null!;

    public virtual Movie Movie { get; set; } = null!;

    public virtual Profile Profile { get; set; } = null!;
}
