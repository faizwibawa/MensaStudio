using System.IO;

namespace MensaStudio.Core;

/// <summary>
/// Formats the Overlay elements for the Photo at the Current_Position: folder name, star symbols and
/// RAW badge. Each element is returned as <c>null</c> when it should be omitted, so the Overlay shows
/// no placeholder for an absent element (Req 17.4–17.6, 17.9, 17.10).
/// </summary>
public static class OverlayFormatter
{
    private const char StarSymbol = '\u2605'; // ★

    /// <summary>
    /// The Folder_Assignment of <paramref name="e"/> exactly as stored, or <c>null</c> when the Photo
    /// has no Folder_Assignment. The folder name is returned unchanged even when it is not in the
    /// current Folder_Mapping (Req 17.4, 17.9, 17.10).
    /// </summary>
    public static string? FolderText(PhotoEntry e)
    {
        ArgumentNullException.ThrowIfNull(e);
        return e.Folder;
    }

    /// <summary>
    /// A string of star symbols (<c>★</c>) equal in length to <paramref name="rating"/> (1 to 5), or
    /// <c>null</c> when the Photo has no Rating (Req 17.5, 17.10).
    /// </summary>
    public static string? Stars(int? rating)
    {
        if (rating is not { } value || value <= 0)
        {
            return null;
        }

        return new string(StarSymbol, value);
    }

    /// <summary>
    /// A RAW badge listing each distinct RAW_Sidecar extension once, without the leading dot, in upper
    /// case, each prefixed by <c>+</c>, sorted ordinally in ascending order and separated by a single
    /// space (e.g. <c>"+CR2 +DNG"</c>), or <c>null</c> when there are no RAW_Sidecars (Req 17.6, 17.10).
    /// </summary>
    public static string? RawBadge(IReadOnlyList<string> sidecars)
    {
        ArgumentNullException.ThrowIfNull(sidecars);
        if (sidecars.Count == 0)
        {
            return null;
        }

        var extensions = new SortedSet<string>(StringComparer.Ordinal);
        foreach (string sidecar in sidecars)
        {
            string extension = Path.GetExtension(sidecar);
            if (extension.Length <= 1)
            {
                // No extension (or just a bare dot) -> nothing to show for this sidecar.
                continue;
            }

            // Strip the leading dot and upper-case (invariant: extension tokens are ASCII).
            extensions.Add(extension[1..].ToUpperInvariant());
        }

        if (extensions.Count == 0)
        {
            return null;
        }

        return string.Join(' ', extensions.Select(static ext => "+" + ext));
    }
}
