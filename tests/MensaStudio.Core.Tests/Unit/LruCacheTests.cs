using MensaStudio.Core;
using Xunit;

namespace MensaStudio.Core.Tests.Unit;

public class LruCacheTests
{
    [Fact]
    public void Add_beyond_capacity_evicts_least_recently_used()
    {
        var cache = new LruCache<int, string>(2);
        cache.Add(1, "a");
        cache.Add(2, "b");
        cache.Add(3, "c");

        Assert.Equal(2, cache.Count);
        Assert.False(cache.Contains(1));
        Assert.True(cache.Contains(2));
        Assert.True(cache.Contains(3));
    }

    [Fact]
    public void TryGet_and_Touch_mark_most_recently_used()
    {
        var cache = new LruCache<int, string>(2);
        cache.Add(1, "a");
        cache.Add(2, "b");
        Assert.True(cache.TryGet(1, out var value));
        Assert.Equal("a", value);
        cache.Add(3, "c"); // evicts 2
        Assert.False(cache.Contains(2));

        Assert.True(cache.Touch(1));
        cache.Add(4, "d"); // evicts 3
        Assert.False(cache.Contains(3));
        Assert.True(cache.Contains(1));
        Assert.False(cache.Touch(3));
    }

    [Fact]
    public void Contains_does_not_change_recency()
    {
        var cache = new LruCache<int, string>(2);
        cache.Add(1, "a");
        cache.Add(2, "b");
        Assert.True(cache.Contains(1));
        cache.Add(3, "c");

        Assert.False(cache.Contains(1));
    }

    [Fact]
    public void Add_existing_key_replaces_value_without_eviction()
    {
        var cache = new LruCache<int, string>(2);
        cache.Add(1, "a");
        cache.Add(2, "b");
        cache.Add(1, "z"); // 1 becomes MRU
        cache.Add(3, "c"); // evicts 2

        Assert.Equal(2, cache.Count);
        Assert.True(cache.TryGet(1, out var value));
        Assert.Equal("z", value);
        Assert.False(cache.Contains(2));
    }

    [Fact]
    public void Comparer_is_honoured()
    {
        var cache = new LruCache<string, int>(15, StringComparer.OrdinalIgnoreCase);
        cache.Add(@"C:\Photos\IMG_1.JPG", 1);

        Assert.True(cache.Contains(@"c:\photos\img_1.jpg"));
        cache.Add(@"c:\photos\img_1.jpg", 2);
        Assert.Equal(1, cache.Count);
        Assert.True(cache.TryGet(@"C:\PHOTOS\IMG_1.jpg", out var value));
        Assert.Equal(2, value);
    }

    [Fact]
    public void Clear_removes_all_entries()
    {
        var cache = new LruCache<int, string>(3);
        cache.Add(1, "a");
        cache.Add(2, "b");
        cache.Clear();

        Assert.Equal(0, cache.Count);
        Assert.False(cache.TryGet(1, out _));
        cache.Add(3, "c");
        Assert.Equal(1, cache.Count);
    }

    [Fact]
    public void Capacity_below_one_is_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new LruCache<int, int>(0));
    }
}
