using System.ComponentModel.DataAnnotations;
namespace NetflixClone.Api.Contracts.Viewing;
// MVC validates positional records using primary-constructor parameter metadata.
public sealed record StartViewingSessionRequest([param: Required, StringLength(64, MinimumLength = 64)] string DeviceIdentifier, Guid ClientSessionId);
public sealed record ViewingCheckpointRequest([param: Required, StringLength(64, MinimumLength = 64)] string DeviceIdentifier,
    long Sequence, long WatchedMilliseconds, string? EndReason);
