using System.Text;
using MensaStudio.Core;

namespace MensaStudio.Core.Tests.Fakes;

/// <summary>
/// An in-memory <see cref="IFileSystem"/> for property and unit tests.
/// Paths are compared OrdinalIgnoreCase (as on NTFS); <see cref="MoveFile"/> never overwrites an
/// existing destination (like <c>File.Move(..., overwrite: false)</c>); every mutating and read
/// operation is appended to <see cref="Operations"/> so tests can assert ordering (for example that
/// folders are created before the moves that target them).
///
/// Fault injection is driven by predicates: register a path (or, for <see cref="MoveFile"/>, a source
/// path) through the <c>FailXxx</c> hooks and the matching operation throws an <see cref="IOException"/>.
/// </summary>
public sealed class InMemoryFileSystem : IFileSystem
{
    private static readonly StringComparer PathComparer = StringComparer.OrdinalIgnoreCase;

    // Full path -> file content. Directories are tracked separately so empty directories exist.
    private readonly Dictionary<string, byte[]> _files = new(PathComparer);
    private readonly HashSet<string> _directories = new(PathComparer);
    private readonly List<FileSystemOperation> _operations = new();

    // Fault-injection predicates. A path matches when any registered predicate returns true.
    private readonly List<Func<string, bool>> _moveFaults = new();
    private readonly List<Func<string, bool>> _writeFaults = new();
    private readonly List<Func<string, bool>> _createDirectoryFaults = new();
    private readonly List<Func<string, bool>> _enumerateFaults = new();
    private readonly List<Func<string, bool>> _readFaults = new();
    private readonly List<Func<string, bool>> _replaceFaults = new();
    private readonly List<Func<string, bool>> _openAppendFaults = new();

    /// <summary>Creates an empty file system.</summary>
    public InMemoryFileSystem()
    {
    }

    /// <summary>The ordered log of every operation performed, for ordering assertions.</summary>
    public IReadOnlyList<FileSystemOperation> Operations => _operations;

    // ----- Seeding helpers (not part of IFileSystem) -----

    /// <summary>Ensures <paramref name="path"/> and all of its ancestors exist as directories.</summary>
    public void AddDirectory(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        AddDirectoryInternal(path);
    }

