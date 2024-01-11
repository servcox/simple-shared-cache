using System.Collections.Concurrent;
using System.Text.Json;
using Azure;
using Azure.Storage.Blobs;
using ServcoX.SimpleSharedCache.Exceptions;
using ServcoX.SimpleSharedCache.Utilities;

namespace ServcoX.SimpleSharedCache;

public sealed class SimpleSharedCacheClient : ISimpleSharedCacheClient
{
    private readonly Configuration _configuration;
    private readonly BlobContainerClient _container;
    private readonly ConcurrentDictionary<Type, ConcurrentDictionary<String, Object>> _localCache = new();

    public SimpleSharedCacheClient(String connectionString, Action<Configuration>? builder = null)
    {
        if (String.IsNullOrEmpty(connectionString)) throw new ArgumentException("Cannot be null or empty", nameof(connectionString));

        _configuration = new();
        builder?.Invoke(_configuration);

        var service = new BlobServiceClient(connectionString, _configuration.BlobClientOptions);

        try
        {
            _container = service.CreateBlobContainer(_configuration.ContainerName);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "ContainerAlreadyExists")
        {
            _container = service.GetBlobContainerClient(_configuration.ContainerName);
        }
    }

    public async Task Set<TRecord>(String key, TRecord record, CancellationToken cancellationToken = default)
    {
        if (String.IsNullOrEmpty(key)) throw new ArgumentException("Cannot be null or empty", nameof(key));

        SetLocalCache(key, record);

        using var stream = new MemoryStream();
        await JsonSerializer.SerializeAsync(stream, record, _configuration.SerializerOptions, cancellationToken).ConfigureAwait(false);
        stream.Seek(0, SeekOrigin.Begin);

        var address = AddressUtilities.Compute<TRecord>(key);
        var blob = _container.GetBlobClient(address);
        await blob.UploadAsync(stream, true, cancellationToken).ConfigureAwait(false);
    }

    public async Task<TRecord> Get<TRecord>(String key, CancellationToken cancellationToken = default)
    {
        if (String.IsNullOrEmpty(key)) throw new ArgumentException("Cannot be null or empty", nameof(key));

        return await TryGet<TRecord>(key, cancellationToken).ConfigureAwait(false) ?? throw new NotFoundException();
    }

    public async Task<TRecord?> TryGet<TRecord>(String key, CancellationToken cancellationToken = default)
    {
        if (String.IsNullOrEmpty(key)) throw new ArgumentException("Cannot be null or empty", nameof(key));

        var r = TryGetLocalCache<TRecord>(key);
        if (r is not null) return r;

        var address = AddressUtilities.Compute<TRecord>(key);
        var blob = _container.GetBlobClient(address);

        if (!await blob.ExistsAsync(cancellationToken).ConfigureAwait(false)) return default;
        var download = await blob.DownloadContentAsync(cancellationToken).ConfigureAwait(false);
        // System.Text.Json has a problem with the recommended
#pragma warning disable CA2007
        // ReSharper disable once UseAwaitUsing
        using var stream = download.Value.Content.ToStream();
#pragma warning restore CA2007
        var record = await JsonSerializer.DeserializeAsync<TRecord>(stream, _configuration.SerializerOptions, cancellationToken).ConfigureAwait(false)!;
        SetLocalCache(key, record);

        return record;
    }

    public async Task<IReadOnlyList<TRecord>> List<TRecord>(CancellationToken cancellationToken = default)
    {
        var modelPrefix = AddressUtilities.ComputeModelPrefix<TRecord>();
        var blobs = await _container.GetBlobsAsync(prefix: modelPrefix, cancellationToken: cancellationToken).ToList().ConfigureAwait(false);

        var records = await Task.WhenAll(blobs.Select(blob =>
        {
            var key = AddressUtilities.ExtractKeyFromAddress(blob.Name);
            return Get<TRecord>(key, cancellationToken);
        })).ConfigureAwait(false);

        return records;
    }

    private TRecord? TryGetLocalCache<TRecord>(String key)
    {
        if (!_localCache.TryGetValue(typeof(TRecord), out var inner)) return default;
        if (!inner.TryGetValue(key, out var record)) return default;
        return (TRecord)record;
    }

    private void SetLocalCache<TRecord>(String key, TRecord record)
    {
        if (!_localCache.TryGetValue(typeof(TRecord), out var inner)) inner = _localCache[typeof(TRecord)] = new();
        inner[key] = record!;
    }
}