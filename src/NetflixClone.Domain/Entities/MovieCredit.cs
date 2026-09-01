using System;
using System.Collections.Generic;

namespace NetflixClone.Domain.Entities;

public partial class MovieCredit
{
    public int Id { get; set; }

    public int MovieId { get; set; }

    public int PersonId { get; set; }

    public string CreditType { get; set; } = null!;

    public string? CharacterName { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Movie Movie { get; set; } = null!;

    public virtual Person Person { get; set; } = null!;
}