    /// <summary>Creates (or replaces) a file with the given <paramref name="content"/>, creating ancestors.</summary>
    public void AddFile(string path, byte[] content)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(content);
        AddDirectoryInternal(GetDirectoryName(path));
        _files[path] = (byte[])content.Clone();
    }

    /// <summary>Creates (or replaces) a file whose content is <paramref name="text"/> as UTF-8 without BOM.</summary>
    public void AddFile(string path, string text = "")
    {
        ArgumentNullException.ThrowIfNull(text);
        AddFile(path, new UTF8Encoding(false).GetBytes(text));
    }

    /// <summary>True when a file exists at <paramref name="path"/> (ignores directories).</summary>
    public bool HasFile(string path) => _files.ContainsKey(path);

    /// <summary>Returns the raw content of the file at <paramref name="path"/>, or null when absent.</summary>
    public byte[]? GetFileContent(string path) =>
        _files.TryGetValue(path, out var content) ? (byte[])content.Clone() : null;

    // ----- Fault-injection hooks (not part of IFileSystem) -----

    /// <summary><see cref="MoveFile"/> throws when <paramref name="sourceMatches"/> matches its source path.</summary>
    public void FailMoveWhen(Func<string, bool> sourceMatches) => _moveFaults.Add(sourceMatches);

    /// <summary><see cref="WriteAllBytesDurable"/> throws when <paramref name="pathMatches"/> matches its path.</summary>
    public void FailWriteWhen(Func<string, bool> pathMatches) => _writeFaults.Add(pathMatches);

    /// <summary><see cref="CreateDirectory"/> throws when <paramref name="pathMatches"/> matches its path.</summary>
    public void FailCreateDirectoryWhen(Func<string, bool> pathMatches) => _createDirectoryFaults.Add(pathMatches);

    /// <summary><see cref="EnumerateFileNames"/>/<see cref="EnumerateDirectoryNames"/> throw when <paramref name="pathMatches"/> matches.</summary>
    public void FailEnumerateWhen(Func<string, bool> pathMatches) => _enumerateFaults.Add(pathMatches);

    /// <summary><see cref="ReadAllBytes"/> throws when <paramref name="pathMatches"/> matches its path.</summary>
    public void FailReadWhen(Func<string, bool> pathMatches) => _readFaults.Add(pathMatches);

    /// <summary><see cref="ReplaceFile"/> throws when <paramref name="pathMatches"/> matches its destination path.</summary>
    public void FailReplaceWhen(Func<string, bool> pathMatches) => _replaceFaults.Add(pathMatches);

    /// <summary><see cref="OpenAppend"/> throws when <paramref name="pathMatches"/> matches its path.</summary>
    public void FailOpenAppendWhen(Func<string, bool> pathMatches) => _openAppendFaults.Add(pathMatches);

    /// <summary>Convenience: <see cref="MoveFile"/> throws when its source equals <paramref name="sourcePath"/>.</summary>
    public void FailMove(string sourcePath) => FailMoveWhen(p => PathComparer.Equals(p, sourcePath));

    /// <summary>Convenience: <see cref="WriteAllBytesDurable"/> throws for <paramref name="path"/>.</summary>
    public void FailWrite(string path) => FailWriteWhen(p => PathComparer.Equals(p, path));

    /// <summary>Convenience: <see cref="CreateDirectory"/> throws for <paramref name="path"/>.</summary>
    public void FailCreateDirectory(string path) => FailCreateDirectoryWhen(p => PathComparer.Equals(p, path));

    /// <summary>Convenience: enumeration throws for <paramref name="directory"/>.</summary>
    public void FailEnumerate(string directory) => FailEnumerateWhen(p => PathComparer.Equals(p, directory));

    // ----- IFileSystem -----

    public bool DirectoryExists(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        return _directories.Contains(path);
    }

    public bool FileExists(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        return _files.ContainsKey(path);
    }

    public IReadOnlyList<string> EnumerateFileNames(string directory)
    {
        ArgumentNullException.ThrowIfNull(directory);
        _operations.Add(new FileSystemOperation(FileSystemOperationKind.EnumerateFiles, directory));
        ThrowIfFault(_enumerateFaults, directory, "enumerate");

        if (!_directories.Contains(directory))
        {
            throw new DirectoryNotFoundException($"Directory not found: {directory}");
        }

        return ChildNames(_files.Keys, directory);
    }

    public IReadOnlyList<string> EnumerateDirectoryNames(string directory)
    {
        ArgumentNullException.ThrowIfNull(directory);
        _operations.Add(new FileSystemOperation(FileSystemOperationKind.EnumerateDirectories, directory));
        ThrowIfFault(_enumerateFaults, directory, "enumerate");

        if (!_directories.Contains(directory))
        {
            throw new DirectoryNotFoundException($"Directory not found: {directory}");
        }

        return ChildNames(_directories, directory);
    }

    public void CreateDirectory(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        _operations.Add(new FileSystemOperation(FileSystemOperationKind.CreateDirectory, path));
        ThrowIfFault(_createDirectoryFaults, path, "create directory");
        AddDirectoryInternal(path);
    }

    public byte[] ReadAllBytes(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        _operations.Add(new FileSystemOperation(FileSystemOperationKind.Read, path));
        ThrowIfFault(_readFaults, path, "read");

        if (!_files.TryGetValue(path, out var content))
        {
            throw new FileNotFoundException($"File not found: {path}", path);
        }

        return (byte[])content.Clone();
    }

    public void WriteAllBytesDurable(string path, byte[] content)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(content);
        _operations.Add(new FileSystemOperation(FileSystemOperationKind.Write, path));
        ThrowIfFault(_writeFaults, path, "write");

        EnsureParentExists(path);
        _files[path] = (byte[])content.Clone();
    }

    public void ReplaceFile(string source, string destination)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        _operations.Add(new FileSystemOperation(FileSystemOperationKind.Replace, source, destination));
        ThrowIfFault(_replaceFaults, destination, "replace");

        if (!_files.TryGetValue(source, out var content))
        {
            throw new FileNotFoundException($"File not found: {source}", source);
        }

        EnsureParentExists(destination);
        _files[destination] = content;
        _files.Remove(source);
    }

    public void MoveFile(string source, string destination)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        _operations.Add(new FileSystemOperation(FileSystemOperationKind.Move, source, destination));
        ThrowIfFault(_moveFaults, source, "move");

        if (!_files.TryGetValue(source, out var content))
        {
            throw new FileNotFoundException($"File not found: {source}", source);
        }

        if (_files.ContainsKey(destination))
        {
            // Matches File.Move(source, destination, overwrite: false).
            throw new IOException($"Cannot create a file when that file already exists: {destination}");
        }

        EnsureParentExists(destination);
        _files[destination] = content;
        _files.Remove(source);
    }

    public TextWriter OpenAppend(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        _operations.Add(new FileSystemOperation(FileSystemOperationKind.OpenAppend, path));
        ThrowIfFault(_openAppendFaults, path, "open for append");

        EnsureParentExists(path);
        if (!_files.ContainsKey(path))
        {
            _files[path] = Array.Empty<byte>();
        }

        return new AppendWriter(this, path);
    }

    // ----- Internals -----

    private void Append(string path, byte[] bytes)
    {
        _files.TryGetValue(path, out var existing);
        existing ??= Array.Empty<byte>();
        var combined = new byte[existing.Length + bytes.Length];
        Buffer.BlockCopy(existing, 0, combined, 0, existing.Length);
        Buffer.BlockCopy(bytes, 0, combined, existing.Length, bytes.Length);
        _files[path] = combined;
    }

    private void AddDirectoryInternal(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        var current = path;
        while (!string.IsNullOrEmpty(current) && _directories.Add(current))
        {
            current = GetDirectoryName(current);
        }
    }

    private void EnsureParentExists(string path)
    {
        var parent = GetDirectoryName(path);
        if (!string.IsNullOrEmpty(parent))
        {
            AddDirectoryInternal(parent);
        }
    }

    private static IReadOnlyList<string> ChildNames(IEnumerable<string> fullPaths, string directory)
    {
        var result = new List<string>();
        foreach (var full in fullPaths)
        {
            if (PathComparer.Equals(GetDirectoryName(full), directory))
            {
                result.Add(GetFileName(full));
            }
        }

        return result;
    }

    private static void ThrowIfFault(List<Func<string, bool>> faults, string path, string verb)
    {
        foreach (var fault in faults)
        {
            if (fault(path))
            {
                throw new IOException($"Injected fault: cannot {verb} '{path}'.");
            }
        }
    }

    private static string GetDirectoryName(string path)
    {
        var normalized = path.Replace('/', '\\');
        var index = normalized.LastIndexOf('\\');
        return index < 0 ? string.Empty : normalized[..index];
    }

    private static string GetFileName(string path)
    {
        var normalized = path.Replace('/', '\\');
        var index = normalized.LastIndexOf('\\');
        return index < 0 ? normalized : normalized[(index + 1)..];
    }

    /// <summary>
    /// A <see cref="TextWriter"/> that appends to the backing file on each write, using UTF-8
    /// without a BOM (Req 15.3). The fault check happened at <see cref="OpenAppend"/> time.
    /// </summary>
    private sealed class AppendWriter : TextWriter
    {
        private readonly InMemoryFileSystem _owner;
        private readonly string _path;

        public AppendWriter(InMemoryFileSystem owner, string path)
        {
            _owner = owner;
            _path = path;
        }

        public override Encoding Encoding { get; } = new UTF8Encoding(false);

        public override void Write(string? value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _owner.Append(_path, Encoding.GetBytes(value));
            }
        }

        public override void Write(char value) => _owner.Append(_path, Encoding.GetBytes(value.ToString()));

        public override void WriteLine(string? value)
        {
            Write(value);
            Write(new string(CoreNewLine));
        }
    }
}

/// <summary>The kind of a recorded <see cref="FileSystemOperation"/>.</summary>
public enum FileSystemOperationKind
{
    EnumerateFiles,
    EnumerateDirectories,
    CreateDirectory,
    Read,
    Write,
    Replace,
    Move,
    OpenAppend,
}

/// <summary>A single recorded file-system operation, for ordering assertions.</summary>
/// <param name="Kind">The operation kind.</param>
/// <param name="Path">The primary path (source for moves/replaces, otherwise the target).</param>
/// <param name="Destination">The destination path for <see cref="FileSystemOperationKind.Move"/>/<see cref="FileSystemOperationKind.Replace"/>, else null.</param>
public sealed record FileSystemOperation(FileSystemOperationKind Kind, string Path, string? Destination = null);
