using System.Globalization;
using System.Text;

namespace MensaStudio.Core;

/// <summary>
/// Formats a <see cref="SessionRecordSet"/> as Session_File text (Req 10.1, 10.2, 10.3):
/// the header line, a <c>#current=</c> line (omitted when <see cref="SessionRecordSet.CurrentFileName"/> is null),
/// then one <c>name TAB folder TAB rating</c> line per entry, every line terminated by CR LF.
/// </summary>
public static class SessionFilePrinter
{
    /// <summary>The first line of every Session_File (Req 10.2).</summary>
    public const string HeaderLine = "# MensaStudio session v1";

    /// <summary>Prefix of the Current_Position line (Req 10.2).</summary>
    public const string CurrentPrefix = "#current=";

    private const string LineEnding = "\r\n";

    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    /// <summary>Returns the Session_File text with CR LF line endings, including after the last line.</summary>
    public static string Print(SessionRecordSet records)
    {
        ArgumentNullException.ThrowIfNull(records);
        ArgumentNullException.ThrowIfNull(records.Entries);

        var builder = new StringBuilder(64 + records.Entries.Count * 32);
        builder.Append(HeaderLine).Append(LineEnding);

        if (records.CurrentFileName is not null)
        {
            builder.Append(CurrentPrefix).Append(records.CurrentFileName).Append(LineEnding);
        }

        foreach (var entry in records.Entries)
        {
            ArgumentNullException.ThrowIfNull(entry, nameof(records));

            builder.Append(entry.FileName)
                .Append('\t')
                .Append(entry.Folder ?? string.Empty)
                .Append('\t');

            if (entry.Rating is int rating)
            {
                builder.Append(rating.ToString(CultureInfo.InvariantCulture));
            }

            builder.Append(LineEnding);
        }

        return builder.ToString();
    }

    /// <summary>Returns the Session_File content encoded as UTF-8 without a byte order mark (Req 10.3).</summary>
    public static byte[] PrintBytes(SessionRecordSet records) => Utf8NoBom.GetBytes(Print(records));
}
