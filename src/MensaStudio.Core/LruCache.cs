using System.Diagnostics.CodeAnalysis;

namespace MensaStudio.Core;

/// <summary>
/// Fixed-capacity least-recently-used cache (Req 5.3, 5.7).
/// </summary>
/// <remarks>
/// A doubly linked list keeps entries in recency order (head = most recently used, tail = least recently
/// used) and a dictionary maps keys to their list nodes, so every operation is O(1). <see cref="TryGet"/>,
/// <see cref="Touch"/> and <see cref="Add"/> mark the key most recently used; <see cref="Contains"/> does not.
/// Adding a new key when the cache is full evicts the least recently used entry first, so
/// <see cref="Count"/> never exceeds <see cref="Capacity"/>. Not thread-safe; callers synchronize.
/// </remarks>
public sealed class LruCache<TKey, TValue> where TKey : notnull
{
    private readonly Dictionary<TKey, LinkedListNode<KeyValuePair<TKey, TValue>>> _map;
    private readonly LinkedList<KeyValuePair<TKey, TValue>> _order = new();

    public LruCache(int capacity, IEqualityComparer<TKey>? comparer = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);
        Capacity = capacity;
        _map = new Dictionary<TKey, LinkedListNode<KeyValuePair<TKey, TValue>>>(capacity, comparer);
    }

    public int Capacity { get; }

    public int Count => _map.Count;

    /// <summary>Gets the value for <paramref name="key"/> and marks it most recently used.</summary>
    public bool TryGet(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        if (_map.TryGetValue(key, out var node))
        {
            MoveToFront(node);
            value = node.Value.Value;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>Marks <paramref name="key"/> most recently used. Returns false if it is not cached.</summary>
    public bool Touch(TKey key)
    {
        if (_map.TryGetValue(key, out var node))
        {
            MoveToFront(node);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Inserts or replaces <paramref name="key"/> as most recently used. When inserting a new key into a full
    /// cache, the least recently used entry is evicted first.
    /// </summary>
    public void Add(TKey key, TValue value)
    {
        if (_map.TryGetValue(key, out var existing))
        {
            existing.Value = new KeyValuePair<TKey, TValue>(existing.Value.Key, value);
            MoveToFront(existing);
            return;
        }

        if (_map.Count >= Capacity)
        {
            var lru = _order.Last!;
            _order.RemoveLast();
            _map.Remove(lru.Value.Key);
        }

        var node = _order.AddFirst(new KeyValuePair<TKey, TValue>(key, value));
        _map.Add(key, node);
    }

    /// <summary>Returns whether <paramref name="key"/> is cached without changing recency.</summary>
    public bool Contains(TKey key) => _map.ContainsKey(key);

    /// <summary>Removes every entry.</summary>
    public void Clear()
    {
        _map.Clear();
        _order.Clear();
    }

    private void MoveToFront(LinkedListNode<KeyValuePair<TKey, TValue>> node)
    {
        if (node != _order.First)
        {
            _order.Remove(node);
            _order.AddFirst(node);
        }
    }
}
