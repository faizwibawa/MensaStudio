using FsCheck;
using FsCheck.Fluent;
using MensaStudio.Core;

namespace MensaStudio.Core.Tests.Generators;

/// <summary>
/// Generates photo entries and valid <see cref="SessionRecordSet"/> values for property tests
/// (design Testing Strategy): photo file names are unique per OrdinalIgnoreCase, folders and ratings
/// are optional, and the current file name is chosen from the entries (or null when empty).
/// Folder names come from a small pool with case variants to force sharing and OrdinalIgnoreCase
/// collisions.
/// </summary>
public static class SessionGen
{
    // Small pool with case variants to force folder sharing and case differences in planning tests.
    private static readonly string[] FolderPool =
    {
        "Good", "good", "Okay", "Bad", "bad", "Unsure", "unsure", "Keep", "Reject",
    };

    /// <summary>An optional folder assignment: null or a non-empty name from the pool.</summary>
    public static Gen<string?> OptionalFolder() =>
        Gen.Frequency(
            (1, Gen.Constant<string?>(null)),
            (2, from name in Gen.Elements(FolderPool) select (string?)name));

    /// <summary>An optional rating: null or an integer in 1..5.</summary>
    public static Gen<int?> OptionalRating() =>
        Gen.Frequency(
            (1, Gen.Constant<int?>(null)),
            (2, from r in Gen.Choose(1, 5) select (int?)r));

    /// <summary>A list of distinct photo file names (OrdinalIgnoreCase), length 0..<paramref name="maxCount"/>.</summary>
    public static Gen<IReadOnlyList<string>> DistinctPhotoNames(int maxCount = 8) =>
        from count in Gen.Choose(0, maxCount)
        from raw in Gen.ArrayOf(FileNameGen.PhotoName(), count)
        select Deduplicate(raw);

    /// <summary>A photo entry with the given name, optional folder/rating and 0..2 RAW sidecars.</summary>
    public static Gen<PhotoEntry> PhotoEntryFor(string fileName) =>
        from folder in OptionalFolder()
        from rating in OptionalRating()
        from sidecarCount in Gen.Choose(0, 2)
        from sidecars in Gen.ArrayOf(FileNameGen.RawName(), sidecarCount)
        select new PhotoEntry(fileName, sidecars, folder, rating);

    /// <summary>A list of photo entries with unique names (OrdinalIgnoreCase), length 0..<paramref name="maxCount"/>.</summary>
    public static Gen<IReadOnlyList<PhotoEntry>> PhotoEntries(int maxCount = 8) =>
        from names in DistinctPhotoNames(maxCount)
        from folders in Gen.ArrayOf(OptionalFolder(), names.Count)
        from ratings in Gen.ArrayOf(OptionalRating(), names.Count)
        from sidecarCounts in Gen.ArrayOf(Gen.Choose(0, 2), names.Count)
        from sidecarPool in Gen.ArrayOf(FileNameGen.RawName(), names.Count * 2)
        select (IReadOnlyList<PhotoEntry>)BuildEntries(names, folders, ratings, sidecarCounts, sidecarPool);

    /// <summary>A valid <see cref="SessionRecord"/> list with unique names (OrdinalIgnoreCase).</summary>
    public static Gen<IReadOnlyList<SessionRecord>> SessionRecords(int maxCount = 8) =>
        from names in DistinctPhotoNames(maxCount)
        from folders in Gen.ArrayOf(OptionalFolder(), names.Count)
        from ratings in Gen.ArrayOf(OptionalRating(), names.Count)
        select (IReadOnlyList<SessionRecord>)BuildRecords(names, folders, ratings);

    /// <summary>A valid <see cref="SessionRecordSet"/>: unique entries with the current chosen from them.</summary>
    public static Gen<SessionRecordSet> SessionRecordSet() =>
        from records in SessionRecords()
        from current in CurrentFrom(records)
        select new SessionRecordSet(records, current);

    /// <summary>Arbitrary for <see cref="SessionRecordSet"/>, for direct FsCheck registration.</summary>
    public static Arbitrary<SessionRecordSet> SessionRecordSetArb() => Arb.From(SessionRecordSet());

    /// <summary>Arbitrary for a list of unique <see cref="PhotoEntry"/> values, wrapped for registration.</summary>
    public static Arbitrary<PhotoEntryList> PhotoEntryListArb() =>
        Arb.From(from entries in PhotoEntries() select new PhotoEntryList(entries));

    private static IReadOnlyList<string> Deduplicate(string[] names)
    {
        // Keep the first occurrence of each name under OrdinalIgnoreCase; names that collide with an
        // already-kept name are appended with a numeric suffix so the count stays close to the draw.
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<string>(names.Length);
        foreach (var name in names)
        {
            var candidate = name;
            var attempt = 1;
            while (!seen.Add(candidate))
            {
                candidate = InsertSuffix(name, attempt++);
            }

            result.Add(candidate);
        }

        return result;
    }

    private static string InsertSuffix(string fileName, int n)
    {
        var dot = fileName.LastIndexOf('.');
        return dot < 0
            ? fileName + "_" + n
            : fileName[..dot] + "_" + n + fileName[dot..];
    }

    private static List<PhotoEntry> BuildEntries(
        IReadOnlyList<string> names,
        string?[] folders,
        int?[] ratings,
        int[] sidecarCounts,
        string[] sidecarPool)
    {
        var result = new List<PhotoEntry>(names.Count);
        var pool = 0;
        for (var i = 0; i < names.Count; i++)
        {
            var take = sidecarCounts[i];
            var sidecars = new List<string>(take);
            for (var s = 0; s < take && pool < sidecarPool.Length; s++, pool++)
            {
                sidecars.Add(sidecarPool[pool]);
            }

            result.Add(new PhotoEntry(names[i], sidecars, folders[i], ratings[i]));
        }

        return result;
    }

    private static List<SessionRecord> BuildRecords(IReadOnlyList<string> names, string?[] folders, int?[] ratings)
    {
        var result = new List<SessionRecord>(names.Count);
        for (var i = 0; i < names.Count; i++)
        {
            result.Add(new SessionRecord(names[i], folders[i], ratings[i]));
        }

        return result;
    }

    private static Gen<string?> CurrentFrom(IReadOnlyList<SessionRecord> records)
    {
        if (records.Count == 0)
        {
            return Gen.Constant<string?>(null);
        }

        return Gen.Frequency(
            (1, Gen.Constant<string?>(null)),
            (4, from i in Gen.Choose(0, records.Count - 1) select (string?)records[i].FileName));
    }

}

/// <summary>A list of photo entries with unique names (wrapper for FsCheck registration).</summary>
public sealed record PhotoEntryList(IReadOnlyList<PhotoEntry> Entries);

/// <summary>
/// Registers the custom generators with FsCheck. Attach to a property via
/// <c>[Property(Arbitrary = new[] { typeof(PhotoArbitraries) })]</c>.
/// </summary>
public static class PhotoArbitraries
{
    public static Arbitrary<PhotoFileName> PhotoFileNames() => FileNameGen.PhotoFileName();

    public static Arbitrary<RawFileName> RawFileNames() => FileNameGen.RawFileName();

    public static Arbitrary<SessionRecordSet> SessionRecordSets() => SessionGen.SessionRecordSetArb();

    public static Arbitrary<PhotoEntryList> PhotoEntryLists() => SessionGen.PhotoEntryListArb();
}
