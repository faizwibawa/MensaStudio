using MensaStudio.Core;
using Xunit;

namespace MensaStudio.Core.Tests.Unit;

public class SessionTests
{
    private static PhotoEntry Photo(string name, string? folder = null, int? rating = null) =>
        new(name, Array.Empty<string>(), folder, rating);

    private static Session ThreePhotos() =>
        new("C:\\photos", new[] { Photo("a.jpg"), Photo("b.jpg", "Good", 3), Photo("c.jpg", rating: 5) }, 0);

    [Fact]
    public void Empty_session_has_current_index_minus_one_and_mutations_are_no_ops()
    {
        var session = new Session("C:\\photos", Array.Empty<PhotoEntry>(), 0);

        Assert.Equal(-1, session.CurrentIndex);
        Assert.Null(session.Current);
        Assert.False(session.SetFolder("Good"));
        Assert.False(session.ClearFolder());
        Assert.False(session.SetRating(3));
        Assert.Equal(0L, session.Version);
        Assert.Null(session.Snapshot().CurrentFileName);
    }

    [Fact]
    public void Constructor_computes_counts_from_entries()
    {
        var session = ThreePhotos();

        Assert.Equal(1, session.AssignedCount);
        Assert.Equal(2, session.UnassignedCount);
        Assert.Equal(2, session.RatedCount);
    }

    [Fact]
    public void Constructor_rejects_duplicate_names_ignoring_case_and_out_of_range_index()
    {
        Assert.Throws<ArgumentException>(() => new Session("C:\\p", new[] { Photo("a.jpg"), Photo("A.JPG") }, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Session("C:\\p", new[] { Photo("a.jpg") }, 1));
    }

    [Fact]
    public void SetFolder_and_ClearFolder_update_counts_version_and_raise_changed()
    {
        var session = ThreePhotos();
        var changes = new List<SessionChange>();
        session.Changed += (_, c) => changes.Add(c);

        Assert.True(session.SetFolder("Bad"));
        Assert.False(session.SetFolder("Bad"));
        Assert.Equal("Bad", session.Current!.Folder);
        Assert.Equal(2, session.AssignedCount);

        Assert.True(session.ClearFolder());
        Assert.False(session.ClearFolder());
        Assert.Null(session.Current!.Folder);
        Assert.Equal(1, session.AssignedCount);
        Assert.Equal(0, session.CurrentIndex);

        Assert.Equal(2L, session.Version);
        Assert.Equal(
            new[] { new SessionChange(SessionChangeKind.Folder, 0, 1), new SessionChange(SessionChangeKind.Folder, 0, 2) },
            changes);
    }

    [Fact]
    public void SetRating_sets_and_clears_rating_and_rejects_out_of_range()
    {
        var session = ThreePhotos();

        Assert.True(session.SetRating(4));
        Assert.Equal(4, session.Current!.Rating);
        Assert.Equal(3, session.RatedCount);
        Assert.False(session.SetRating(4));

        Assert.True(session.SetRating(null));
        Assert.False(session.SetRating(null));
        Assert.Equal(2, session.RatedCount);

        Assert.Throws<ArgumentOutOfRangeException>(() => session.SetRating(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => session.SetRating(6));
    }

    [Fact]
    public void MoveTo_changes_position_only_when_different()
    {
        var session = ThreePhotos();
        SessionChange? last = null;
        session.Changed += (_, c) => last = c;

        Assert.False(session.MoveTo(0));
        Assert.Null(last);
        Assert.True(session.MoveTo(2));
        Assert.Equal(2, session.CurrentIndex);
        Assert.Equal(new SessionChange(SessionChangeKind.Position, 2, 1), last);
        Assert.Throws<ArgumentOutOfRangeException>(() => session.MoveTo(3));
    }

    [Fact]
    public void Snapshot_is_an_independent_copy()
    {
        var session = ThreePhotos();
        session.MoveTo(1);
        var snapshot = session.Snapshot();

        session.SetFolder("Okay");
        session.MoveTo(2);

        Assert.Equal("b.jpg", snapshot.CurrentFileName);
        Assert.Equal("Good", snapshot.Photos[1].Folder);
        Assert.Equal("Okay", session.Snapshot().Photos[1].Folder);
    }

    [Fact]
    public void SessionStatistics_counts_assigned_unassigned_and_rated()
    {
        var stats = SessionStatistics.Compute(new[] { Photo("a"), Photo("b", "X", 1), Photo("c", "Y"), Photo("d", rating: 2) });

        Assert.Equal((2, 2, 2), stats);
    }
}
