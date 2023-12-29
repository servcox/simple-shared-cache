namespace ServcoX.SimpleSharedCache;

public interface ISimpleSharedCacheClient
{
    /// <summary>
    /// Store a record for a given key and record type.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="record"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="TRecord"></typeparam>
    /// <returns></returns>
    Task Set<TRecord>(String key, TRecord record, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieve a record for a given key and record type. Throws exception if not found.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <exception cref="Exceptions.NotFoundException"></exception>
    Task<T> Get<T>(String key, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieve a record for a given key and record type. Returns `null` if not found.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    Task<T?> TryGet<T>(String key, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieve all records for a given record type.
    /// </summary>
    /// <remarks>
    /// This may be performance intensive if there are many records of this type.
    /// </remarks>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="TRecord"></typeparam>
    /// <returns></returns>
    Task<IReadOnlyList<TRecord>> List<TRecord>(CancellationToken cancellationToken = default);
}