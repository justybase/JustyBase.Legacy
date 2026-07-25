using System.Text.RegularExpressions;

namespace AppBase.Common;

/// <summary>Recognizes the legacy inline-command syntax without shell state.</summary>
public static partial class InlineCommandPattern
{
    [GeneratedRegex(@"___run: (?<programPath>[^>\n]*) -> (?<arguments>.*)")]
    public static partial Regex Regex();
}
