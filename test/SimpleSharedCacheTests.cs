using System.Text;
using System.Text.Json;
using ServcoX.SimpleSharedCache.Test.Fixtures;
using ServcoX.SimpleSharedCache.Test.Records;
using ServcoX.SimpleSharedCache.Utilities;

namespace ServcoX.SimpleSharedCache.Test;

public class SimpleSharedCacheTests
{
    private static readonly TestStruct TestStruct = new()
    {
        A = GenerateId(),
    };

    private static readonly String TestStructSerialised = JsonSerializer.Serialize(TestStruct);

    [Fact]
    public async Task CanSet()
    {
        using var wrapper = new Wrapper();
        var key = GenerateId();
        await wrapper.Sut.Set(key, TestStruct);
        var blobName = AddressUtilities.Compute<TestStruct>(key);

        var blob = wrapper.Container.GetBlobClient(blobName);
        var read = await blob.DownloadContentAsync();
        var raw = Encoding.UTF8.GetString(read.Value.Content.ToArray());
        raw.Should().Be(TestStructSerialised);
    }

    [Fact]
    public async Task CanTryGet()
    {
        using var wrapper = new Wrapper();
        var key = GenerateId();
        var blobName = AddressUtilities.Compute<TestStruct>(key);
        await wrapper.Container.UploadBlobAsync(blobName, new BinaryData(Encoding.UTF8.GetBytes(TestStructSerialised)));
        var read = await wrapper.Sut.TryGet<TestStruct>(key);
        read.Should().BeEquivalentTo(TestStruct);
    }

    [Fact]
    public async Task CanTryGetCached()
    {
        using var wrapper = new Wrapper();
        var key = GenerateId();
        var blobName = AddressUtilities.Compute<TestStruct>(key);
        await wrapper.Container.UploadBlobAsync(blobName, new BinaryData(Encoding.UTF8.GetBytes(TestStructSerialised)));
        await wrapper.Sut.TryGet<TestStruct>(key);

        var blob = wrapper.Container.GetBlobClient(blobName);
        await blob.UploadAsync(new BinaryData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new TestStruct
        {
            A = GenerateId(),
        }))), overwrite: true);

        var read = await wrapper.Sut.TryGet<TestStruct>(key);
        read.Should().BeEquivalentTo(TestStruct);
    }

    [Fact]
    public async Task CanGetNotFound()
    {
        using var wrapper = new Wrapper();
        var key = GenerateId();
        var read = await wrapper.Sut.TryGet<TestStruct>(key);
        read.Should().BeNull();
    }

    [Fact]
    public async Task CanList()
    {
        using var wrapper = new Wrapper();
        var keyA1 = GenerateId();
        await wrapper.Sut.Set(keyA1, new TestStruct { A = keyA1 });

        var keyA2 = GenerateId();
        await wrapper.Sut.Set(keyA2, new TestStruct { A = keyA2 });

        var keyB = GenerateId();
        await wrapper.Sut.Set(keyB, new TestAlternativeStruct { A = keyB });

        var records = await wrapper.Sut.List<TestStruct>();
        records.Count.Should().Be(2);
        records.Should().ContainSingle(a => a.A == keyA1);
        records.Should().ContainSingle(a => a.A == keyA2);
    }

    private static String GenerateId() => Guid.NewGuid().ToString("N");
}