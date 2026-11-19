namespace MensaStudio.Core;

/// <summary>One Session_File entry line: Photo file name, Folder_Assignment and Rating (Req 10.1).</summary>
/// <param name="FileName">The Photo file name (non-empty, no TAB/CR/LF).</param>
/// <param name="Folder">The assigned folder name, or null when unassigned.</param>
/// <param name="Rating">The Rating 1..5, or null when unrated.</param>
public sealed record SessionRecord(string FileName, string? Folder, int? Rating);

/// <summary>
/// The persisted form of a Session: entry records in Session order plus the Current_Position file name
/// (Req 10). Equality is structural: two sets are equal when their entries are equal element by element
/// and their current file names are equal (ordinal).
/// </summary>
/// <param name="Entries">Entry records in Session order.</param>
/// <param name="CurrentFileName">File name of the Photo at the Current_Position, or null when there is none.</param>
public sealed record SessionRecordSet(IReadOnlyList<SessionRecord> Entries, string? CurrentFileName)
{
    /// <summary>An empty record set with no current file name.</summary>
    public static SessionRecordSet Empty { get; } = new(Array.Empty<SessionRecord>(), null);

    public bool Equals(SessionRecordSet? other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return other is not null
            && string.Equals(CurrentFileName, other.CurrentFileName, StringComparison.Ordinal)
            && Entries.SequenceEqual(other.Entries);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(CurrentFileName, StringComparer.Ordinal);
        foreach (var entry in Entries)
        {
            hash.Add(entry);
        }

        return hash.ToHashCode();
    }
}
