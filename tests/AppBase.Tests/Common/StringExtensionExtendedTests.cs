using AppBase.Common;

namespace AppBase.Tests.Common;

public sealed class StringExtensionExtendedTests
{
    // ── SqlSplit ──

    [Fact]
    public void SqlSplit_simple_csv()
    {
        string[] parts = "a,b,c".SqlSplit();
        Assert.Equal(3, parts.Length);
        Assert.Equal("a", parts[0]);
        Assert.Equal("b", parts[1]);
        Assert.Equal("c", parts[2]);
    }

    [Fact]
    public void SqlSplit_respects_single_quotes()
    {
        string[] parts = "'hello, world',b".SqlSplit();
        Assert.Equal(2, parts.Length);
        Assert.Equal("'hello, world'", parts[0].Trim());
        Assert.Equal("b", parts[1].Trim());
    }

    [Fact]
    public void SqlSplit_respects_double_quotes()
    {
        string[] parts = "\"col,name\",other".SqlSplit();
        Assert.Equal(2, parts.Length);
    }

    [Fact]
    public void SqlSplit_single_element()
    {
        string[] parts = "only".SqlSplit();
        Assert.Single(parts);
        Assert.Equal("only", parts[0]);
    }

    // ── IsAllComments ──

    [Fact]
    public void IsAllComments_single_line_comment()
    {
        Assert.True("-- this is a comment".IsAllComments());
    }

    [Fact]
    public void IsAllComments_block_comment()
    {
        Assert.True("/* block comment */".IsAllComments());
    }

    [Fact]
    public void IsAllComments_mixed_comments()
    {
        Assert.True("-- line\n/* block */".IsAllComments());
    }

    [Fact]
    public void IsAllComments_code_is_not_all_comments()
    {
        Assert.False("SELECT 1".IsAllComments());
    }

    [Fact]
    public void IsAllComments_whitespace_only()
    {
        Assert.True("   \n\t  ".IsAllComments());
    }

    [Fact]
    public void IsAllComments_empty_string()
    {
        Assert.True("".IsAllComments());
    }

    // ── DotCounter ──

    [Fact]
    public void DotCounter_counts_unquoted_dots()
    {
        Assert.Equal(2, "schema.table.column".DotCounter());
    }

    [Fact]
    public void DotCounter_ignores_dots_inside_quotes()
    {
        Assert.Equal(1, "\"schema.table\".column".DotCounter());
    }

    [Fact]
    public void DotCounter_no_dots()
    {
        Assert.Equal(0, "column".DotCounter());
    }

    // ── FirstDot / LastDot ──

    [Fact]
    public void FirstDot_finds_first_unquoted_dot()
    {
        Assert.Equal(6, "schema.table.column".FirstDot());
    }

    [Fact]
    public void FirstDot_ignores_quoted_dots()
    {
        // "schema.table".column → first unquoted dot is at index 14
        Assert.Equal(14, "\"schema.table\".column".FirstDot());
    }

    [Fact]
    public void FirstDot_no_dot_returns_minus_one()
    {
        Assert.Equal(-1, "column".FirstDot());
    }

    [Fact]
    public void LastDot_finds_last_unquoted_dot()
    {
        Assert.Equal(12, "schema.table.column".LastDot());
    }

    [Fact]
    public void LastDot_ignores_quoted_dots()
    {
        Assert.Equal(11, "\"a.b\".table.".LastDot());
    }

    [Fact]
    public void LastDot_no_dot_returns_minus_one()
    {
        Assert.Equal(-1, "column".LastDot());
    }

    // ── LastDotSpaceOrNewline ──

    [Fact]
    public void LastDotSpaceOrNewline_finds_last_separator()
    {
        Assert.Equal(6, "SELECT col".LastDotSpaceOrNewline());
    }

    [Fact]
    public void LastDotSpaceOrNewline_finds_newline()
    {
        Assert.Equal(6, "SELECT\nFROM".LastDotSpaceOrNewline());
    }

