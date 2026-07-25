using AppBase.Services;

namespace AppBase.Tests.Security;

public sealed class SensitiveDataRedactorExtendedTests
{
    [Fact]
    public void Redact_returns_empty_for_null()
    {
        Assert.Equal(string.Empty, SensitiveDataRedactor.Redact(null!));
    }

    [Fact]
    public void Redact_returns_empty_for_empty()
    {
        Assert.Equal(string.Empty, SensitiveDataRedactor.Redact(""));
    }

    [Fact]
    public void Redact_preserves_non_sensitive_text()
    {
        var input = "SELECT * FROM users WHERE id = 42";
        Assert.Equal(input, SensitiveDataRedactor.Redact(input));
    }

    [Fact]
    public void Redact_masks_password_with_equals()
    {
        var result = SensitiveDataRedactor.Redact("password=secret123");
        Assert.Contains("password=<redacted>", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret123", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Redact_masks_password_with_colon()
    {
        var result = SensitiveDataRedactor.Redact("password: secret123");
        Assert.Contains("password=<redacted>", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret123", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Redact_masks_pwd()
    {
        var result = SensitiveDataRedactor.Redact("PWD='mypassword'");
        Assert.Contains("PWD=<redacted>", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("mypassword", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Redact_masks_user_id()
    {
        var result = SensitiveDataRedactor.Redact("User Id=admin");
        Assert.Contains("User Id=<redacted>", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("admin", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Redact_masks_uid()
    {
        var result = SensitiveDataRedactor.Redact("uid=testuser");
        Assert.Contains("uid=<redacted>", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("testuser", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Redact_masks_connection_string()
    {
        var result = SensitiveDataRedactor.Redact("Connection String=Host=db.example.com");
        Assert.Contains("Connection String=<redacted>", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("db.example.com", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Redact_multiple_keys_in_one_string()
    {
        var input = "Password=secret;User Id=admin;Server=myserver";
        var result = SensitiveDataRedactor.Redact(input);

        Assert.Contains("Password=<redacted>", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("User Id=<redacted>", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret", result, StringComparison.Ordinal);
        Assert.DoesNotContain("admin", result, StringComparison.Ordinal);
        // Server is NOT a sensitive key
        Assert.Contains("Server=myserver", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Redact_exception_returns_redacted_string()
    {
        var exception = new InvalidOperationException("password='leaked'");
        var result = SensitiveDataRedactor.RedactException(exception);

        Assert.DoesNotContain("leaked", result, StringComparison.Ordinal);
        Assert.Contains("password=<redacted>", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("InvalidOperationException", result, StringComparison.Ordinal);
    }

    [Fact]
    public void RedactException_throws_on_null()
    {
        Assert.Throws<ArgumentNullException>(() => SensitiveDataRedactor.RedactException(null!));
    }

    [Fact]
    public void Redact_preserves_case_of_key_word()
    {
        var result = SensitiveDataRedactor.Redact("Password=test");
        Assert.StartsWith("Password=", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Redact_handles_quoted_value()
    {
        var result = SensitiveDataRedactor.Redact("password=\"quoted_secret\"");
        Assert.Contains("password=<redacted>", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("quoted_secret", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Redact_handles_single_quoted_value()
    {
        var result = SensitiveDataRedactor.Redact("password='single_quoted'");
        Assert.Contains("password=<redacted>", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("single_quoted", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Redact_sensitive_word_in_middle_of_string()
    {
        var input = "Server=mydb;Password=secret;Port=5432";
        var result = SensitiveDataRedactor.Redact(input);

        Assert.Contains("Server=mydb", result, StringComparison.Ordinal);
        Assert.Contains("Password=<redacted>", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Port=5432", result, StringComparison.Ordinal);
    }
}
