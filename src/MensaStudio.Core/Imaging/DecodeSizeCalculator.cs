namespace MensaStudio.Core;

/// <summary>
/// Computes the stored-orientation decode size for a Photo: the largest size not exceeding the
/// original that keeps the aspect ratio and fits both the stored and the EXIF-oriented image within
/// the physical screen bounds (Req 4.2).
/// </summary>
/// <remarks>
/// The decoder works in stored-pixel space, but the displayed image may be rotated 90° or 270° by its
/// EXIF orientation, which swaps width and height. To guarantee the oriented image also fits the
/// screen, the scale is constrained by both the stored dimensions and the oriented dimensions:
/// <c>scale = min(1, sw/w, sh/h, sw/ow, sh/oh)</c>, where <c>(ow, oh)</c> are the oriented dimensions.
/// The returned size is each original dimension multiplied by that scale, rounded to the nearest
/// integer with a floor of 1 pixel. When the scale is exactly 1 — the Photo already fits in both
/// orientations — the original dimensions are returned unchanged, with no rounding.
/// </remarks>
public static class DecodeSizeCalculator
{
    /// <summary>
    /// Returns the decode <c>(Width, Height)</c> in stored-pixel space for a Photo of the given
    /// original dimensions and EXIF <paramref name="orientation"/>, bounded by the physical screen
    /// size (Req 4.2).
    /// </summary>
    /// <param name="width">Original stored width in pixels; must be at least 1.</param>
    /// <param name="height">Original stored height in pixels; must be at least 1.</param>
    /// <param name="orientation">EXIF orientation tag value (1–8 swap for 5–8; other values do not swap).</param>
    /// <param name="screenWidth">Physical screen width in pixels; must be at least 1.</param>
    /// <param name="screenHeight">Physical screen height in pixels; must be at least 1.</param>
    /// <exception cref="ArgumentOutOfRangeException">Any dimension is less than 1.</exception>
    public static (int Width, int Height) Compute(int width, int height, int orientation, int screenWidth, int screenHeight)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(screenWidth, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(screenHeight, 1);

        // Oriented dimensions: a 90°/270° rotation swaps width and height.
        bool swaps = ExifOrientation.SwapsDimensions(orientation);
        int orientedWidth = swaps ? height : width;
        int orientedHeight = swaps ? width : height;

        double scale = Math.Min(
            1.0,
            Math.Min(
                Math.Min((double)screenWidth / width, (double)screenHeight / height),
                Math.Min((double)screenWidth / orientedWidth, (double)screenHeight / orientedHeight)));

        // Scale 1 (the Photo already fits in both orientations) returns the original size exactly.
        if (scale >= 1.0)
        {
            return (width, height);
        }

        int decodeWidth = Math.Max(1, (int)Math.Round(width * scale, MidpointRounding.AwayFromZero));
        int decodeHeight = Math.Max(1, (int)Math.Round(height * scale, MidpointRounding.AwayFromZero));
        return (decodeWidth, decodeHeight);
    }
}
