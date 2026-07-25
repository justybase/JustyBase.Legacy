using AppBase.Common;
using AppBase.Services.Sql;

namespace AppBase.Tests.Sql;

public sealed class SqlTextScannerExtendedTests
{
    // ── Interface contract ──

    [Fact]
    public void Implements_ISqlTextScanner()
    {
        Assert.IsAssignableFrom<ISqlTextScanner>(new SqlTextScanner());
    }

    [Fact]
    public void Default_is_singleton()
    {
        Assert.Same(SqlTextScanner.Default, SqlTextScanner.Default);
    }

    [Fact]
    public void Default_implements_interface()
    {
        Assert.IsAssignableFrom<ISqlTextScanner>(SqlTextScanner.Default);
    }

    [Fact]
    public void Static_methods_delegate_to_default()
    {
        // Static calls should produce same result as instance calls
        const string sql = "select 'hello'";
        int pos = sql.IndexOf("hello", StringComparison.Ordinal);
        var staticResult = SqlTextScanner.IsInsideQuotedLiteral(sql, pos);
        var instanceResult = SqlTextScanner.Default.DoIsInsideQuotedLiteral(sql, pos);
        Assert.Equal(staticResult, instanceResult);
    }

    [Fact]
    public void Interface_methods_delegate_correctly()
    {
        ISqlTextScanner scanner = SqlTextScanner.Default;
        const string sql = "select 'hello'";
        int pos = sql.IndexOf("hello", StringComparison.Ordinal);
        var interfaceResult = scanner.IsInsideQuotedLiteral(sql, pos);
        var instanceResult = SqlTextScanner.Default.DoIsInsideQuotedLiteral(sql, pos);
        Assert.Equal(interfaceResult, instanceResult);
    }

    // ── IsInsideQuotedLiteral — edge cases ──

    [Theory]
    [InlineData(null, 0, false)]
    [InlineData("", 0, false)]
    [InlineData(" ", 0, false)]
    [InlineData("select 1", 0, false)]
    [InlineData("select 1", -1, false)]
    public void IsInsideQuotedLiteral_null_or_empty(string? sql, int position, bool expected)
    {
        Assert.Equal(expected, SqlTextScanner.IsInsideQuotedLiteral(sql!, position));
    }

    [Theory]
    [InlineData("'hello'", 0, false)]        // position 0 — before any char
    [InlineData("'hello'", 1, true)]         // just inside opening quote
    [InlineData("'hello'", 6, true)]         // inside the literal
    [InlineData("'hello'", 7, false)]        // after closing quote
    [InlineData("'hello'", 99, false)]       // position beyond length, clamped
    public void IsInsideQuotedLiteral_position_edge_cases(string sql, int position, bool expected)
    {
        Assert.Equal(expected, SqlTextScanner.IsInsideQuotedLiteral(sql, position));
    }

    [Theory]
    [InlineData("'a''b'", 1, true)]   // inside 'a'
    [InlineData("'a''b'", 3, false)]  // second ' of '' pair — escape not detected yet (needs position > 3)
    [InlineData("'a''b'", 4, true)]   // inside 'b'
    [InlineData("'a''b'", 6, false)]  // after closing quote
    public void IsInsideQuotedLiteral_escaped_quotes(string sql, int position, bool expected)
    {
        Assert.Equal(expected, SqlTextScanner.IsInsideQuotedLiteral(sql, position));
    }

    [Theory]
    [InlineData("\"hello\"", 1, true)]
    [InlineData("\"hello\"", 7, false)]
    [InlineData("select \"col\" from t", 8, true)]   // inside double-quoted identifier
    [InlineData("select \"col\" from t", 12, false)] // after closing double quote (position 12 is space)
    public void IsInsideQuotedLiteral_double_quotes(string sql, int position, bool expected)
    {
        Assert.Equal(expected, SqlTextScanner.IsInsideQuotedLiteral(sql, position));
    }

    [Theory]
    [InlineData("'mix\"ed' \"test'ing\"", 1, true)]   // inside single-quoted with double inside
    [InlineData("'mix\"ed' \"test'ing\"", 12, true)]  // inside double-quoted with single inside
    [InlineData("'mix\"ed' \"test'ing\"", 8, false)]  // between the two literals
    public void IsInsideQuotedLiteral_mixed_quotes(string sql, int position, bool expected)
    {
        Assert.Equal(expected, SqlTextScanner.IsInsideQuotedLiteral(sql, position));
    }

