using NetflixClone.Application.Catalog.Movies;
namespace NetflixClone.Application.Collections;
public sealed record CollectionMovie(int MovieId, string Title, string? ThumbnailUrl, bool IsDeleted);
public sealed record CollectionDetail(int CollectionId, string Title, bool IsPublished, int DisplayOrder,
    DateTime UpdatedAtUtc, IReadOnlyList<CollectionMovie> Movies);
public sealed record CollectionSummary(int CollectionId, string Title, bool IsPublished, int DisplayOrder, int MovieCount, DateTime UpdatedAtUtc);
public sealed record CollectionPage(IReadOnlyList<CollectionSummary> Items, int Page, int PageSize, int TotalCount);
public sealed record SaveCollectionCommand(int ActorUserAccountId, int? CollectionId, string? Title,
    bool IsPublished, int DisplayOrder, IReadOnlyList<int>? MovieIds, DateTime? ExpectedUpdatedAtUtc);
public sealed record BrowseCollection(int CollectionId, string Title, IReadOnlyList<MovieSummary> Movies);
