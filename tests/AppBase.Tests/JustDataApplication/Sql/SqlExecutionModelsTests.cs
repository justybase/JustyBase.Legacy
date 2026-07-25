using JustData.Application.Editor;
using JustData.Application.Sql;

namespace AppBase.Tests.JustDataApplication.Sql;

// ──────────────────────────────────────────────
// SqlExecutionRequest
// ──────────────────────────────────────────────

public sealed class SqlExecutionRequestTests
{
    [Fact]
    public void Constructor_sets_document_id_and_sql()
    {
        var docId = EditorDocumentId.New();
        var req = new SqlExecutionRequest(docId, "SELECT 1");
        Assert.Equal(docId, req.DocumentId);
        Assert.Equal("SELECT 1", req.SqlText);
    }

    [Fact]
    public void Constructor_null_sql_becomes_empty()
    {
        var docId = EditorDocumentId.New();
        var req = new SqlExecutionRequest(docId, null!);
        Assert.Equal(string.Empty, req.SqlText);
    }

    [Fact]
    public void Defaults_are_set_correctly()
    {
        var req = new SqlExecutionRequest(EditorDocumentId.New(), "SELECT 1");
        Assert.Equal(string.Empty, req.ConnectionName);
        Assert.Equal(string.Empty, req.DatabaseName);
        Assert.Equal(SqlExecutionMode.Selection, req.Mode);
        Assert.Equal(SqlOutputMode.Grid, req.OutputMode);
        Assert.Equal(0, req.SelectionStart);
        Assert.Equal(0, req.SelectionLength);
        Assert.Equal(0, req.CaretOffset);
        Assert.False(req.KeepConnectionOpen);
        Assert.False(req.ContinueOnError);
        Assert.False(req.Explain);
        Assert.Null(req.OutputPath);
        Assert.Null(req.CommandTimeoutSeconds);
        Assert.Null(req.RowLimit);
        Assert.True(req.ConfirmRiskyQueries);
        Assert.True(req.ConfirmParameters);
        Assert.NotNull(req.Parameters);
        Assert.Empty(req.Parameters);
    }

    [Fact]
    public void With_preserves_existing_values()
    {
        var docId = EditorDocumentId.New();
        var req = new SqlExecutionRequest(docId, "SELECT 1")
        {
            ConnectionName = "conn",
            DatabaseName = "db",
            CommandTimeoutSeconds = 60
        };

        var updated = req with { ConnectionName = "other" };
        Assert.Equal("other", updated.ConnectionName);
        Assert.Equal("db", updated.DatabaseName); // preserved
        Assert.Equal(60, updated.CommandTimeoutSeconds); // preserved
    }

    [Fact]
    public void WithMode_changes_mode()
    {
        var req = new SqlExecutionRequest(EditorDocumentId.New(), "SELECT 1");
        var updated = req.WithMode(SqlExecutionMode.Script);

        Assert.Equal(SqlExecutionMode.Script, updated.Mode);
        Assert.Equal(SqlOutputMode.Grid, updated.OutputMode); // unchanged
    }

    [Fact]
    public void WithMode_changes_both_mode_and_output()
    {
        var req = new SqlExecutionRequest(EditorDocumentId.New(), "SELECT 1");
        var updated = req.WithMode(SqlExecutionMode.SingleBatch, SqlOutputMode.Csv);

        Assert.Equal(SqlExecutionMode.SingleBatch, updated.Mode);
        Assert.Equal(SqlOutputMode.Csv, updated.OutputMode);
    }

    [Fact]
    public void Parameters_are_case_insensitive()
    {
        var docId = EditorDocumentId.New();
        var req = new SqlExecutionRequest(docId, "SELECT 1")
        {
            Parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["VAR"] = "value"
            }
        };

        Assert.Equal("value", req.Parameters["var"]);
        Assert.Equal("value", req.Parameters["VAR"]);
        Assert.Equal("value", req.Parameters["Var"]);
    }

    [Fact]
    public void WithMode_does_not_mutate_original()
    {
        var req = new SqlExecutionRequest(EditorDocumentId.New(), "SELECT 1")
        {
            Mode = SqlExecutionMode.Selection
        };

        req.WithMode(SqlExecutionMode.Script);
        Assert.Equal(SqlExecutionMode.Selection, req.Mode); // unchanged
    }
}

