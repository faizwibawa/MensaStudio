namespace MensaStudio.Core;

/// <summary>
/// Base for Core's typed I/O failures. Carries the affected path and a human-readable reason
/// (normally the inner exception's message) for display by the App.
/// </summary>
public abstract class PathOperationException : Exception
{
    protected PathOperationException(string what, string path, string reason, Exception? innerException)
        : base($"{what} '{path}': {reason}", innerException)
    {
        Path = path;
        Reason = reason;
    }

    /// <summary>Path of the file or folder the operation failed on.</summary>
    public string Path { get; }

    /// <summary>Human-readable reason for the failure.</summary>
    public string Reason { get; }

    protected static string ReasonOf(Exception innerException)
    {
        ArgumentNullException.ThrowIfNull(innerException);
        return innerException.Message;
    }
}

/// <summary>A folder could not be read (Req 1.7, 14.7).</summary>
public sealed class FolderReadException : PathOperationException
{
    public FolderReadException(string path, string reason, Exception? innerException = null)
        : base("Could not read folder", path, reason, innerException) { }

    public FolderReadException(string path, Exception innerException)
        : this(path, ReasonOf(innerException), innerException) { }
}

/// <summary>The Session_File could not be read or decoded (Req 11.11).</summary>
public sealed class SessionFileReadException : PathOperationException
{
    public SessionFileReadException(string path, string reason, Exception? innerException = null)
        : base("Could not read session file", path, reason, innerException) { }

    public SessionFileReadException(string path, Exception innerException)
        : this(path, ReasonOf(innerException), innerException) { }
}

/// <summary>The apply log could not be opened; no apply action is performed (Req 15.12).</summary>
public sealed class ApplyLogOpenException : PathOperationException
{
    public ApplyLogOpenException(string path, string reason, Exception? innerException = null)
        : base("Could not open apply log", path, reason, innerException) { }

    public ApplyLogOpenException(string path, Exception innerException)
        : this(path, ReasonOf(innerException), innerException) { }
}

/// <summary>The settings file could not be written (Req 19.11).</summary>
public sealed class SettingsWriteException : PathOperationException
{
    public SettingsWriteException(string path, string reason, Exception? innerException = null)
        : base("Could not write settings file", path, reason, innerException) { }

    public SettingsWriteException(string path, Exception innerException)
        : this(path, ReasonOf(innerException), innerException) { }
}
