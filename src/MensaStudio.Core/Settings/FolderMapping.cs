using System.Collections.ObjectModel;
using System.Text;

namespace MensaStudio.Core;

/// <summary>
/// The nine numpad folder slots. Index 0 corresponds to numpad key 1; a <c>null</c> slot is an Unmapped_Key.
/// Equality compares slot contents element-wise with ordinal (case-sensitive) string comparison (Req 19.9).
/// </summary>
public sealed record FolderMapping(IReadOnlyList<string?> Slots)
{
    /// <summary>Number of slots (numpad keys 1 to 9).</summary>
    public const int SlotCount = 9;

    /// <summary>Default mapping: 1=Good, 2=Okay, 3=Bad, 4–8 unmapped, 9=Unsure (Req 16.1, 19.1).</summary>
    public static FolderMapping Default { get; } =
        new(new string?[] { "Good", "Okay", "Bad", null, null, null, null, null, "Unsure" });

    /// <summary>Immutable copy of the nine slots; index 0 = numpad 1.</summary>
    public IReadOnlyList<string?> Slots { get; } = CopySlots(Slots);

    /// <summary>Folder name mapped to numpad <paramref name="key"/> (1 to 9), or <c>null</c> when unmapped.</summary>
    public string? this[int key]
    {
        get
        {
            if (key < 1 || key > SlotCount)
            {
                throw new ArgumentOutOfRangeException(nameof(key), key, "Numpad key must be between 1 and 9.");
            }

            return Slots[key - 1];
        }
    }

    public bool Equals(FolderMapping? other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (other is null)
        {
            return false;
        }

        for (var i = 0; i < SlotCount; i++)
        {
            if (!string.Equals(Slots[i], other.Slots[i], StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var slot in Slots)
        {
            hash.Add(slot, StringComparer.Ordinal);
        }

        return hash.ToHashCode();
    }

    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append("Slots = [");
        for (var i = 0; i < SlotCount; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }

            builder.Append(Slots[i] is { } name ? $"\"{name}\"" : "null");
        }

        builder.Append(']');
        return true;
    }

    private static IReadOnlyList<string?> CopySlots(IReadOnlyList<string?> slots)
    {
        ArgumentNullException.ThrowIfNull(slots);
        if (slots.Count != SlotCount)
        {
            throw new ArgumentException($"A folder mapping must have exactly {SlotCount} slots.", nameof(slots));
        }

        return new ReadOnlyCollection<string?>(slots.ToArray());
    }
}
