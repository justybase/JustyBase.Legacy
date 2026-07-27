using AppBase.Common.Enums;
using FastColoredTextBoxNS;

namespace JustyBaseLegacy.UI.Sql;

/// <summary>Operations requested by an editor panel from its WinForms host.</summary>
public interface ISqlEditorUiPort : IWin32Window
{
    void GetTextCommentRanges(FastColoredTextBox editor);
    void WireEditorEvents(FastColoredTextBox editor, bool isNetezza);
    Task CbConnectionsSelectedIndexChanged(Action<bool> setEnabled);
    Task RunSQL(int mode = 0, ExportOptions exportOption = ExportOptions.grid, bool explain = false, string? filePath = null);
    void Stop_Click(object sender, EventArgs e);
    void XLSXtoolStripMenuItem_Click(object sender, EventArgs e);
    void RefreshTabKeepConnectionProperty();
    void OpenNewSqlDocument();
    void SaveOnTabEventHandler(object sender, EventArgs e);
    Task OpenAsync();
    bool ForceNormalPaste { get; set; }
}
