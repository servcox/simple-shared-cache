using ServcoX.SimpleSharedCache.Test.Records;
using ServcoX.SimpleSharedCache.Utilities;
using Record = Xunit.Record;

namespace ServcoX.SimpleSharedCache.Test;

public class AddressUtilitiesTests
{
    [Fact]
    public void CanMatchSelf() => AddressUtilities.Compute<Record>("1").Should().BeEquivalentTo(AddressUtilities.Compute<Record>("1"));

    [Fact]
    public void CanDetectDifferentKey() => AddressUtilities.Compute<Record>("1").Should().BeEquivalentTo(AddressUtilities.Compute<Record>("2"));

    [Fact]
    public void CanDetectFieldAddition() => AddressUtilities.Compute<Record>("1").Should().NotBeEquivalentTo(AddressUtilities.Compute<RecordAddition>("1"));

    [Fact]
    public void CanDetectFieldReplacement() => AddressUtilities.Compute<Record>("1").Should().NotBeEquivalentTo(AddressUtilities.Compute<RecordReplacement>("1"));

    [Fact]
    public void CanDetectFieldSubtraction() => AddressUtilities.Compute<Record>("1").Should().NotBeEquivalentTo(AddressUtilities.Compute<RecordSubtraction>("1"));
}