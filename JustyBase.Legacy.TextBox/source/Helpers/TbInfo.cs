using System.Collections.Generic;
using System;

namespace FastColoredTextBoxNS.Helpers;

public sealed class TbInfo
{
    public AutocompleteMenu PopupMenu { get; set; }
    public IEnumerable<AutocompleteItem> SuggestionList { get; set; }


    public Dictionary<string, List<string>> AdditionalDataWith;
    public Dictionary<string, List<string>> AdditionalTableData;

    public TbInfo(StringComparer comparer)
    {
        AdditionalDataWith = new Dictionary<string, List<string>>(comparer);
        AdditionalTableData = new Dictionary<string, List<string>>(comparer);
    }
}
