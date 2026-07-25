using AppBase.Services.Sql;
using FastColoredTextBoxNS;
using System.Drawing;

namespace AppBase.Tests.Sql;

public class NetezzaSqlErrorHighlighterTests
{
    private readonly NetezzaSqlErrorHighlighter _highlighter = new();

    private static bool TryMatch(string msg, string sql, out string? word)
    {
        var highlighter = new NetezzaSqlErrorHighlighter();
        if (highlighter.TryGetHighlight(msg, fromOleDb: false, sql, sql.AsSpan(), 0, out var match))
        {
            word = match.Word;
            return true;
        }

        word = null;
        return false;
    }

    [Fact]
    public void TryGetHighlight_AttributeNotFound_ReturnsName()
    {
        Assert.True(TryMatch("ERROR: Attribute 'MY_COL' not found", "select MY_COL from t", out var word));
        Assert.Equal("MY_COL", word);
    }

    [Fact]
    public void TryGetHighlight_WrongSet_ReturnsToken()
    {
        Assert.True(TryMatch("ERROR: 'SET search_path TO bad'", "set search_path to bad", out var word));
        Assert.Equal("search_path TO bad", word);
    }

    [Fact]
    public void TryGetHighlight_ExceptAtChar_ReturnsFoundText()
    {
        const string msg = "ERROR [42000] ERROR: syntax error ^ found \"FOO\" (at char 8) expecting";
        Assert.True(TryMatch(msg, "select FOO from t", out var word));
        Assert.Equal("FOO", word);
    }

    [Fact]
    public void TryGetHighlight_IncorrectDropType_ReturnsObject()
    {
        const string msg = "ERROR: DROP TABLE: object \"ADMIN.T\", incorrect type.";
        Assert.True(TryMatch(msg, "drop table ADMIN.T", out var word));
        Assert.Equal("ADMIN.T", word);
    }

    [Fact]
    public void TryGetHighlight_TransformColumnType_ReturnsType()
    {
        const string msg = "ERROR: transformColumnType: error reading type 'badtype'";
        Assert.True(TryMatch(msg, "select badtype from t", out var word));
        Assert.Equal("badtype", word);
    }

    [Fact]
    public void TryGetHighlight_GroomError_ReturnsTable()
    {
        const string msg = "ERROR: GROOM VERSIONS must be run on ADMIN.T before any other GROOM operation";
        Assert.True(TryMatch(msg, "groom table ADMIN.T", out var word));
        Assert.Equal("ADMIN.T", word);
    }

    [Fact]
    public void TryGetHighlight_RepeatedAttribute_ReturnsName()
    {
        const string msg = "ERROR: Attribute 'COL1' is repeated. Must have an appropriate alias.";
        Assert.True(TryMatch(msg, "select COL1, COL1 from t", out var word));
        Assert.Equal("COL1", word);
    }

    [Fact]
    public void TryGetHighlight_AlreadyExists_ReturnsObject()
    {
        const string msg = "ERROR: CREATE TABLE: object \"ADMIN.T\" already exists.";
        Assert.True(TryMatch(msg, "create table ADMIN.T", out var word));
        Assert.Equal("ADMIN.T", word);
    }

    [Fact]
    public void TryGetHighlight_RelationNotExists_ReturnsRelation()
    {
        const string msg = "ERROR: relation does not exist TEST.ADMIN.MISSING";
        Assert.True(TryMatch(msg, "select * from TEST.ADMIN.MISSING", out var word));
        Assert.Equal("MISSING", word);
    }

    [Fact]
    public void TryGetHighlight_FunctionNotExists_ReturnsName()
    {
        const string msg = "ERROR: Function 'bad_fn(x int)' does not exist";
        Assert.True(TryMatch(msg, "select bad_fn(1)", out var word));
        Assert.Equal("bad_fn", word);
    }

    [Fact]
    public void TryGetHighlight_GroupError_ReturnsColumn()
    {
        const string msg = "ERROR: Attribute COL1 must be GROUPed or used in an aggregate function";
        Assert.True(TryMatch(msg, "select COL1 from t group by 1", out var word));
        Assert.Equal("COL1", word);
    }

