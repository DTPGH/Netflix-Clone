using System;
using System.Collections.Generic;

namespace NetflixClone.Domain.Entities;

public partial class MovieCollectionItem
{
    public int CollectionId { get; set; }

    public int MovieId { get; set; }

    public int Position { get; set; }

    public virtual MovieCollection Collection { get; set; } = null!;

    public virtual Movie Movie { get; set; } = null!;
}
