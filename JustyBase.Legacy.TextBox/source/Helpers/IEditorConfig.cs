using System.Collections.Generic;

namespace FastColoredTextBoxNS.Helpers;


public interface IEditorConfig
{
    int PopupMenuDefaultAppearInterval { get; set; }
    bool TypoCorrect { get; set; }
    int CurrentWordLengthLimit { get; set; }
    List<string> TypoPatternList { get; set; }
    Dictionary<string, string> QuickSnippets { get; set; }
    int TypoLimit { get; set; }
    bool BracketFolding { get; set; }
    int GenerateToolTipTime { get; set; }
    bool DontUseIndent { get; set; }
    bool AutoCompleteBrackets { get; set; }
    string EditorHotkeys { get; set; }
}
