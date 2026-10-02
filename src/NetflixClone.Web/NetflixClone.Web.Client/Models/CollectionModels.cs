using System.ComponentModel.DataAnnotations;
namespace NetflixClone.Web.Client.Models;
public sealed record CollectionMovie(int MovieId, string Title, string? ThumbnailUrl, bool IsDeleted);
public sealed record CollectionDetail(int CollectionId, string Title, bool IsPublished, int DisplayOrder, DateTime UpdatedAtUtc, CollectionMovie[] Movies);
public sealed record CollectionSummary(int CollectionId, string Title, bool IsPublished, int DisplayOrder, int MovieCount, DateTime UpdatedAtUtc);
public sealed record CollectionPage(CollectionSummary[] Items, int Page, int PageSize, int TotalCount);
public sealed record BrowseCollection(int CollectionId, string Title, MovieCard[] Movies);
public sealed record SaveCollectionPayload(string Title, bool IsPublished, int DisplayOrder, int[] MovieIds, DateTime? ExpectedUpdatedAtUtc);
public sealed class CollectionForm : IValidatableObject
{
    [Required, MaxLength(200)] public string Title { get; set; } = "";
    [Range(0, int.MaxValue)] public int DisplayOrder { get; set; }
    public bool IsPublished { get; set; }
    public List<CollectionMovie> Movies { get; } = [];
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (string.IsNullOrWhiteSpace(Title)) yield return new("Enter a collection title.", [nameof(Title)]);
        if (Movies.Count > 50) yield return new("Choose at most 50 movies.", [nameof(Movies)]);
        if (IsPublished && !Movies.Any(m => !m.IsDeleted)) yield return new("Choose an active movie before publishing.", [nameof(Movies)]);
    }
    public SaveCollectionPayload Payload(DateTime? version) => new(Title, IsPublished, DisplayOrder, Movies.Select(m => m.MovieId).ToArray(), version);
    public static CollectionForm From(CollectionDetail detail)
    {
        var form = new CollectionForm { Title = detail.Title, IsPublished = detail.IsPublished, DisplayOrder = detail.DisplayOrder };
        form.Movies.AddRange(detail.Movies);
        return form;
    }
}
