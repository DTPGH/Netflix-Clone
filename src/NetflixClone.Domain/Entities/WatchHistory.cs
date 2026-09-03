using System;
using System.Collections.Generic;

namespace NetflixClone.Domain.Entities;

public partial class WatchHistory
{
    public int Id { get; set; }

    public int ProfileId { get; set; }

    public int MovieId { get; set; }

    public int LastPositionSeconds { get; set; }

    public DateTime LastWatchedAt { get; set; }

    public bool IsCompleted { get; set; }

    public DateTime? CompletedAt { get; set; }

    public bool IsHidden { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Movie Movie { get; set; } = null!;

    public virtual Profile Profile { get; set; } = null!;
}