    // ── LeftParenthesesBalance ──

    [Fact]
    public void LeftParenthesesBalance_balanced()
    {
        Assert.Equal(0, "func(a, b)".LeftParenthesesBalance());
    }

    [Fact]
    public void LeftParenthesesBalance_unbalanced_left()
    {
        Assert.Equal(1, "func(a".LeftParenthesesBalance());
    }

    [Fact]
    public void LeftParenthesesBalance_unbalanced_right()
    {
        Assert.Equal(-1, ")func(a)".LeftParenthesesBalance());
    }

    [Fact]
    public void LeftParenthesesBalance_ignores_parens_in_quotes()
    {
        Assert.Equal(1, "\"(a, b)\"(".LeftParenthesesBalance());
    }

    [Fact]
    public void LeftParenthesesBalance_with_length_parameter()
    {
        Assert.Equal(1, "func(a, b)".LeftParenthesesBalance(5));
    }

    // ── LastWord ──

    [Fact]
    public void LastWord_extracts_after_space()
    {
        Assert.Equal("K1", "COL AS K1".LastWord());
    }

    [Fact]
    public void LastWord_extracts_after_dot()
    {
        Assert.Equal("col", "table.col".LastWord());
    }

    [Fact]
    public void LastWord_single_word()
    {
        Assert.Equal("column", "column".LastWord());
    }

    // ── NormalizeName ──

    [Fact]
    public void NormalizeName_replaces_special_characters()
    {
        var result = "col-name".NormalizeName([]);
        Assert.Equal("COL_NAME", result);
    }

    [Fact]
    public void NormalizeName_replaces_polish_characters()
    {
        var result = "KOLUMNA ĄĆĘŁŃÓŚŻŹ".NormalizeName([]);
        Assert.Equal("KOLUMNA_ACELNOSZZ", result);
    }

    [Fact]
    public void NormalizeName_prepends_K_when_starts_with_digit()
    {
        var result = "123col".NormalizeName([]);
        Assert.StartsWith("K", result);
    }

    [Fact]
    public void NormalizeName_appends_underscore_for_reserved_keywords()
    {
        var result = "select".NormalizeName(["select"]);
        Assert.Equal("SELECT_", result);
    }

    [Fact]
    public void NormalizeName_truncates_at_128_characters()
    {
        var longName = new string('A', 130);
        var result = longName.NormalizeName([]);
        Assert.Equal(126, result.Length);
    }

    [Fact]
    public void NormalizeName_removes_leading_underscores()
    {
        var result = "__col".NormalizeName([]);
        Assert.Equal("COL", result);
    }

    // ── CutToLongNumeric ──

    [Fact]
    public void CutToLongNumeric_truncates_long_decimals()
    {
        Assert.Equal("12.12345678", "12.12345678901".CutToLongNumeric());
    }

    [Fact]
    public void CutToLongNumeric_short_decimal_unchanged()
    {
        Assert.Equal("12.5", "12.5".CutToLongNumeric());
    }

    [Fact]
    public void CutToLongNumeric_integer_unchanged()
    {
        Assert.Equal("42", "42".CutToLongNumeric());
    }

    [Fact]
    public void CutToLongNumeric_custom_precision_short_decimal()
    {
        // When decimal part length ≤ 8, the method returns the input unchanged
        // (the precision parameter only affects truncation when decimal > 8 digits)
        Assert.Equal("12.123456", "12.123456".CutToLongNumeric(3));
    }

    [Fact]
    public void CutToLongNumeric_truncates_when_decimal_exceeds_eight_digits()
    {
        // Only truncates when decimal part has more than 8 digits
        Assert.Equal("12.12345678", "12.12345678901".CutToLongNumeric(8));
    }

    // ── RemoveDuplicates ──

