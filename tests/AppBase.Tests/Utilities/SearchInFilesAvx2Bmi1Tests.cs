using System.Text;
using AppBase.Services.Utilities;

namespace AppBase.Tests.Utilities;

public sealed class SearchInFilesAvx2Bmi1Tests : IDisposable
{
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "SearchInFilesAvx2Bmi1Tests_" + Guid.NewGuid().ToString("N"));

    public SearchInFilesAvx2Bmi1Tests()
    {
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }
        catch
        {
            // best-effort cleanup
        }
    }

    [Fact]
    public void FindInFileSmallSteps_finds_case_insensitive_match()
    {
        string path = WriteFile("hit.txt", "hello SELECT world");
        var search = new SearchInFilesAvx2Bmi1(path.AsSpan(), "select".AsSpan());

        int offset = search.FindInFileSmallSteps(path.AsSpan(), "select".AsSpan());

        Assert.True(offset >= 0);
        AssertFileContainsAt(path, offset, "SELECT");
    }

    [Fact]
    public void FindInFileSmallSteps_returns_minus_one_when_missing()
    {
        string path = WriteFile("miss.txt", "alpha beta gamma");
        var search = new SearchInFilesAvx2Bmi1(path.AsSpan(), "omega".AsSpan());

        Assert.Equal(-1, search.FindInFileSmallSteps(path.AsSpan(), "omega".AsSpan()));
    }

    [Fact]
    public void FindInFileSmallSteps_skips_utf8_bom()
    {
        string path = Path.Combine(_tempDir, "bom.txt");
        byte[] content = Encoding.UTF8.GetPreamble().Concat(Utf8NoBom.GetBytes("needle here")).ToArray();
        File.WriteAllBytes(path, content);
        var search = new SearchInFilesAvx2Bmi1(path.AsSpan(), "needle".AsSpan());

        int offset = search.FindInFileSmallSteps(path.AsSpan(), "needle".AsSpan());

        Assert.True(offset >= 0);
    }

    [Fact]
    public void FindInFileSmallSteps_finds_match_across_buffer_boundary()
    {
        // BUFFER_SIZE is 65536; place the needle so it straddles two reads.
        const string needle = "CROSSBOUNDARY";
        var prefix = new string('a', 65_536 - 5);
        string path = WriteFile("boundary.txt", prefix + needle + "zzzz");
        var search = new SearchInFilesAvx2Bmi1(path.AsSpan(), needle.AsSpan());

        int offset = search.FindInFileSmallSteps(path.AsSpan(), needle.AsSpan());

        Assert.True(offset >= 0);
        AssertFileContainsAt(path, offset, needle);
    }

    [Fact]
    public void SearchInFileOriginal_finds_line_containing_text()
    {
        string path = WriteFile("original.txt", "line1\nfind ME please\nline3");

        Assert.True(SearchInFilesAvx2Bmi1.SearchInFileOriginal(path, "find me"));
        Assert.False(SearchInFilesAvx2Bmi1.SearchInFileOriginal(path, "absent"));
    }

    private string WriteFile(string name, string content)
    {
        string path = Path.Combine(_tempDir, name);
        File.WriteAllText(path, content, Utf8NoBom);
        return path;
    }

    private static void AssertFileContainsAt(string path, int offset, string expected)
    {
        byte[] bytes = File.ReadAllBytes(path);
        Assert.True(offset + expected.Length <= bytes.Length,
            $"offset {offset} + length {expected.Length} exceeds file size {bytes.Length}");

        string actual = Utf8NoBom.GetString(bytes, offset, expected.Length);
        Assert.Equal(expected, actual, ignoreCase: true);
    }
}
