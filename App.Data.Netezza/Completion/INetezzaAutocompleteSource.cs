namespace AppBase.Data.Completion;

/// <summary>
/// Contract for Netezza autocomplete sources updated by AutocompleteClass / BaseWindow.
/// </summary>
public interface INetezzaAutocompleteSource
{
    List<(string basicHint, string description)> AliasHints { get; set; }
    List<string> HintWithTable { get; set; }
}