    [Theory]
    [InlineData("''", 1, true)]    // empty literal — inside
    [InlineData("''", 2, false)]   // after empty literal
    [InlineData("\"\"", 1, true)]  // empty double-quoted identifier
    public void IsInsideQuotedLiteral_empty_literals(string sql, int position, bool expected)
    {
        Assert.Equal(expected, SqlTextScanner.IsInsideQuotedLiteral(sql, position));
    }

    [Fact]
    public void IsInsideQuotedLiteral_unclosed_at_end()
    {
        const string sql = "select 'unclosed";
        Assert.True(SqlTextScanner.IsInsideQuotedLiteral(sql, 15)); // position at end 'd', inside
        Assert.True(SqlTextScanner.IsInsideQuotedLiteral(sql, 8));  // position just after opening quote
    }

    [Fact]
    public void IsInsideQuotedLiteral_position_at_boundary()
    {
        // position exactly at sql.Length
        const string sql = "'hello'";
        Assert.False(SqlTextScanner.IsInsideQuotedLiteral(sql, sql.Length));
    }

    // ── IsInsideComment — edge cases ──

    [Theory]
    [InlineData(null, 0, false)]
    [InlineData("", 0, false)]
    [InlineData("select 1", 0, false)]
    [InlineData("select 1", -1, false)]
    public void IsInsideComment_null_or_empty(string? sql, int position, bool expected)
    {
        Assert.Equal(expected, SqlTextScanner.IsInsideComment(sql!, position));
    }

    [Theory]
    [InlineData("-- comment\nselect 1", 2, true)]   // inside line comment
    [InlineData("-- comment\nselect 1", 12, false)]  // after newline, not in comment
    [InlineData("-- comment\r\nselect 1", 10, true)]  // inside before \r (position 10 = \r, not processed yet)
    [InlineData("-- comment\r\nselect 1", 12, false)] // after \r\n
    public void IsInsideComment_line_comment_with_newlines(string sql, int position, bool expected)
    {
        Assert.Equal(expected, SqlTextScanner.IsInsideComment(sql, position));
    }

    [Theory]
    [InlineData("/* block */ select 1", 2, true)]   // inside block comment
    [InlineData("/* block */ select 1", 11, false)]  // after block comment close
    [InlineData("/* block */ select 1", 0, false)]   // before block comment starts
    public void IsInsideComment_block_comment(string sql, int position, bool expected)
    {
        Assert.Equal(expected, SqlTextScanner.IsInsideComment(sql, position));
    }

    [Theory]
    [InlineData("/* outer /* inner */ still? */ select 1", 20, true)]  // inside nested block
    [InlineData("/* outer /* inner */ still? */ select 1", 28, true)]  // still inside outer after inner closes
    [InlineData("/* outer /* inner */ still? */ select 1", 37, false)] // after both close
    public void IsInsideComment_nested_block_comments(string sql, int position, bool expected)
    {
        Assert.Equal(expected, SqlTextScanner.IsInsideComment(sql, position));
    }

    [Fact]
    public void IsInsideComment_unclosed_block_comment()
    {
        const string sql = "/* unclosed block";
        Assert.True(SqlTextScanner.IsInsideComment(sql, sql.Length));
        Assert.True(SqlTextScanner.IsInsideComment(sql, 3));
    }

    [Fact]
    public void IsInsideComment_unclosed_line_comment()
    {
        const string sql = "-- no newline at end";
        Assert.True(SqlTextScanner.IsInsideComment(sql, sql.Length));
        Assert.True(SqlTextScanner.IsInsideComment(sql, 3));
    }

