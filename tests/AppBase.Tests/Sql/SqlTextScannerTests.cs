using AppBase.Services.Sql;

namespace AppBase.Tests.Sql;

public sealed class SqlTextScannerTests
{
    [Theory]
    [InlineData("select '$value'", 8, true)]
    [InlineData("select '$value'", 16, false)]
    [InlineData("select \"$value\"", 8, true)]
    [InlineData("select '$value' || $value", 23, false)]
    public void IsInsideQuotedLiteral_TracksSqlQuotes(string sql, int position, bool expected)
    {
        Assert.Equal(expected, SqlTextScanner.IsInsideQuotedLiteral(sql, position));
    }

    [Fact]
    public void IsInsideQuotedLiteral_SupportsEscapedQuotes()
    {
        const string sql = "select 'it''s $value'";
        int variablePosition = sql.IndexOf("$value", StringComparison.Ordinal);

        Assert.True(SqlTextScanner.IsInsideQuotedLiteral(sql, variablePosition));
    }

    [Theory]
    [InlineData("select 1 -- $value\n, $other", "$value", true)]
    [InlineData("select 1 -- $value\n, $other", "$other", false)]
    [InlineData("select /* $value */ 1", "$value", true)]
    [InlineData("select /* outer /* $value */ inner */ 1", "$value", true)]
    [InlineData("select '-- $value'", "$value", false)]
    [InlineData("select '/* $value */'", "$value", false)]
    public void IsInsideComment_TracksSqlCommentsAndIgnoresQuotedMarkers(
        string sql,
        string token,
        bool expected)
    {
        Assert.Equal(expected, SqlTextScanner.IsInsideComment(sql, sql.IndexOf(token, StringComparison.Ordinal)));
    }
}
