using AppBase.Data.Completion;
using JustyBase.NetezzaSqlParser.Authoring;
using JustyBase.NetezzaSqlParser.Linter;

namespace JustyBaseLegacy.UI.Sql;

public sealed class CodeActionProvider : ICodeActionProvider
{
    public static readonly CodeActionProvider Default = new();

    public static IReadOnlyList<CodeAction> GetActions(LintIssue issue, string fullSql)
        => Default.DoGetActions(issue, fullSql);

    public static CodeAction GetFormatAction()
        => Default.DoGetFormatAction();

    // --- Instance methods (DoXxx pattern) ---
    public IReadOnlyList<CodeAction> DoGetActions(LintIssue issue, string fullSql)
    {
        var actions = new List<CodeAction>();

        var message = GetMessage(issue);
        var severity = GetSeverityLabel(issue);

        var fix = NzLintCodeActions.GetQuickFix(issue, fullSql);
        if (fix is { } f)
        {
            actions.Add(new CodeAction
            {
                Description = f.Description,
                Apply = f.Apply,
                RuleId = issue.RuleId,
                Kind = CodeActionKind.QuickFix,
                TooltipMessage = message,
                SeverityLabel = severity
            });
        }

        actions.Add(new CodeAction
        {
            Description = $"Disable rule {issue.RuleId}",
            Apply = static sql => sql,
            RuleId = issue.RuleId,
            Kind = CodeActionKind.DisableRule,
            TooltipMessage = message,
            SeverityLabel = severity
        });

        return actions;
    }

    public CodeAction DoGetFormatAction()
    {
        return new CodeAction
        {
            Description = "Format SQL",
            Apply = LegacySqlAuthoringServices.FormatSql,
            RuleId = string.Empty,
            Kind = CodeActionKind.FormatDocument
        };
    }

    private static string GetMessage(LintIssue issue)
    {
        string msg = issue.Message;
        string prefix = issue.RuleId + ": ";
        return msg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? msg[prefix.Length..]
            : msg;
    }

    private static string GetSeverityLabel(LintIssue issue)
    {
        return issue.Severity switch
        {
            LintSeverity.Error => "Error",
            LintSeverity.Warning => "Warning",
            LintSeverity.Information => "Info",
            LintSeverity.Hint => "Hint",
            _ => "Diagnostic"
        };
    }

    // --- Explicit interface implementation ---
    IReadOnlyList<CodeAction> ICodeActionProvider.GetActions(LintIssue issue, string fullSql) => DoGetActions(issue, fullSql);
    CodeAction ICodeActionProvider.GetFormatAction() => DoGetFormatAction();
}
