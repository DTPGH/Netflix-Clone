using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using NetflixClone.Application.Common.Abstractions.Security;
using NetflixClone.Application.Common.Abstractions.Time;

namespace NetflixClone.Infrastructure.Security;

public sealed class JwtAccessTokenGenerator : IAccessTokenGenerator
{
    private readonly JwtOptions _options;
    private readonly IClock _clock;

    public JwtAccessTokenGenerator(JwtOptions options, IClock clock)
    {
        _options = options;
        _clock = clock;
    }

    public GeneratedAccessToken Generate(int userAccountId, IReadOnlyCollection<string> roles)
    {
        var utcNow = _clock.UtcNow;
        var expiresAt = utcNow.AddMinutes(_options.AccessTokenLifetimeMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userAccountId.ToString(CultureInfo.InvariantCulture)),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(JwtRegisteredClaimNames.Iat,
                EpochTime.GetIntDate(utcNow).ToString(CultureInfo.InvariantCulture),
                ClaimValueTypes.Integer64)
        };
        claims.AddRange(roles.Select(role => new Claim("role", role)));

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(_options.GetSigningKeyBytes()),
                SecurityAlgorithms.HmacSha256));

        return new GeneratedAccessToken(new JwtSecurityTokenHandler().WriteToken(token), token.ValidTo);
    }
}
