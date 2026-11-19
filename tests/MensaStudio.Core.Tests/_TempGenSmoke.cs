using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using MensaStudio.Core;
using MensaStudio.Core.Tests.Fakes;
using MensaStudio.Core.Tests.Generators;
using Xunit;

namespace MensaStudio.Core.Tests;

public class _TempGenSmoke
{
    [Property(Arbitrary = new[] { typeof(PhotoArbitraries) }, MaxTest = 200)]
    public void SessionRecordSets_have_unique_names_and_valid_current(SessionRecordSet set)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var r in set.Entries)
        {
            Assert.True(seen.Add(r.FileName), $"duplicate: {r.FileName}");
            Assert.True(r.Rating is null or (>= 1 and <= 5));
            Assert.True(r.Folder is null || r.Folder.Length > 0);
        }

        if (set.CurrentFileName is not null)
        {
            Assert.Contains(set.Entries, e => e.FileName == set.CurrentFileName);
        }
    }

    [Property(Arbitrary = new[] { typeof(PhotoArbitraries) }, MaxTest = 200)]
    public void PhotoNames_are_photos_and_raw_names_are_raws(PhotoFileName photo, RawFileName raw)
    {
        Assert.True(FileKinds.IsPhoto(photo.Value), $"not photo: {photo.Value}");
        Assert.True(FileKinds.IsRaw(raw.Value), $"not raw: {raw.Value}");
        Assert.Equal(photo.Value.TrimEnd(' ', '.'), photo.Value);
    }

    [Fact]
    public void InMemoryFileSystem_move_records_and_refuses_overwrite()
    {
        var fs = new InMemoryFileSystem();
        fs.AddDirectory(@"C:\src");
        fs.AddFile(@"C:\src\a.jpg", "hello");

        fs.MoveFile(@"C:\src\a.jpg", @"C:\src\b.jpg");
        Assert.False(fs.FileExists(@"C:\src\a.jpg"));
        Assert.True(fs.FileExists(@"C:\src\B.JPG")); // OrdinalIgnoreCase

        fs.AddFile(@"C:\src\c.jpg", "x");
        Assert.Throws<IOException>(() => fs.MoveFile(@"C:\src\c.jpg", @"C:\src\b.jpg"));

        Assert.Contains(fs.Operations, o => o.Kind == FileSystemOperationKind.Move);

        fs.FailWrite(@"C:\src\d.jpg");
        Assert.Throws<IOException>(() => fs.WriteAllBytesDurable(@"C:\src\d.jpg", new byte[] { 1 }));

        using var w = fs.OpenAppend(@"C:\src\log.txt");
        w.Write("line\r\n");
        w.Dispose();
        var bytes = fs.GetFileContent(@"C:\src\log.txt")!;
        Assert.False(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF);
    }
}
