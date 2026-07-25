using AppBase.Services;

namespace AppBase.Tests.Security;

public sealed class SensitiveDataRedactorTests
{
    [Theory]
    [InlineData("Password=secret", "Password=<redacted>")]
    [InlineData("PWD: 'secret'; Host=db", "PWD=<redacted>; Host=db")]
    [InlineData("User Id=admin;Connection String=Host=db;Password=secret", "User Id=<redacted>;Connection String=<redacted>;Password=<redacted>")]
    public void Redact_RemovesSensitiveConnectionValues(string input, string expected)
    {
        Assert.Equal(expected, SensitiveDataRedactor.Redact(input));
    }

    [Fact]
    public void RedactException_RedactsNestedExceptionText()
    {
        Exception exception = new InvalidOperationException(
            "Connection failed: Password=secret; Host=db",
            new Exception("PWD=another-secret"));

        string result = SensitiveDataRedactor.RedactException(exception);

        Assert.DoesNotContain("secret", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Password=<redacted>", result, StringComparison.Ordinal);
        Assert.Contains("PWD=<redacted>", result, StringComparison.Ordinal);
    }
}
