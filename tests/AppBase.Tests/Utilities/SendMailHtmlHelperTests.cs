using AppBase.Services.Utilities;

namespace AppBase.Tests.Utilities;

public sealed class SendMailHtmlHelperTests
{
    [Fact]
    public void BuildHtmlBody_null_returns_empty()
    {
        var (html, paths) = SendMailHtmlHelper.BuildHtmlBody(null);
        Assert.Equal(string.Empty, html);
        Assert.Empty(paths);
    }

    [Fact]
    public void BuildHtmlBody_empty_returns_empty()
    {
        var (html, paths) = SendMailHtmlHelper.BuildHtmlBody(string.Empty);
        Assert.Equal(string.Empty, html);
        Assert.Empty(paths);
    }

    [Fact]
    public void BuildHtmlBody_no_images_returns_original()
    {
        var (html, paths) = SendMailHtmlHelper.BuildHtmlBody("Hello world");
        Assert.Equal("Hello world", html);
        Assert.Empty(paths);
    }

    [Fact]
    public void BuildHtmlBody_single_image_inserts_img_tag()
    {
        var (html, paths) = SendMailHtmlHelper.BuildHtmlBody("Before #IMAGE##[c:\\image.png] After");

        Assert.Contains("<img src='cid:", html);
        Assert.Contains("'/>", html);
        Assert.Contains("Before ", html);
        Assert.Contains(" After", html);
        Assert.Single(paths);
        Assert.Equal(@"c:\image.png", paths[0]);
    }

    [Fact]
    public void BuildHtmlBody_multiple_images_returns_all_paths()
    {
        var body = "A #IMAGE##[img1.png] B #IMAGE##[img2.png] C";
        var (html, paths) = SendMailHtmlHelper.BuildHtmlBody(body);

        Assert.Equal(2, paths.Length);
        Assert.Equal("img1.png", paths[0]);
        Assert.Equal("img2.png", paths[1]);
    }

    [Fact]
    public void BuildHtmlBody_consecutive_text_around_images_preserved()
    {
        var (html, paths) = SendMailHtmlHelper.BuildHtmlBody("Start #IMAGE##[a.png] Middle #IMAGE##[b.png] End");

        Assert.StartsWith("Start ", html);
        Assert.Contains(" Middle ", html);
        Assert.EndsWith(" End", html);
    }

    [Fact]
    public void BuildHtmlBody_empty_image_path_produces_empty_path()
    {
        var (html, paths) = SendMailHtmlHelper.BuildHtmlBody("#IMAGE##[]");
        Assert.Single(paths);
        Assert.Equal(string.Empty, paths[0]);
    }

    [Fact]
    public void BuildHtmlBody_image_at_start()
    {
        var (html, paths) = SendMailHtmlHelper.BuildHtmlBody("#IMAGE##[top.png]text");

        Assert.StartsWith("<img src='cid:", html);
        Assert.EndsWith("text", html);
        Assert.Single(paths);
    }

    [Fact]
    public void BuildHtmlBody_image_at_end()
    {
        var (html, paths) = SendMailHtmlHelper.BuildHtmlBody("text#IMAGE##[bottom.png]");

        Assert.StartsWith("text", html);
        Assert.EndsWith("'/>", html);
        Assert.Single(paths);
    }

    [Fact]
    public void BuildHtmlBody_only_image()
    {
        var (html, paths) = SendMailHtmlHelper.BuildHtmlBody("#IMAGE##[only.png]");

        Assert.StartsWith("<img src='cid:", html);
        Assert.Single(paths);
        Assert.Equal("only.png", paths[0]);
    }

    [Fact]
    public void BuildHtmlBody_content_ids_are_unique()
    {
        var body = "#IMAGE##[a.png]#IMAGE##[b.png]";
        var (html, paths) = SendMailHtmlHelper.BuildHtmlBody(body);

        Assert.Equal(2, paths.Length);
        // Extract content IDs from the HTML
        var cid1 = ExtractCid(html, 0);
        var cid2 = ExtractCid(html, 1);
        Assert.NotEqual(cid1, cid2);
    }

    private static string ExtractCid(string html, int occurrence)
    {
        const string prefix = "src='cid:";
        int startIndex = 0;
        for (int i = 0; i <= occurrence; i++)
        {
            startIndex = html.IndexOf(prefix, startIndex, StringComparison.Ordinal);
            if (startIndex < 0) return string.Empty;
            startIndex += prefix.Length;
        }
        var endIndex = html.IndexOf("'", startIndex, StringComparison.Ordinal);
        return endIndex < 0 ? html[startIndex..] : html[startIndex..endIndex];
    }
}
