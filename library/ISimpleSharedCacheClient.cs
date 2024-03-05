namespace ServcoX.SimpleSharedCache;

public interface ISimpleSharedCacheClient
{
    Task Set<TRecord>(String key, TRecord record, CancellationToken cancellationToken = default) where TRecord : class;

    Task<TRecord> Get<TRecord>(String key, CancellationToken cancellationToken = default) where TRecord : class;

    Task<TRecord?> TryGet<TRecord>(String key, CancellationToken cancellationToken = default) where TRecord : class;

    Task<IReadOnlyDictionary<String, TRecord>> List<TRecord>(CancellationToken cancellationToken = default) where TRecord : class;
}