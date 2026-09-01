using System;
using System.Collections.Generic;

namespace NetflixClone.Domain.Entities;

public partial class Person
{
    public int Id { get; set; }

    public string FullName { get; set; } = null!;

    public string? PhotoUrl { get; set; }

    public DateOnly? BirthDate { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<MovieCredit> MovieCredits { get; set; } = new List<MovieCredit>();
}
