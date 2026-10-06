using System.ComponentModel.DataAnnotations;
namespace NetflixClone.Api.Contracts.Authentication;
public sealed class ForgotPasswordRequest { [Required, EmailAddress, MaxLength(255)] public string Email { get; init; } = ""; }
public class NewPasswordRequest
{
    [Required, MinLength(8), MaxLength(64)] public string NewPassword { get; init; } = "";
    [Required, Compare(nameof(NewPassword))] public string ConfirmPassword { get; init; } = "";
}
public sealed class ResetPasswordRequest : NewPasswordRequest { public int AccountId { get; init; } [Required, MaxLength(64)] public string Token { get; init; } = ""; }
public sealed class ChangePasswordRequest : NewPasswordRequest { [Required, MaxLength(64)] public string OldPassword { get; init; } = ""; }
