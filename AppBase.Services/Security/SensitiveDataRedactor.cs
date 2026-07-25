using System.Text.RegularExpressions;

namespace AppBase.Services;

public static partial class SensitiveDataRedactor
{
    public static string Redact(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text ?? string.Empty;
        }

        return SensitiveValuePattern().Replace(text, "$1=<redacted>");
    }

    public static string RedactException(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return Redact(exception.ToString());
    }

    [GeneratedRegex(
        "(?ix)(password|pwd|user\\s*id|uid|connection\\s*string)\\s*[:=]\\s*(?:\"[^\"]*\"|'[^']*'|[^;\\s,]+)",
        RegexOptions.CultureInvariant)]
    private static partial Regex SensitiveValuePattern();
}
