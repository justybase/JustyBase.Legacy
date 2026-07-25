using FastColoredTextBoxNS;
using FastColoredTextBoxNS.Helpers;
using DatabaseDataGridView.WinForms.Interfaces;


namespace DatabaseDataGridView.WinForms.Coloring;


/// <summary>
/// Provides a centralized manager for the application's color theme, styles, and custom rendering.
/// This class replaces the previous combination of Colorize and _colorTheme.
/// It should be instantiated once and shared or passed as a dependency.
/// </summary>
public sealed class ColorTheme : IDisposable, IColorTheme, IEditorColorTheme
{
    private readonly IColorConfig _config;
    private bool _disposed = false;

    // Color properties (formerly ColorizeHelpers static fields)
    public Color MainFore { get; private set; }
    public Color MainBack { get; private set; }

    public Brush NonSelectedTabBrush { get; private set; } = SystemBrushes.Control;
    public Brush TitleBrushBackground { get; private set; } = Brushes.Blue;

    public Color GridViewDefaultCellStyleBackColor { get; private set; }
    public Color GridViewDefaultCellStyleForeColor { get; private set; }

    public Brush GeneralBrush { get; private set; } = SystemBrushes.ControlText;
    public Brush SelectedTabBrush { get; private set; } = Brushes.LightGray;
    public Brush TitleBrush { get; private set; } = Brushes.Black;

    public Color TreeViewBackColor { get; private set; }
    public Color TreeViewForeColor { get; private set; }
    public Color TreeViewLineColor { get; private set; }

    public Color CbForeColor { get; private set; }
    public Color CbBackColor { get; private set; }

    public Color TabPageForeColor { get; private set; }
    public Color TabPageBackColor { get; private set; }

    public Color ButtonForeColor { get; private set; }
    public Color ButtonBackColor { get; private set; }

    public Color TextBoxForeColor { get; private set; }
    public Color TextBoxBackColor { get; private set; }

    public Color PropertyForeColor { get; private set; }
    public Color PropertyBackColor { get; private set; }

    public Color PropertyForeViewColor { get; private set; }
    public Color PropertyBackViewColor { get; private set; }

    public FctbColors CurrentFctbColors { get; init; }

    public ColorTheme(IColorConfig config)
    {
        _config = config;
        CurrentFctbColors = new FctbColors();
    }

    public void InitColors()
    {
        SetMainFore();
        SetMainBack();
        GeneralBrush = new SolidBrush(MainFore);

        if (_config.UseSpecialColoring)
        {
            CbForeColor = MainFore;
            CbBackColor = MainBack;
            ButtonForeColor = MainFore;
            ButtonBackColor = MainBack;
            TabPageForeColor = MainFore;
            TabPageBackColor = MainBack;
            TreeViewForeColor = Color.FromArgb(_config.TreeViewForeColor[0], _config.TreeViewForeColor[1], _config.TreeViewForeColor[2]);
            TreeViewBackColor = Color.FromArgb(_config.TreeViewBackColor[0], _config.TreeViewBackColor[1], _config.TreeViewBackColor[2]);
            TreeViewLineColor = Color.FromArgb(_config.TreeViewLineColor[0], _config.TreeViewLineColor[1], _config.TreeViewLineColor[2]);
            TextBoxForeColor = MainFore;
            TextBoxBackColor = MainBack;
            PropertyForeColor = TreeViewForeColor;
            PropertyBackColor = TreeViewBackColor;
            PropertyForeViewColor = TreeViewForeColor;
            PropertyBackViewColor = TreeViewBackColor;
            GridViewDefaultCellStyleForeColor = Color.FromArgb(_config.DgvDefaultCellStyleForeColor[0], _config.DgvDefaultCellStyleForeColor[1], _config.DgvDefaultCellStyleForeColor[2]);
            GridViewDefaultCellStyleBackColor = Color.FromArgb(_config.DgvDefaultCellStyleBackColor[0], _config.DgvDefaultCellStyleBackColor[1], _config.DgvDefaultCellStyleBackColor[2]);
            NonSelectedTabBrush = new SolidBrush(Color.FromArgb(_config.TabColor[0], _config.TabColor[1], _config.TabColor[2]));
            SelectedTabBrush = new SolidBrush(Color.FromArgb(_config.SelectedtabColor[0], _config.SelectedtabColor[1], _config.SelectedtabColor[2]));
            TitleBrush = new SolidBrush(Color.FromArgb(_config.TabTitleColor[0], _config.TabTitleColor[1], _config.TabTitleColor[2]));
        }
        else
        {
            CbForeColor = Color.Black;
            CbBackColor = Color.White;
            ButtonForeColor = Color.Black;
            ButtonBackColor = Color.Transparent;
            TabPageForeColor = Color.Black;
            TabPageBackColor = Color.Transparent;
            TreeViewForeColor = Color.Black;
            TreeViewBackColor = Color.White;
            TreeViewLineColor = Color.FromArgb(0, 0, 0, 0);
            TextBoxForeColor = Color.Black;
            TextBoxBackColor = Color.White;
            PropertyForeColor = SystemColors.ControlText;
            PropertyBackColor = SystemColors.Control;
            PropertyForeViewColor = Color.Black;
            PropertyBackViewColor = Color.White;
            GridViewDefaultCellStyleForeColor = Color.Black;
            GridViewDefaultCellStyleBackColor = Color.White;
            NonSelectedTabBrush = SystemBrushes.Control;
            SelectedTabBrush = Brushes.LightGray;
            TitleBrush = Brushes.Black;
        }

        //if (mainBack.R + mainBack.G + mainBack.B < 300) // tab is Dark
        if (IsDark(MainBack))
        {
            TitleBrushBackground = Brushes.White;
        }
        else
        {
            TitleBrushBackground = Brushes.Blue;
        }

        MyRendererToolStrinRenderer.SelectedMenuBrush = new SolidBrush(Color.FromArgb(_config.MenuItemSelectedGradientBegin[0], _config.MenuItemSelectedGradientBegin[1], _config.MenuItemSelectedGradientBegin[2]));
        SetFctbColors();
    }

