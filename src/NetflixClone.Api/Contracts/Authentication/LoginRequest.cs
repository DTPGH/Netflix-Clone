using System.ComponentModel.DataAnnotations;

namespace NetflixClone.Api.Contracts.Authentication;

public sealed class LoginRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; init; } = string.Empty;

    [Required]
    [MaxLength(64)]
    public string Password { get; init; } = string.Empty;
}
