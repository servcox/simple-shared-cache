namespace ServcoX.SimpleSharedCache;

public interface ISimpleSharedCacheClient
{
    Task Set<T>(String key, T value, CancellationToken cancellationToken = default);
    Task<T> Get<T>(String key, CancellationToken cancellationToken = default);
    Task<T?> TryGet<T>(String key, CancellationToken cancellationToken = default);
}