using JustData.Application.Sql;

namespace AppBase.Tests.JustDataApplication.Sql;

public sealed class SqlPreprocessingServiceTests
{
    // ──────────────────────────────────────────────
    // Basic preprocessing
    // ──────────────────────────────────────────────

    [Fact]
    public async Task PreprocessAsync_passthrough_unchanged()
    {
        var service = new SqlPreprocessingService();
        var request = CreateRequest("SELECT 1");

        var result = await service.PreprocessAsync(request);

        Assert.Equal("SELECT 1", result.ProcessedSql);
        Assert.Null(result.ExportFilePath);
        Assert.Null(result.ExportOptionDirective);
        Assert.Empty(result.UpdatedSessionVariables);
        Assert.Empty(result.UpdatedGlobalVariables);
    }

    [Fact]
    public async Task PreprocessAsync_canonicalizes_sleep_outside_literals()
    {
        var service = new SqlPreprocessingService();
        var request = CreateRequest("___sleep 100;\nSELECT 1;");

        var result = await service.PreprocessAsync(request);

        Assert.Contains("@sleep:100", result.ProcessedSql, StringComparison.Ordinal);
        Assert.DoesNotContain("___sleep", result.ProcessedSql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PreprocessAsync_does_not_rewrite_sleep_inside_string_literal()
    {
        var service = new SqlPreprocessingService();
        var request = CreateRequest("SELECT '___sleep 99' AS note;");

        var result = await service.PreprocessAsync(request);

        Assert.Equal("SELECT '___sleep 99' AS note;", result.ProcessedSql);
    }

    [Fact]
    public async Task PreprocessAsync_null_sql_becomes_empty()
    {
        var service = new SqlPreprocessingService();
        var request = CreateRequest(null!);

        var result = await service.PreprocessAsync(request);

        Assert.Equal(string.Empty, result.ProcessedSql);
    }

    [Fact]
    public async Task PreprocessAsync_null_request_throws()
    {
        var service = new SqlPreprocessingService();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.PreprocessAsync(null!));
    }

    // ──────────────────────────────────────────────
    // __Let directive
    // ──────────────────────────────────────────────

    [Fact]
    public async Task PreprocessAsync_Let_directive_defines_parameter()
    {
        var service = new SqlPreprocessingService();
        var request = CreateRequest("__Let $schema=APP|$table=users\nSELECT * FROM $schema..$table");

        var result = await service.PreprocessAsync(request);

        Assert.Equal("\nSELECT * FROM APP..users", result.ProcessedSql);
    }

    [Fact]
    public async Task PreprocessAsync_Let_directive_updates_known_parameters()
    {
        var service = new SqlPreprocessingService();
        var request = CreateRequest("__Let $x=42\nSELECT 1");

        var result = await service.PreprocessAsync(request);
        Assert.Equal("42", result.UpdatedKnownParameters["$X"]);
        Assert.Equal("42", result.UpdatedKnownParameters["$x"]);
    }

    [Fact]
    public async Task PreprocessAsync_Let_directive_without_newline_does_not_process()
    {
        var service = new SqlPreprocessingService();
        var request = CreateRequest("__Let $x=42");

        var result = await service.PreprocessAsync(request);

        Assert.Equal("__Let $x=42", result.ProcessedSql);
        Assert.Empty(result.UpdatedKnownParameters);
    }

    // ──────────────────────────────────────────────
    // __LetFor directive
    // ──────────────────────────────────────────────

    [Fact]
    public async Task PreprocessAsync_LetFor_directive_generates_multiple_statements()
    {
        var service = new SqlPreprocessingService();
        var sql = "__LetFor $t|users|orders|products\nDROP TABLE IF EXISTS $t;";
        var request = CreateRequest(sql);

        var result = await service.PreprocessAsync(request);

        // Each replacement includes the original \n and adds a trailing ;
        Assert.Contains("DROP TABLE IF EXISTS users;", result.ProcessedSql);
        Assert.Contains("DROP TABLE IF EXISTS orders;", result.ProcessedSql);
        Assert.Contains("DROP TABLE IF EXISTS products;", result.ProcessedSql);
    }

    [Fact]
    public async Task PreprocessAsync_LetFor_directive_single_variable()
    {
        var service = new SqlPreprocessingService();
        var sql = "__LetFor $t|users\nSELECT * FROM $t;";
        var request = CreateRequest(sql);

        var result = await service.PreprocessAsync(request);

        Assert.Contains("SELECT * FROM users;", result.ProcessedSql);
    }

    [Fact]
    public async Task PreprocessAsync_LetFor_directive_preserves_sql_after_newline()
    {
        var service = new SqlPreprocessingService();
        var sql = "__LetFor $t|users|orders\nSELECT * FROM $t;";
        var request = CreateRequest(sql);

        var result = await service.PreprocessAsync(request);

        // The SQL after \n is the template that gets repeated
        Assert.StartsWith("\n", result.ProcessedSql);
    }

    // ──────────────────────────────────────────────
    // __SessionVar__ directive (defines variable in result, not auto-replaced in SQL)
    // ──────────────────────────────────────────────

    [Fact]
    public async Task PreprocessAsync_SessionVar_directive_evaluates_expression()
    {
        var service = new SqlPreprocessingService();
        var sql = "__SessionVar__$counter = 1 + 2\nSELECT $counter";
        var request = CreateRequest(sql);

        var result = await service.PreprocessAsync(request);

        // Directive line is removed, $counter is NOT auto-replaced in SQL
        Assert.Equal("\nSELECT $counter", result.ProcessedSql);
        Assert.Equal("3", result.UpdatedSessionVariables["$counter"]);
    }

    [Fact]
    public async Task PreprocessAsync_SessionVar_directive_passthrough_on_error()
    {
        var service = new SqlPreprocessingService();
        var sql = "__SessionVar__$bad = invalid expression\nSELECT 1";
        var request = CreateRequest(sql);

        var result = await service.PreprocessAsync(request);

        Assert.Equal("\nSELECT 1", result.ProcessedSql);
        Assert.True(result.UpdatedSessionVariables.ContainsKey("$bad"));
    }

    [Fact]
    public async Task PreprocessAsync_SessionVar_with_SQL_RESULT_updates_session_variables()
    {
        var service = new SqlPreprocessingService();
        var sql = "__SessionVar__$val = SQL_RESULT[SELECT 42]\nSELECT $val";
        var request = CreateRequest(sql);
        object? sqlResult = 42;
        Func<string, Task<object?>> evaluator = _ => Task.FromResult<object?>(sqlResult);

        var result = await service.PreprocessAsync(request, sqlEvaluator: evaluator);

        Assert.Equal("\nSELECT $val", result.ProcessedSql);
        Assert.Equal("42", result.UpdatedSessionVariables["$val"]);
    }

    [Fact]
    public async Task PreprocessAsync_SessionVar_with_SQL_RECORDS_AFFECTED_updates_session_variables()
    {
        var service = new SqlPreprocessingService();
        var sql = "__SessionVar__$cnt = SQL_RECORDS_AFFECTED[DELETE FROM t]\nSELECT $cnt";
        var request = CreateRequest(sql);
        object? affected = 5;
        Func<string, Task<object?>> evaluator = _ => Task.FromResult<object?>(affected);

        var result = await service.PreprocessAsync(request, sqlEvaluator: evaluator);

        Assert.Equal("\nSELECT $cnt", result.ProcessedSql);
        Assert.Equal("5", result.UpdatedSessionVariables["$cnt"]);
    }

    // ──────────────────────────────────────────────
    // __GlobalVar__ directive
    // ──────────────────────────────────────────────

    [Fact]
    public async Task PreprocessAsync_GlobalVar_directive_updates_global_variables()
    {
        var service = new SqlPreprocessingService();
        var sql = "__GlobalVar__$user = 'admin'\nSELECT $user";
        var request = CreateRequest(sql);

        var result = await service.PreprocessAsync(request);

        // $user is NOT auto-replaced in SQL text
        Assert.Equal("\nSELECT $user", result.ProcessedSql);
        Assert.Equal("admin", result.UpdatedGlobalVariables["$user"]);
    }

    // ──────────────────────────────────────────────
    // Export directive (__xlsx)
    // ──────────────────────────────────────────────

    [Fact]
    public async Task PreprocessAsync_xlsx_export_directive_extracts_path()
    {
        var service = new SqlPreprocessingService();
        var sql = "SELECT * FROM users; __xlsx \"C:\\reports\\output.xlsx\"";
        var request = CreateRequest(sql);

        var result = await service.PreprocessAsync(request);

        Assert.Equal("C:\\reports\\output.xlsx", result.ExportFilePath);
        Assert.Equal("xlsx", result.ExportOptionDirective);
    }

    [Fact]
    public async Task PreprocessAsync_xlsx_export_directive_no_path_returns_null()
    {
        var service = new SqlPreprocessingService();
        var request = CreateRequest("SELECT 1");

        var result = await service.PreprocessAsync(request);

        Assert.Null(result.ExportFilePath);
    }

    // ──────────────────────────────────────────────
    // Variable resolution via prompt
    // ──────────────────────────────────────────────

    [Fact]
    public async Task PreprocessAsync_unknown_variable_prompts_user()
    {
        var service = new SqlPreprocessingService();
        var sql = "SELECT * FROM $my_table";
        var request = CreateRequest(sql, allowPrompts: true);
        var prompt = new FakePromptService(new Dictionary<string, string>
        {
            ["$my_table"] = "users"
        });

        var result = await service.PreprocessAsync(request, promptService: prompt);

        Assert.True(prompt.WasCalled);
        Assert.Equal("users", result.UpdatedKnownParameters["$MY_TABLE"]);
    }

    [Fact]
    public async Task PreprocessAsync_prompt_not_called_when_disallowed()
    {
        var service = new SqlPreprocessingService();
        var sql = "SELECT * FROM $unknown";
        var request = CreateRequest(sql, allowPrompts: false);
        var prompt = new FakePromptService(new Dictionary<string, string>());

        var result = await service.PreprocessAsync(request, promptService: prompt);

        Assert.Equal("SELECT * FROM $unknown", result.ProcessedSql);
        Assert.False(prompt.WasCalled);
    }

    [Fact]
    public async Task PreprocessAsync_variable_in_quotes_skips_prompt()
    {
        var service = new SqlPreprocessingService();
        var sql = "SELECT '$var' AS result";
        var request = CreateRequest(sql, allowPrompts: true);
        var prompt = new FakePromptService(new Dictionary<string, string>());

        var result = await service.PreprocessAsync(request, promptService: prompt);

        // $var is inside a string literal, should not be prompted
        Assert.Equal("SELECT '$var' AS result", result.ProcessedSql);
        Assert.False(prompt.WasCalled);
    }

    // ──────────────────────────────────────────────
    // Preloaded parameters (constructor)
    // ──────────────────────────────────────────────

    [Fact]
    public async Task PreprocessAsync_preloaded_parameters_are_replaced()
    {
        var preloaded = new Dictionary<string, string>
        {
            ["$SCHEMA"] = "APP"
        };
        var service = new SqlPreprocessingService(preloaded);
        var request = CreateRequest("SELECT * FROM $schema..t");

        var result = await service.PreprocessAsync(request);

        Assert.Equal("SELECT * FROM APP..t", result.ProcessedSql);
    }

    [Fact]
    public async Task PreprocessAsync_parameter_replacement_uses_longest_key_first()
    {
        var preloaded = new Dictionary<string, string>
        {
            ["$var"] = "x",
            ["$variable"] = "y"
        };
        var service = new SqlPreprocessingService(preloaded);
        var request = CreateRequest("SELECT $variable, $var");

        var result = await service.PreprocessAsync(request);

        Assert.Equal("SELECT y, x", result.ProcessedSql);
    }

    // ──────────────────────────────────────────────
    // Multiple directives combined
    // ──────────────────────────────────────────────

    [Fact]
    public async Task PreprocessAsync_Let_and_SessionVar_combined()
    {
        var service = new SqlPreprocessingService();
        // __Let adds to known params (auto-replaced), __SessionVar__ does not
        var sql = "__Let $db=SYSTEM\n__SessionVar__$cnt = 2 + 3\nSELECT $cnt FROM $db..t";
        var request = CreateRequest(sql);

        var result = await service.PreprocessAsync(request);

        // $db is replaced (from __Let), $cnt is NOT (from __SessionVar__)
        Assert.Contains("SYSTEM", result.ProcessedSql);
        Assert.Contains("$cnt", result.ProcessedSql);
        Assert.Equal("5", result.UpdatedSessionVariables["$cnt"]);
    }

    // ──────────────────────────────────────────────
    // Edge cases
    // ──────────────────────────────────────────────

    [Fact]
    public async Task PreprocessAsync_empty_sql_returns_empty()
    {
        var service = new SqlPreprocessingService();
        var request = CreateRequest(string.Empty);

        var result = await service.PreprocessAsync(request);

        Assert.Equal(string.Empty, result.ProcessedSql);
    }

    [Fact]
    public async Task PreprocessAsync_whitespace_sql_returns_whitespace()
    {
        var service = new SqlPreprocessingService();
        var request = CreateRequest("   \n  ");

        var result = await service.PreprocessAsync(request);

        Assert.Equal("   \n  ", result.ProcessedSql);
    }

    [Fact]
    public async Task PreprocessAsync_Let_directive_with_empty_value()
    {
        var service = new SqlPreprocessingService();
        var request = CreateRequest("__Let $x=\nSELECT $x");

        var result = await service.PreprocessAsync(request);

        Assert.Equal("\nSELECT ", result.ProcessedSql);
        Assert.Equal("", result.UpdatedKnownParameters["$X"]);
    }

    // ──────────────────────────────────────────────
    // Cancellation
    // ──────────────────────────────────────────────

    [Fact]
    public async Task PreprocessAsync_prompt_cancellation_propagates()
    {
        var service = new SqlPreprocessingService();
        var sql = "SELECT * FROM $unknown";
        var request = CreateRequest(sql, allowPrompts: true);
        using var cts = new CancellationTokenSource();
        var prompt = new CancellablePromptService();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            service.PreprocessAsync(request, promptService: prompt, cancellationToken: cts.Token));
    }

