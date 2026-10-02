using System.ComponentModel.DataAnnotations;
namespace NetflixClone.Web.Client.Models;
public sealed record AdminGenre(int GenreId, string Name, int MovieCount);
public sealed class AdminGenreForm : IValidatableObject
{
    [Required, MaxLength(100)] public string Name { get; set; } = "";
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (string.IsNullOrWhiteSpace(Name)) yield return new("Enter a genre name.", [nameof(Name)]);
    }
}
