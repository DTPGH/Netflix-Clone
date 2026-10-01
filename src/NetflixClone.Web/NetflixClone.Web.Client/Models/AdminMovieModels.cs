using System.ComponentModel.DataAnnotations;

namespace NetflixClone.Web.Client.Models;

public sealed record AdminMovieCard(int MovieId, string Title, DateOnly? ReleaseDate, string? ThumbnailUrl,
    string MaturityRating, bool IsFeatured, bool IsAvailable, bool IsDeleted, DateTime UpdatedAtUtc);
public sealed record AdminMoviesReply(AdminMovieCard[] Items, int Page, int PageSize, int TotalCount);
public sealed record AdminMovieGenre(int GenreId, string Name);
public sealed record AdminPerson(int PersonId, string FullName);
public sealed record AdminPersonDetail(int PersonId, string FullName, string? PhotoUrl, DateOnly? BirthDate);
public sealed record CreateAdminPersonPayload(string FullName, string? PhotoUrl, DateOnly? BirthDate);
public sealed class AdminMovieCreditModel
{
    public int PersonId { get; set; }
    public string FullName { get; set; } = "";
    public string CreditType { get; set; } = "Actor";
    public string? CharacterName { get; set; }
}
public sealed record SaveAdminMovieCredit(int PersonId, string CreditType, string? CharacterName);
public sealed record AdminMovieReply(int MovieId, string Title, string? Description, DateOnly? ReleaseDate,
    int DurationSeconds, string? ThumbnailUrl, string? BackdropUrl, string? TrailerUrl, string? VideoUrl,
    string MaturityRating, byte MinAge, bool IsFeatured, bool IsAvailable, bool IsDeleted,
    DateTime CreatedAtUtc, DateTime UpdatedAtUtc, AdminMovieGenre[] Genres, AdminMovieCreditModel[] Credits);
public sealed record AdminMediaUploadReply(string Value, string FileName, long SizeBytes);
public sealed record SaveAdminMoviePayload(string Title, string? Description, DateOnly? ReleaseDate,
    int DurationSeconds, string? ThumbnailUrl, string? BackdropUrl, string? TrailerUrl, string? VideoUrl,
    string MaturityRating, bool IsFeatured, bool IsAvailable, int[] GenreIds, SaveAdminMovieCredit[] Credits);
public sealed record UpdateAdminMoviePayload(string Title, string? Description, DateOnly? ReleaseDate,
    int DurationSeconds, string? ThumbnailUrl, string? BackdropUrl, string? TrailerUrl, string? VideoUrl,
    string MaturityRating, bool IsFeatured, bool IsAvailable, int[] GenreIds, DateTime ExpectedUpdatedAtUtc, SaveAdminMovieCredit[] Credits);

public sealed class AdminMovieFormModel : IValidatableObject
{
    [Required, MaxLength(255)] public string Title { get; set; } = "";
    [MaxLength(2000)] public string? Description { get; set; }
    public DateOnly? ReleaseDate { get; set; }
    [Range(1, int.MaxValue)] public int DurationSeconds { get; set; } = 1;
    [MaxLength(500)] public string? ThumbnailUrl { get; set; }
    [MaxLength(500)] public string? BackdropUrl { get; set; }
    [MaxLength(500)] public string? TrailerUrl { get; set; }
    [MaxLength(500)] public string? VideoUrl { get; set; }
    [Required] public string MaturityRating { get; set; } = "P";
    public bool IsFeatured { get; set; }
    public bool IsAvailable { get; set; }
    public HashSet<int> GenreIds { get; } = [];
    public List<AdminMovieCreditModel> Credits { get; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Title))
            yield return new("Enter a movie title.", [nameof(Title)]);
        if (MaturityRating is not ("P" or "T13" or "T16" or "T18"))
            yield return new("Choose a supported maturity rating.", [nameof(MaturityRating)]);
        if (GenreIds.Count == 0)
            yield return new("Select at least one genre.", [nameof(GenreIds)]);
        if (IsAvailable && string.IsNullOrWhiteSpace(VideoUrl))
            yield return new("An available movie needs a demo video identifier.", [nameof(VideoUrl)]);
        if (Credits.Count > 100 || Credits.Any(c => c.PersonId <= 0 || c.CreditType is not ("Actor" or "Director") ||
            c.CharacterName?.Trim().Length > 200 || c.CreditType == "Director" && !string.IsNullOrWhiteSpace(c.CharacterName)) ||
            Credits.Select(c => (c.PersonId, c.CreditType)).Distinct().Count() != Credits.Count)
            yield return new("Check credits: no duplicate person/role; character names are only for actors (maximum 200 characters).", [nameof(Credits)]);
    }

    public SaveAdminMoviePayload CreatePayload() => new(Title, Description, ReleaseDate, DurationSeconds,
        ThumbnailUrl, BackdropUrl, TrailerUrl, VideoUrl, MaturityRating, IsFeatured, IsAvailable,
        GenreIds.Order().ToArray(), CreditPayload());
    public UpdateAdminMoviePayload UpdatePayload(DateTime version) => new(Title, Description, ReleaseDate,
        DurationSeconds, ThumbnailUrl, BackdropUrl, TrailerUrl, VideoUrl, MaturityRating, IsFeatured,
        IsAvailable, GenreIds.Order().ToArray(), version, CreditPayload());
    private SaveAdminMovieCredit[] CreditPayload() => Credits.Select(c => new SaveAdminMovieCredit(c.PersonId, c.CreditType, c.CharacterName)).ToArray();
    public static AdminMovieFormModel From(AdminMovieReply movie)
    {
        var model = new AdminMovieFormModel { Title = movie.Title, Description = movie.Description,
            ReleaseDate = movie.ReleaseDate, DurationSeconds = movie.DurationSeconds,
            ThumbnailUrl = movie.ThumbnailUrl, BackdropUrl = movie.BackdropUrl, TrailerUrl = movie.TrailerUrl,
            VideoUrl = movie.VideoUrl, MaturityRating = movie.MaturityRating, IsFeatured = movie.IsFeatured,
            IsAvailable = movie.IsAvailable };
        model.GenreIds.UnionWith(movie.Genres.Select(genre => genre.GenreId));
        model.Credits.AddRange(movie.Credits.Select(c => new AdminMovieCreditModel { PersonId = c.PersonId,
            FullName = c.FullName, CreditType = c.CreditType, CharacterName = c.CharacterName }));
        return model;
    }
}