    [Fact]
    public void RemoveDuplicates_appends_numbers_to_duplicates()
    {
        string[] list = ["col", "col", "col", "other"];
        StringExtension.RemoveDuplicates(list);

        // RemoveDuplicates appends _N suffix to all occurrences of duplicated names
        Assert.Equal("col_1", list[0]);
        Assert.Equal("col_2", list[1]);
        Assert.Equal("col_3", list[2]);
        Assert.Equal("other", list[3]);
    }

    [Fact]
    public void RemoveDuplicates_no_duplicates_unchanged()
    {
        string[] list = ["a", "b", "c"];
        StringExtension.RemoveDuplicates(list);

        Assert.Equal(["a", "b", "c"], list);
    }

    // ── IsGoodName ──

    [Fact]
    public void IsGoodName_valid_uppercase_name()
    {
        Assert.True("COLUMN_1".IsGoodName());
    }

    [Fact]
    public void IsGoodName_invalid_with_lowercase()
    {
        Assert.False("col_name".IsGoodName());
    }

    [Fact]
    public void IsGoodName_invalid_with_special_character()
    {
        Assert.False("col-name".IsGoodName());
    }

    [Fact]
    public void IsGoodName_empty_string()
    {
        Assert.True("".IsGoodName());
    }

    // ── StartsWithAny / EndsWithAny / ContainsAny ──

    [Fact]
    public void StartsWithAny_matches()
    {
        Assert.True("SELECT * FROM".StartsWithAny(["SELECT", "INSERT"]));
    }

    [Fact]
    public void StartsWithAny_no_match()
    {
        Assert.False("DELETE FROM".StartsWithAny(["SELECT", "INSERT"]));
    }

    [Fact]
    public void EndsWithAny_matches()
    {
        Assert.True("SELECT * FROM t".EndsWithAny(["FROM t", "FROM x"]));
    }

    [Fact]
    public void EndsWithAny_no_match()
    {
        Assert.False("SELECT * FROM t".EndsWithAny(["FROM x", "FROM y"]));
    }

    [Fact]
    public void ContainsAny_matches()
    {
        Assert.True("SELECT id FROM users".ContainsAny(["id", "name"]));
    }

    [Fact]
    public void ContainsAny_no_match()
    {
        Assert.False("SELECT id FROM users".ContainsAny(["email", "phone"]));
    }

    // ── UnquoteName ──

    [Fact]
    public void UnquoteName_removes_surrounding_quotes()
    {
        Assert.Equal("TABLE", StringExtension.UnquoteName("\"TABLE\""));
    }

    [Fact]
    public void UnquoteName_unquoted_unchanged()
    {
        Assert.Equal("TABLE", StringExtension.UnquoteName("TABLE"));
    }

    // ── QuoteNameIfNeeded ──

    [Fact]
    public void QuoteNameIfNeeded_quotes_name_with_hyphen()
    {
        Assert.Equal("\"col-name\"", StringExtension.QuoteNameIfNeeded("col-name"));
    }

    [Fact]
    public void QuoteNameIfNeeded_leaves_proper_name()
    {
        Assert.Equal("COLUMN_1", StringExtension.QuoteNameIfNeeded("COLUMN_1"));
    }

    [Fact]
    public void QuoteNameIfNeeded_escapes_embedded_quotes()
    {
        Assert.Equal("\"col\"\"name\"", StringExtension.QuoteNameIfNeeded("col\"name"));
    }

    // ── SqlSplitAdvanced ──

    [Fact]
    public void SqlSplitAdvanced_ignores_comments()
    {
        string[] parts = "a, -- comment\nb".SqlSplitAdvanced();
        Assert.Equal(2, parts.Length);
        Assert.Equal("a", parts[0].Trim());
        Assert.Contains("b", parts[1]);
    }

    [Fact]
    public void SqlSplitAdvanced_ignores_block_comments()
    {
        string[] parts = "a, /* comment */ b".SqlSplitAdvanced();
        Assert.Equal(2, parts.Length);
    }
}