    public void SetMainFore()
    {
        if (_config.UseSpecialColoring)
        {
            MainFore = Color.FromArgb(_config.StripFore[0], _config.StripFore[1], _config.StripFore[2]);
        }
        else
        {
            MainFore = Color.Black;
        }
    }

    public void SetMainBack()
    {
        if (_config.UseSpecialColoring)
        {
            MainBack = Color.FromArgb(_config.StripBack[0], _config.StripBack[1], _config.StripBack[2]);
        }
        else
        {
            MainBack = Color.FromArgb(240, 240, 240);
        }
    }

    public ToolStripProfessionalRenderer GetRenderer()
    {
        if (_config.UseSpecialColoring)
        {
            return new MyRendererToolStrinRenderer(_config, this);
        }
        else
        {
            return new ToolStripProfessionalRenderer();
        }
    }

    private void SetFctbColors()
    {
        if (_config.UseSpecialColoring)
        {
            CurrentFctbColors.FctbSelectionColor = Color.FromArgb(_config.SelectionColorFastColored[0], _config.SelectionColorFastColored[1], _config.SelectionColorFastColored[2]);
            CurrentFctbColors.FctbDisabledColor = Color.FromArgb(_config.DisabledColorFastColored[3], _config.DisabledColorFastColored[0], _config.DisabledColorFastColored[1], _config.DisabledColorFastColored[2]);
            CurrentFctbColors.FctbBackColor = Color.FromArgb(_config.BackgroundFastColored[0], _config.BackgroundFastColored[1], _config.BackgroundFastColored[2]);
            CurrentFctbColors.FctbIndentBackColor = Color.FromArgb(_config.IndentBackColorFastColored[0], _config.IndentBackColorFastColored[1], _config.IndentBackColorFastColored[2]);
            CurrentFctbColors.FctbLineNumberColor = Color.FromArgb(_config.LineNumberColorFastColored[0], _config.LineNumberColorFastColored[1], _config.LineNumberColorFastColored[2]);
            CurrentFctbColors.FctbFoldingIndicatorColor = Color.FromArgb(_config.FoldingIndicatorColorFastColored[0], _config.FoldingIndicatorColorFastColored[1], _config.FoldingIndicatorColorFastColored[2]);
            CurrentFctbColors.FctbForeColor = Color.FromArgb(_config.ForeColorFastColored[0], _config.ForeColorFastColored[1], _config.ForeColorFastColored[2]);
            CurrentFctbColors.FctbPopupMenuSelected = Color.FromArgb(_config.LineNumberColorFastColored[0], _config.LineNumberColorFastColored[1], _config.LineNumberColorFastColored[2]);
        }
        else
        {
            CurrentFctbColors.FctbSelectionColor = Color.Blue;
            CurrentFctbColors.FctbDisabledColor = Color.LightGray;
            CurrentFctbColors.FctbBackColor = Color.White;
            CurrentFctbColors.FctbIndentBackColor = Color.LightGray;
            CurrentFctbColors.FctbLineNumberColor = Color.Black;
            CurrentFctbColors.FctbFoldingIndicatorColor = Color.Green;
            CurrentFctbColors.FctbForeColor = Color.Black;
            CurrentFctbColors.FctbPopupMenuSelected = Color.LightBlue;
        }
    }

