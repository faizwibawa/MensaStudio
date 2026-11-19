using System.Text;
using MensaStudio.Core;
using Xunit;

namespace MensaStudio.Core.Tests.Unit;

public class SessionFilePrinterTests
{
    private static readonly SessionRecordSet Sample = new(
        new[]
        {
            new SessionRecord("IMG_0101.jpg", "Good", 5),
            new SessionRecord("IMG_0102.jpg", "Unsure", null),
            new SessionRecord("IMG_0103.jpg", null, 3),
            new SessionRecord("IMG_0104.jpg", null, null),
        },
        "IMG_0103.jpg");

    [Fact]
    public void Print_writes_header_current_and_one_tab_separated_line_per_entry_with_crlf()
    {
        var expected =
            "# MensaStudio session v1\r\n" +
            "#current=IMG_0103.jpg\r\n" +
            "IMG_0101.jpg\tGood\t5\r\n" +
            "IMG_0102.jpg\tUnsure\t\r\n" +
            "IMG_0103.jpg\t\t3\r\n" +
            "IMG_0104.jpg\t\t\r\n";

        Assert.Equal(expected, SessionFilePrinter.Print(Sample));
    }

    [Fact]
    public void Print_omits_current_line_when_current_file_name_is_null()
    {
        var records = new SessionRecordSet(new[] { new SessionRecord("a.jpg", null, null) }, null);

        Assert.Equal("# MensaStudio session v1\r\na.jpg\t\t\r\n", SessionFilePrinter.Print(records));
    }

    [Fact]
    public void Print_of_empty_set_is_header_only()
    {
        Assert.Equal("# MensaStudio session v1\r\n", SessionFilePrinter.Print(SessionRecordSet.Empty));
    }

    [Fact]
    public void PrintBytes_is_utf8_without_bom()
    {
        var records = new SessionRecordSet(new[] { new SessionRecord("Bäume_é.jpg", "Gut", 2) }, "Bäume_é.jpg");

        var bytes = SessionFilePrinter.PrintBytes(records);

        Assert.False(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF);
        Assert.Equal((byte)'#', bytes[0]);
        Assert.Equal(SessionFilePrinter.Print(records), new UTF8Encoding(false, true).GetString(bytes));
    }

    [Fact]
    public void Record_sets_with_equal_entries_are_equal()
    {
        var copy = new SessionRecordSet(Sample.Entries.ToList(), "IMG_0103.jpg");

        Assert.Equal(Sample, copy);
        Assert.Equal(Sample.GetHashCode(), copy.GetHashCode());
        Assert.NotEqual(Sample, copy with { CurrentFileName = "IMG_0101.jpg" });
    }
}
