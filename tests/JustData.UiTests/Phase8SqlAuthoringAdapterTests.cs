using AppBase.Data.Completion;
using AppBase.Common.Enums;
using AppBase.Common.Interfaces;
using AppBase.Data.Core.Core;
using AppBase.Data.Core.Interfaces;
using AppBase.Data.Core.Models;
using AppBase.Common.Configuration;
using JustData.Application.Editor;
using JustData.Application.Sql;
using JustyBase.Netezza.Models;
using JustyBase.NetezzaSqlParser.Visitor;
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
    public async Task Lint_accepts_ampersand_variables_and_legacy_declarations()
    {
        var completion = CreateCompletion();
        using var legacy = new LegacySqlAuthoringServices(completion);
        var adapter = new NetezzaSqlAuthoringUseCaseAdapter(
            new NetezzaSqlAuthoringUseCase(completion, legacy),
            legacy);

        SqlLintResult result = await adapter.LintAsync(new SqlLintRequest(
            EditorDocumentId.New(),
            "declare &SOME_VAR = 'test';\nSELECT &SOME_VAR"));

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Code == "LEX001");
    }

    [Fact]
    public async Task Lint_does_not_treat_ampersand_variable_as_column_with_schema()
    {
        var completion = CreateCompletion();
        completion.SchemaProvider.AddTable(new JustyBase.NetezzaSqlParser.Ast.TableInfo(
            "ORDERS", "PUBLIC", "TESTDB",
            Columns: [new JustyBase.NetezzaSqlParser.Ast.ColumnInfo("STATUS")]));

        using var legacy = new LegacySqlAuthoringServices(completion);
        var adapter = new NetezzaSqlAuthoringUseCaseAdapter(
            new NetezzaSqlAuthoringUseCase(completion, legacy),
            legacy);

        SqlLintResult result = await adapter.LintAsync(new SqlLintRequest(
            EditorDocumentId.New(),
            "declare &SEARCHED = 'x';\nSELECT STATUS FROM TESTDB.PUBLIC.ORDERS WHERE STATUS LIKE &SEARCHED"));

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Code is "LEX001" or "SQL004");
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

    [Fact]
    public async Task Netezza_authoring_uses_request_database_for_completion_and_lint()
    {
        var catalog = Substitute.For<INetezzaSchemaTableCatalog>();
        catalog.TablesByConnection.Returns(new Dictionary<string, Dictionary<int, NetezzaTableInfo>>
        {
            ["nz"] = new()
            {
                [1] = new()
                {
                    DATABASE_ID = 10, TABLE_NAME = "LOCAL_TABLE", TABLE_DESC = "",
                    TABLE_OWNER = "ADMIN", TABLE_SCHEMA = "ADMIN", TABLE_OBJECT_OWNER = "ADMIN",
                    TABLE_KIND = TypeInDatabase.table, COLUMN_COUNT = 0
                },
                [2] = new()
                {
                    DATABASE_ID = 20, TABLE_NAME = "FOREIGN_TABLE", TABLE_DESC = "",
                    TABLE_OWNER = "ADMIN", TABLE_SCHEMA = "ADMIN", TABLE_OBJECT_OWNER = "ADMIN",
                    TABLE_KIND = TypeInDatabase.table, COLUMN_COUNT = 0
                }
            }
        });

        var context = Substitute.For<INetezzaCompletionContext>();
        context.SelectedConnectionName.Returns("nz");
        context.SelectedDatabase.Returns("JUST_DATA");
        context.DatabaseDictionary.Returns(new Dictionary<string, Dictionary<int, DatabaseInfo>>
        {
            ["nz"] = new()
            {
                [10] = new(10, "JUST_DATA", "ADMIN", "ADMIN"),
                [20] = new(20, "OTHER_DB", "ADMIN", "ADMIN")
            }
        });

        var completion = new NetezzaSqlCompletionServices(catalog);
        var db = Substitute.For<IGeneralDbService>();
        db.DriverName("nz").Returns("NetezzaSQL");
        using var legacy = new LegacySqlAuthoringServices(completion, db);
        var useCase = new NetezzaSqlAuthoringUseCase(completion, legacy, db, context);

        const string completionSql = "SELECT * FROM JUST_DATA.ADMIN.";
        var items = await useCase.CompleteAsync(
            new SqlCompletionRequest(EditorDocumentId.New(), completionSql, completionSql.Length, "nz", "JUST_DATA"));
        var lint = await useCase.LintAsync(
            new SqlLintRequest(EditorDocumentId.New(), "SELECT * FROM ADMIN.LOCAL_TABLE", "nz", DatabaseName: "JUST_DATA"));

        Assert.Contains(items, item => item.Label == "LOCAL_TABLE");
        Assert.DoesNotContain(items, item => item.Label == "FOREIGN_TABLE");
        Assert.DoesNotContain(lint.Diagnostics, diagnostic => diagnostic.Code is "SQL006" or "SQL007");
    }

    [Fact]
    public async Task Db2_connection_selects_db2_authoring_catalog_and_diagnostic_source()
    {
        var completion = CreateCompletion();
        var generalDbService = Substitute.For<IGeneralDbService>();
        generalDbService.DriverName(Arg.Any<string>()).Returns("DB2");

        using var legacy = new LegacySqlAuthoringServices(completion, generalDbService);
        var adapter = new NetezzaSqlAuthoringUseCaseAdapter(
            new NetezzaSqlAuthoringUseCase(completion, legacy, generalDbService),
            legacy);
        EditorDocumentId documentId = EditorDocumentId.New();

        IReadOnlyList<SqlCompletionItem> items = await adapter.CompleteAsync(
            new SqlCompletionRequest(documentId, "SELECT COA", 10, "db2"));
        SqlLintResult lint = await adapter.LintAsync(
            new SqlLintRequest(documentId, "SELECT * FROM missing_table", "db2"));

        Assert.Contains(items, item => item.Label.Equals("COALESCE", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(lint.Diagnostics, diagnostic => diagnostic.Source == "Db2 SQL");
        Assert.DoesNotContain(lint.Diagnostics, diagnostic => diagnostic.Code?.StartsWith("NZ", StringComparison.OrdinalIgnoreCase) == true);
    }

    [Fact]
    public async Task Mssql_connection_selects_mssql_authoring_catalog_and_diagnostic_source()
    {
        var completion = CreateCompletion();
        var generalDbService = Substitute.For<IGeneralDbService>();
        generalDbService.DriverName(Arg.Any<string>()).Returns("MsSqlTrusted");

        using var legacy = new LegacySqlAuthoringServices(completion, generalDbService);
        var adapter = new NetezzaSqlAuthoringUseCaseAdapter(
            new NetezzaSqlAuthoringUseCase(completion, legacy, generalDbService),
            legacy);
        EditorDocumentId documentId = EditorDocumentId.New();

        IReadOnlyList<SqlCompletionItem> items = await adapter.CompleteAsync(
            new SqlCompletionRequest(documentId, "SELECT COA", 10, "mssql"));
        SqlLintResult lint = await adapter.LintAsync(
            new SqlLintRequest(documentId, "SELECT * FROM missing_table", "mssql"));

        Assert.Contains(items, item => item.Label.Equals("COALESCE", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(lint.Diagnostics, diagnostic => diagnostic.Source == "MSSQL SQL");
        Assert.Contains(lint.Diagnostics, diagnostic => diagnostic.Code == "MSS001");
        Assert.DoesNotContain(lint.Diagnostics, diagnostic => diagnostic.Code?.StartsWith("NZ", StringComparison.OrdinalIgnoreCase) == true);
    }

    [Fact]
    public async Task Postgres_connection_selects_postgres_authoring_catalog_and_diagnostic_source()
    {
        var completion = CreateCompletion();
        var generalDbService = Substitute.For<IGeneralDbService>();
        generalDbService.DriverName(Arg.Any<string>()).Returns("Postgres");

        using var legacy = new LegacySqlAuthoringServices(completion, generalDbService);
        var adapter = new NetezzaSqlAuthoringUseCaseAdapter(
            new NetezzaSqlAuthoringUseCase(completion, legacy, generalDbService),
            legacy);
        EditorDocumentId documentId = EditorDocumentId.New();

        IReadOnlyList<SqlCompletionItem> items = await adapter.CompleteAsync(
            new SqlCompletionRequest(documentId, "SELECT COA", 10, "pg"));
        SqlLintResult lint = await adapter.LintAsync(
            new SqlLintRequest(documentId, "SELECT * FROM missing_table", "pg"));

        Assert.Contains(items, item => item.Label.Equals("COALESCE", StringComparison.OrdinalIgnoreCase));
        // PostgreSqlLintRules is still empty in the parser package, so the
        // dialect is verified through the completion catalog here.
        Assert.Empty(lint.Diagnostics);
        Assert.DoesNotContain(lint.Diagnostics, diagnostic => diagnostic.Code?.StartsWith("NZ", StringComparison.OrdinalIgnoreCase) == true);
    }

    [Fact]
    public async Task Unknown_driver_falls_back_to_netezza_catalog()
    {
        var completion = CreateCompletion();
        var generalDbService = Substitute.For<IGeneralDbService>();
        generalDbService.DriverName(Arg.Any<string>()).Returns("CustomDriver");

        using var legacy = new LegacySqlAuthoringServices(completion, generalDbService);
        var adapter = new NetezzaSqlAuthoringUseCaseAdapter(
            new NetezzaSqlAuthoringUseCase(completion, legacy, generalDbService),
            legacy);
        EditorDocumentId documentId = EditorDocumentId.New();

        SqlLintResult lint = await adapter.LintAsync(
            new SqlLintRequest(documentId, "SELECT * FROM TEST_TABLE", "custom"));

        Assert.Contains(lint.Diagnostics, diagnostic => diagnostic.Source == "Netezza SQL");
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
