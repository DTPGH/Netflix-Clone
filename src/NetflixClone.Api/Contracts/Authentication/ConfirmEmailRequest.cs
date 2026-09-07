using System.ComponentModel.DataAnnotations;

namespace NetflixClone.Api.Contracts.Authentication;

public sealed class ConfirmEmailRequest
{
    [Range(1, int.MaxValue)]
    public int UserAccountId { get; init; }

    [Required]
    public string Token { get; init; } = string.Empty;
}