using AppBase.Data.Completion;
using AppBase.Data.Core.Interfaces;
using JustData.Application.Editor;
using JustData.Application.Sql;
using JustyBaseLegacy.UI.Sql;
using NSubstitute;

namespace JustData.UiTests;

public sealed class Phase8SqlAuthoringAdapterTests
{
    private static NetezzaSqlCompletionServices CreateCompletion()
    {
        var catalog = Substitute.For<INetezzaSchemaTableCatalog>();
        catalog.TablesByConnection.Returns(new Dictionary<string, Dictionary<int, AppBase.Data.Core.Models.NetezzaTableInfo>>());
        return new NetezzaSqlCompletionServices(catalog);
    }

    [Fact]
    public async Task Lint_disable_enable_and_code_actions_keep_stable_document_identity()
    {
        var completion = CreateCompletion();
        using var legacy = new LegacySqlAuthoringServices(completion);
        var inner = new NetezzaSqlAuthoringUseCase(completion, legacy);
        var adapter = new NetezzaSqlAuthoringUseCaseAdapter(inner, legacy);
        EditorDocumentId documentId = EditorDocumentId.New();
        const string selectStar = "SELECT * FROM TEST_TABLE";

        SqlLintResult initial = await adapter.LintAsync(new SqlLintRequest(documentId, selectStar));
        Assert.Equal(documentId, initial.DocumentId);
        Assert.Contains(initial.Diagnostics, diagnostic => diagnostic.Code == "NZ001");

        adapter.DisableRule("NZ001");
        SqlLintResult disabled = await adapter.LintAsync(new SqlLintRequest(documentId, selectStar));
        Assert.DoesNotContain(disabled.Diagnostics, diagnostic => diagnostic.Code == "NZ001");

        adapter.EnableRule("NZ001");
        SqlLintResult enabled = await adapter.LintAsync(new SqlLintRequest(documentId, selectStar));
        Assert.Contains(enabled.Diagnostics, diagnostic => diagnostic.Code == "NZ001");

        const string mixedCase = "select value FROM TEST_TABLE";
        SqlLintResult mixedResult = await adapter.LintAsync(new SqlLintRequest(documentId, mixedCase));
        SqlDiagnostic casing = Assert.Single(mixedResult.Diagnostics, diagnostic => diagnostic.Code == "NZ007");
        IReadOnlyList<SqlCodeAction> actions = await adapter.GetCodeActionsAsync(
            new SqlCodeActionRequest(documentId, mixedCase, casing));

        SqlCodeAction quickFix = Assert.Single(actions, action => action.Edits.Count > 0);
        Assert.Equal("NZ007", quickFix.RuleId);
        Assert.Equal("SELECT value FROM TEST_TABLE", ApplyEdits(mixedCase, quickFix.Edits));
        Assert.Contains(actions, action => action.Title.Contains("Disable rule NZ007", StringComparison.Ordinal));

        adapter.Release(documentId);
    }

    [Fact]
    public async Task Completion_and_signature_requests_are_mapped_without_parser_types()
    {
        var completion = CreateCompletion();
        using var legacy = new LegacySqlAuthoringServices(completion);
        var adapter = new NetezzaSqlAuthoringUseCaseAdapter(
            new NetezzaSqlAuthoringUseCase(completion, legacy),
            legacy);
        EditorDocumentId documentId = EditorDocumentId.New();

        IReadOnlyList<SqlCompletionItem> items = await adapter.CompleteAsync(
            new SqlCompletionRequest(documentId, "SEL", 3));
        SqlSignatureHelp? signature = await adapter.GetSignatureHelpAsync(
            new SqlSignatureHelpRequest(documentId, "COUNT(", 6));

        Assert.Contains(items, item => item.Label.Equals("SELECT", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(signature);
        Assert.Contains(signature.Signatures, item => item.Label.StartsWith("COUNT(", StringComparison.OrdinalIgnoreCase));
    }

    private static string ApplyEdits(string sql, IReadOnlyList<SqlTextEdit> edits)
    {
        string result = sql;
        foreach (SqlTextEdit edit in edits.OrderByDescending(edit => edit.StartOffset))
        {
            result = result.Remove(edit.StartOffset, edit.Length)
                .Insert(edit.StartOffset, edit.NewText);
        }
        return result;
    }
}
