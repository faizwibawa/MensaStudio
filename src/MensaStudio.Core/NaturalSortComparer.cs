namespace MensaStudio.Core;

/// <summary>
/// Natural_Sort_Order comparer (Req 2.4–2.7).
/// </summary>
/// <remarks>
/// Each string is read as a sequence of tokens: a maximal run of ASCII digits <c>0</c>–<c>9</c> is a
/// Number token; every other character is a Char token holding <see cref="char.ToUpperInvariant(char)"/>.
/// Number tokens precede Char tokens. Two Numbers compare by the length of their digit string without
/// leading zeros, then ordinally by that digit string, so runs of any length compare by value without
/// integer parsing. Two Chars compare by UTF-16 code unit. Token sequences compare lexicographically and a
/// proper prefix comes first. <see cref="Compare"/> adds an ordinal tie-break, yielding a total order.
/// </remarks>
public sealed class NaturalSortComparer : IComparer<string>
{
    public static NaturalSortComparer Instance { get; } = new();

    /// <summary>
    /// Natural comparison only (may return 0 for distinct strings, e.g. "IMG_7" vs "IMG_007").
    /// </summary>
    public static int CompareNatural(string x, string y)
    {
        ArgumentNullException.ThrowIfNull(x);
        ArgumentNullException.ThrowIfNull(y);

        int i = 0;
        int j = 0;
        while (i < x.Length && j < y.Length)
        {
            bool xDigit = IsAsciiDigit(x[i]);
            bool yDigit = IsAsciiDigit(y[j]);

            if (xDigit && yDigit)
            {
                int result = CompareNumberTokens(x, ref i, y, ref j);
                if (result != 0)
                {
                    return result;
                }
            }
            else if (xDigit)
            {
                return -1; // Number < Char
            }
            else if (yDigit)
            {
                return 1; // Char > Number
            }
            else
            {
                char cx = char.ToUpperInvariant(x[i]);
                char cy = char.ToUpperInvariant(y[j]);
                if (cx != cy)
                {
                    return cx < cy ? -1 : 1;
                }

                i++;
                j++;
            }
        }

        // Both exhausted: equal. Otherwise the proper prefix (exhausted side) comes first.
        bool xDone = i >= x.Length;
        bool yDone = j >= y.Length;
        if (xDone && yDone)
        {
            return 0;
        }

        return xDone ? -1 : 1;
    }

    /// <summary>
    /// <see cref="CompareNatural"/>, then <see cref="string.CompareOrdinal(string, string)"/> as tie-break.
    /// Total order. Null sorts before any non-null string.
    /// </summary>
    public int Compare(string? x, string? y)
    {
        if (ReferenceEquals(x, y))
        {
            return 0;
        }

        if (x is null)
        {
            return -1;
        }

        if (y is null)
        {
            return 1;
        }

        int natural = CompareNatural(x, y);
        if (natural != 0)
        {
            return natural;
        }

        return Math.Sign(string.CompareOrdinal(x, y));
    }

    private static bool IsAsciiDigit(char c) => c >= '0' && c <= '9';

    /// <summary>
    /// Reads the digit runs starting at <paramref name="i"/> and <paramref name="j"/>, advancing both indices
    /// past their runs, and compares them by value (significant length, then ordinal digits).
    /// </summary>
    private static int CompareNumberTokens(string x, ref int i, string y, ref int j)
    {
        int xStart = i;
        while (i < x.Length && IsAsciiDigit(x[i]))
        {
            i++;
        }

        int yStart = j;
        while (j < y.Length && IsAsciiDigit(y[j]))
        {
            j++;
        }

        // Skip leading zeros.
        int xSig = xStart;
        while (xSig < i && x[xSig] == '0')
        {
            xSig++;
        }

        int ySig = yStart;
        while (ySig < j && y[ySig] == '0')
        {
            ySig++;
        }

        int xLen = i - xSig;
        int yLen = j - ySig;
        if (xLen != yLen)
        {
            return xLen < yLen ? -1 : 1;
        }

        int digits = string.CompareOrdinal(x, xSig, y, ySig, xLen);
        return Math.Sign(digits);
    }
}