// ──────────────────────────────────────────────
// ResultColumnDescriptor
// ──────────────────────────────────────────────

public sealed class ResultColumnDescriptorTests
{
    [Fact]
    public void Creates_with_required_fields()
    {
        var col = new ResultColumnDescriptor(0, "id", "INTEGER");
        Assert.Equal(0, col.Ordinal);
        Assert.Equal("id", col.Name);
        Assert.Equal("INTEGER", col.TypeName);
        Assert.True(col.IsNullable); // default
    }

    [Fact]
    public void Creates_as_not_nullable()
    {
        var col = new ResultColumnDescriptor(1, "name", "VARCHAR", false);
        Assert.False(col.IsNullable);
    }

    [Fact]
    public void Equality()
    {
        var c1 = new ResultColumnDescriptor(0, "id", "INTEGER");
        var c2 = new ResultColumnDescriptor(0, "id", "INTEGER");
        Assert.Equal(c1, c2);
    }
}

// ──────────────────────────────────────────────
// ResultSetDescriptor
// ──────────────────────────────────────────────

public sealed class ResultSetDescriptorTests
{
    [Fact]
    public void Creates_with_required_fields()
    {
        var cols = new List<ResultColumnDescriptor> { new(0, "id", "INTEGER") };
        var rs = new ResultSetDescriptor("rs1", "Results", cols);

        Assert.Equal("rs1", rs.ResultSetId);
        Assert.Equal("Results", rs.Name);
        Assert.Same(cols, rs.Columns);
        Assert.Equal(0, rs.StatementIndex);
        Assert.False(rs.IsPinned);
    }

    [Fact]
    public void Creates_with_optional_fields()
    {
        var cols = new List<ResultColumnDescriptor>();
        var rs = new ResultSetDescriptor("rs1", "Results", cols, 2, true);
        Assert.Equal(2, rs.StatementIndex);
        Assert.True(rs.IsPinned);
    }

    [Fact]
    public void Equality()
    {
        var cols = new List<ResultColumnDescriptor> { new(0, "id", "INTEGER") };
        var rs1 = new ResultSetDescriptor("rs1", "Results", cols);
        var rs2 = new ResultSetDescriptor("rs1", "Results", cols);
        Assert.Equal(rs1, rs2);
    }
}

// ──────────────────────────────────────────────
// SqlDiagnostic
// ──────────────────────────────────────────────

public sealed class SqlDiagnosticTests
{
    [Fact]
    public void Creates_with_required_fields()
    {
        var diag = new SqlDiagnostic(SqlDiagnosticSeverity.Error, "Syntax error");
        Assert.Equal(SqlDiagnosticSeverity.Error, diag.Severity);
        Assert.Equal("Syntax error", diag.Message);
        Assert.Equal(-1, diag.StartOffset);
        Assert.Equal(0, diag.Length);
        Assert.Null(diag.Code);
        Assert.Null(diag.Source);
        Assert.Null(diag.CodeActions);
    }

    [Fact]
    public void Creates_with_all_fields()
    {
        var actions = new List<SqlCodeAction>();
        var diag = new SqlDiagnostic(SqlDiagnosticSeverity.Warning, "Unused var",
            10, 5, "W001", "Analyzer", actions);

        Assert.Equal(SqlDiagnosticSeverity.Warning, diag.Severity);
        Assert.Equal(10, diag.StartOffset);
        Assert.Equal(5, diag.Length);
        Assert.Equal("W001", diag.Code);
        Assert.Equal("Analyzer", diag.Source);
        Assert.Same(actions, diag.CodeActions);
    }

    [Fact]
    public void Equality()
    {
        var d1 = new SqlDiagnostic(SqlDiagnosticSeverity.Error, "msg", 0, 5);
        var d2 = new SqlDiagnostic(SqlDiagnosticSeverity.Error, "msg", 0, 5);
        Assert.Equal(d1, d2);
    }
}

