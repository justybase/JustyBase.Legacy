using System.Runtime.CompilerServices;
using AppBase.Common.Interfaces;
using AppBase.Data.Core.Core;
using AppBase.Data.Core.Interfaces;
using FastColoredTextBoxNS;
using JustyBase.Core.Database;

namespace AppBase.Data.Completion;

/// <summary>
/// WinForms host adapter for the shared <see cref="ISqlDbWordListProvider"/>
/// contract. Delegates the query to the existing live-DB fallback
/// (<see cref="LegacyDbCompletionFallback"/>) and maps its FCTB items
/// (<see cref="AutocompleteItem"/>) onto the neutral <see cref="SqlWordListItem"/>
/// contract. The FCTB hot completion path is unchanged (it stays synchronous).
/// </summary>
/// <remarks>
/// The underlying fallback resolves the Netezza catalog through the completion
/// context's currently selected connection (existing behavior); the request's
/// <see cref="SqlWordListRequest.ConnectionName"/> is honored on the DB2 path.
/// </remarks>
public sealed class LegacyDbWordListProvider : ISqlDbWordListProvider
{
    private readonly LegacyDbCompletionFallback _fallback;

    public LegacyDbWordListProvider(
        INetezzaCompletionContext completionContext,
        IGeneralDbService generalDbService,
        INetezzaSchemaTableCatalog schemaTables,
        IConnectionSessionRegistry? connectionSessions = null)
    {
        _fallback = new LegacyDbCompletionFallback(
            completionContext ?? throw new ArgumentNullException(nameof(completionContext)),
            generalDbService ?? throw new ArgumentNullException(nameof(generalDbService)),
            schemaTables ?? throw new ArgumentNullException(nameof(schemaTables)),
            connectionSessions);
    }

    public async IAsyncEnumerable<SqlWordListItem> GetWordsListAsync(
        SqlWordListRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(request.ConnectionName)
            || string.IsNullOrEmpty(request.DatabaseName))
        {
            yield break;
        }

        foreach (var item in _fallback.GetCompletions(
                     request.Fragment,
                     request.ConnectionName,
                     request.DatabaseName))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return ToNeutral(item);
        }
    }

    /// <summary>Maps an FCTB item to the neutral contract using its icon slot.</summary>
    public static SqlWordListItem ToNeutral(AutocompleteItem item)
    {
        CompletionIconKind icon = item.Tag is CompletionIconKind tagged
            ? tagged
            : (CompletionIconKind)Math.Max(0, item.ImageIndex);

        return new SqlWordListItem(
            item.Text,
            ToKind(icon),
            item.DetailText,
            item.DescriptionText);
    }

    private static SqlWordListKind ToKind(CompletionIconKind icon) => icon switch
    {
        CompletionIconKind.Table => SqlWordListKind.Table,
        CompletionIconKind.View => SqlWordListKind.View,
        CompletionIconKind.Column => SqlWordListKind.Column,
        CompletionIconKind.Database => SqlWordListKind.Database,
        CompletionIconKind.Schema => SqlWordListKind.Schema,
        CompletionIconKind.Function => SqlWordListKind.Function,
        CompletionIconKind.Cte => SqlWordListKind.With,
        CompletionIconKind.Alias => SqlWordListKind.Alias,
        CompletionIconKind.Keyword => SqlWordListKind.Keyword,
        CompletionIconKind.Snippet => SqlWordListKind.Snippet,
        CompletionIconKind.DataType => SqlWordListKind.DataType,
        CompletionIconKind.Variable => SqlWordListKind.Variable,
        CompletionIconKind.Reference => SqlWordListKind.Reference,
        _ => SqlWordListKind.Keyword
    };
}
