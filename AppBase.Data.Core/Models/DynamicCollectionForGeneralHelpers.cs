namespace AppBase.Data.Core.Models;

/// <summary>
/// Static access point for the autocomplete suggestion store.
/// Production code uses <see cref="Default"/>; tests can inject a mock <see cref="IAutocompleteSuggestionStore"/>.
/// </summary>
public static class DynamicCollectionForGeneralHelpers
{
    /// <summary>
    /// Gets or sets the default store instance. Replace in tests with a mock/fake.
    /// </summary>
    public static IAutocompleteSuggestionStore Default { get; set; } = new AutocompleteSuggestionStore();

    // Backward-compatible accessors for legacy code
    public static List<string> OneWord { get => Default.OneWord; set => Default.OneWord = value; }
    public static List<string> OneWordAdditions { get => Default.OneWordAdditions; set => Default.OneWordAdditions = value; }
    public static List<string> TwoWords { get => Default.TwoWords; set => Default.TwoWords = value; }
    public static List<string> TwoWordsAdditions { get => Default.TwoWordsAdditions; set => Default.TwoWordsAdditions = value; }
    public static List<string> TreeWords { get => Default.TreeWords; set => Default.TreeWords = value; }
    public static List<string> ActualColumnList { get => Default.ActualColumnList; set => Default.ActualColumnList = value; }
}
