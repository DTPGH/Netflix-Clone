using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
namespace NetflixClone.Api.Contracts.Profiles;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SetProfilePinRequest([Required, StringLength(64)] string AccountPassword,
    [Required, RegularExpression("^[0-9]{4}$")] string Pin);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ProfilePasswordRequest([Required, StringLength(64)] string AccountPassword);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record UnlockProfileRequest([RegularExpression("^[0-9]{4}$")] string? Pin);
public sealed record UnlockProfileResponse(string UnlockToken, DateTime ExpiresAtUtc);
