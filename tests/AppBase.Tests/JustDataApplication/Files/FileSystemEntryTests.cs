using JustData.Application.Files;

namespace AppBase.Tests.JustDataApplication.Files;

public sealed class FileSystemEntryTests
{
    [Fact]
    public void FileSystemEntry_creates_for_file()
    {
        var entry = new FileSystemEntry(@"C:\test.sql", false);
        Assert.Equal(@"C:\test.sql", entry.Path);
        Assert.False(entry.IsDirectory);
        Assert.Null(entry.LastWriteTimeUtc);
    }

    [Fact]
    public void FileSystemEntry_creates_for_directory()
    {
        var entry = new FileSystemEntry(@"C:\folder", true, DateTime.UtcNow);
        Assert.True(entry.IsDirectory);
        Assert.NotNull(entry.LastWriteTimeUtc);
    }

    [Fact]
    public void FileEnumerationOptions_creates_correctly()
    {
        var options = new FileEnumerationOptions(new[] { ".sql" }, true, false);
        Assert.Single(options.Extensions);
        Assert.True(options.SortByLastWrite);
        Assert.False(options.SortByName);
    }

    [Fact]
    public void FileSearchRequest_defaults()
    {
        var req = new FileSearchRequest("SELECT", new[] { ".sql" });
        Assert.Equal("SELECT", req.Query);
        Assert.False(req.MatchWholeWord);
        Assert.False(req.MatchCase);
        Assert.False(req.UseRegex);
        Assert.Equal(200, req.MaxFiles);
        Assert.Equal(50, req.MaxMatchesPerFile);
        Assert.Null(req.Timeout);
    }

    [Fact]
    public void FileSearchMatch_creates_correctly()
    {
        var match = new FileSearchMatch(5, "SELECT *", 0, 6);
        Assert.Equal(5, match.LineNumber);
        Assert.Equal("SELECT *", match.LineText);
        Assert.Equal(0, match.MatchIndex);
        Assert.Equal(6, match.MatchLength);
    }

    [Fact]
    public void FileSearchFileResult_creates_correctly()
    {
        var matches = new List<FileSearchMatch> { new(1, "SELECT", 0, 6) };
        var result = new FileSearchFileResult(@"C:\test.sql", matches, false);
        Assert.Single(result.Matches);
        Assert.False(result.IsTruncated);
    }

    [Fact]
    public void FileSearchResult_creates_correctly()
    {
        var files = new List<FileSearchFileResult>();
        var result = new FileSearchResult(files, false, false, 0);
        Assert.Empty(result.Files);
        Assert.False(result.WasCancelled);
        Assert.False(result.WasTruncated);
        Assert.Equal(0, result.MatchCount);
    }

    [Fact]
    public void FileChange_created()
    {
        var change = new FileChange(FileChangeKind.Deleted, @"C:\test.sql");
        Assert.Equal(FileChangeKind.Deleted, change.Kind);
        Assert.Equal(@"C:\test.sql", change.Path);
        Assert.Null(change.OldPath);
    }

    [Fact]
    public void FileChange_with_old_path()
    {
        var change = new FileChange(FileChangeKind.Renamed, @"C:\new.sql", @"C:\old.sql");
        Assert.Equal(FileChangeKind.Renamed, change.Kind);
        Assert.Equal(@"C:\old.sql", change.OldPath);
    }

    [Fact]
    public void FileChangeKind_has_expected_values()
    {
        Assert.Equal(0, (int)FileChangeKind.Created);
        Assert.Equal(1, (int)FileChangeKind.Deleted);
        Assert.Equal(2, (int)FileChangeKind.Renamed);
    }

    [Fact]
    public void RecentFileKind_has_expected_values()
    {
        Assert.Equal(0, (int)RecentFileKind.Single);
        Assert.Equal(1, (int)RecentFileKind.ManySql);
    }
}
