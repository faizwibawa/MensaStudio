using System.Text;

namespace MensaStudio.Core;

/// <summary><see cref="IFileSystem"/> implementation backed by <see cref="System.IO"/>.</summary>
public sealed class PhysicalFileSystem : IFileSystem
{
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    public bool DirectoryExists(string path) => Directory.Exists(path);

    public bool FileExists(string path) => File.Exists(path);

    public IReadOnlyList<string> EnumerateFileNames(string directory) =>
        Directory.EnumerateFiles(directory)
            .Select(p => Path.GetFileName(p))
            .ToList();

    public IReadOnlyList<string> EnumerateDirectoryNames(string directory) =>
        Directory.EnumerateDirectories(directory)
            .Select(p => Path.GetFileName(p))
            .ToList();

    public void CreateDirectory(string path) => Directory.CreateDirectory(path);

    public byte[] ReadAllBytes(string path) => File.ReadAllBytes(path);

    public void WriteAllBytesDurable(string path, byte[] content)
    {
        ArgumentNullException.ThrowIfNull(content);
        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        stream.Write(content, 0, content.Length);
        // Flush OS buffers to the physical disk before the file is swapped into place.
        stream.Flush(flushToDisk: true);
    }

    public void ReplaceFile(string source, string destination)
    {
        if (File.Exists(destination))
        {
            File.Replace(source, destination, destinationBackupFileName: null);
        }
        else
        {
            File.Move(source, destination, overwrite: false);
        }
    }

    public void MoveFile(string source, string destination) =>
        File.Move(source, destination, overwrite: false);

    public TextWriter OpenAppend(string path)
    {
        var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read);
        try
        {
            return new StreamWriter(stream, Utf8NoBom);
        }
        catch
        {
            stream.Dispose();
            throw;
        }
    }
}
