using System.Text.Json;
using System.Text.Json.Serialization;
using Azure;
using Azure.Core;
using Azure.Storage.Blobs;
using ServcoX.SimpleSharedCache.Exceptions;
using ServcoX.SimpleSharedCache.Utilities;

namespace ServcoX.SimpleSharedCache;

public sealed class SimpleSharedCacheClient : ISimpleSharedCacheClient
{
    private readonly BlobContainerClient _container;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
    };

    private static readonly BlobClientOptions BlobClientOptions = new()
    {
        Retry =
        {
            Mode = RetryMode.Exponential,
            Delay = TimeSpan.FromSeconds(0.5),
            MaxRetries = 4,
        },
    };

    public SimpleSharedCacheClient(String connectionString, String? postfix = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(connectionString, nameof(connectionString));
        
        var service = new BlobServiceClient(connectionString, BlobClientOptions);

        var containerName = postfix is null ? "cache" : $"cache{postfix.ToLowerInvariant()}";
        try
        {
            _container = service.CreateBlobContainer(containerName);
        }
        catch (RequestFailedException ex)when (ex.ErrorCode == "ContainerAlreadyExists")
        {
            _container = service.GetBlobContainerClient(containerName);
        }
    }

    public async Task Set<T>(String key, T value, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(key, nameof(key));
        
        var address = AddressUtilities.Compute<T>(key);
        var blob = _container.GetBlobClient(address);

        using var stream = new MemoryStream();
        await JsonSerializer.SerializeAsync(stream, value, SerializerOptions, cancellationToken).ConfigureAwait(false);
        stream.Seek(0, SeekOrigin.Begin);
        await blob.UploadAsync(stream, true, cancellationToken).ConfigureAwait(false);
    }

    public async Task<T> Get<T>(String key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(key, nameof(key));
        
        var value = await TryGet<T>(key, cancellationToken).ConfigureAwait(false);
        if (value is null) throw new NotFoundException();
        return value;
    }

    public async Task<T?> TryGet<T>(String key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(key, nameof(key));
        
        var address = AddressUtilities.Compute<T>(key);
        var blob = _container.GetBlobClient(address);

        if (!await blob.ExistsAsync(cancellationToken).ConfigureAwait(false)) return default;
        var download = await blob.DownloadContentAsync(cancellationToken).ConfigureAwait(false);
        await using var stream = download.Value.Content.ToStream().ConfigureAwait(false);
        return await JsonSerializer.DeserializeAsync<T>(stream, SerializerOptions, cancellationToken);
    }
}