// ──────────────────────────────────────────────
// SqlLogEntry
// ──────────────────────────────────────────────

public sealed class SqlLogEntryTests
{
    [Fact]
    public void Creates_with_required_fields()
    {
        var now = DateTimeOffset.UtcNow;
        var entry = new SqlLogEntry(now, SqlLogLevel.Information, "Started");
        Assert.Equal(now, entry.Timestamp);
        Assert.Equal(SqlLogLevel.Information, entry.Level);
        Assert.Equal("Started", entry.Message);
        Assert.Null(entry.StatementIndex);
    }

    [Fact]
    public void Creates_with_statement_index()
    {
        var entry = new SqlLogEntry(DateTimeOffset.UtcNow, SqlLogLevel.Warning, "Slow", 0);
        Assert.Equal(0, entry.StatementIndex);
    }

    [Fact]
    public void Equality()
    {
        var now = DateTimeOffset.UtcNow;
        var e1 = new SqlLogEntry(now, SqlLogLevel.Error, "Failed", 1);
        var e2 = new SqlLogEntry(now, SqlLogLevel.Error, "Failed", 1);
        Assert.Equal(e1, e2);
    }
}

// ──────────────────────────────────────────────
// SqlExecutionEvent
// ──────────────────────────────────────────────

public sealed class SqlExecutionEventTests
{
    [Fact]
    public void Constructor_sets_kind_and_document_id()
    {
        var id = EditorDocumentId.New();
        var evt = new SqlExecutionEvent(SqlExecutionEventKind.Log, id);
        Assert.Equal(SqlExecutionEventKind.Log, evt.Kind);
        Assert.Equal(id, evt.DocumentId);
        Assert.Equal(-1, evt.StatementIndex); // default
        Assert.Equal(-1, evt.StatementCount); // default
    }

    // ── Started factory ──

    [Fact]
    public void Started_creates_event_with_kind_and_id()
    {
        var id = EditorDocumentId.New();
        var evt = SqlExecutionEvent.Started(id);
        Assert.Equal(SqlExecutionEventKind.Started, evt.Kind);
        Assert.Equal(id, evt.DocumentId);
        Assert.Equal(-1, evt.StatementCount);
    }

    [Fact]
    public void Started_with_statement_count()
    {
        var id = EditorDocumentId.New();
        var evt = SqlExecutionEvent.Started(id, 5);
        Assert.Equal(5, evt.StatementCount);
    }

    // ── Result factory ──

    [Fact]
    public void Result_creates_event_with_descriptor()
    {
        var id = EditorDocumentId.New();
        var descriptor = new ResultSetDescriptor("rs1", "Results",
            new List<ResultColumnDescriptor>());

        var evt = SqlExecutionEvent.Result(id, descriptor);
        Assert.Equal(SqlExecutionEventKind.ResultSet, evt.Kind);
        Assert.Same(descriptor, evt.ResultSet);
    }

    // ── RowsBatch factory ──

    [Fact]
    public void RowsBatch_creates_event()
    {
        var id = EditorDocumentId.New();
        var rows = new List<IReadOnlyList<object?>>
        {
            new object?[] { 1, "hello" },
            new object?[] { 2, "world" }
        };

        var evt = SqlExecutionEvent.RowsBatch(id, rows);
        Assert.Equal(SqlExecutionEventKind.Rows, evt.Kind);
        Assert.Same(rows, evt.Rows);
        Assert.Equal(2, evt.RowCount);
        Assert.Equal(-1, evt.StatementIndex);
        Assert.Null(evt.ResultSetId);
    }

    [Fact]
    public void RowsBatch_with_statement_and_result_set()
    {
        var id = EditorDocumentId.New();
        var rows = new List<IReadOnlyList<object?>> { new object?[] { 1 } };

        var evt = SqlExecutionEvent.RowsBatch(id, rows, 0, "rs1");
        Assert.Equal(0, evt.StatementIndex);
        Assert.Equal("rs1", evt.ResultSetId);
    }

    // ── RowsObserved factory ──

