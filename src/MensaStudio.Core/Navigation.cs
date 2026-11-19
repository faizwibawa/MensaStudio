namespace MensaStudio.Core;

/// <summary>
/// Result of a <see cref="Navigation.NextUnassigned"/> search (Req 6.5, 6.6, 6.8).
/// </summary>
public enum NextUnassignedOutcome
{
    /// <summary>An unassigned photo other than the current one was found; the index moved there (Req 6.5).</summary>
    Moved,

    /// <summary>Every photo has a Folder_Assignment; the index is unchanged (Req 6.6).</summary>
    AllAssigned,

    /// <summary>The current photo is the only unassigned photo; the index is unchanged (Req 6.8).</summary>
    OnlyCurrentUnassigned,
}

/// <summary>
/// Pure Current_Position arithmetic for the Viewer (Req 6). All methods are index-only and
/// side-effect free; the caller applies the returned index to the <see cref="Session"/>.
/// </summary>
public static class Navigation
{
    /// <summary>
    /// Moves one photo forward, clamping at the last photo (Req 6.1, 6.7).
    /// Returns <paramref name="index"/> unchanged when already at the last photo or when there are no photos.
    /// </summary>
    public static int Next(int index, int count)
    {
        if (count <= 0)
        {
            return index;
        }

        return index + 1 >= count ? count - 1 : index + 1;
    }

    /// <summary>
    /// Moves one photo back, clamping at the first photo (Req 6.2, 6.7).
    /// Returns <paramref name="index"/> unchanged when already at the first photo or when there are no photos.
    /// </summary>
    public static int Previous(int index, int count)
    {
        if (count <= 0)
        {
            return index;
        }

        return index - 1 <= 0 ? 0 : index - 1;
    }

    /// <summary>Returns the index of the first photo, or -1 when there are no photos (Req 6.3).</summary>
    public static int First(int count) => count <= 0 ? -1 : 0;

    /// <summary>Returns the index of the last photo, or -1 when there are no photos (Req 6.4).</summary>
    public static int Last(int count) => count <= 0 ? -1 : count - 1;

    /// <summary>
    /// Cyclically searches for the first photo without a Folder_Assignment, starting at
    /// <paramref name="current"/> + 1 through the last photo, then wrapping to the first photo
    /// through <paramref name="current"/> - 1 (Req 6.5).
    /// </summary>
    /// <returns>
    /// <see cref="NextUnassignedOutcome.Moved"/> with the index of that photo when one is found;
    /// otherwise <paramref name="current"/> unchanged with
    /// <see cref="NextUnassignedOutcome.AllAssigned"/> when the current photo is assigned (Req 6.6)
    /// or <see cref="NextUnassignedOutcome.OnlyCurrentUnassigned"/> when it is not (Req 6.8).
    /// </returns>
    public static (NextUnassignedOutcome Outcome, int Index) NextUnassigned(IReadOnlyList<PhotoEntry> photos, int current)
    {
        ArgumentNullException.ThrowIfNull(photos);

        int count = photos.Count;
        if (count == 0 || current < 0 || current >= count)
        {
            return (NextUnassignedOutcome.AllAssigned, current);
        }

        // Scan current+1 … count-1, then 0 … current-1 (every photo except the current one).
        for (int offset = 1; offset < count; offset++)
        {
            int index = (current + offset) % count;
            if (photos[index].Folder is null)
            {
                return (NextUnassignedOutcome.Moved, index);
            }
        }

        // No other unassigned photo: distinguish the current photo's own state.
        return photos[current].Folder is null
            ? (NextUnassignedOutcome.OnlyCurrentUnassigned, current)
            : (NextUnassignedOutcome.AllAssigned, current);
    }
}
