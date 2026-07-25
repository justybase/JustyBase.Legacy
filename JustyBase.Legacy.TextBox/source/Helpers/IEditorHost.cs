using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastColoredTextBoxNS.Helpers;

public interface IEditorHost
{
    Dictionary<string, List<string>> AdditionalDataWith { get; }
    Dictionary<string, List<string>> AdditionalTabletData { get; }
    FastColoredTextBox AddMainTab(string fileName, string title = "", string sqlContent = "");
    FastColoredTextBox CurrentTB { get; }
    IEnumerable<AutocompleteItem> ActualSuggestionList { get; }
    void CollapseAllregion(FastColoredTextBox fctb);
    void GetTextCommentRanges(FastColoredTextBox fctb);

    void GetTextCommentRanges(string txt, ref string res);
}

