using System.Text;
using System.Text.Json;
using ServcoX.SimpleSharedCache.Test.Fixtures;
using ServcoX.SimpleSharedCache.Utilities;
using Record = Xunit.Record;

namespace ServcoX.SimpleSharedCache.Test;

public class SimpleSharedCacheTests
{
    private const String DevelopmentConnectionString = "UseDevelopmentStorage=true;";

    private static readonly Records.Record TestRecord = new()
    {
        A = Guid.NewGuid().ToString("N"),
    };

    private static readonly String TestRecordSerialised = JsonSerializer.Serialize(TestRecord);

    [Fact]
    public async Task CanWrite()
    {
        using var wrapper = new Wrapper();
        var key = Guid.NewGuid().ToString("N");
        await wrapper.Sut.Set(key, TestRecord);
        var blobName = AddressUtilities.Compute<Record>(key);

        var blob = wrapper.Container.GetBlobClient(blobName);
        var read = await blob.DownloadContentAsync();
        var raw = Encoding.UTF8.GetString(read.Value.Content.ToArray());
        raw.Should().Be(TestRecordSerialised);
    }

    [Fact]
    public async Task CanRead()
    {
        using var wrapper = new Wrapper();
        var key = Guid.NewGuid().ToString("N");
        var blobName = AddressUtilities.Compute<Record>(key);
        await wrapper.Container.UploadBlobAsync(blobName, new BinaryData(Encoding.UTF8.GetBytes(TestRecordSerialised)));
        var read = await wrapper.Sut.TryGet<Record>(key);
        read.Should().BeEquivalentTo(TestRecord);
    }

    [Fact]
    public async Task CanReadNotFound()
    {
        using var wrapper = new Wrapper();
        var key = Guid.NewGuid().ToString("N");
        var read = await wrapper.Sut.TryGet<Record>(key);
        read.Should().BeNull();
    }
}