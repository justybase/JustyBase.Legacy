using JustData.Application.Editor;
using JustData.Application.Sql;

namespace AppBase.Tests.JustDataApplication.Sql;

public sealed class SqlAuthoringContractsTests
{
    [Fact]
    public void SqlLintRequest_default_connection_is_empty()
    {
        var docId = EditorDocumentId.New();
        var req = new SqlLintRequest(docId, "SELECT 1");
        Assert.Equal(docId, req.DocumentId);
        Assert.Equal("SELECT 1", req.SqlText);
        Assert.Equal("", req.ConnectionName);
        Assert.True(req.IncludeQuickFixes);
    }

    [Fact]
    public void SqlLintRequest_with_optional_fields()
    {
        var docId = EditorDocumentId.New();
        var req = new SqlLintRequest(docId, "SELECT 1", "my_conn", false);
        Assert.Equal("my_conn", req.ConnectionName);
        Assert.False(req.IncludeQuickFixes);
    }

    [Fact]
    public void SqlCompletionRequest_creates_correctly()
    {
        var docId = EditorDocumentId.New();
        var req = new SqlCompletionRequest(docId, "SELECT ", 8, "my_conn");
        Assert.Equal(docId, req.DocumentId);
        Assert.Equal("SELECT ", req.SqlText);
        Assert.Equal(8, req.CaretOffset);
        Assert.Equal("my_conn", req.ConnectionName);
    }

    [Fact]
    public void SqlCompletionItem_creates_with_required_only()
    {
        var item = new SqlCompletionItem("select", "SELECT");
        Assert.Equal("select", item.Label);
        Assert.Equal("SELECT", item.InsertText);
        Assert.Null(item.Detail);
        Assert.Null(item.Documentation);
        Assert.Null(item.Kind);
        Assert.Equal(0, item.SortPriority);
    }

    [Fact]
    public void SqlCompletionItem_with_all_fields()
    {
        var item = new SqlCompletionItem("select", "SELECT", "keyword", "SELECT statement", "Keyword", 10);
        Assert.Equal("keyword", item.Detail);
        Assert.Equal("SELECT statement", item.Documentation);
        Assert.Equal("Keyword", item.Kind);
        Assert.Equal(10, item.SortPriority);
    }

    [Fact]
    public void SqlSignatureHelp_creates_correctly()
    {
        var signatures = new List<SqlSignatureInformation>
        {
            new("COUNT(*)", "Counts rows"),
            new("COUNT(DISTINCT expr)")
        };
        var help = new SqlSignatureHelp(signatures, 1, 0);
        Assert.Equal(2, help.Signatures.Count);
        Assert.Equal(1, help.ActiveSignature);
        Assert.Equal(0, help.ActiveParameter);
    }

    [Fact]
    public void SqlSignatureInformation_creates_with_optional_params()
    {
        var info = new SqlSignatureInformation("COUNT(*)", "Description", new[] { "expr" });
        Assert.Equal("COUNT(*)", info.Label);
        Assert.Equal("Description", info.Documentation);
        Assert.Single(info.Parameters!);
    }

    [Fact]
    public void SqlCodeAction_creates_with_edits()
    {
        var edits = new List<SqlTextEdit> { new(0, 5, "SELECT") };
        var action = new SqlCodeAction("Fix spelling", edits, "SP001");
        Assert.Equal("Fix spelling", action.Title);
        Assert.Single(action.Edits);
        Assert.Equal("SP001", action.RuleId);
        Assert.True(action.IsEnabled);
        Assert.Null(action.DisabledReason);
    }

    [Fact]
    public void SqlCodeAction_disabled_state()
    {
        var edits = new List<SqlTextEdit>();
        var action = new SqlCodeAction("Fix", edits, null, false, "Not applicable");
        Assert.False(action.IsEnabled);
        Assert.Equal("Not applicable", action.DisabledReason);
    }

    [Fact]
    public void SqlTextEdit_creates_correctly()
    {
        var edit = new SqlTextEdit(10, 5, "replacement");
        Assert.Equal(10, edit.StartOffset);
        Assert.Equal(5, edit.Length);
        Assert.Equal("replacement", edit.NewText);
    }

    [Fact]
    public void SqlSignatureHelpRequest_creates_correctly()
    {
        var docId = EditorDocumentId.New();
        var req = new SqlSignatureHelpRequest(docId, "SELECT ", 8, "conn");
        Assert.Equal(docId, req.DocumentId);
        Assert.Equal(8, req.CaretOffset);
    }

    [Fact]
    public void SqlCodeActionRequest_creates_correctly()
    {
        var docId = EditorDocumentId.New();
        var diagnostic = new SqlDiagnostic(SqlDiagnosticSeverity.Error, "bad syntax");
        var req = new SqlCodeActionRequest(docId, "SELECT ", diagnostic, "conn");
        Assert.Equal(docId, req.DocumentId);
        Assert.Same(diagnostic, req.Diagnostic);
    }
}
