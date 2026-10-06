using System.ComponentModel.DataAnnotations;
namespace NetflixClone.Web.Client.Models;

public sealed record ProfilePayload(string Name, bool IsKids, string AccountPassword);
public sealed record ProfileReply(int ProfileId, string Name, string? AvatarUrl, bool IsKids,
    byte MaturityLevel, bool OnboardingCompleted, bool HasPin);
public sealed record ProfilePasswordPayload(string AccountPassword);
public sealed record ProfilePinPayload(string AccountPassword, string Pin);
public sealed record ProfileUnlockPayload(string? Pin);
public sealed record ProfileUnlockReply(string UnlockToken, DateTime ExpiresAtUtc);
public sealed record ProfilesReply(List<ProfileReply> Profiles);
public sealed class ProfileUnlockFormModel
{
    [Required(ErrorMessage = "Enter the profile PIN."), RegularExpression("^[0-9]{4}$", ErrorMessage = "Enter exactly four digits (0–9).")]
    public string Pin { get; set; } = "";
}
public sealed class ProfilePinFormModel : IValidatableObject
{
    [Required(ErrorMessage = "Enter your account password."), StringLength(64)]
    public string AccountPassword { get; set; } = "";
    public string Pin { get; set; } = "";
    public bool Remove { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Remove && (Pin.Length != 4 || !Pin.All(c => c is >= '0' and <= '9')))
            yield return new ValidationResult("Enter exactly four digits (0–9).", [nameof(Pin)]);
    }
}
public sealed class ProfileFormModel : IValidatableObject
{
    public string Name { get; set; } = "";
    public bool IsKids { get; set; }
    [Required(ErrorMessage = "Enter your account password."), StringLength(64)]
    public string AccountPassword { get; set; } = "";
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Name) || Name.Trim().Length > 100)
            yield return new ValidationResult("Enter a name between 1 and 100 characters.", new[] { nameof(Name) });
    }
}
