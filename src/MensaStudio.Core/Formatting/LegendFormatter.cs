namespace MensaStudio.Core;

/// <summary>
/// Formats the Legend / Help_Overlay rows: the nine numpad keys 1 to 9 in ascending order, each with
/// its Folder_Mapping folder name, or <c>"unmapped"</c> for an Unmapped_Key (Req 17.7, 18.4).
/// </summary>
public static class LegendFormatter
{
    /// <summary>Indicator shown in place of a folder name for an Unmapped_Key.</summary>
    public const string UnmappedText = "unmapped";

    /// <summary>
    /// Returns nine rows for numpad keys 1 to 9 in ascending order. Each row's text is the folder name
    /// mapped to that key, or <see cref="UnmappedText"/> when the key is unmapped (Req 17.7, 18.4).
    /// </summary>
    public static IReadOnlyList<(int Key, string Text)> Rows(FolderMapping mapping)
    {
        ArgumentNullException.ThrowIfNull(mapping);

        var rows = new (int Key, string Text)[FolderMapping.SlotCount];
        for (int key = 1; key <= FolderMapping.SlotCount; key++)
        {
            string? folder = mapping[key];
            rows[key - 1] = (key, folder ?? UnmappedText);
        }

        return rows;
    }
}
