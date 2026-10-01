namespace NetflixClone.Api.Contracts.Admin.Movies;

public sealed class ListAdminMoviesRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
    public int? GenreId { get; set; }
    public bool? IsAvailable { get; set; }
    public string? Deletion { get; set; } = "active";
    public string? Sort { get; set; } = "updatedAtDesc";
}

public class SaveAdminMovieRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public DateOnly? ReleaseDate { get; set; }
    public int DurationSeconds { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? BackdropUrl { get; set; }
    public string? TrailerUrl { get; set; }
    public string? VideoUrl { get; set; }
    public string? MaturityRating { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsAvailable { get; set; }
    public int[]? GenreIds { get; set; }
    public SaveAdminMovieCreditRequest[]? Credits { get; set; }
}

public sealed record SaveAdminMovieCreditRequest(int PersonId, string? CreditType, string? CharacterName);
public sealed record CreateAdminPersonRequest(string? FullName, string? PhotoUrl, DateOnly? BirthDate);

public sealed class UpdateAdminMovieRequest : SaveAdminMovieRequest
{
    public DateTime? ExpectedUpdatedAtUtc { get; set; }
}

public sealed class RestoreAdminMovieRequest
{
    public DateTime? ExpectedUpdatedAtUtc { get; set; }
}
