namespace MensaStudio.Core;

/// <summary>
/// A Photo in the scanned Source_Folder together with its matched RAW_Sidecars (Req 3).
/// </summary>
/// <param name="FileName">The Photo file name (directly inside the Source_Folder).</param>
/// <param name="RawSidecars">
/// The RAW_Sidecar file names associated with this Photo, ordered by Natural_Sort_Order (Req 3.3).
/// </param>
public sealed record ScannedPhoto(string FileName, IReadOnlyList<string> RawSidecars);

/// <summary>
/// The result of scanning a Source_Folder: the ordered Photos with their RAW_Sidecars (Req 2, 3).
/// </summary>
/// <param name="SourceFolder">The full path of the scanned Source_Folder.</param>
/// <param name="Photos">The Photos in ascending Natural_Sort_Order of their file names (Req 2.4).</param>
/// <param name="HasSessionFile">True when a Session_File is present in the Source_Folder.</param>
public sealed record ScanResult(
    string SourceFolder,
    IReadOnlyList<ScannedPhoto> Photos,
    bool HasSessionFile);

/// <summary>
/// Pairs RAW_Sidecars with Photos by Base_Name (Req 3). Pure and independent of enumeration order.
/// </summary>
public static class SidecarMatcher
{
    /// <summary>
    /// Associates each RAW file in <paramref name="rawNames"/> with the Photo in <paramref name="photoNames"/>
    /// whose Base_Name matches, returning the Photos in Natural_Sort_Order with their RAW_Sidecars.
    /// </summary>
    /// <remarks>
    /// <paramref name="photoNames"/> and <paramref name="rawNames"/> are file names directly inside the
    /// Source_Folder. Base_Name is the file name with only its final extension removed
    /// (<see cref="Path.GetFileNameWithoutExtension(string)"/>), compared OrdinalIgnoreCase (Req 3.1).
    /// A Base_Name shared by a png and a jpg/jpeg Photo resolves to the jpg/jpeg Photo (Req 3.2); within a
    /// preference group the first Photo in Natural_Sort_Order wins (Req 3.4). Each Photo's RAW_Sidecars are
    /// in Natural_Sort_Order (Req 3.3). RAW files that match no Photo are discarded (Req 3.5). Because inputs
    /// are sorted first, the output is independent of enumeration order (Req 3.7).
    /// </remarks>
    public static IReadOnlyList<ScannedPhoto> Match(IEnumerable<string> photoNames, IEnumerable<string> rawNames)
    {
        ArgumentNullException.ThrowIfNull(photoNames);
        ArgumentNullException.ThrowIfNull(rawNames);

        // Sort both inputs naturally so the result does not depend on enumeration order (Req 3.7).
        List<string> photos = new(photoNames);
        photos.Sort(NaturalSortComparer.Instance);

        List<string> raws = new(rawNames);
        raws.Sort(NaturalSortComparer.Instance);

        // Build Base_Name -> Photo index, png pass first, then jpg/jpeg pass overriding (Req 3.2, 3.4).
        // Each pass inserts only when the key is absent within that pass, so the first Photo in
        // Natural_Sort_Order wins inside its preference group; the jpg/jpeg pass overwrites png entries.
        Dictionary<string, int> baseNameToPhoto = new(StringComparer.OrdinalIgnoreCase);

        AddPass(photos, baseNameToPhoto, preferJpg: false); // png
        AddPass(photos, baseNameToPhoto, preferJpg: true);  // jpg/jpeg override png

        // Collect RAW_Sidecars per Photo in the natural RAW order (raws is already sorted, Req 3.3).
        List<string>?[] sidecars = new List<string>?[photos.Count];
        foreach (string raw in raws)
        {
            string baseName = Path.GetFileNameWithoutExtension(raw);
            if (baseNameToPhoto.TryGetValue(baseName, out int photoIndex))
            {
                (sidecars[photoIndex] ??= new List<string>()).Add(raw);
            }
            // Unmatched RAW files are discarded (Req 3.5).
        }

        ScannedPhoto[] result = new ScannedPhoto[photos.Count];
        for (int i = 0; i < photos.Count; i++)
        {
            IReadOnlyList<string> photoRaws = sidecars[i] ?? (IReadOnlyList<string>)Array.Empty<string>();
            result[i] = new ScannedPhoto(photos[i], photoRaws);
        }

        return result;
    }

    /// <summary>
    /// Processes the Photos of one preference group (png when <paramref name="preferJpg"/> is false,
    /// jpg/jpeg when true) in Natural_Sort_Order, inserting each Base_Name into <paramref name="map"/> only
    /// when it was not already inserted during this pass. The first Photo in Natural_Sort_Order therefore
    /// wins within its group (Req 3.4), and because the jpg/jpeg pass runs second it overrides png (Req 3.2).
    /// </summary>
    private static void AddPass(List<string> sortedPhotos, Dictionary<string, int> map, bool preferJpg)
    {
        HashSet<string> seenThisPass = new(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < sortedPhotos.Count; i++)
        {
            string name = sortedPhotos[i];
            if (IsJpeg(name) != preferJpg)
            {
                continue;
            }

            string baseName = Path.GetFileNameWithoutExtension(name);
            if (seenThisPass.Add(baseName))
            {
                map[baseName] = i;
            }
        }
    }

    private static bool IsJpeg(string fileName)
    {
        string extension = Path.GetExtension(fileName);
        return extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase);
    }
}
