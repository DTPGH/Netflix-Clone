using NetflixClone.Application.Common.Abstractions.Time;

namespace NetflixClone.Infrastructure.Time;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}