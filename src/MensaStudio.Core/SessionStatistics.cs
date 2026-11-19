namespace MensaStudio.Core;

/// <summary>Recomputes the Status_Bar counts over a list of photos (Req 17.3).</summary>
public static class SessionStatistics
{
    /// <summary>
    /// Assigned = photos with a Folder_Assignment, Unassigned = photos without one (Assigned + Unassigned = count),
    /// Rated = photos with a Rating.
    /// </summary>
    public static (int Assigned, int Unassigned, int Rated) Compute(IReadOnlyList<PhotoEntry> photos)
    {
        ArgumentNullException.ThrowIfNull(photos);

        int assigned = 0;
        int rated = 0;
        foreach (var photo in photos)
        {
            if (photo.Folder is not null)
            {
                assigned++;
            }

            if (photo.Rating is not null)
            {
                rated++;
            }
        }

        return (assigned, photos.Count - assigned, rated);
    }
}
