using ServcoX.SimpleSharedCache.Test.Records;
using ServcoX.SimpleSharedCache.Utilities;
using Record = Xunit.Record;

namespace ServcoX.SimpleSharedCache.Test;

public class AddressUtilitiesTests
{
    [Fact]
    public void CanMatchSelf() => AddressUtilities.Compute<Record>("1").Should().BeEquivalentTo(AddressUtilities.Compute<Record>("1"));

    [Fact]
    public void CanDetectDifferentKey() => AddressUtilities.Compute<Record>("1").Should().NotBeEquivalentTo(AddressUtilities.Compute<Record>("2"));

    [Fact]
    public void CanDetectFieldAddition() => AddressUtilities.Compute<Record>("1").Should().NotBeEquivalentTo(AddressUtilities.Compute<TestRecordAddition>("1"));

    [Fact]
    public void CanDetectFieldReplacement() => AddressUtilities.Compute<Record>("1").Should().NotBeEquivalentTo(AddressUtilities.Compute<TestRecordReplacement>("1"));

    [Fact]
    public void CanDetectFieldSubtraction() => AddressUtilities.Compute<Record>("1").Should().NotBeEquivalentTo(AddressUtilities.Compute<TestRecordSubtraction>("1"));
}