    [Fact]
    public void RowsObserved_creates_event()
    {
        var id = EditorDocumentId.New();
        var evt = SqlExecutionEvent.RowsObserved(id, 100);
        Assert.Equal(SqlExecutionEventKind.Rows, evt.Kind);
        Assert.Equal(100, evt.RowCount);
    }

    [Fact]
    public void RowsObserved_clamps_negative_to_zero()
    {
        var id = EditorDocumentId.New();
        var evt = SqlExecutionEvent.RowsObserved(id, -50);
        Assert.Equal(0, evt.RowCount);
    }

    [Fact]
    public void RowsObserved_with_optional_fields()
    {
        var id = EditorDocumentId.New();
        var evt = SqlExecutionEvent.RowsObserved(id, 50, 1, "rs1");
        Assert.Equal(1, evt.StatementIndex);
        Assert.Equal("rs1", evt.ResultSetId);
    }

    // ── Completed factory ──

    [Fact]
    public void Completed_creates_event()
    {
        var id = EditorDocumentId.New();
        var evt = SqlExecutionEvent.Completed(id, SqlExecutionOutcome.Success);
        Assert.Equal(SqlExecutionEventKind.Completed, evt.Kind);
        Assert.Equal(SqlExecutionOutcome.Success, evt.Outcome);
    }

    [Fact]
    public void Completed_with_error_message()
    {
        var id = EditorDocumentId.New();
        var evt = SqlExecutionEvent.Completed(id, SqlExecutionOutcome.Failed, "Timeout");
        Assert.Equal(SqlExecutionOutcome.Failed, evt.Outcome);
        Assert.Equal("Timeout", evt.ErrorMessage);
    }

    [Fact]
    public void Completed_cancelled_outcome()
    {
        var id = EditorDocumentId.New();
        var evt = SqlExecutionEvent.Completed(id, SqlExecutionOutcome.Cancelled);
        Assert.Equal(SqlExecutionOutcome.Cancelled, evt.Outcome);
    }

    [Fact]
    public void Init_style_properties_are_settable()
    {
        var id = EditorDocumentId.New();
        var evt = new SqlExecutionEvent(SqlExecutionEventKind.Log, id)
        {
            Message = "test message",
            Log = new SqlLogEntry(DateTimeOffset.UtcNow, SqlLogLevel.Trace, "trace"),
            AffectedRows = 42,
            IsTruncated = true
        };

        Assert.Equal("test message", evt.Message);
        Assert.NotNull(evt.Log);
        Assert.Equal(42, evt.AffectedRows);
        Assert.True(evt.IsTruncated);
    }
}

// ──────────────────────────────────────────────
// SqlSensitiveDataRedactor
// ──────────────────────────────────────────────

public sealed class SqlSensitiveDataRedactorTests
{
    [Fact]
    public void Redact_returns_empty_for_null()
    {
        Assert.Equal(string.Empty, SqlSensitiveDataRedactor.Redact(null));
    }

    [Fact]
    public void Redact_returns_empty_for_empty()
    {
        Assert.Equal(string.Empty, SqlSensitiveDataRedactor.Redact(""));
    }

    [Fact]
    public void Redact_returns_whitespace_unchanged()
    {
        // IsNullOrEmpty returns false for whitespace, so whitespace passes through
        Assert.Equal("   ", SqlSensitiveDataRedactor.Redact("   "));
    }

    [Fact]
    public void Redact_preserves_non_sensitive_sql()
    {
        var input = "SELECT * FROM users WHERE id = 42";
        Assert.Equal(input, SqlSensitiveDataRedactor.Redact(input));
    }

    // ── Password patterns ──

    [Fact]
    public void Redact_masks_password_single_quoted()
    {
        var result = SqlSensitiveDataRedactor.Redact("password='mysecret'");
        Assert.Equal("password=[redacted]", result);
    }

    [Fact]
    public void Redact_masks_password_double_quoted()
    {
        var result = SqlSensitiveDataRedactor.Redact("password=\"mysecret\"");
        Assert.Equal("password=[redacted]", result);
    }

