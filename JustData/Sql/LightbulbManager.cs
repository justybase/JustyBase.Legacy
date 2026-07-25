using FastColoredTextBoxNS;
using JustyBase.NetezzaSqlParser.Linter;

namespace JustyBaseLegacy.UI.Sql;

/// <summary>
/// Manages lightbulb markers in the FCTB gutter.
/// Groups lint issues by line and creates/updates lightbulb markers
/// with associated CodeActions for each line that has issues.
/// </summary>
public sealed class LightbulbManager
{
    private readonly Dictionary<FastColoredTextBox, IReadOnlyList<LintIssue>> _issuesCache;
    private readonly ICodeActionProvider _codeActionProvider;

    public LightbulbManager(Dictionary<FastColoredTextBox, IReadOnlyList<LintIssue>> issuesCache, ICodeActionProvider codeActionProvider)
    {
        _issuesCache = issuesCache ?? throw new ArgumentNullException(nameof(issuesCache));
        _codeActionProvider = codeActionProvider ?? throw new ArgumentNullException(nameof(codeActionProvider));
    }

    /// <summary>
    /// Refreshes lightbulb markers for the given editor based on cached lint issues.
    /// Call this whenever new lint results arrive.
    /// </summary>
    public void RefreshLightbulbs(FastColoredTextBox editor)
    {
        if (editor is null || editor.IsDisposed)
            return;

        editor.ClearLightbulbMarkers();

        if (!_issuesCache.TryGetValue(editor, out var issues) || issues is null || issues.Count == 0)
        {
            editor.Invalidate();
            return;
        }

        var fullSql = editor.Text;

        // Group issues by their line number so we show one lightbulb per line
        // that aggregates all issues on that line.
        var perLine = new Dictionary<int, List<LintIssue>>();
        foreach (var issue in issues)
        {
            if (issue.StartOffset < 0 || issue.StartOffset >= fullSql.Length)
                continue;

            // StartLine is the location emitted by the parser/linter and is also
            // the location displayed in the Diagnostics grid. Prefer it over
            // converting StartOffset through FCTB: the two coordinate systems can
            // differ when the editor normalizes line endings (CRLF/LF), which
            // makes the bulb land on a nearby keyword such as WHERE.
            int line = issue.StartLine > 0
                ? issue.StartLine - 1
                : editor.PositionToPlace(issue.StartOffset).iLine;

            if (line < 0 || line >= editor.LinesCount)
                continue;
            if (!perLine.TryGetValue(line, out var list))
            {
                list = new List<LintIssue>();
                perLine[line] = list;
            }
            list.Add(issue);
        }

        foreach (var kvp in perLine)
        {
            int line = kvp.Key;
            var lineIssues = GetRelevantLineIssues(kvp.Value);

            // Collect all CodeActions for issues on this line.
            var actions = new List<CodeAction>();
            foreach (var issue in lineIssues)
            {
                actions.AddRange(_codeActionProvider.GetActions(issue, fullSql));
            }

            if (actions.Count > 0)
            {
                editor.SetLightbulbMarker(line, actions);
            }
        }

        editor.Invalidate();
    }

    /// <summary>
    /// Removes parser follow-up diagnostics when a more precise diagnostic
    /// already explains the problem on the same source line. For example,
    /// an unknown column in a WHERE expression can also make the parser emit
    /// SQL010 ("WHERE/ON expression must be boolean"). The latter is a
    /// consequence of the unknown column, not an additional useful fix.
    /// </summary>
    private static IReadOnlyList<LintIssue> GetRelevantLineIssues(IReadOnlyList<LintIssue> issues)
    {
        if (issues.Count <= 1)
            return issues;

        bool hasConcreteIssue = issues.Any(issue => !IsCascadingWhereDiagnostic(issue));
        if (!hasConcreteIssue)
            return issues;

        return issues
            .Where(issue => !IsCascadingWhereDiagnostic(issue))
            .OrderBy(issue => (int)issue.Severity)
            .ThenBy(issue => issue.StartOffset)
            .ToArray();
    }

    private static bool IsCascadingWhereDiagnostic(LintIssue issue) =>
        string.Equals(issue.RuleId, "SQL010", StringComparison.OrdinalIgnoreCase)
        || issue.Message.Contains("WHERE/ON expression must be boolean", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Clears all lightbulb markers from the given editor.
    /// </summary>
    public void ClearLightbulbs(FastColoredTextBox editor)
    {
        if (editor is null || editor.IsDisposed)
            return;

        editor.ClearLightbulbMarkers();
        editor.Invalidate();
    }
}
