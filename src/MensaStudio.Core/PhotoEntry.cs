namespace MensaStudio.Core;

/// <summary>
/// One Photo in a Session: its file name, matched RAW sidecars (natural order), Folder_Assignment and Rating.
/// Equality is structural, including the sidecar list (ordinal, element by element).
/// </summary>
/// <param name="FileName">The Photo file name.</param>
/// <param name="RawSidecars">Matched RAW sidecar file names in natural order.</param>
/// <param name="Folder">The assigned folder name (non-empty), or null when unassigned.</param>
/// <param name="Rating">The Rating 1..5, or null when unrated.</param>
public sealed record PhotoEntry(string FileName, IReadOnlyList<string> RawSidecars, string? Folder, int? Rating)
{
    public bool Equals(PhotoEntry? other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return other is not null
            && string.Equals(FileName, other.FileName, StringComparison.Ordinal)
            && string.Equals(Folder, other.Folder, StringComparison.Ordinal)
            && Rating == other.Rating
            && RawSidecars.SequenceEqual(other.RawSidecars, StringComparer.Ordinal);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(FileName, StringComparer.Ordinal);
        hash.Add(Folder, StringComparer.Ordinal);
        hash.Add(Rating);
        foreach (var sidecar in RawSidecars)
        {
            hash.Add(sidecar, StringComparer.Ordinal);
        }

        return hash.ToHashCode();
    }
}
