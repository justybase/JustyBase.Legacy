using JustData.Application.Sql;

namespace AppBase.Tests.Security;

public sealed class SqlSensitiveDataRedactorExtendedTests
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
    public void Redact_preserves_non_sensitive_sql()
    {
        var input = "SELECT * FROM users WHERE id = 42";
        Assert.Equal(input, SqlSensitiveDataRedactor.Redact(input));
    }

    [Fact]
    public void Redact_masks_password_single_quoted()
    {
        var result = SqlSensitiveDataRedactor.Redact("password='mysecret'");
        Assert.Contains("password=[redacted]", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("mysecret", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Redact_masks_password_double_quoted()
    {
        var result = SqlSensitiveDataRedactor.Redact("password=\"mysecret\"");
        Assert.Contains("password=[redacted]", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("mysecret", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Redact_masks_password_unquoted()
    {
        var result = SqlSensitiveDataRedactor.Redact("password=mysecret");
        Assert.Contains("password=[redacted]", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("mysecret", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Redact_masks_token()
    {
        var result = SqlSensitiveDataRedactor.Redact("token='abc123'");
        Assert.Contains("token=[redacted]", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("abc123", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Redact_masks_secret()
    {
        var result = SqlSensitiveDataRedactor.Redact("secret='hidden'");
        Assert.Contains("secret=[redacted]", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("hidden", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Redact_masks_access_token()
    {
        var result = SqlSensitiveDataRedactor.Redact("access_token='xyz'");
        Assert.Contains("access_token=[redacted]", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("xyz", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Redact_masks_access_token_with_underscore()
    {
        var result = SqlSensitiveDataRedactor.Redact("access_token='xyz'");
        Assert.Contains("access_token=[redacted]", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Redact_masks_user_id()
    {
        var result = SqlSensitiveDataRedactor.Redact("user_id='admin'");
        Assert.Contains("user_id=[redacted]", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("admin", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Redact_masks_uid()
    {
        var result = SqlSensitiveDataRedactor.Redact("uid='admin'");
        Assert.Contains("uid=[redacted]", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("admin", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Redact_multiple_secrets_in_string()
    {
        var input = "password='secret1';token='secret2'";
        var result = SqlSensitiveDataRedactor.Redact(input);

        Assert.Contains("password=[redacted]", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("token=[redacted]", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret1", result, StringComparison.Ordinal);
        Assert.DoesNotContain("secret2", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Redact_preserves_non_sensitive_keys()
    {
        var input = "Server=mydb;Port=5432;password=secret";
        var result = SqlSensitiveDataRedactor.Redact(input);

        Assert.Contains("Server=mydb", result, StringComparison.Ordinal);
        Assert.Contains("Port=5432", result, StringComparison.Ordinal);
        Assert.Contains("password=[redacted]", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Redact_key_with_spaces_around_equals()
    {
        var result = SqlSensitiveDataRedactor.Redact("password = secretvalue");
        Assert.Contains("password=[redacted]", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secretvalue", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Redact_key_with_tab_separator()
    {
        // Regex requires '=' between key and value; tab alone is not sufficient
        var result = SqlSensitiveDataRedactor.Redact("password\tsecretvalue");
        Assert.Equal("password\tsecretvalue", result);
    }

    [Fact]
    public void Redact_pwd_key()
    {
        var result = SqlSensitiveDataRedactor.Redact("pwd='mypassword'");
        Assert.Contains("pwd=[redacted]", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("mypassword", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Redact_in_complex_error_message()
    {
        var input = "ERROR: authentication failed for user 'admin' with password='wrongpass'";
        var result = SqlSensitiveDataRedactor.Redact(input);

        Assert.Contains("password=[redacted]", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("wrongpass", result, StringComparison.Ordinal);
        Assert.Contains("authentication failed", result, StringComparison.Ordinal);
    }
}
