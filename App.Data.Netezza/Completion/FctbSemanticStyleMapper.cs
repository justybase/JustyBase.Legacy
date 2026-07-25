using FastColoredTextBoxNS;
using FastColoredTextBoxNS.Helpers;
using JustyBase.NetezzaSqlParser.Authoring;

namespace AppBase.Data.Completion;

public static class FctbSemanticStyleMapper
{
    public static TextStyle? Resolve(SemanticTokenKind kind, FctbColors colors)
    {
        return kind switch
        {
            SemanticTokenKind.Table => colors.TableStyle,
            SemanticTokenKind.Column => colors.ColumnStyle,
            SemanticTokenKind.Cte => colors.CteStyle,
            SemanticTokenKind.Alias => colors.AliasStyle,
            _ => null
        };
    }
}