    public void ColorForm(Control form, bool force = false)
    {
        if (_config.UseSpecialColoring || force)
        {

            form.BackColor = MainBack;
            form.ForeColor = MainFore;

            Stack<Control> stack = new Stack<Control>();
            foreach (Control item in form.Controls.OfType<Control>())
            {
                stack.Push(item);
            }

            while (stack.Count > 0)
            {
                var item = stack.Pop();
                item.ForeColor = form.ForeColor;
                item.BackColor = form.BackColor;

                if (item is DataGridView gridView)
                {
                    ColorDataGridView(gridView);
                    gridView.EnableHeadersVisualStyles = false;
                }

                if (item.Controls.Count > 0)
                {
                    foreach (Control item2 in item.Controls.OfType<Control>())
                    {
                        stack.Push(item2);
                    }
                }
            }
        }
    }

    public void ColorDataGridView(DataGridView dataGridViewNew, bool forceDoNotAlter = false)
    {
        dataGridViewNew.RowTemplate.Height = (int)Math.Ceiling(dataGridViewNew.Font.GetHeight()) + Math.Min(_config.GrifOffsetHeight, 4);
        dataGridViewNew.DefaultCellStyle.NullValue = "NULL";
        if (_config.UseSpecialColoring)
        {
            if (_config.AlternatingRows && !forceDoNotAlter)
            {
                dataGridViewNew.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(_config.DgvAlternatingRowsDefaultCellStyleBackColor[0], _config.DgvAlternatingRowsDefaultCellStyleBackColor[1], _config.DgvAlternatingRowsDefaultCellStyleBackColor[2]);
            }
            dataGridViewNew.DefaultCellStyle.ForeColor = Color.FromArgb(_config.DgvDefaultCellStyleForeColor[0], _config.DgvDefaultCellStyleForeColor[1], _config.DgvDefaultCellStyleForeColor[2]);
            dataGridViewNew.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(_config.DgvColumnHeadersDefaultCellStyleFore[0], _config.DgvColumnHeadersDefaultCellStyleFore[1], _config.DgvColumnHeadersDefaultCellStyleFore[2]);
            dataGridViewNew.RowHeadersDefaultCellStyle.BackColor = Color.FromArgb(_config.DgvRowHeadersDefaultCellStyleBack[0], _config.DgvRowHeadersDefaultCellStyleBack[1], _config.DgvRowHeadersDefaultCellStyleBack[2]);
            dataGridViewNew.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(_config.DgvColumnHeadersDefaultCellStyleBack[0], _config.DgvColumnHeadersDefaultCellStyleBack[1], _config.DgvColumnHeadersDefaultCellStyleBack[2]);
            dataGridViewNew.DefaultCellStyle.BackColor = Color.FromArgb(_config.DgvDefaultCellStyleBackColor[0], _config.DgvDefaultCellStyleBackColor[1], _config.DgvDefaultCellStyleBackColor[2]);
            dataGridViewNew.BackColor = Color.FromArgb(_config.DgvRowHeadersDefaultCellStyleBack[0], _config.DgvRowHeadersDefaultCellStyleBack[1], _config.DgvRowHeadersDefaultCellStyleBack[2]);
            dataGridViewNew.BackgroundColor = Color.FromArgb(_config.DgvRowHeadersDefaultCellStyleBack[0], _config.DgvRowHeadersDefaultCellStyleBack[1], _config.DgvRowHeadersDefaultCellStyleBack[2]);
            ApplyDataGridViewSelectionAndGrid(dataGridViewNew, isDark: true);
        }
        else
        {
            if (_config.AlternatingRows && !forceDoNotAlter)
            {
                dataGridViewNew.AlternatingRowsDefaultCellStyle.BackColor = Color.LightGray;
            }
            dataGridViewNew.DefaultCellStyle.ForeColor = Color.Black;
            dataGridViewNew.ColumnHeadersDefaultCellStyle.ForeColor = Color.Black;
            dataGridViewNew.RowHeadersDefaultCellStyle.BackColor = MainBack;
            dataGridViewNew.ColumnHeadersDefaultCellStyle.BackColor = MainBack;
            dataGridViewNew.DefaultCellStyle.BackColor = Color.White;
            dataGridViewNew.BackColor = Color.White;
            dataGridViewNew.BackgroundColor = Color.White;
            ApplyDataGridViewSelectionAndGrid(dataGridViewNew, isDark: false);
        }
    }