    [Fact]
    public void TryGetHighlight_WrongOption_ReturnsOption()
    {
        const string msg = "ERROR: Option 'BADOPT' is not recognized";
        Assert.True(TryMatch(msg, "select * from t with (BADOPT=1)", out var word));
        Assert.Equal("BADOPT", word);
    }

    [Fact]
    public void TryGetHighlight_DuplicateTableAlias_ReturnsName()
    {
        const string msg = "ERROR: Table name \"T1\" specified more than once";
        Assert.True(TryMatch(msg, "from t t1, t t1", out var word));
        Assert.Equal("T1", word);
    }

    [Fact]
    public void TryGetHighlight_AmbiguousColumn_SetsRegex2()
    {
        const string msg = "ERROR: Column reference \"ID\" is ambiguous";
        var highlighter = new NetezzaSqlErrorHighlighter();
        Assert.True(highlighter.TryGetHighlight(msg, false, "select id from a join b using(id)", "select id".AsSpan(), 0, out var match));
        Assert.Equal("ID", match.Word);
        Assert.True(match.UseRegex2);
    }

    [Fact]
    public void TryGetHighlight_CouldNotAcquireLock_ReturnsDatabase()
    {
        const string msg = "ERROR: DROP DATABASE: could not acquire lock for \"MYDB\"";
        Assert.True(TryMatch(msg, "drop database MYDB", out var word));
        Assert.Equal("MYDB", word);
    }

    [Fact]
    public void TryGetHighlight_Hy000PermissionDenied_ReturnsObject()
    {
        const string msg = "ERROR [HY000] ERROR:  Permission denied on \"ADMIN.T\".";
        Assert.True(TryMatch(msg, "select * from ADMIN.T", out var word));
        Assert.Equal("ADMIN.T", word);
    }

    [Fact]
    public void TryGetHighlight_Hy000ObjectAlreadyExists_ReturnsObject()
    {
        const string msg = "ERROR [HY000] ERROR:  CREATE TEMP TABLE: object \"POM3\" already exists.";
        Assert.True(TryMatch(msg, "create temp table POM3", out var word));
        Assert.Equal("POM3", word);
    }

    [Fact]
    public void TryGetHighlight_Hy000SchemaMissing_ReturnsSchema()
    {
        const string msg = "ERROR [HY000] ERROR:  Schema 'BADSCHEMA' does not exist";
        Assert.True(TryMatch(msg, "set schema badschema", out var word));
        Assert.Equal("BADSCHEMA", word);
    }

    [Fact]
    public void TryGetHighlight_42S02_ReturnsTrailingIdentifier()
    {
        const string msg = "ERROR [42S02] ERROR:  relation does not exist TEST.ADMIN.SSFSDFGSDG";
        Assert.True(TryMatch(msg, "select * from TEST.ADMIN.SSFSDFGSDG", out var word));
        Assert.Equal("SSFSDFGSDG", word);
    }

    [Fact]
    public void TryGetHighlight_42S22_ReturnsAttribute()
    {
        const string msg = "ERROR [42S22] ERROR:  Attribute 'alias.column' not found";
        Assert.True(TryMatch(msg, "select alias.column from t", out var word));
        Assert.Equal("alias.column", word);
    }

    [Fact]
    public void TryGetHighlight_UnknownMessage_ReturnsFalse()
    {
        Assert.False(TryMatch("ERROR: something completely unknown happened", "select 1", out _));
    }

    [Fact]
    public void Highlight_WhenEditorChangedAndCapturedStartIsStale_DoesNotThrow()
    {
        using var editor = new FastColoredTextBox { Text = "select 1" };
        var style = new TextStyle(Brushes.White, Brushes.Red, FontStyle.Regular);

        _highlighter.Highlight(
            "ERROR: Attribute 'MISSING' not found",
            editor,
            style,
            selectionStart: 100,
            selectionLength: 20);
    }
}
