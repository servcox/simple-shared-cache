using Azure.Storage.Blobs;

namespace ServcoX.SimpleSharedCache.Test.Fixtures;

public sealed class Wrapper : IDisposable
{
    private const String DevelopmentConnectionString = "UseDevelopmentStorage=true;";

    public BlobContainerClient Container { get; }
    public ISimpleSharedCacheClient Sut { get; }

    public Wrapper()
    {
        var containerName = $"cache-{Guid.NewGuid().ToString("N").ToLowerInvariant()}";
        Sut = new SimpleSharedCacheClient(DevelopmentConnectionString, cfg => cfg
            .UseContainerName(containerName)
        );

        var service = new BlobServiceClient(DevelopmentConnectionString);
        Container = service.GetBlobContainerClient(containerName);
    }

    public void Dispose()
    {
        Container.Delete();
    }
}