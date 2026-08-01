using JustyBase.NetezzaSqlParser.Authoring;

namespace JustyBaseLegacy.UI.ObjectExplorer;

internal static class OutlineRefreshPolicy
{
    public static bool ShouldRefresh(
        bool enabled,
        bool visible,
        bool isLargeDocument,
        int lineCount,
        int characterCount) =>
        enabled
        && visible
        && !isLargeDocument
        && !SqlPerformancePolicy.ShouldSkipOutline(lineCount, characterCount);
}
