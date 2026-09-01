using System;
using System.Collections.Generic;

namespace NetflixClone.Domain.Entities;

public partial class Movie
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public DateOnly? ReleaseDate { get; set; }

    public int DurationSeconds { get; set; }

    public string? ThumbnailUrl { get; set; }

    public string? BackdropUrl { get; set; }

    public string? TrailerUrl { get; set; }

    public string? VideoUrl { get; set; }

    public string MaturityRating { get; set; } = null!;

    public byte MinAge { get; set; }

    public bool IsDeleted { get; set; }

    public bool IsFeatured { get; set; }

    public bool IsAvailable { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<MovieCredit> MovieCredits { get; set; } = new List<MovieCredit>();

    public virtual ICollection<MyListItem> MyListItems { get; set; } = new List<MyListItem>();

    public virtual ICollection<ProfilePreference> ProfilePreferences { get; set; } = new List<ProfilePreference>();

    public virtual ICollection<Rating> Ratings { get; set; } = new List<Rating>();

    public virtual ICollection<ViewingSession> ViewingSessions { get; set; } = new List<ViewingSession>();

    public virtual ICollection<WatchHistory> WatchHistories { get; set; } = new List<WatchHistory>();

    public virtual ICollection<Genre> Genres { get; set; } = new List<Genre>();
}
