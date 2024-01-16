namespace ServcoX.SimpleSharedCache;

public interface ISimpleSharedCacheClient
{
    Task Set<TRecord>(String key, TRecord record, CancellationToken cancellationToken = default) where TRecord : struct;

    Task<TRecord> Get<TRecord>(String key, CancellationToken cancellationToken = default) where TRecord : struct;

    Task<TRecord?> TryGet<TRecord>(String key, CancellationToken cancellationToken = default) where TRecord : struct;

    Task<IReadOnlyList<TRecord>> List<TRecord>(CancellationToken cancellationToken = default) where TRecord : struct;
}