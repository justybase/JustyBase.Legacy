using JustyBase.Netezza.Completion;
using JustyBase.NetezzaSqlParser.Completion;

namespace AppBase.Data.Completion;

/// <summary>
/// Engine-first completion policy shared with Avalonia via <see cref="SqlCompletionMergePolicy"/>.
/// </summary>
public static class LegacyCompletionPolicy
{
    public static bool ShouldRunLegacyPath(IReadOnlyList<CompletionItem> engineItems, string sql)
        => SqlCompletionMergePolicy.ShouldRunLegacyPath(engineItems, sql);
}
