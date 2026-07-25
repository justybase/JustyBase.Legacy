using FastColoredTextBoxNS.Helpers;
namespace DatabaseDataGridView.WinForms.Coloring;

public interface IColorTheme : IEditorColorTheme
{
    Brush GeneralBrush { get; }

    Color ButtonBackColor { get; }
    Color ButtonForeColor { get; }
    Color CbBackColor { get; }
    Color CbForeColor { get; }


    Color GridViewDefaultCellStyleBackColor { get; }
    Color GridViewDefaultCellStyleForeColor { get; }

    Color MainBack { get; }
    Color MainFore { get; }
    Brush NonSelectedTabBrush { get; }


    Color PropertyBackColor { get; }
    Color PropertyBackViewColor { get; }
    Color PropertyForeColor { get; }
    Color PropertyForeViewColor { get; }

    Brush SelectedTabBrush { get; }

    Color TabPageBackColor { get; }
    Color TabPageForeColor { get; }
    Color TextBoxBackColor { get; }
    Color TextBoxForeColor { get; }
    Brush TitleBrush { get; }
    Brush TitleBrushBackground { get; }
    Color TreeViewBackColor { get; }
    Color TreeViewForeColor { get; }
    Color TreeViewLineColor { get; }

    void ColorDataGridView(DataGridView dataGridViewNew, bool forceDoNotAlter = false);
    void ColorForm(Control form, bool force = false);
    void ColorMyDataGridView(ICustomDataGridView myDataGrid);
    void Dispose();
    ToolStripProfessionalRenderer GetRenderer();
    void InitColors();
    bool IsDark(Color color);
    void SetMainBack();
    void SetMainFore();
}