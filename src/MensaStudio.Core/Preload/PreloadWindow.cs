namespace MensaStudio.Core;

/// <summary>
/// Computes the Preload_Window around the Current_Position (Req 5.2, 5.6).
/// </summary>
public static class PreloadWindow
{
    /// <summary>Offsets relative to the Current_Position, in decode order (Req 5.2).</summary>
    private static readonly int[] DecodeOrderOffsets = { 0, 1, 2, 3, -1, -2 };

    /// <summary>
    /// Positions in decode order: p, p+1, p+2, p+3, p-1, p-2, filtered to [0, n-1] without wrapping.
    /// Empty when <paramref name="n"/> == 0.
    /// </summary>
    /// <param name="p">Zero-based Current_Position.</param>
    /// <param name="n">Number of Photos in the Session.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is negative.</exception>
    public static IReadOnlyList<int> Compute(int p, int n)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(n);
        if (n == 0)
        {
            return Array.Empty<int>();
        }

        var positions = new List<int>(DecodeOrderOffsets.Length);
        foreach (int offset in DecodeOrderOffsets)
        {
            // long arithmetic avoids overflow for extreme p values.
            long position = (long)p + offset;
            if (position >= 0 && position < n)
            {
                positions.Add((int)position);
            }
        }

        return positions;
    }
}
