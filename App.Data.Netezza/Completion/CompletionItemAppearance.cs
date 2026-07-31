using FastColoredTextBoxNS;
using JustyBase.NetezzaSqlParser.Completion;

namespace AppBase.Data.Completion;

/// <summary>
/// Stable icon slots used by the SQL autocomplete ImageList in the WinForms host.
/// The enum deliberately lives next to the completion mapper so all completion
/// sources use the same contract without depending on JustData resources.
/// </summary>
public enum CompletionIconKind
{
    Table,
    View,
    Column,
    Database,
    Schema,
    Function,
    Cte,
    Alias,
    Keyword,
    Snippet,
    DataType,
    Variable,
    Reference
}

public static class CompletionItemAppearance
{
    public static T Apply<T>(
        T item,
        CompletionIconKind icon,
        string detail = null,
        string description = null)
        where T : AutocompleteItem
    {
        item.ImageIndex = (int)icon;
        item.Tag = icon;
        item.DetailText = detail;
        item.DescriptionText = description;
        return item;
    }

    public static T ApplyKind<T>(
        T item,
        CompletionKind kind,
        string detail = null,
        string description = null)
        where T : AutocompleteItem
    {
        return Apply(item, ToIconKind(kind), detail ?? kind.ToString(), description);
    }

    public static CompletionIconKind ToIconKind(CompletionKind kind) => kind switch
    {
        CompletionKind.Table => CompletionIconKind.Table,
        CompletionKind.View => CompletionIconKind.View,
        CompletionKind.Column => CompletionIconKind.Column,
        CompletionKind.Database => CompletionIconKind.Database,
        CompletionKind.Schema => CompletionIconKind.Schema,
        CompletionKind.Function => CompletionIconKind.Function,
        CompletionKind.Cte => CompletionIconKind.Cte,
        CompletionKind.Alias => CompletionIconKind.Alias,
        CompletionKind.Keyword => CompletionIconKind.Keyword,
        CompletionKind.Snippet => CompletionIconKind.Snippet,
        CompletionKind.DataType => CompletionIconKind.DataType,
        CompletionKind.Variable => CompletionIconKind.Variable,
        CompletionKind.ExternalTable => CompletionIconKind.Table,
        _ => CompletionIconKind.Reference
    };
}
