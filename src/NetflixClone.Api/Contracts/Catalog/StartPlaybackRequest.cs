using System.ComponentModel.DataAnnotations;
namespace NetflixClone.Api.Contracts.Catalog;
public sealed record StartPlaybackRequest([Required, StringLength(64, MinimumLength = 64)] string DeviceIdentifier, Guid ClientSessionId);
