using System.Globalization;

namespace MensaStudio.Core;

/// <summary>
/// Formats Status_Bar text for the Current_Position (Req 17.1).
/// </summary>
public static class StatusFormatter
{
    /// <summary>
    /// Formats the one-based position of the Current_Position over the total Photo count as
    /// <c>"{index + 1} / {count}"</c>, using <see cref="CultureInfo.InvariantCulture"/> so the
    /// integers carry no digit grouping (Req 17.1), e.g. <c>"142 / 1284"</c>.
    /// </summary>
    /// <param name="index">Zero-based index of the Current_Position.</param>
    /// <param name="count">Total number of Photos in the Session.</param>
    public static string Position(int index, int count)
    {
        string one = (index + 1).ToString(CultureInfo.InvariantCulture);
        string total = count.ToString(CultureInfo.InvariantCulture);
        return $"{one} / {total}";
    }
}
