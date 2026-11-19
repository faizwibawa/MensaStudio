namespace MensaStudio.Core;

/// <summary>
/// Maps an EXIF orientation tag (tag 274) to the rotation and horizontal flip that bring the
/// stored pixels into their upright view, and reports whether the tag swaps width and height
/// (Req 4.3, 4.4).
/// </summary>
/// <remarks>
/// The eight defined EXIF orientation values describe how the stored pixels relate to the upright
/// scene. To display the upright view the stored image must be flipped horizontally (for the mirrored
/// values) and then rotated clockwise. Tag values 5–8 rotate by 90° or 270° and therefore swap the
/// width and height of the oriented image. Any value outside 1–8, including a missing tag (null),
/// maps to the identity transform (no rotation, no flip) so the stored pixels are shown as-is.
/// </remarks>
public static class ExifOrientation
{
    /// <summary>
    /// Returns the clockwise <c>Rotation</c> in degrees (0, 90, 180 or 270) and whether the image is
    /// flipped horizontally (<c>FlipX</c>) before the rotation, for the given EXIF orientation tag
    /// value. The flip is applied in stored-pixel space, then the rotation. Tag values outside 1–8 and
    /// a null tag return the identity transform <c>(0, false)</c> (Req 4.3, 4.4).
    /// </summary>
    /// <param name="tagValue">The EXIF orientation tag value, or null when the Photo has no tag.</param>
    public static (int Rotation, bool FlipX) ToTransform(int? tagValue) => tagValue switch
    {
        1 => (0, false),
        2 => (0, true),
        3 => (180, false),
        4 => (180, true),
        5 => (90, true),
        6 => (90, false),
        7 => (270, true),
        8 => (270, false),
        _ => (0, false),
    };

    /// <summary>
    /// Returns whether the given EXIF orientation tag value swaps the width and height of the oriented
    /// image. This is true exactly for tag values 5–8 (the 90°/270° rotations). Any other value,
    /// including a null tag, returns false.
    /// </summary>
    /// <param name="tagValue">The EXIF orientation tag value, or null when the Photo has no tag.</param>
    public static bool SwapsDimensions(int? tagValue) => tagValue is >= 5 and <= 8;
}
