using System.ComponentModel.DataAnnotations;

namespace NetflixClone.Web.Client.Models;

public sealed class LoginForm
{
    [Required, EmailAddress, MaxLength(255)] public string Email { get; set; } = "";
    [Required, MaxLength(64)] public string Password { get; set; } = "";
    [Required, MaxLength(200)] public string DeviceName { get; set; } = "My browser";
}
public sealed class RegisterForm
{
    [Required, EmailAddress, MaxLength(255)] public string Email { get; set; } = "";
    [Required, MinLength(8), MaxLength(64)] public string Password { get; set; } = "";
    [Required, Compare(nameof(Password), ErrorMessage = "Passwords must match.")]
    public string ConfirmPassword { get; set; } = "";
}
public sealed class ConfirmForm
{
    [Range(1, int.MaxValue, ErrorMessage = "Enter the account number from registration.")]
    public int UserAccountId { get; set; }
    [Required] public string Token { get; set; } = "";
}
public sealed class ResendForm
{
    [Required, EmailAddress, MaxLength(255)] public string Email { get; set; } = "";
}
// API DTOs belong to the frontend. Never log these objects: several contain credentials.
public sealed record LoginPayload(string Email, string Password, string? DeviceIdentifier, string DeviceName, string DeviceType);
public sealed record LoginReply(int UserAccountId, string Email, string AccessToken, DateTime ExpiresAtUtc,
    string RefreshToken, DateTime RefreshTokenExpiresAtUtc, string DeviceIdentifier);
public sealed record RegisterReply(int UserAccountId, string Email, bool EmailConfirmed);
public sealed record TokenPayload(string RefreshToken);
public sealed record TokenReply(string AccessToken, DateTime ExpiresAtUtc, string RefreshToken, DateTime RefreshTokenExpiresAtUtc);
public sealed record MeReply(string UserAccountId, string[] Roles);
public sealed record StoredCredential(string RefreshToken, DateTime RefreshTokenExpiresAtUtc);
public sealed record ApiResult<T>(T? Value, string? Error, int Status)
{
    public bool Success => Error is null;
}

