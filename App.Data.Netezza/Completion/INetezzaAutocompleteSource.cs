using AppBase.Data.Core.Models;

namespace AppBase.Data.Completion;

/// <summary>
/// Contract for Netezza autocomplete sources updated by AutocompleteClass / BaseWindow.
/// </summary>
public interface INetezzaAutocompleteSource
{
    INetezzaAutocompleteState State { get; }
    List<(string basicHint, string description)> AliasHints { get; set; }
    List<string> HintWithTable { get; set; }
}
