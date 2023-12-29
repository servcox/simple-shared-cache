using System.Text;
using System.Text.Json;
using ServcoX.SimpleSharedCache.Test.Fixtures;
using ServcoX.SimpleSharedCache.Test.Records;
using ServcoX.SimpleSharedCache.Utilities;

namespace ServcoX.SimpleSharedCache.Test;

public class SimpleSharedCacheTests
{
    private const String DevelopmentConnectionString = "UseDevelopmentStorage=true;";

    private static readonly Records.TestRecord TestRecord = new()
    {
        A = GenerateId(),
    };


    private static readonly String TestRecordSerialised = JsonSerializer.Serialize(TestRecord);

    [Fact]
    public async Task CanWrite()
    {
        using var wrapper = new Wrapper();
        var key = GenerateId();
        await wrapper.Sut.Set(key, TestRecord);
        var blobName = AddressUtilities.Compute<Records.TestRecord>(key);

        var blob = wrapper.Container.GetBlobClient(blobName);
        var read = await blob.DownloadContentAsync();
        var raw = Encoding.UTF8.GetString(read.Value.Content.ToArray());
        raw.Should().Be(TestRecordSerialised);
    }

    [Fact]
    public async Task CanRead()
    {
        using var wrapper = new Wrapper();
        var key = GenerateId();
        var blobName = AddressUtilities.Compute<Records.TestRecord>(key);
        await wrapper.Container.UploadBlobAsync(blobName, new BinaryData(Encoding.UTF8.GetBytes(TestRecordSerialised)));
        var read = await wrapper.Sut.TryGet<Records.TestRecord>(key);
        read.Should().BeEquivalentTo(TestRecord);
    }

    [Fact]
    public async Task CanReadNotFound()
    {
        using var wrapper = new Wrapper();
        var key = GenerateId();
        var read = await wrapper.Sut.TryGet<Records.TestRecord>(key);
        read.Should().BeNull();
    }

    [Fact]
    public async Task CanList()
    {
        using var wrapper = new Wrapper();
        var keyA1 = GenerateId();
        await wrapper.Sut.Set(keyA1, new TestRecord { A = keyA1 });

        var keyA2 = GenerateId();
        await wrapper.Sut.Set(keyA2, new TestRecord { A = keyA2 });

        var keyB = GenerateId();
        await wrapper.Sut.Set(keyB, new TestAlternativeRecord { A = keyB });

        var records = await wrapper.Sut.List<TestRecord>();
        records.Count.Should().Be(2);
        records.Should().ContainSingle(a => a.A == keyA1);
        records.Should().ContainSingle(a => a.A == keyA2);
    }

    private static String GenerateId() => Guid.NewGuid().ToString("N");
}