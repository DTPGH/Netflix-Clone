namespace NetflixClone.Application.Common.Abstractions.Persistence;
public interface IProfileCreationScope : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
}
