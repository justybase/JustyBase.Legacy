namespace JustyBaseLegacy.UI.Sql;

public enum CodeActionKind
{
    QuickFix,
    DisableRule,
    FormatDocument
}

public sealed class CodeAction
{
    public required string Description { get; init; }
    public required Func<string, string> Apply { get; init; }
    public required string RuleId { get; init; }
    public CodeActionKind Kind { get; init; } = CodeActionKind.QuickFix;

    /// <summary>
    /// Optional diagnostic message from the lint issue, shown as a tooltip
    /// when hovering over the action in the context menu.
    /// </summary>
    public string? TooltipMessage { get; init; }

    /// <summary>
    /// Optional severity label (e.g. "Error", "Warning") displayed alongside the action.
    /// </summary>
    public string? SeverityLabel { get; init; }
}
