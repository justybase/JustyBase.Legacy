using FastColoredTextBoxNS;
using System.Drawing;

namespace FastColoredTextBoxNS.Helpers;

public interface IEditorColorTheme
{
    FctbColors CurrentFctbColors { get; }
    void SetStylesForFastColoring();
}