    [Theory]
    [InlineData("select '-- not a comment' from t", 9, false)]  // inside quotes, -- is not a comment
    [InlineData("select '/* not a block */'", 9, false)]        // inside quotes, /* is not a block comment
    public void IsInsideComment_quotes_protect_from_comment_markers(string sql, int insidePos, bool expected)
    {
        var actual = SqlTextScanner.IsInsideComment(sql, insidePos);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void IsInsideComment_comment_markers_inside_strings()
    {
        // The '--' inside a string should not start a line comment
        const string sql = "select '--embedded' from t";
        // Position inside the string
        int insidePos = sql.IndexOf("embedded", StringComparison.Ordinal);
        Assert.False(SqlTextScanner.IsInsideComment(sql, insidePos));
    }

    [Fact]
    public void IsInsideComment_mixed_content()
    {
        // Use a controlled SQL with explicit positions
        const string sql = "'hello'";
        // Inside single-quoted string - NOT in a comment
        Assert.False(SqlTextScanner.IsInsideComment(sql, 3)); // 'l' inside 'hello'
        Assert.False(SqlTextScanner.IsInsideComment(sql, 1)); // 'h' inside 'hello'
        Assert.False(SqlTextScanner.IsInsideComment(sql, 5)); // 'o' inside 'hello'
    }

    [Fact]
    public void IsInsideComment_after_block_comment_then_string()
    {
        // String that alternates: block comment, then string literal
        const string sql = "/* block */ 'hello'";
        // Inside block comment
        Assert.True(SqlTextScanner.IsInsideComment(sql, 5)); // inside /* block */
        // After block comment, before string
        Assert.False(SqlTextScanner.IsInsideComment(sql, 12)); // space between */ and '
        // Inside string literal
        Assert.False(SqlTextScanner.IsInsideComment(sql, 15)); // 'l' inside 'hello'
    }

    [Fact]
    public void IsInsideComment_after_line_comment_then_string_with_comment_markers()
    {
        // String: line comment newline then string with /* */ inside it
        const string sql = "-- line\n'hello /* world */'";
        // After line comment ends, inside string
        int inside = sql.IndexOf("hello", StringComparison.Ordinal);
        Assert.False(SqlTextScanner.IsInsideComment(sql, inside)); // inside string, not in comment

        // Inside the string at the /* marker
        int atSlash = sql.IndexOf("/*", sql.IndexOf('\'', StringComparison.Ordinal), StringComparison.Ordinal);
        Assert.False(SqlTextScanner.IsInsideComment(sql, atSlash)); // inside string, /* is not a comment start
    }

    [Fact]
    public void IsInsideComment_double_quote_inside_single_quote()
    {
        // Double quotes inside single-quoted strings should not end the string
        const string sql = "select '\"hello\"' from t";
        int pos = sql.IndexOf("hello", StringComparison.Ordinal);
        Assert.False(SqlTextScanner.IsInsideComment(sql, pos));
        Assert.True(SqlTextScanner.IsInsideQuotedLiteral(sql, pos));
    }

    [Fact]
    public void IsInsideComment_single_quote_inside_double_quote()
    {
        // Single quotes inside double-quoted identifiers should not end the identifier
        const string sql = "select \"it's fine\" from t";
        int pos = sql.IndexOf("fine", StringComparison.Ordinal);
        Assert.False(SqlTextScanner.IsInsideComment(sql, pos));
        Assert.True(SqlTextScanner.IsInsideQuotedLiteral(sql, pos));
    }

    [Fact]
    public void IsInsideQuotedLiteral_complex_escaped_sequence()
    {
        // Multiple escaped quotes in sequence: 'a''''b'
        // Positions: '=0 a=1 '=2 '=3 '=4 '=5 b=6 '=7
        const string sql = "'a''''b'";
        Assert.True(SqlTextScanner.IsInsideQuotedLiteral(sql, 1));  // inside 'a'
        Assert.False(SqlTextScanner.IsInsideQuotedLiteral(sql, 3)); // second ' of first '' pair — escape not yet detected
        Assert.False(SqlTextScanner.IsInsideQuotedLiteral(sql, 5)); // second ' of second '' pair — escape not yet detected
        Assert.True(SqlTextScanner.IsInsideQuotedLiteral(sql, 6));  // inside 'b'
        Assert.False(SqlTextScanner.IsInsideQuotedLiteral(sql, 8)); // after close
    }

    [Fact]
    public void IsInsideComment_very_long_string_no_overflow()
    {
        // Very long SQL with no comments — should not break
        string sql = string.Join(", ", Enumerable.Range(1, 100).Select(i => $"col{i}"));
        string fullSql = $"SELECT {sql} FROM large_table WHERE id = 1";
        Assert.False(SqlTextScanner.IsInsideComment(fullSql, fullSql.Length / 2));
        Assert.False(SqlTextScanner.IsInsideComment(fullSql, fullSql.Length));
    }

    [Fact]
    public void IsInsideQuotedLiteral_very_long_string_no_overflow()
    {
        string sql = string.Join(", ", Enumerable.Range(1, 100).Select(i => $"col{i}"));
        string fullSql = $"SELECT {sql} FROM large_table WHERE id = 1";
        Assert.False(SqlTextScanner.IsInsideQuotedLiteral(fullSql, fullSql.Length / 2));
        Assert.False(SqlTextScanner.IsInsideQuotedLiteral(fullSql, fullSql.Length));
    }
}
