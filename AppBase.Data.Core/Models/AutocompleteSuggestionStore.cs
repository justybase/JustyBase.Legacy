namespace AppBase.Data.Core.Models;

/// <summary>
/// Default implementation of IAutocompleteSuggestionStore used for SQL autocomplete state.
/// </summary>
public sealed class AutocompleteSuggestionStore : IAutocompleteSuggestionStore
{
    public static IAutocompleteSuggestionStore Default { get; } = new AutocompleteSuggestionStore();

    public List<string> OneWord { get; set; } = [];
    public List<string> OneWordAdditions { get; set; } = [];
    public List<string> TwoWords { get; set; } = [];
    public List<string> TwoWordsAdditions { get; set; } = [];
    public List<string> TreeWords { get; set; } = [];
    public List<string> ActualColumnList { get; set; } = [];
}
