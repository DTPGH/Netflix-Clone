namespace NetflixClone.Api.Contracts.Ratings;
public sealed record SetRatingRequest(string? Value);
public sealed record RatingResponse(int MovieId, string? Value);
