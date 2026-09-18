using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
namespace NetflixClone.Api.Contracts.Profiles;
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CreateProfileRequest([Required] string Name, bool IsKids);