    private static void ApplyDataGridViewSelectionAndGrid(DataGridView dataGridViewNew, bool isDark)
    {
        if (isDark)
        {
            var selectionBack = Color.FromArgb(38, 79, 120);
            var selectionFore = Color.FromArgb(241, 241, 241);
            var gridColor = Color.FromArgb(60, 60, 60);
            dataGridViewNew.DefaultCellStyle.SelectionBackColor = selectionBack;
            dataGridViewNew.DefaultCellStyle.SelectionForeColor = selectionFore;
            dataGridViewNew.AlternatingRowsDefaultCellStyle.SelectionBackColor = selectionBack;
            dataGridViewNew.AlternatingRowsDefaultCellStyle.SelectionForeColor = selectionFore;
            dataGridViewNew.GridColor = gridColor;
        }
        else
        {
            dataGridViewNew.DefaultCellStyle.SelectionBackColor = SystemColors.Highlight;
            dataGridViewNew.DefaultCellStyle.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewNew.AlternatingRowsDefaultCellStyle.SelectionBackColor = SystemColors.Highlight;
            dataGridViewNew.AlternatingRowsDefaultCellStyle.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewNew.GridColor = SystemColors.ControlDark;
        }
    }

    public void ColorMyDataGridView(ICustomDataGridView myDataGrid)
    {
        ColorDataGridView(myDataGrid.InnerDataGridView);

        int r = 195;
        int g = 185;
        int b = 215;

        if (_config.UseSpecialColoring)
        {
            r = _config.GroupingRowColorBack[0];
            g = _config.GroupingRowColorBack[1];
            b = _config.GroupingRowColorBack[2];
        }

        if (r > 215)
            r = 215;
        if (g > 215)
            g = 215;
        if (b > 215)
            b = 215;

        if (r < 40)
            r = 40;
        if (g < 40)
            g = 40;
        if (b < 40)
            b = 40;

        myDataGrid.GroupBackgroundActiveStart = Color.FromArgb(r + 40, g + 40, b + 40);
        myDataGrid.GroupBackgroundActiveMiddle = Color.FromArgb(r + 20, g + 20, b + 20);
        myDataGrid.GroupBackgroundActiveEnd = Color.FromArgb(r, g, b);

        myDataGrid.GroupBackgroundStart = Color.FromArgb(r, g, b);
        myDataGrid.GroupBackgroundMiddle = Color.FromArgb(r - 20, g - 20, b - 20);
        myDataGrid.GroupBackgroundEnd = Color.FromArgb(r - 40, g - 40, b - 40);


        myDataGrid.FinishColorize(this, _config.UseSpecialColoring);
    }

