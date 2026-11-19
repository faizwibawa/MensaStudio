namespace MensaStudio.Core;

/// <summary>Result of extracting a Rating_Prefix from a file name.</summary>
/// <param name="Rating">The prefix digit 1..5, or null when the name has no Rating_Prefix.</param>
/// <param name="Unprefixed">The file name without its Rating_Prefix (unchanged when there is none).</param>
public readonly record struct RatingExtraction(int? Rating, string Unprefixed);

/// <summary>
/// Encodes and decodes the Rating_Prefix (<c>^[1-5]_</c>) of file names (Req 9) and inserts
/// Conflict_Suffixes (Req 13.7).
/// </summary>
public static class RatingPrefixCodec
{
    /// <summary>
    /// "^[1-5]_" → (digit, rest after first underscore); otherwise (null, name). Case and extension are preserved
    /// (Req 9.1, 9.2).
    /// </summary>
    public static RatingExtraction Extract(string fileName)
    {
        ArgumentNullException.ThrowIfNull(fileName);

        if (fileName.Length >= 2 && fileName[0] >= '1' && fileName[0] <= '5' && fileName[1] == '_')
        {
            return new RatingExtraction(fileName[0] - '0', fileName.Substring(2));
        }

        return new RatingExtraction(null, fileName);
    }

    /// <summary>
    /// rating 1..5 → $"{rating}_{Extract(name).Unprefixed}"; null → Extract(name).Unprefixed (Req 9.3, 9.4).
    /// </summary>
    public static string Apply(string fileName, int? rating)
    {
        ArgumentNullException.ThrowIfNull(fileName);

        string unprefixed = Extract(fileName).Unprefixed;
        if (rating is null)
        {
            return unprefixed;
        }

        if (rating < 1 || rating > 5)
        {
            throw new ArgumentOutOfRangeException(nameof(rating), rating, "Rating must be between 1 and 5.");
        }

        return $"{rating.Value}_{unprefixed}";
    }

    /// <summary>
    /// Inserts " (n)" between the (unprefixed) base name and the final extension of an already-prefixed name.
    /// The suffix goes immediately before <see cref="Path.GetExtension(string)"/>; existing " (n)" text is left
    /// untouched (Req 13.7).
    /// </summary>
    public static string WithConflictSuffix(string fileName, int n)
    {
        ArgumentNullException.ThrowIfNull(fileName);
        if (n < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(n), n, "Conflict suffix number must be positive.");
        }

        string extension = Path.GetExtension(fileName);
        string stem = fileName.Substring(0, fileName.Length - extension.Length);
        return $"{stem} ({n}){extension}";
    }
}
