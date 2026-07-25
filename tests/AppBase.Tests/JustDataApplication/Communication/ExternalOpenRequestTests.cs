using JustData.Application.Communication;

namespace AppBase.Tests.JustDataApplication.Communication;

public sealed class ExternalOpenRequestTests
{
    [Fact]
    public void TryCreate_with_sql_path_returns_true()
    {
        bool result = ExternalOpenRequest.TryCreate(@"C:\test\query.sql", out var request);
        Assert.True(result);
        Assert.NotNull(request);
        Assert.EndsWith("query.sql", request!.Path, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryCreate_with_manysql_path_returns_true()
    {
        bool result = ExternalOpenRequest.TryCreate(@"C:\test\bundle.manysql", out var request);
        Assert.True(result);
        Assert.NotNull(request);
    }

    [Fact]
    public void TryCreate_with_manysql_enc_path_returns_true()
    {
        bool result = ExternalOpenRequest.TryCreate(@"C:\test\bundle.manysql.enc", out var request);
        Assert.True(result);
        Assert.NotNull(request);
    }

    [Fact]
    public void TryCreate_with_null_returns_false()
    {
        bool result = ExternalOpenRequest.TryCreate(null, out var request);
        Assert.False(result);
        Assert.Null(request);
    }

    [Fact]
    public void TryCreate_with_empty_returns_false()
    {
        bool result = ExternalOpenRequest.TryCreate("", out var request);
        Assert.False(result);
        Assert.Null(request);
    }

    [Fact]
    public void TryCreate_with_whitespace_returns_false()
    {
        bool result = ExternalOpenRequest.TryCreate("   ", out var request);
        Assert.False(result);
        Assert.Null(request);
    }

    [Fact]
    public void TryCreate_with_null_char_returns_false()
    {
        bool result = ExternalOpenRequest.TryCreate("test\0.sql", out var request);
        Assert.False(result);
        Assert.Null(request);
    }

    [Fact]
    public void TryCreate_with_invalid_extension_returns_false()
    {
        bool result = ExternalOpenRequest.TryCreate(@"C:\test\readme.txt", out var request);
        Assert.False(result);
        Assert.Null(request);
    }

    [Fact]
    public void TryCreate_with_non_existent_path_still_returns_true()
    {
        // GetFullPath doesn't check existence
        bool result = ExternalOpenRequest.TryCreate(@"C:\nonexistent\file.sql", out var request);
        Assert.True(result);
        Assert.NotNull(request);
    }

    [Fact]
    public void TryCreate_trims_path()
    {
        bool result = ExternalOpenRequest.TryCreate(@"  C:\test\query.sql  ", out var request);
        Assert.True(result);
        Assert.NotNull(request);
        Assert.DoesNotContain(" ", request!.Path);
    }
}
