using System.ComponentModel.DataAnnotations;

namespace NetflixClone.Api.Contracts.Authentication;

public sealed class ResendEmailConfirmationRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; init; } = string.Empty;
}