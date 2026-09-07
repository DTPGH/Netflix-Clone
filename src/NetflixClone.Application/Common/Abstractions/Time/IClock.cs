namespace NetflixClone.Application.Common.Abstractions.Time;

public interface IClock
{
    DateTime UtcNow { get; }
}