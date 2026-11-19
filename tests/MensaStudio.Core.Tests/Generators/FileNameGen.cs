using FsCheck;
using FsCheck.Fluent;

namespace MensaStudio.Core.Tests.Generators;

/// <summary>
/// Generates Windows-valid photo/RAW file names for property tests (design Testing Strategy).
/// Names use only Windows-valid characters (mixed-case letters, ASCII digits, <c>_ - . ( ) # =</c>
/// and non-ASCII letters such as <c>é ß ü</c>), never end in a space or period, are biased toward
/// rating prefixes (<c>3_…</c>, <c>3_3_…</c>, <c>0_…</c>, <c>6_…</c>), include digit runs with leading
/// zeros and existing <c> (n)</c> conflict suffixes, and carry photo or RAW extensions in random case.
/// </summary>
public static class FileNameGen
{
    // Characters valid in a Windows file-name stem. Excludes the invalid set \ / : * ? " &lt; &gt; |
    // and includes the design's special characters and a few non-ASCII letters.
    private static readonly char[] StemChars =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_-()#= éßüÉÜ".ToCharArray();

    private static readonly string[] PhotoExtensions = { ".jpg", ".jpeg", ".png" };

    private static readonly string[] RawExtensions =
    {
        ".cr2", ".cr3", ".nef", ".arw", ".raf", ".orf", ".rw2", ".dng", ".raw", ".pef", ".srw",
    };

    private static readonly string[] RatingPrefixes =
    {
        "", "", "", // bias toward no prefix
        "1_", "2_", "3_", "4_", "5_",
        "0_", "6_",   // out-of-range digits (Req 9.1 boundaries)
        "3_3_",       // stacked prefix (Req 9.2)
    };

    /// <summary>A single random letter-case variant of <paramref name="extension"/> (e.g. <c>.JpG</c>).</summary>
    public static Gen<string> RandomCaseExtension(string extension) =>
        from flips in Gen.ArrayOf(Gen.Elements(true, false), extension.Length)
        select RandomizeCase(extension, flips);

    /// <summary>A stem: 1..12 valid characters with no trailing space or period.</summary>
    public static Gen<string> Stem() =>
        from length in Gen.Choose(1, 12)
        from chars in Gen.ArrayOf(Gen.Elements(StemChars), length)
        select Trim(new string(chars));

    /// <summary>A digit run with a random number of leading zeros, e.g. <c>007</c> or <c>0042</c>.</summary>
    public static Gen<string> DigitRun() =>
        from zeros in Gen.Choose(0, 4)
        from value in Gen.Choose(0, 99999)
        select new string('0', zeros) + value.ToString(System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>An optional <c> (n)</c> conflict suffix for n in 1..9, or the empty string.</summary>
    public static Gen<string> ConflictSuffix() =>
        Gen.Frequency(
            (3, Gen.Constant(string.Empty)),
            (1, from n in Gen.Choose(1, 9) select $" ({n})"));

    /// <summary>A photo file name with a photo extension in random case.</summary>
    public static Gen<string> PhotoName() => NameWithExtensions(PhotoExtensions);

    /// <summary>A RAW file name with a RAW extension in random case.</summary>
    public static Gen<string> RawName() => NameWithExtensions(RawExtensions);

    /// <summary>A file name that is either a photo or a RAW name.</summary>
    public static Gen<string> AnyName() =>
        Gen.OneOf(PhotoName(), RawName());

    /// <summary>Arbitrary for <see cref="PhotoName"/> as a distinct struct wrapper for registration.</summary>
    public static Arbitrary<PhotoFileName> PhotoFileName() =>
        Arb.From(from name in PhotoName() select new PhotoFileName(name));

    /// <summary>Arbitrary for <see cref="RawName"/> as a distinct struct wrapper for registration.</summary>
    public static Arbitrary<RawFileName> RawFileName() =>
        Arb.From(from name in RawName() select new RawFileName(name));

    private static Gen<string> NameWithExtensions(string[] extensions) =>
        from prefix in Gen.Elements(RatingPrefixes)
        from body in BodyGen()
        from suffix in ConflictSuffix()
        from extension in Gen.Elements(extensions)
        from cased in RandomCaseExtension(extension)
        select Trim(prefix + body) + suffix + cased;

    private static Gen<string> BodyGen() =>
        from stem in Stem()
        from includeDigits in Gen.Elements(true, false)
        from digits in DigitRun()
        select includeDigits ? stem + "_" + digits : stem;

    private static string RandomizeCase(string value, bool[] upperFlags)
    {
        var chars = value.ToCharArray();
        for (var i = 0; i < chars.Length && i < upperFlags.Length; i++)
        {
            chars[i] = upperFlags[i] ? char.ToUpperInvariant(chars[i]) : char.ToLowerInvariant(chars[i]);
        }

        return new string(chars);
    }

    private static string Trim(string value)
    {
        var trimmed = value.TrimEnd(' ', '.');
        return trimmed.Length == 0 ? "x" : trimmed;
    }
}

/// <summary>A generated photo file name (wrapper so it can be a distinct FsCheck type).</summary>
public readonly record struct PhotoFileName(string Value)
{
    public override string ToString() => Value;
}

/// <summary>A generated RAW file name (wrapper so it can be a distinct FsCheck type).</summary>
public readonly record struct RawFileName(string Value)
{
    public override string ToString() => Value;
}
