using System.ComponentModel.DataAnnotations;
namespace NetflixClone.Web.Client.Models;
public sealed record AdminPlan(int PlanId, string Name, decimal Price, int MaxConcurrentStreams, string MaxQuality,
    bool IsActive, int SubscriptionCount, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);
public sealed class AdminPlanForm : IValidatableObject
{
    [Required, MaxLength(100)] public string Name { get; set; } = "";
    public decimal Price { get; set; }
    [Range(1, 10)] public int MaxConcurrentStreams { get; set; } = 1;
    public string MaxQuality { get; set; } = "720p";
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (string.IsNullOrWhiteSpace(Name)) yield return new("Enter a plan name.", [nameof(Name)]);
        if (Price < 0 || Price > 9999999999999999.99m || decimal.Round(Price, 2) != Price)
            yield return new("Enter a non-negative price with at most two decimal places.", [nameof(Price)]);
        if (MaxQuality is not ("480p" or "720p" or "1080p" or "4K"))
            yield return new("Choose a supported quality.", [nameof(MaxQuality)]);
    }
    public static AdminPlanForm From(AdminPlan p) => new() { Name = p.Name, Price = p.Price, MaxConcurrentStreams = p.MaxConcurrentStreams, MaxQuality = p.MaxQuality };
}
