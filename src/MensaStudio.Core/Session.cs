namespace MensaStudio.Core;

/// <summary>What a <see cref="Session"/> change touched.</summary>
public enum SessionChangeKind
{
    /// <summary>The Folder_Assignment of a photo was set or cleared.</summary>
    Folder,

    /// <summary>The Rating of a photo was set or cleared.</summary>
    Rating,

    /// <summary>The Current_Position moved.</summary>
    Position,
}

/// <summary>Arguments of <see cref="Session.Changed"/>.</summary>
/// <param name="Kind">What changed.</param>
/// <param name="Index">The affected photo index (for <see cref="SessionChangeKind.Position"/>, the new index).</param>
/// <param name="Version">The session <see cref="Session.Version"/> after the change.</param>
public sealed record SessionChange(SessionChangeKind Kind, int Index, long Version);

/// <summary>
/// A consistent, immutable copy of a Session taken by <see cref="Session.Snapshot"/> (Req 11.1).
/// Equality is structural: photos element by element plus the current file name (ordinal).
/// </summary>
/// <param name="SourceFolder">The Source_Folder path.</param>
/// <param name="Photos">The photos in Session order.</param>
/// <param name="CurrentFileName">File name of the photo at the Current_Position, or null when empty.</param>
public sealed record SessionSnapshot(string SourceFolder, IReadOnlyList<PhotoEntry> Photos, string? CurrentFileName)
{
    public bool Equals(SessionSnapshot? other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return other is not null
            && string.Equals(SourceFolder, other.SourceFolder, StringComparison.Ordinal)
            && string.Equals(CurrentFileName, other.CurrentFileName, StringComparison.Ordinal)
            && Photos.SequenceEqual(other.Photos);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(SourceFolder, StringComparer.Ordinal);
        hash.Add(CurrentFileName, StringComparer.Ordinal);
        foreach (var photo in Photos)
        {
            hash.Add(photo);
        }

        return hash.ToHashCode();
    }
}

/// <summary>
/// The in-memory Session: photos in natural order, Current_Position, Folder_Assignments and Ratings
/// (Req 6, 7, 8, 17). Mutations happen on the UI thread; <see cref="Snapshot"/> may be called from any thread
/// and always observes a consistent state (Req 11.1). <see cref="Changed"/> is raised outside the lock on the
/// mutating thread.
/// </summary>
public sealed class Session
{
    private readonly object _gate = new();
    private readonly PhotoEntry[] _photos;
    private int _currentIndex;
    private int _assignedCount;
    private int _ratedCount;
    private long _version;

    /// <param name="sourceFolder">The Source_Folder path.</param>
    /// <param name="photos">Photos in natural order; file names must be unique (OrdinalIgnoreCase).</param>
    /// <param name="currentIndex">Initial Current_Position; must be in range when photos is non-empty, ignored (-1) when empty.</param>
    public Session(string sourceFolder, IReadOnlyList<PhotoEntry> photos, int currentIndex)
    {
        ArgumentNullException.ThrowIfNull(sourceFolder);
        ArgumentNullException.ThrowIfNull(photos);

        _photos = photos.ToArray();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var photo in _photos)
        {
            if (photo is null)
            {
                throw new ArgumentException("Photos must not contain null entries.", nameof(photos));
            }

            ValidateFolder(photo.Folder, nameof(photos));
            ValidateRating(photo.Rating, nameof(photos));
            if (!names.Add(photo.FileName))
            {
                throw new ArgumentException($"Duplicate photo file name '{photo.FileName}'.", nameof(photos));
            }
        }

        if (_photos.Length == 0)
        {
            _currentIndex = -1;
        }
        else
        {
            if (currentIndex < 0 || currentIndex >= _photos.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(currentIndex), currentIndex, "Current index is out of range.");
            }

