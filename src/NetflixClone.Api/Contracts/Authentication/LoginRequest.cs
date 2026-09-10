using System.ComponentModel.DataAnnotations;

namespace NetflixClone.Api.Contracts.Authentication;

public sealed class LoginRequest
{
    public string? DeviceIdentifier { get; init; }

    [Required]
    [MaxLength(200)]
    public string DeviceName { get; init; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string DeviceType { get; init; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; init; } = string.Empty;

    [Required]
    [MaxLength(64)]
    public string Password { get; init; } = string.Empty;
}
