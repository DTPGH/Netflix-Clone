namespace NetflixClone.Infrastructure.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    // Base64-encoded cryptographically random bytes, supplied through secrets/configuration.
    public string SigningKey { get; init; } = string.Empty;
    public int AccessTokenLifetimeMinutes { get; init; } = 15;

    public byte[] GetSigningKeyBytes()
    {
        if (string.IsNullOrWhiteSpace(SigningKey))
        {
            throw new InvalidOperationException(
                "Jwt:SigningKey is missing. Configure a Base64-encoded key of at least 32 random bytes " +
                "in User Secrets (Jwt:SigningKey) or the Jwt__SigningKey environment variable. " +
                "Do not store the key in source-controlled settings.");
        }

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(SigningKey);
        }
        catch (FormatException)
        {
            throw new InvalidOperationException("Jwt:SigningKey must be Base64-encoded random bytes.");
        }

        if (bytes.Length < 32)
        {
            throw new InvalidOperationException("Jwt:SigningKey must contain at least 32 random bytes.");
        }

        return bytes;
    }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Issuer) || string.IsNullOrWhiteSpace(Audience))
        {
            throw new InvalidOperationException("Jwt:Issuer and Jwt:Audience are required.");
        }

        if (AccessTokenLifetimeMinutes != 15)
        {
            throw new InvalidOperationException("Jwt:AccessTokenLifetimeMinutes must be 15.");
        }

        GetSigningKeyBytes();
    }
}
