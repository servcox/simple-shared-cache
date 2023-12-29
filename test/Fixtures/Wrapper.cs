using Azure.Storage.Blobs;

namespace ServcoX.SimpleSharedCache.Test.Fixtures;

public class Wrapper : IDisposable
{
    private const String DevelopmentConnectionString = "UseDevelopmentStorage=true;";
    
    public String Postfix { get; }
    public BlobContainerClient Container { get; }
    public ISimpleSharedCache Sut { get; }

    public Wrapper()
    {
        Postfix = Guid.NewGuid().ToString("N").ToUpperInvariant();
        Sut = new(DevelopmentConnectionString, Postfix);

        var service = new BlobServiceClient(DevelopmentConnectionString);
        Container = service.GetBlobContainerClient($"cache{Postfix.ToLowerInvariant()}");
    }

    public void Dispose()
    {
        Container.Delete();
    }
}