    [Fact]
    public void Redact_masks_password_unquoted()
    {
        var result = SqlSensitiveDataRedactor.Redact("password=mysecret");
        Assert.Equal("password=[redacted]", result);
    }

    [Fact]
    public void Redact_masks_password_with_spaces_around_equals()
    {
        var result = SqlSensitiveDataRedactor.Redact("password = mysecret");
        Assert.Equal("password=[redacted]", result);
    }

    // ── Pwd/passwd variants ──

    [Fact]
    public void Redact_masks_pwd()
    {
        var result = SqlSensitiveDataRedactor.Redact("pwd='test'");
        Assert.Equal("pwd=[redacted]", result);
    }

    [Fact]
    public void Redact_masks_passwd()
    {
        var result = SqlSensitiveDataRedactor.Redact("passwd='test'");
        Assert.Equal("passwd=[redacted]", result);
    }

    // ── Token / secret ──

    [Fact]
    public void Redact_masks_token()
    {
        var result = SqlSensitiveDataRedactor.Redact("token='abc123'");
        Assert.Equal("token=[redacted]", result);
    }

    [Fact]
    public void Redact_masks_secret()
    {
        var result = SqlSensitiveDataRedactor.Redact("secret='hidden'");
        Assert.Equal("secret=[redacted]", result);
    }

    // ── Access token variants ──

    [Fact]
    public void Redact_masks_access_token_with_underscore()
    {
        var result = SqlSensitiveDataRedactor.Redact("access_token='xyz'");
        Assert.Equal("access_token=[redacted]", result);
    }

    [Fact]
    public void Redact_masks_accesstoken_no_separator()
    {
        var result = SqlSensitiveDataRedactor.Redact("accesstoken='xyz'");
        Assert.Equal("accesstoken=[redacted]", result);
    }

    [Fact]
    public void Redact_masks_access_token_with_space()
    {
        var result = SqlSensitiveDataRedactor.Redact("access token='xyz'");
        Assert.Equal("access token=[redacted]", result);
    }

    [Fact]
    public void Redact_masks_access_token_with_dash()
    {
        var result = SqlSensitiveDataRedactor.Redact("access-token='xyz'");
        Assert.Equal("access-token=[redacted]", result);
    }

    // ── User ID variants ──

    [Fact]
    public void Redact_masks_user_id_with_underscore()
    {
        var result = SqlSensitiveDataRedactor.Redact("user_id='admin'");
        Assert.Equal("user_id=[redacted]", result);
    }

    [Fact]
    public void Redact_masks_userid_no_separator()
    {
        var result = SqlSensitiveDataRedactor.Redact("userid='admin'");
        Assert.Equal("userid=[redacted]", result);
    }

    [Fact]
    public void Redact_masks_uid()
    {
        var result = SqlSensitiveDataRedactor.Redact("uid='admin'");
        Assert.Equal("uid=[redacted]", result);
    }

    // ── Multiple secrets ──

    [Fact]
    public void Redact_multiple_secrets_in_string()
    {
        var input = "password='secret1';token='secret2'";
        var result = SqlSensitiveDataRedactor.Redact(input);
        Assert.Equal("password=[redacted];token=[redacted]", result);
    }

    [Fact]
    public void Redact_preserves_non_sensitive_keys_in_connection_string()
    {
        var input = "Server=mydb;Port=5432;password=secret";
        var result = SqlSensitiveDataRedactor.Redact(input);
        Assert.Equal("Server=mydb;Port=5432;password=[redacted]", result);
    }

    // ── In complex error message ──

    [Fact]
    public void Redact_in_complex_error_message()
    {
        var input = "ERROR: authentication failed for user 'admin' with password='wrongpass'";
        var result = SqlSensitiveDataRedactor.Redact(input);
        Assert.Equal("ERROR: authentication failed for user 'admin' with password=[redacted]", result);
    }

    [Fact]
    public void Redact_preserves_semicolons_in_sql()
    {
        var input = "SELECT 1; password='secret'; SELECT 2";
        var result = SqlSensitiveDataRedactor.Redact(input);
        Assert.Equal("SELECT 1; password=[redacted]; SELECT 2", result);
    }

