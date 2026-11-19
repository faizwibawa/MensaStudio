using System.Collections.Frozen;

namespace MensaStudio.Core;

/// <summary>
/// Classifies file names found directly inside a Source_Folder (Req 2.1, 2.3, 3.1).
/// All checks use the final extension only and compare case-insensitively.
/// </summary>
public static class FileKinds
{
    /// <summary>Photo extensions: .jpg, .jpeg, .png (OrdinalIgnoreCase).</summary>
    public static readonly IReadOnlySet<string> PhotoExtensions =
        new[] { ".jpg", ".jpeg", ".png" }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>RAW_Extension set (OrdinalIgnoreCase).</summary>
    public static readonly IReadOnlySet<string> RawExtensions =
        new[] { ".cr2", ".cr3", ".nef", ".arw", ".raf", ".orf", ".rw2", ".dng", ".raw", ".pef", ".srw" }
            .ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>Session_File name in the Source_Folder.</summary>
    public const string SessionFileName = "MensaStudio.session.txt";

    /// <summary>Temporary file written by the Session_Store during an atomic save (Req 11.3).</summary>
    public const string SessionTempFileName = "MensaStudio.session.txt.tmp";

    /// <summary>Apply_Log file name in the Source_Folder.</summary>
    public const string ApplyLogFileName = "MensaStudio.apply.log.txt";

    /// <summary>Suffix appended to the settings file name for its atomic save.</summary>
    public const string SettingsTempSuffix = ".tmp";

    private static readonly FrozenSet<string> AppFileNames =
        new[] { SessionFileName, SessionTempFileName, ApplyLogFileName }
            .ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>True when the final extension of <paramref name="fileName"/> is a Photo extension.</summary>
    public static bool IsPhoto(string fileName) => HasExtensionIn(fileName, PhotoExtensions);

    /// <summary>True when the final extension of <paramref name="fileName"/> is a RAW_Extension.</summary>
    public static bool IsRaw(string fileName) => HasExtensionIn(fileName, RawExtensions);

    /// <summary>
    /// True for files owned by MensaStudio: the Session_File, its save temp file and the Apply_Log
    /// (compared case-insensitively, as on NTFS).
    /// </summary>
    public static bool IsAppFile(string fileName)
    {
        ArgumentNullException.ThrowIfNull(fileName);
        return AppFileNames.Contains(fileName);
    }

    private static bool HasExtensionIn(string fileName, IReadOnlySet<string> extensions)
    {
        ArgumentNullException.ThrowIfNull(fileName);
        // Path.GetExtension returns only the final extension ("IMG_1.jpg.txt" -> ".txt").
        string extension = Path.GetExtension(fileName);
        return extension.Length > 0 && extensions.Contains(extension);
    }
}
