using NetflixClone.Api.Contracts.Catalog;
namespace NetflixClone.Api.Contracts.Collections;
public sealed record SaveCollectionRequest(string? Title, bool IsPublished, int DisplayOrder, int[]? MovieIds, DateTime? ExpectedUpdatedAtUtc);
public sealed record CollectionMovieResponse(int MovieId, string Title, string? ThumbnailUrl, bool IsDeleted);
public sealed record CollectionDetailResponse(int CollectionId, string Title, bool IsPublished, int DisplayOrder,
    DateTime UpdatedAtUtc, IReadOnlyList<CollectionMovieResponse> Movies);
public sealed record CollectionSummaryResponse(int CollectionId, string Title, bool IsPublished, int DisplayOrder, int MovieCount, DateTime UpdatedAtUtc);
public sealed record CollectionPageResponse(IReadOnlyList<CollectionSummaryResponse> Items, int Page, int PageSize, int TotalCount);
public sealed record BrowseCollectionResponse(int CollectionId, string Title, IReadOnlyList<MovieSummaryResponse> Movies);