    public void SetStylesForFastColoring()
    {
        if (_config.UseSpecialColoring)
        {
            CurrentFctbColors.KeyWordsStyle1 = new TextStyle(new SolidBrush(Color.FromArgb(_config.FontkeyWordsStyle1[0], _config.FontkeyWordsStyle1[1], _config.FontkeyWordsStyle1[2])), null, FontStyle.Regular);
            CurrentFctbColors.KeyWordsStyle2 = new TextStyle(new SolidBrush(Color.FromArgb(_config.FontkeyWordsStyle2[0], _config.FontkeyWordsStyle2[1], _config.FontkeyWordsStyle2[2])), null, FontStyle.Regular);
            CurrentFctbColors.ParamStyle = new TextStyle(new SolidBrush(Color.FromArgb(_config.FontparamStyle[0], _config.FontparamStyle[1], _config.FontparamStyle[2])), null, FontStyle.Regular);
            CurrentFctbColors.MyCommandsStyle = new TextStyle(new SolidBrush(Color.FromArgb(_config.FontmyCommandsStyle[0], _config.FontmyCommandsStyle[1], _config.FontmyCommandsStyle[2])), Brushes.LightGray, FontStyle.Regular);
            CurrentFctbColors.BoldUnderlineStyle = new TextStyle(null, null, FontStyle.Bold | FontStyle.Underline);
            CurrentFctbColors.NumberStyle = new TextStyle(new SolidBrush(Color.FromArgb(_config.FontnumberStyle[0], _config.FontnumberStyle[1], _config.FontnumberStyle[2])), null, FontStyle.Regular);
            CurrentFctbColors.CommentsStyle = new TextStyle(new SolidBrush(Color.FromArgb(_config.FontcommentsStyle[0], _config.FontcommentsStyle[1], _config.FontcommentsStyle[2])), null, FontStyle.Italic);
            CurrentFctbColors.StringsStyle = new TextStyle(new SolidBrush(Color.FromArgb(_config.FontstringsStyle[0], _config.FontstringsStyle[1], _config.FontstringsStyle[2])), null, FontStyle.Italic);
            CurrentFctbColors.SameWordsStyle = new MarkerStyle(new SolidBrush(Color.FromArgb(_config.FontsameWordsStyle[3], _config.FontsameWordsStyle[0], _config.FontsameWordsStyle[1], _config.FontsameWordsStyle[2])));
            CurrentFctbColors.QuotedTextStyle = new TextStyle(GeneralBrush, null, FontStyle.Italic);

            if (IsDark(GridViewDefaultCellStyleBackColor))
            {
                MyColors.LogErrorStdColor = Color.DarkRed;
                CurrentFctbColors.ErrorStyle = new TextStyle(
                    new SolidBrush(Color.FromArgb(255, 140, 140)),
                    new SolidBrush(Color.FromArgb(80, 30, 30)),
                    FontStyle.Regular);
                CurrentFctbColors.WarningStyle = new TextStyle(
                    new SolidBrush(Color.FromArgb(255, 210, 100)),
                    new SolidBrush(Color.FromArgb(70, 55, 15)),
                    FontStyle.Regular);
                CurrentFctbColors.LintInfoStyle = new TextStyle(
                    new SolidBrush(Color.FromArgb(150, 200, 255)),
                    new SolidBrush(Color.FromArgb(25, 40, 55)),
                    FontStyle.Regular);
                CurrentFctbColors.TableStyle = new TextStyle(new SolidBrush(Color.FromArgb(100, 180, 255)), null, FontStyle.Regular);
                CurrentFctbColors.ColumnStyle = new TextStyle(new SolidBrush(Color.FromArgb(80, 220, 80)), null, FontStyle.Regular);
                CurrentFctbColors.CteStyle = new TextStyle(new SolidBrush(Color.FromArgb(200, 130, 255)), null, FontStyle.Regular);
                CurrentFctbColors.AliasStyle = new TextStyle(null, null, FontStyle.Italic);
            }
            else
            {
                MyColors.LogErrorStdColor = Color.Pink;
                CurrentFctbColors.ErrorStyle = new TextStyle(new SolidBrush(Color.White), Brushes.Red, FontStyle.Regular);
                CurrentFctbColors.WarningStyle = new TextStyle(new SolidBrush(Color.Black), Brushes.Gold, FontStyle.Regular);
                CurrentFctbColors.LintInfoStyle = new TextStyle(new SolidBrush(Color.DimGray), Brushes.LightSteelBlue, FontStyle.Regular);
                CurrentFctbColors.TableStyle = new TextStyle(new SolidBrush(Color.FromArgb(0, 100, 200)), null, FontStyle.Regular);
                CurrentFctbColors.ColumnStyle = new TextStyle(new SolidBrush(Color.FromArgb(0, 128, 0)), null, FontStyle.Regular);
                CurrentFctbColors.CteStyle = new TextStyle(new SolidBrush(Color.FromArgb(160, 32, 240)), null, FontStyle.Regular);
                CurrentFctbColors.AliasStyle = new TextStyle(null, null, FontStyle.Italic);
            }

        }
        else
        {
            CurrentFctbColors.KeyWordsStyle1 = new TextStyle(new SolidBrush(Color.FromArgb(0, 0, 255)), null, FontStyle.Regular);
            CurrentFctbColors.KeyWordsStyle2 = new TextStyle(new SolidBrush(Color.FromArgb(250, 0, 250)), null, FontStyle.Regular);
            CurrentFctbColors.ParamStyle = new TextStyle(new SolidBrush(Color.FromArgb(0, 128, 0)), null, FontStyle.Regular);
            CurrentFctbColors.MyCommandsStyle = new TextStyle(new SolidBrush(Color.FromArgb(_config.FontmyCommandsStyle[0], _config.FontmyCommandsStyle[1], _config.FontmyCommandsStyle[2])), Brushes.LightGray, FontStyle.Regular);
            CurrentFctbColors.BoldUnderlineStyle = new TextStyle(null, null, FontStyle.Bold | FontStyle.Underline);
            CurrentFctbColors.NumberStyle = new TextStyle(new SolidBrush(Color.FromArgb(128, 0, 0)), null, FontStyle.Regular);
            CurrentFctbColors.CommentsStyle = new TextStyle(new SolidBrush(Color.FromArgb(0, 128, 128)), null, FontStyle.Regular);
            CurrentFctbColors.StringsStyle = new TextStyle(new SolidBrush(Color.FromArgb(255, 0, 0)), null, FontStyle.Regular);
            CurrentFctbColors.SameWordsStyle = new MarkerStyle(new SolidBrush(Color.FromArgb(_config.FontsameWordsStyle[3], _config.FontsameWordsStyle[0], _config.FontsameWordsStyle[1], _config.FontsameWordsStyle[2])));
            CurrentFctbColors.QuotedTextStyle = new TextStyle(GeneralBrush, null, FontStyle.Italic);
            CurrentFctbColors.TableStyle = new TextStyle(new SolidBrush(Color.FromArgb(0, 100, 200)), null, FontStyle.Regular);
            CurrentFctbColors.ColumnStyle = new TextStyle(new SolidBrush(Color.FromArgb(0, 128, 0)), null, FontStyle.Regular);
            CurrentFctbColors.CteStyle = new TextStyle(new SolidBrush(Color.FromArgb(160, 32, 240)), null, FontStyle.Regular);
            CurrentFctbColors.AliasStyle = new TextStyle(null, null, FontStyle.Italic);
            MyColors.LogErrorStdColor = Color.Pink;
            CurrentFctbColors.ErrorStyle = new TextStyle(new SolidBrush(Color.White), Brushes.Red, FontStyle.Regular);
            CurrentFctbColors.WarningStyle = new TextStyle(new SolidBrush(Color.Black), Brushes.Gold, FontStyle.Regular);
            CurrentFctbColors.LintInfoStyle = new TextStyle(new SolidBrush(Color.DimGray), Brushes.LightSteelBlue, FontStyle.Regular);
        }
    }

    /// <summary>
    /// Determines if a color is considered dark based on its luminance.
    /// </summary>
    /// <param name="color">The color to evaluate</param>
    /// <returns>True if the color is dark, false otherwise</returns>
    public bool IsDark(Color color)
    {
        var luma = 0.2126 * color.R + 0.7152 * color.G + 0.0722 * color.B;
        return luma < 255 / 3;
    }

    /// <summary>
    /// Disposes of managed resources, particularly brushes that were created.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            // Dispose of brushes that we created (not system brushes)
            if (GeneralBrush != null
                && GeneralBrush != SystemBrushes.Control
                && GeneralBrush != SystemBrushes.ControlText)
            {
                GeneralBrush.Dispose();
            }

            if (NonSelectedTabBrush != null && NonSelectedTabBrush != SystemBrushes.Control)
            {
                NonSelectedTabBrush.Dispose();
            }

            if (SelectedTabBrush != null && SelectedTabBrush != Brushes.LightGray)
            {
                SelectedTabBrush.Dispose();
            }

            if (TitleBrush != null && TitleBrush != Brushes.Black)
            {
                TitleBrush.Dispose();
            }

            // Note: TitleBrushBackground uses system brushes, so we don't dispose it

            _disposed = true;
        }
    }
}
