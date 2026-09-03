using System;
using System.Collections.Generic;

namespace NetflixClone.Domain.Entities;

public partial class ProfilePreference
{
    public int Id { get; set; }

    public int ProfileId { get; set; }

    public int MovieId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Movie Movie { get; set; } = null!;

    public virtual Profile Profile { get; set; } = null!;
}
