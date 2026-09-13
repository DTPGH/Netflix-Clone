namespace NetflixClone.Application.Common.Abstractions.Security;

public interface IDeviceIdentifierService
{
    GeneratedDeviceIdentifier Generate();
    string? HashIfValid(string? identifier);
}