            _currentIndex = currentIndex;
        }

        (_assignedCount, _, _ratedCount) = SessionStatistics.Compute(_photos);
        SourceFolder = sourceFolder;
    }

    /// <summary>Raised after every change (folder, rating or position) with the new <see cref="Version"/>.</summary>
    public event EventHandler<SessionChange>? Changed;

    public string SourceFolder { get; }

    public int Count => _photos.Length;

    /// <summary>The Current_Position, or -1 when the session has no photos.</summary>
    public int CurrentIndex
    {
        get { lock (_gate) { return _currentIndex; } }
    }

    public PhotoEntry this[int index]
    {
        get
        {
            lock (_gate)
            {
                if ((uint)index >= (uint)_photos.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(index), index, "Photo index is out of range.");
                }

                return _photos[index];
            }
        }
    }

    /// <summary>The photo at the Current_Position, or null when the session has no photos.</summary>
    public PhotoEntry? Current
    {
        get { lock (_gate) { return _currentIndex < 0 ? null : _photos[_currentIndex]; } }
    }

    /// <summary>Number of photos with a Folder_Assignment (maintained incrementally).</summary>
    public int AssignedCount
    {
        get { lock (_gate) { return _assignedCount; } }
    }

    /// <summary>Number of photos with a Rating (maintained incrementally).</summary>
    public int RatedCount
    {
        get { lock (_gate) { return _ratedCount; } }
    }

    public int UnassignedCount => Count - AssignedCount;

    /// <summary>Incremented on every change.</summary>
    public long Version
    {
        get { lock (_gate) { return _version; } }
    }

    /// <summary>Sets the Folder_Assignment of the current photo (Req 7.1). Returns true if anything changed.</summary>
    public bool SetFolder(string folder)
    {
        ArgumentNullException.ThrowIfNull(folder);
        ValidateFolder(folder, nameof(folder));
        return UpdateCurrentFolder(folder);
    }

    /// <summary>Clears the Folder_Assignment of the current photo (Req 7.3). Returns true if anything changed.</summary>
    public bool ClearFolder() => UpdateCurrentFolder(null);

    /// <summary>Sets (1..5) or clears (null) the Rating of the current photo (Req 8.1, 8.2). Returns true if anything changed.</summary>
    public bool SetRating(int? rating)
    {
        ValidateRating(rating, nameof(rating));

        SessionChange change;
        lock (_gate)
        {
            if (_currentIndex < 0)
            {
                return false;
            }

            var entry = _photos[_currentIndex];
            if (entry.Rating == rating)
            {
                return false;
            }

            _ratedCount += (rating is null ? 0 : 1) - (entry.Rating is null ? 0 : 1);
            _photos[_currentIndex] = entry with { Rating = rating };
            change = new SessionChange(SessionChangeKind.Rating, _currentIndex, ++_version);
        }

        Changed?.Invoke(this, change);
        return true;
    }

    /// <summary>Moves the Current_Position to <paramref name="index"/>. Returns true if the position changed.</summary>
    public bool MoveTo(int index)
    {
        SessionChange change;
        lock (_gate)
        {
            if ((uint)index >= (uint)_photos.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "Photo index is out of range.");
            }

            if (index == _currentIndex)
            {
                return false;
            }

            _currentIndex = index;
            change = new SessionChange(SessionChangeKind.Position, index, ++_version);
        }

        Changed?.Invoke(this, change);
        return true;
    }

    /// <summary>Returns a consistent copy of the photos and current file name; safe to call from any thread.</summary>
    public SessionSnapshot Snapshot()
    {
        lock (_gate)
        {
            var photos = (PhotoEntry[])_photos.Clone();
            var current = _currentIndex < 0 ? null : photos[_currentIndex].FileName;
            return new SessionSnapshot(SourceFolder, photos, current);
        }
    }

    private bool UpdateCurrentFolder(string? folder)
    {
        SessionChange change;
        lock (_gate)
        {
            if (_currentIndex < 0)
            {
                return false;
            }

            var entry = _photos[_currentIndex];
            if (string.Equals(entry.Folder, folder, StringComparison.Ordinal))
            {
                return false;
            }

            _assignedCount += (folder is null ? 0 : 1) - (entry.Folder is null ? 0 : 1);
            _photos[_currentIndex] = entry with { Folder = folder };
            change = new SessionChange(SessionChangeKind.Folder, _currentIndex, ++_version);
        }

        Changed?.Invoke(this, change);
        return true;
    }

    private static void ValidateFolder(string? folder, string paramName)
    {
        if (folder is not null && folder.Length == 0)
        {
            throw new ArgumentException("Folder must be non-empty or null.", paramName);
        }
    }

    private static void ValidateRating(int? rating, string paramName)
    {
        if (rating is < 1 or > 5)
        {
            throw new ArgumentOutOfRangeException(paramName, rating, "Rating must be between 1 and 5 or null.");
        }
    }
}
