using System.Collections.Concurrent;
using System.Text.Json;
using Azure;
using Azure.Storage.Blobs;
using ServcoX.SimpleSharedCache.Exceptions;
using ServcoX.SimpleSharedCache.Utilities;

namespace ServcoX.SimpleSharedCache;

public class SimpleSharedCacheClient : ISimpleSharedCacheClient
{
    private readonly Configuration _configuration;
    private readonly BlobContainerClient _container;
    private readonly ConcurrentDictionary<Type, ConcurrentDictionary<String, Object>> _localCache = new();

    public SimpleSharedCacheClient(String connectionString, Action<Configuration>? builder = null)
    {
        if (String.IsNullOrEmpty(connectionString)) throw new ArgumentException("Cannot be null or empty", nameof(connectionString));

        _configuration = new();
        builder?.Invoke(_configuration);

        _container = GetCreateContainer(connectionString);
    }

    /// <summary>
    /// Store a record for a given key and record type.
    /// </summary>
    public async Task Set<TRecord>(String key, TRecord record, CancellationToken cancellationToken = default) where TRecord : class
    {
        if (String.IsNullOrEmpty(key)) throw new ArgumentException("Cannot be null or empty", nameof(key));

        SetLocalCache(key, record);
        await SetRemoteCache(key, record, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieve a record for a given key and record type. Throws exception if not found.
    /// </summary>
    public async Task<TRecord> Get<TRecord>(String key, CancellationToken cancellationToken = default) where TRecord : class =>
        await TryGet<TRecord>(key, cancellationToken).ConfigureAwait(false) ?? throw new NotFoundException();

    /// <summary>
    /// Retrieve a record for a given key and record type. Returns `null` if not found.
    /// </summary>
    public async Task<TRecord?> TryGet<TRecord>(String key, CancellationToken cancellationToken = default) where TRecord : class
    {
        if (String.IsNullOrEmpty(key)) throw new ArgumentException("Cannot be null or empty", nameof(key));

        var record = TryGetLocalCache<TRecord>(key);
        if (record is not null) return record;

        record = await TryGetRemoteCache<TRecord>(key, cancellationToken).ConfigureAwait(false);
        if (record is not null) SetLocalCache(key, record);

        return record;
    }

    /// <summary>
    /// Retrieve all records for a given record type.
    /// </summary>
    /// <remarks>
    /// This may be performance intensive if there are many records of this type.
    /// </remarks>
    public async Task<IReadOnlyDictionary<String, TRecord>> List<TRecord>(CancellationToken cancellationToken = default) where TRecord : class
    {
        var keys = await GetRemoteCacheKeys<TRecord>(cancellationToken).ConfigureAwait(false);

        var tasks = keys.Select(async key => new
        {
            Key = key,
            Values = await Get<TRecord>(key, cancellationToken).ConfigureAwait(false)
        });

        var records = await Task.WhenAll(tasks).ConfigureAwait(false);

        return records.ToDictionary(record => record.Key, record => record.Values).AsReadOnly();
    }

    private async Task<List<String>> GetRemoteCacheKeys<TRecord>(CancellationToken cancellationToken) where TRecord : class
    {
        var modelPrefix = AddressUtilities.ComputeModelPrefix<TRecord>();
        var blobs = await _container.GetBlobsAsync(prefix: modelPrefix, cancellationToken: cancellationToken).ToList().ConfigureAwait(false);
        return blobs.Select(blob => AddressUtilities.ExtractKeyFromAddress(blob.Name)).ToList();
    }

    private BlobContainerClient GetCreateContainer(String connectionString)
    {
        var service = new BlobServiceClient(connectionString, _configuration.BlobClientOptions);

        try
        {
            return service.CreateBlobContainer(_configuration.ContainerName);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "ContainerAlreadyExists")
        {
            return service.GetBlobContainerClient(_configuration.ContainerName);
        }
    }

    private void SetLocalCache<TRecord>(String key, TRecord record) where TRecord : class
    {
        if (!_localCache.TryGetValue(typeof(TRecord), out var inner)) inner = _localCache[typeof(TRecord)] = new();
        inner[key] = record;
    }

    private TRecord? TryGetLocalCache<TRecord>(String key) where TRecord : class
    {
        if (!_localCache.TryGetValue(typeof(TRecord), out var inner)) return default;
        if (!inner.TryGetValue(key, out var record)) return default;
        return (TRecord?)record;
    }

    private async Task SetRemoteCache<TRecord>(String key, TRecord record, CancellationToken cancellationToken) where TRecord : class
    {
        using var stream = new MemoryStream();
        await JsonSerializer.SerializeAsync(stream, record, _configuration.SerializerOptions, cancellationToken).ConfigureAwait(false);
        stream.Seek(0, SeekOrigin.Begin);

        var address = AddressUtilities.Compute<TRecord>(key);
        var blob = _container.GetBlobClient(address);
        await blob.UploadAsync(stream, true, cancellationToken).ConfigureAwait(false);
    }

    private async Task<TRecord?> TryGetRemoteCache<TRecord>(String key, CancellationToken cancellationToken) where TRecord : class
    {
        var address = AddressUtilities.Compute<TRecord>(key);
        var blob = _container.GetBlobClient(address);

        if (!await blob.ExistsAsync(cancellationToken).ConfigureAwait(false)) return default;
        var download = await blob.DownloadContentAsync(cancellationToken).ConfigureAwait(false);
        // System.Text.Json has a problem with the recommended
#pragma warning disable CA2007
        // ReSharper disable once UseAwaitUsing
        using var stream = download.Value.Content.ToStream();
#pragma warning restore CA2007
        var record = await JsonSerializer.DeserializeAsync<TRecord>(stream, _configuration.SerializerOptions, cancellationToken).ConfigureAwait(false);

        return record;
    }
}