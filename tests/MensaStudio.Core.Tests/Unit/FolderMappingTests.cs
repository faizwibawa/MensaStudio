using MensaStudio.Core;
using Xunit;

namespace MensaStudio.Core.Tests.Unit;

public class FolderMappingTests
{
    [Fact]
    public void Default_maps_good_okay_bad_and_unsure_with_4_to_8_unmapped()
    {
        var mapping = FolderMapping.Default;

        Assert.Equal("Good", mapping[1]);
        Assert.Equal("Okay", mapping[2]);
        Assert.Equal("Bad", mapping[3]);
        for (var key = 4; key <= 8; key++)
        {
            Assert.Null(mapping[key]);
        }

        Assert.Equal("Unsure", mapping[9]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    public void Indexer_rejects_keys_outside_1_to_9(int key)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => FolderMapping.Default[key]);
    }

    [Fact]
    public void Constructor_rejects_wrong_slot_count()
    {
        Assert.Throws<ArgumentException>(() => new FolderMapping(new string?[8]));
    }

    [Fact]
    public void Equality_is_structural_over_slot_contents()
    {
        var copy = new FolderMapping(new List<string?> { "Good", "Okay", "Bad", null, null, null, null, null, "Unsure" });

        Assert.Equal(FolderMapping.Default, copy);
        Assert.Equal(FolderMapping.Default.GetHashCode(), copy.GetHashCode());
    }

    [Fact]
    public void Equality_is_case_sensitive_and_distinguishes_unmapped()
    {
        var lower = new FolderMapping(new string?[] { "good", "Okay", "Bad", null, null, null, null, null, "Unsure" });
        var mapped4 = new FolderMapping(new string?[] { "Good", "Okay", "Bad", "X", null, null, null, null, "Unsure" });

        Assert.NotEqual(FolderMapping.Default, lower);
        Assert.NotEqual(FolderMapping.Default, mapped4);
    }

    [Fact]
    public void Slots_are_copied_so_later_source_changes_do_not_leak_in()
    {
        var source = new string?[] { "A", null, null, null, null, null, null, null, null };
        var mapping = new FolderMapping(source);

        source[0] = "B";

        Assert.Equal("A", mapping[1]);
    }
}