    // ──────────────────────────────────────────────
    // PreprocessRequest / PreprocessResult records
    // ──────────────────────────────────────────────

    [Fact]
    public void PreprocessRequest_creates_with_all_properties()
    {
        var known = new Dictionary<string, string> { ["$x"] = "42" };
        var request = new PreprocessRequest(
            SqlText: "SELECT 1",
            ConnectionName: "conn",
            DatabaseName: "db",
            DocumentKey: "doc",
            KnownParameters: known,
            AllowPrompts: true);

        Assert.Equal("SELECT 1", request.SqlText);
        Assert.Equal("conn", request.ConnectionName);
        Assert.Equal("db", request.DatabaseName);
        Assert.Equal("doc", request.DocumentKey);
        Assert.Same(known, request.KnownParameters);
        Assert.True(request.AllowPrompts);
    }

    [Fact]
    public void PreprocessResult_creates_with_all_properties()
    {
        var session = new Dictionary<string, string> { ["$x"] = "3" };
        var global = new Dictionary<string, string> { ["$y"] = "4" };

        var result = new PreprocessResult(
            ProcessedSql: "SELECT 3",
            ExportFilePath: "out.xlsx",
            ExportOptionDirective: "xlsx",
            UpdatedKnownParameters: new Dictionary<string, string>(),
            UpdatedSessionVariables: session,
            UpdatedGlobalVariables: global);

        Assert.Equal("SELECT 3", result.ProcessedSql);
        Assert.Equal("out.xlsx", result.ExportFilePath);
        Assert.Equal("xlsx", result.ExportOptionDirective);
        Assert.Same(session, result.UpdatedSessionVariables);
        Assert.Same(global, result.UpdatedGlobalVariables);
    }

