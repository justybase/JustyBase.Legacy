using FastColoredTextBoxNS;
using JustData.Application.Editor;
using JustData.Application.Sql;
using JustyBase.NetezzaSqlParser.Linter;

namespace JustyBaseLegacy.UI
{
    public partial class BaseWindow
    {
        private readonly Dictionary<FastColoredTextBox, IReadOnlyList<LintIssue>> _lintIssuesByEditor = new();
        private readonly Dictionary<EditorDocumentId, IReadOnlyList<SqlDiagnostic>> _cachedDiagnostics = new();

        private void CacheLintIssues(EditorDocumentId documentId, IReadOnlyList<LintIssue> issues)
        {
            if (_lintDiagnosticsTargets.TryGetValue(documentId, out var target))
            {
                _lintIssuesByEditor[target.Editor] = issues;
            }
        }

        private void CacheDiagnostics(EditorDocumentId documentId, IReadOnlyList<SqlDiagnostic> diagnostics)
        {
            _cachedDiagnostics[documentId] = diagnostics;
        }

        /// <summary>
        /// Exposes the lint issues cache for LightbulbManager and other consumers.
        /// </summary>
        private IReadOnlyDictionary<FastColoredTextBox, IReadOnlyList<LintIssue>> LintIssuesByEditor => _lintIssuesByEditor;

        private bool TryGetLintIssue(FastColoredTextBox editor, int position, out LintIssue issue)
        {
            issue = default!;
            if (!_lintIssuesByEditor.TryGetValue(editor, out var issues))
            {
                return false;
            }

            foreach (var candidate in issues)
            {
                int start = candidate.StartOffset;
                int end = Math.Max(start + 1, candidate.EndOffset);
                if (position >= start && position < end)
                {
                    issue = candidate;
                    return true;
                }
            }

            return false;
        }

        private bool TryGetLintIssueOnLine(FastColoredTextBox editor, int line, out LintIssue issue)
        {
            issue = default!;
            if (!_lintIssuesByEditor.TryGetValue(editor, out var issues))
                return false;

            var lineIssues = issues
                .Where(candidate => candidate.StartLine == line + 1)
                .Where(candidate => !IsCascadingWhereDiagnostic(candidate))
                .OrderBy(candidate => (int)candidate.Severity)
                .ThenBy(candidate => candidate.StartOffset)
                .ToArray();

            if (lineIssues.Length == 0)
                return false;

            issue = lineIssues[0];
            return true;
        }

        private static string GetDiagnosticMessage(LintIssue issue)
        {
            string message = issue.Message;
            string prefix = issue.RuleId + ": ";
            return message.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                ? message[prefix.Length..]
                : message;
        }

        private static string GetLintSeverityLabel(LintSeverity severity) => severity switch
        {
            LintSeverity.Error => "Error",
            LintSeverity.Warning => "Warning",
            LintSeverity.Information => "Info",
            LintSeverity.Hint => "Hint",
            _ => "Diagnostic"
        };

        private static bool IsCascadingWhereDiagnostic(LintIssue issue) =>
            string.Equals(issue.RuleId, "SQL010", StringComparison.OrdinalIgnoreCase)
            || issue.Message.Contains("WHERE/ON expression must be boolean", StringComparison.OrdinalIgnoreCase);
    }
}
