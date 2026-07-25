using System.Drawing;

namespace FastColoredTextBoxNS.Helpers;

public sealed class FctbColors
{
    public TextStyle StringsStyle { get; set; }
    public TextStyle QuotedTextStyle { get; set; } = new TextStyle(null, null, FontStyle.Italic);
    public TextStyle CommentsStyle { get; set; }
    public TextStyle ErrorStyle { get; set; } = new TextStyle(new SolidBrush(Color.White), Brushes.Red, FontStyle.Regular);
    public TextStyle WarningStyle { get; set; } = new TextStyle(new SolidBrush(Color.Black), Brushes.Gold, FontStyle.Regular);
    public TextStyle LintInfoStyle { get; set; } = new TextStyle(new SolidBrush(Color.DimGray), Brushes.LightSteelBlue, FontStyle.Regular);
    public TextStyle KeyWordsStyle1 { get; set; }
    public TextStyle KeyWordsStyle2 { get; set; }
    public TextStyle BoldUnderlineStyle { get; set; }
    public  TextStyle ParamStyle { get; set; }
    public TextStyle MyCommandsStyle { get; set; }
    public Color FctbPopupMenuSelected { get; set; }
    public Color FctbSelectionColor { get; set; }
    public Color FctbDisabledColor { get; set; }
    public Color FctbBackColor { get; set; }
    public Color FctbIndentBackColor { get; set; }
    public Color FctbLineNumberColor { get; set; }
    public Color FctbFoldingIndicatorColor { get; set; }
    public Color FctbForeColor { get; set; }
    public TextStyle NumberStyle { get; set; }
    public MarkerStyle SameWordsStyle { get; set; }

    public TextStyle TableStyle { get; set; }
    public TextStyle ColumnStyle { get; set; }
    public TextStyle CteStyle { get; set; }
    public TextStyle AliasStyle { get; set; }

    public TextStyle[] SemanticStyles => [TableStyle, ColumnStyle, CteStyle, AliasStyle];
}