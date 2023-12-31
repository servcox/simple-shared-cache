﻿using System.Text.Json;
using Azure;
using Azure.Storage.Blobs;
using ServcoX.SimpleSharedCache.Exceptions;
using ServcoX.SimpleSharedCache.Utilities;

namespace ServcoX.SimpleSharedCache;

public sealed class SimpleSharedCacheClient : ISimpleSharedCacheClient
{
    private readonly Configuration _configuration;
    private readonly BlobContainerClient _container;

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

        var address = AddressUtilities.Compute<TRecord>(key);
        var blob = _container.GetBlobClient(address);

        using var stream = new MemoryStream();
        await JsonSerializer.SerializeAsync(stream, record, _configuration.SerializerOptions, cancellationToken).ConfigureAwait(false);
        stream.Seek(0, SeekOrigin.Begin);
        await blob.UploadAsync(stream, true, cancellationToken).ConfigureAwait(false);
    }

    public async Task<TRecord> Get<TRecord>(String key, CancellationToken cancellationToken = default)
    {
        if (String.IsNullOrEmpty(key)) throw new ArgumentException("Cannot be null or empty", nameof(key));

        var value = await TryGet<TRecord>(key, cancellationToken).ConfigureAwait(false);
        if (value is null) throw new NotFoundException();
        return value;
    }

    public async Task<TRecord?> TryGet<TRecord>(String key, CancellationToken cancellationToken = default)
    {
        if (String.IsNullOrEmpty(key)) throw new ArgumentException("Cannot be null or empty", nameof(key));

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

    public async Task<IReadOnlyList<TRecord>> List<TRecord>(CancellationToken cancellationToken = default)
    {
        var modelPrefix = AddressUtilities.ComputeModelPrefix<TRecord>();
        var blobs = _container.GetBlobsAsync(prefix: modelPrefix, cancellationToken: cancellationToken);

        var output = new List<TRecord>();
        await foreach (var blob in blobs)
        {
            var key = AddressUtilities.ExtractKeyFromAddress(blob.Name);
            output.Add(await Get<TRecord>(key, cancellationToken).ConfigureAwait(false));
        }

        return output;
    }
}