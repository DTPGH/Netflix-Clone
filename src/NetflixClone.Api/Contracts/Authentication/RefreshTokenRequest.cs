using System.ComponentModel.DataAnnotations;
namespace NetflixClone.Api.Contracts.Authentication;

public sealed class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; init; } = string.Empty;
}
