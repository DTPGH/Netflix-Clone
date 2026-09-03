using System;
using System.Collections.Generic;

namespace NetflixClone.Domain.Entities;

public partial class Rating
{
    public int Id { get; set; }

    public int ProfileId { get; set; }

    public int MovieId { get; set; }

    public string Value { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Movie Movie { get; set; } = null!;

    public virtual Profile Profile { get; set; } = null!;
}
