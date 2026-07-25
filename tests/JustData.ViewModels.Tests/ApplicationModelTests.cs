using JustData.Application.Login;
using JustData.Application.Schema;
using JustData.Application.Sql;
using JustData.Application.Editor;
using JustData.Application.ImportExport;
using JustData.Application.Files;

namespace JustData.ViewModels.Tests;

public sealed class ApplicationModelTests
{
    // ── ApplicationSession ──

    [Fact]
    public void ApplicationSession_initially_has_no_login()
    {
        var session = new ApplicationSession();

        Assert.Null(session.CurrentLogin);
        Assert.Empty(session.Profiles);
    }

    [Fact]
    public void ApplicationSession_SetLogin_stores_login_and_clones_profiles()
    {
        var session = new ApplicationSession();
        var profile = new ConnectionProfile { Name = "prod", Password = "secret" };
        var profiles = new List<ConnectionProfile> { profile };

        session.SetLogin(new LoginSelection(profile, true), profiles);

        Assert.NotNull(session.CurrentLogin);
        Assert.Equal("prod", session.CurrentLogin!.Profile.Name);
        Assert.Single(session.Profiles);
        // Profiles should be cloned - modifying original shouldn't affect session
        profile.Name = "changed";
        Assert.Equal("prod", session.Profiles[0].Name);
    }

    [Fact]
    public void ApplicationSession_SetLogin_throws_on_null()
    {
        var session = new ApplicationSession();
        Assert.Throws<ArgumentNullException>(() => session.SetLogin(null!, Array.Empty<ConnectionProfile>()));
    }

    // ── ConnectionProfile ──

    [Fact]
    public void ConnectionProfile_Clone_creates_deep_copy()
    {
        var original = new ConnectionProfile
        {
            Name = "test",
            Driver = "NetezzaSQL",
            Server = "server",
            UserName = "user",
            Password = "secret",
            Database = "SYSTEM"
        };

        var clone = original.Clone();

        Assert.Equal(original.Name, clone.Name);
        Assert.Equal(original.Password, clone.Password);
        // Modify original - clone should be independent
        original.Name = "changed";
        Assert.Equal("test", clone.Name);
    }

    [Fact]
    public void ConnectionProfile_ToString_redacts_password()
    {
        var profile = new ConnectionProfile { Name = "test", Password = "supersecret" };

        var result = profile.ToString();

        Assert.DoesNotContain("supersecret", result, StringComparison.Ordinal);
        Assert.Contains("[REDACTED]", result, StringComparison.Ordinal);
    }

    // ── LoginSelection ──

    [Fact]
    public void LoginSelection_stores_profile_and_fast_login()
    {
        var profile = new ConnectionProfile { Name = "local" };
        var selection = new LoginSelection(profile, fastLogin: true);

        Assert.Same(profile, selection.Profile);
        Assert.True(selection.FastLogin);
    }

    [Fact]
    public void LoginSelection_throws_on_null_profile()
    {
        Assert.Throws<ArgumentNullException>(() => new LoginSelection(null!, fastLogin: false));
    }

    [Fact]
    public void LoginSelection_ToString_redacts_password()
    {
        var profile = new ConnectionProfile { Name = "test", Password = "secret" };
        var selection = new LoginSelection(profile, fastLogin: false);

        var result = selection.ToString();

        Assert.DoesNotContain("secret", result, StringComparison.Ordinal);
        Assert.Contains("[REDACTED]", result, StringComparison.Ordinal);
    }

    // ── SqlExecutionRequest ──

    [Fact]
    public void SqlExecutionRequest_WithMode_returns_new_instance_with_updated_mode()
    {
        var request = new SqlExecutionRequest(EditorDocumentId.New(), "select 1")
        {
            Mode = SqlExecutionMode.Selection,
            OutputMode = SqlOutputMode.Grid
        };

        var updated = request.WithMode(SqlExecutionMode.Script, SqlOutputMode.Csv);

        Assert.Equal(SqlExecutionMode.Script, updated.Mode);
        Assert.Equal(SqlOutputMode.Csv, updated.OutputMode);
        Assert.NotSame(request, updated);
    }

    [Fact]
    public void SqlExecutionRequest_WithMode_preserves_outputMode_when_not_specified()
    {
        var request = new SqlExecutionRequest(EditorDocumentId.New(), "select 1")
        {
            OutputMode = SqlOutputMode.Xlsx
        };

        var updated = request.WithMode(SqlExecutionMode.SingleBatch);

        Assert.Equal(SqlExecutionMode.SingleBatch, updated.Mode);
        Assert.Equal(SqlOutputMode.Xlsx, updated.OutputMode);
    }

