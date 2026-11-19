namespace MensaStudio.Core;

/// <summary>
/// Abstraction over the file system operations Core needs, so that stores and executors
/// can run against a real directory or an in-memory / fault-injecting fake.
/// </summary>
public interface IFileSystem
{
    bool DirectoryExists(string path);

    bool FileExists(string path);

    /// <summary>
    /// Names (not paths) of files directly inside <paramref name="directory"/>.
    /// Throws <see cref="IOException"/> or <see cref="UnauthorizedAccessException"/>.
    /// </summary>
    IReadOnlyList<string> EnumerateFileNames(string directory);

    /// <summary>
    /// Names (not paths) of directories directly inside <paramref name="directory"/>.
    /// Throws <see cref="IOException"/> or <see cref="UnauthorizedAccessException"/>.
    /// </summary>
    IReadOnlyList<string> EnumerateDirectoryNames(string directory);

    void CreateDirectory(string path);

    byte[] ReadAllBytes(string path);

    /// <summary>Writes <paramref name="content"/> to <paramref name="path"/> and flushes it to disk.</summary>
    void WriteAllBytesDurable(string path, byte[] content);

    /// <summary>
    /// Replaces <paramref name="destination"/> with <paramref name="source"/>; moves
    /// <paramref name="source"/> into place when <paramref name="destination"/> does not exist.
    /// </summary>
    void ReplaceFile(string source, string destination);

    /// <summary>Moves a file; never overwrites an existing <paramref name="destination"/>.</summary>
    void MoveFile(string source, string destination);

    /// <summary>Opens <paramref name="path"/> for appending (created if absent), UTF-8 without BOM, readable by others.</summary>
    TextWriter OpenAppend(string path);
}
