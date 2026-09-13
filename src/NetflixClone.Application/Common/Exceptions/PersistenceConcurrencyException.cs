namespace NetflixClone.Application.Common.Exceptions;

public sealed class PersistenceConcurrencyException : Exception
{
    public PersistenceConcurrencyException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
