using AppBase.Data.Completion;
using JustyBase.NetezzaSqlParser.Linter;

namespace JustyBaseLegacy.UI.Sql;

public interface ICodeActionProvider
{
    IReadOnlyList<CodeAction> GetActions(LintIssue issue, string fullSql);
    CodeAction GetFormatAction();
}
