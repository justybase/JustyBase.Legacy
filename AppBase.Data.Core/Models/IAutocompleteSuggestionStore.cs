namespace AppBase.Data.Core.Models;

public interface IAutocompleteSuggestionStore
{
    List<string> OneWord { get; set; }
    List<string> OneWordAdditions { get; set; }
    List<string> TwoWords { get; set; }
    List<string> TwoWordsAdditions { get; set; }
    List<string> TreeWords { get; set; }
    List<string> ActualColumnList { get; set; }
}
