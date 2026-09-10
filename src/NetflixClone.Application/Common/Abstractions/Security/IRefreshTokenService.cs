namespace NetflixClone.Application.Common.Abstractions.Security;

public interface IRefreshTokenService
{
    GeneratedRefreshToken Generate();
}
