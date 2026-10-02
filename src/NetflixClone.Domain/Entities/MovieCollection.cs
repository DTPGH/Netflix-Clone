using System;
using System.Collections.Generic;

namespace NetflixClone.Domain.Entities;

public partial class MovieCollection
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;

    public bool IsPublished { get; set; }

    public int DisplayOrder { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<MovieCollectionItem> MovieCollectionItems { get; set; } = new List<MovieCollectionItem>();
}
