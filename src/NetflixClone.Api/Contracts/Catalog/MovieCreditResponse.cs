namespace NetflixClone.Api.Contracts.Catalog;
public sealed record MovieCreditResponse(int PersonId, string FullName, string? PhotoUrl, string CreditType, string? CharacterName);
