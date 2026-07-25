namespace DatabaseDataGridView.WinForms.Interfaces;


public interface IColorConfig
{
    bool UseSpecialColoring { get; set; }
    bool AlternatingRows { get; set; }
    int GrifOffsetHeight { get; set; }
    List<byte> BackgroundFastColored { get; set; }
    List<byte> SelectionColorFastColored { get; set; }
    List<byte> DisabledColorFastColored { get; set; }
    List<byte> IndentBackColorFastColored { get; set; }
    List<byte> LineNumberColorFastColored { get; set; }
    List<byte> FoldingIndicatorColorFastColored { get; set; }
    List<byte> ForeColorFastColored { get; set; }
    List<byte> FontkeyWordsStyle1 { get; set; }
    List<byte> FontkeyWordsStyle2 { get; set; }
    List<byte> FontparamStyle { get; set; }
    List<byte> FontmyCommandsStyle { get; set; }
    List<byte> FontnumberStyle { get; set; }
    List<byte> FontcommentsStyle { get; set; }
    List<byte> FontstringsStyle { get; set; }
    List<byte> FontsameWordsStyle { get; set; }
    List<byte> DgvDefaultCellStyleBackColor { get; set; }
    List<byte> DgvAlternatingRowsDefaultCellStyleBackColor { get; set; }
    List<byte> DgvDefaultCellStyleForeColor { get; set; }
    List<byte> DgvRowHeadersDefaultCellStyleBack { get; set; }
    List<byte> DgvColumnHeadersDefaultCellStyleFore { get; set; }
    List<byte> DgvColumnHeadersDefaultCellStyleBack { get; set; }
    List<byte> DocMapBackColor { get; set; }
    List<byte> DocMapForeColor { get; set; }
    List<byte> TabColor { get; set; }
    List<byte> SelectedtabColor { get; set; }
    List<byte> TabTitleColor { get; set; }
    List<byte> StripBack { get; set; }
    List<byte> StripFore { get; set; }
    List<byte> TreeViewBackColor { get; set; }
    List<byte> TreeViewForeColor { get; set; }
    List<byte> TreeViewLineColor { get; set; }
    List<byte> TextBoxFileSearchBackColor { get; set; }
    List<byte> TextBoxFileSearchForeColor { get; set; }
    List<byte> MenuItemSelected { get; set; }
    List<byte> MenuItemSelectedGradientBegin { get; set; }
    List<byte> MenuItemSelectedGradientEnd { get; set; }
    List<byte> MenuItemBorder { get; set; }
    List<byte> MenuItemPressedGradientBegin { get; set; }
    List<byte> MenuItemPressedGradientMiddle { get; set; }
    List<byte> MenuItemPressedGradientEnd { get; set; }
    List<byte> ButtonSelectedHighlightBorder { get; set; }
    List<byte> GroupingRowColorBack { get; set; }
}