    [Fact]
    public void Redact_handles_escaped_quote_in_single_quoted_value()
    {
        // '' is SQL escape for single quote
        var result = SqlSensitiveDataRedactor.Redact("password='it''s secret'");
        Assert.Equal("password=[redacted]", result);
    }

    [Fact]
    public void Redact_handles_escaped_quote_in_double_quoted_value()
    {
        // "" is escape for double quote
        var result = SqlSensitiveDataRedactor.Redact("password=\"say \"\"hello\"\"\"");
        Assert.Equal("password=[redacted]", result);
    }
}

// ──────────────────────────────────────────────
// Enums
// ──────────────────────────────────────────────

public sealed class SqlExecutionEnumsTests
{
    [Fact]
    public void SqlExecutionMode_values()
    {
        Assert.Equal(0, (int)SqlExecutionMode.Selection);
        Assert.Equal(1, (int)SqlExecutionMode.RunToCursor);
        Assert.Equal(2, (int)SqlExecutionMode.SingleBatch);
        Assert.Equal(3, (int)SqlExecutionMode.Script);
    }

    [Fact]
    public void SqlOutputMode_values()
    {
        Assert.Equal(0, (int)SqlOutputMode.Grid);
        Assert.Equal(1, (int)SqlOutputMode.Csv);
        Assert.Equal(2, (int)SqlOutputMode.Xlsx);
        Assert.Equal(3, (int)SqlOutputMode.Xlsb);
        Assert.Equal(4, (int)SqlOutputMode.LogOnly);
    }

    [Fact]
    public void SqlExecutionState_values()
    {
        Assert.Equal(0, (int)SqlExecutionState.Idle);
        Assert.Equal(1, (int)SqlExecutionState.Running);
        Assert.Equal(2, (int)SqlExecutionState.Cancelling);
        Assert.Equal(3, (int)SqlExecutionState.Succeeded);
        Assert.Equal(4, (int)SqlExecutionState.Failed);
    }

    [Fact]
    public void SqlExecutionOutcome_values()
    {
        Assert.Equal(0, (int)SqlExecutionOutcome.Success);
        Assert.Equal(1, (int)SqlExecutionOutcome.Cancelled);
        Assert.Equal(2, (int)SqlExecutionOutcome.Failed);
        Assert.Equal(3, (int)SqlExecutionOutcome.Blocked);
    }

    [Fact]
    public void SqlDiagnosticSeverity_values()
    {
        Assert.Equal(0, (int)SqlDiagnosticSeverity.Error);
        Assert.Equal(1, (int)SqlDiagnosticSeverity.Warning);
        Assert.Equal(2, (int)SqlDiagnosticSeverity.Information);
        Assert.Equal(3, (int)SqlDiagnosticSeverity.Hint);
    }

    [Fact]
    public void SqlLogLevel_values()
    {
        Assert.Equal(0, (int)SqlLogLevel.Trace);
        Assert.Equal(1, (int)SqlLogLevel.Information);
        Assert.Equal(2, (int)SqlLogLevel.Warning);
        Assert.Equal(3, (int)SqlLogLevel.Error);
    }

    [Fact]
    public void SqlExecutionEventKind_values()
    {
        Assert.Equal(0, (int)SqlExecutionEventKind.Started);
        Assert.Equal(1, (int)SqlExecutionEventKind.StatementStarted);
        Assert.Equal(2, (int)SqlExecutionEventKind.StatementCompleted);
        Assert.Equal(3, (int)SqlExecutionEventKind.Log);
        Assert.Equal(4, (int)SqlExecutionEventKind.Diagnostic);
        Assert.Equal(5, (int)SqlExecutionEventKind.AffectedRows);
        Assert.Equal(6, (int)SqlExecutionEventKind.ResultSet);
        Assert.Equal(7, (int)SqlExecutionEventKind.Rows);
        Assert.Equal(8, (int)SqlExecutionEventKind.Truncated);
        Assert.Equal(9, (int)SqlExecutionEventKind.Completed);
    }
}
