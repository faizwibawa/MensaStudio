using MensaStudio.Core;
using Xunit;

namespace MensaStudio.Core.Tests.Unit;

public class PreloadWindowTests
{
    [Fact]
    public void Empty_session_yields_empty_window()
    {
        Assert.Empty(PreloadWindow.Compute(0, 0));
    }

    [Fact]
    public void Single_photo_yields_only_current_position()
    {
        Assert.Equal(new[] { 0 }, PreloadWindow.Compute(0, 1));
    }

    [Fact]
    public void Middle_position_yields_full_window_in_decode_order()
    {
        Assert.Equal(new[] { 5, 6, 7, 8, 4, 3 }, PreloadWindow.Compute(5, 20));
    }

    [Fact]
    public void Start_of_session_does_not_wrap_backwards()
    {
        Assert.Equal(new[] { 0, 1, 2, 3 }, PreloadWindow.Compute(0, 10));
        Assert.Equal(new[] { 1, 2, 3, 4, 0 }, PreloadWindow.Compute(1, 10));
    }

    [Fact]
    public void End_of_session_does_not_wrap_forwards()
    {
        Assert.Equal(new[] { 9, 8, 7 }, PreloadWindow.Compute(9, 10));
        Assert.Equal(new[] { 8, 9, 7, 6 }, PreloadWindow.Compute(8, 10));
    }

    [Fact]
    public void Negative_count_throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PreloadWindow.Compute(0, -1));
    }
}