    // ──────────────────────────────────────────────
    // IVariablePromptService contract
    // ──────────────────────────────────────────────

    [Fact]
    public async Task PromptService_returns_values_for_unresolved_variables()
    {
        var prompt = new FakePromptService(new Dictionary<string, string>
        {
            ["$name"] = "test"
        });

        var result = await prompt.PromptAsync(
            new Dictionary<string, string> { ["$NAME"] = "$name" },
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("test", result["$NAME"]);
        Assert.True(prompt.WasCalled);
    }

    [Fact]
    public void Service_implements_ISqlPreprocessingService()
    {
        var service = new SqlPreprocessingService();
        Assert.IsAssignableFrom<ISqlPreprocessingService>(service);
    }

    // ──────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────

    private static PreprocessRequest CreateRequest(
        string sqlText,
        bool allowPrompts = false,
        IReadOnlyDictionary<string, string>? knownParams = null)
    {
        return new PreprocessRequest(
            SqlText: sqlText,
            ConnectionName: "test_conn",
            DatabaseName: "test_db",
            DocumentKey: "test_doc",
            KnownParameters: knownParams ?? new Dictionary<string, string>(),
            AllowPrompts: allowPrompts);
    }

    private sealed class FakePromptService : IVariablePromptService
    {
        private readonly Dictionary<string, string> _values;
        public bool WasCalled { get; private set; }

        public FakePromptService(Dictionary<string, string> values)
        {
            _values = values;
        }

        public Task<IReadOnlyDictionary<string, string>> PromptAsync(
            IReadOnlyDictionary<string, string> unresolvedVariables,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            WasCalled = true;
            return Task.FromResult<IReadOnlyDictionary<string, string>>(
                new Dictionary<string, string>(_values, StringComparer.OrdinalIgnoreCase));
        }
    }

    private sealed class CancellablePromptService : IVariablePromptService
    {
        public Task<IReadOnlyDictionary<string, string>> PromptAsync(
            IReadOnlyDictionary<string, string> unresolvedVariables,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyDictionary<string, string>>(
                new Dictionary<string, string>());
        }
    }
}
