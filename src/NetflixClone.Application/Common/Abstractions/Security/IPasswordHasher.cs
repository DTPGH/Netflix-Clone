namespace NetflixClone.Application.Common.Abstractions.Security;

public interface IPasswordHasher
{
    string Hash(string password);
}