    // ── SqlSensitiveDataRedactor ──

    [Fact]
    public void Redact_returns_empty_for_null_or_empty()
    {
        Assert.Equal(string.Empty, SqlSensitiveDataRedactor.Redact(null));
        Assert.Equal(string.Empty, SqlSensitiveDataRedactor.Redact(""));
    }

    [Fact]
    public void Redact_masks_password_values()
    {
        var result = SqlSensitiveDataRedactor.Redact("password='mysecret123'");
        Assert.Contains("password=[redacted]", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("mysecret123", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Redact_masks_token_values()
    {
        var result = SqlSensitiveDataRedactor.Redact("token=\"abc123def\"");
        Assert.Contains("token=[redacted]", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("abc123def", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Redact_masks_pwd_values()
    {
        var result = SqlSensitiveDataRedactor.Redact("pwd=plainpassword");
        Assert.Contains("pwd=[redacted]", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("plainpassword", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Redact_preserves_non_sensitive_text()
    {
        var input = "SELECT * FROM users WHERE id = 42";
        Assert.Equal(input, SqlSensitiveDataRedactor.Redact(input));
    }

    // ── SchemaPath ──

    [Fact]
    public void SchemaPath_ToString_joins_non_empty_parts()
    {
        var path = new SchemaPath("conn", "db", "schema", "table");
        Assert.Equal("conn.db.schema.table", path.ToString());
    }

    [Fact]
    public void SchemaPath_ToString_skips_null_parts()
    {
        var path = new SchemaPath("conn", Database: null, Schema: null, Object: "table");
        Assert.Equal("conn.table", path.ToString());
    }

    [Fact]
    public void SchemaPath_ToString_skips_whitespace_parts()
    {
        var path = new SchemaPath("conn", "  ", "schema", "table");
        Assert.Equal("conn.schema.table", path.ToString());
    }

    // ── EditorDocumentSnapshot ──

    [Fact]
    public void EditorDocumentSnapshot_stores_all_fields()
    {
        var snapshot = new EditorDocumentSnapshot(
            EditorDocumentId.New(), "title", "text", "/tmp/test.sql",
            true, false, "conn", "db", true, false, true);

        Assert.Equal("title", snapshot.Title);
        Assert.Equal("text", snapshot.Text);
        Assert.True(snapshot.IsDirty);
        Assert.False(snapshot.IsReadOnly);
        Assert.True(snapshot.ExternalChangePending);
    }

    // ── ManySqlBundle ──

    [Fact]
    public void ManySqlBundle_stores_all_fields()
    {
        var bundle = new ManySqlBundle(
            ["/tmp/a.sql", "/tmp/b.sql"],
            [new ManySqlContent("tab1", "select 1")],
            ["/tmp/a.sql", "tab1"],
            0);

        Assert.Equal(2, bundle.SqlPaths.Count);
        Assert.Single(bundle.SqlContentList);
        Assert.Equal(2, bundle.TabsOrder.Count);
        Assert.Equal(0, bundle.SelectedTabNum);
    }

    // ── ImportResult ──

    [Fact]
    public void ImportResult_stores_all_fields()
    {
        var result = new ImportResult(100, 95, 5, ["row 3 failed"], "target_table", IsPartial: true);

        Assert.Equal(100, result.RowsRead);
        Assert.Equal(95, result.RowsImported);
        Assert.Equal(5, result.RowsSkipped);
        Assert.Single(result.Errors);
        Assert.Equal("target_table", result.TargetTable);
        Assert.True(result.IsPartial);
    }

    // ── ExportProgress ──

    [Fact]
    public void ExportProgress_stores_all_fields()
    {
        var progress = new ExportProgress("writing", 42, "msg", true, null);

        Assert.Equal("writing", progress.Stage);
        Assert.Equal(42, progress.RowsWritten);
        Assert.Equal("msg", progress.Message);
        Assert.True(progress.IsCompleted);
        Assert.Null(progress.ErrorMessage);
    }

    // ── FileSearchRequest ──

    [Fact]
    public void FileSearchRequest_default_values()
    {
        var request = new FileSearchRequest("query", [".sql"]);

        Assert.Equal("query", request.Query);
        Assert.False(request.MatchWholeWord);
        Assert.False(request.MatchCase);
        Assert.False(request.UseRegex);
        Assert.Equal(200, request.MaxFiles);
        Assert.Equal(50, request.MaxMatchesPerFile);
        Assert.Null(request.Timeout);
    }
}
