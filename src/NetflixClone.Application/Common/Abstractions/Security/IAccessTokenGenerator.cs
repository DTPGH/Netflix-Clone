namespace NetflixClone.Application.Common.Abstractions.Security;

public interface IAccessTokenGenerator
{
    GeneratedAccessToken Generate(int userAccountId, IReadOnlyCollection<string> roles);
}
