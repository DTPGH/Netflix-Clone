using System.ComponentModel.DataAnnotations;

namespace NetflixClone.Api.Contracts.Authentication;

public sealed class LogoutRequest
{
    [Required]
    public string RefreshToken { get; init; } = string.Empty;
}
