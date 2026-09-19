using System.ComponentModel.DataAnnotations;
namespace NetflixClone.Web.Client.Models;

public sealed record ProfilePayload(string Name, bool IsKids);
public sealed record ProfileReply(int ProfileId, string Name, string? AvatarUrl, bool IsKids,
    byte MaturityLevel, bool OnboardingCompleted);
public sealed record ProfilesReply(List<ProfileReply> Profiles);
public sealed class ProfileFormModel : IValidatableObject
{
    public string Name { get; set; } = "";
    public bool IsKids { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Name) || Name.Trim().Length > 100)
            yield return new ValidationResult("Enter a name between 1 and 100 characters.", new[] { nameof(Name) });
    }
}
