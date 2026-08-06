using AppBase.Common;
using JustyBase.ImportExport.Import;
using System.Text;

namespace JustyBaseLegacy.UI;

public partial class ImportProgressForm : Form, IImportProgressForm
{
    private static readonly Color AccentColor = Color.FromArgb(0, 120, 215);
    private bool _isApplyingLayout;

    public ImportProgressForm(Action<Form> DoColorize, Action<DataGridView> DoubleBuff)
    {
        InitializeComponent();
        DoColorize(this);
        DoubleBuff(dgvMain);
        ApplyModernAppearance();
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        ApplyDpiLayout();
    }

    protected override void OnDpiChangedAfterParent(EventArgs e)
    {
        base.OnDpiChangedAfterParent(e);
        ApplyDpiLayout();
    }

    protected override void OnClientSizeChanged(EventArgs e)
    {
        base.OnClientSizeChanged(e);
        if (IsHandleCreated)
        {
            ApplyDpiLayout();
        }
    }

    private static int ScaleDpi(int logicalPixels, int dpi) =>
        (int)Math.Round(logicalPixels * dpi / 96f);

    private void ApplyModernAppearance()
    {
        Color gridBack = dgvMain.BackColor.IsEmpty ? SystemColors.Window : dgvMain.BackColor;
        Color gridFore = dgvMain.DefaultCellStyle.ForeColor.IsEmpty
            ? SystemColors.WindowText
            : dgvMain.DefaultCellStyle.ForeColor;
        bool dark = IsDarkColor(gridBack);
        Color surface = dark ? gridBack : Color.FromArgb(246, 248, 251);
        Color border = dark ? ControlPaint.Light(gridBack, 0.28f) : Color.FromArgb(203, 213, 225);

        BackColor = surface;
        ForeColor = gridFore;

        dgvMain.BackgroundColor = gridBack;
        dgvMain.BorderStyle = BorderStyle.FixedSingle;
        dgvMain.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        dgvMain.GridColor = border;
        dgvMain.EnableHeadersVisualStyles = false;
        dgvMain.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvMain.DefaultCellStyle.BackColor = gridBack;
        dgvMain.DefaultCellStyle.ForeColor = gridFore;
        dgvMain.DefaultCellStyle.SelectionBackColor = AccentColor;
        dgvMain.DefaultCellStyle.SelectionForeColor = Color.White;
        dgvMain.DefaultCellStyle.Padding = new Padding(4, 0, 4, 0);
        dgvMain.Columns[0].DefaultCellStyle.Format = "HH:mm:ss";
        dgvMain.Columns[1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

        fctb.BorderStyle = BorderStyle.FixedSingle;

        ApplyButtonTheme(btSelect, surface, gridFore, border, primary: true);
        ApplyButtonTheme(btRename, surface, gridFore, border);
        ApplyButtonTheme(btRecreate, surface, gridFore, border);
    }

    private void ApplyButtonTheme(Button button, Color surface, Color fore, Color border, bool primary = false)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = primary ? AccentColor : border;
        button.FlatAppearance.MouseOverBackColor = primary
            ? Color.FromArgb(0, 102, 184)
            : ControlPaint.Light(surface, 0.08f);
        button.FlatAppearance.MouseDownBackColor = primary
            ? Color.FromArgb(0, 86, 156)
            : ControlPaint.Light(surface, 0.14f);
        button.BackColor = primary ? AccentColor : surface;
        button.ForeColor = primary ? Color.White : fore;
        button.UseVisualStyleBackColor = false;
        button.TextAlign = ContentAlignment.MiddleCenter;
        button.Cursor = Cursors.Hand;
    }

    private void ApplyDpiLayout()
    {
        if (_isApplyingLayout || !IsHandleCreated)
        {
            return;
        }

        _isApplyingLayout = true;
        try
        {
            int dpi = DeviceDpi;
            int margin = ScaleDpi(12, dpi);
            int gap = ScaleDpi(10, dpi);
            int contentWidth = Math.Max(ScaleDpi(320, dpi), ClientSize.Width - 2 * margin);
            int logHeight = Math.Min(
                ScaleDpi(260, dpi),
                Math.Max(ScaleDpi(180, dpi), (int)(ClientSize.Height * 0.38f)));
            int progressHeight = ScaleDpi(18, dpi);
            int buttonHeight = ScaleDpi(36, dpi);
            int logTop = margin;
            int progressTop = logTop + logHeight + gap;
            int buttonTop = ClientSize.Height - margin - buttonHeight;
            int editorTop = progressTop + progressHeight + gap;
            int editorHeight = Math.Max(ScaleDpi(120, dpi), buttonTop - gap - editorTop);

            dgvMain.SetBounds(margin, logTop, contentWidth, logHeight);
            progressBar1.SetBounds(margin, progressTop, contentWidth, progressHeight);
            fctb.SetBounds(margin, editorTop, contentWidth, editorHeight);

            int buttonGap = ScaleDpi(8, dpi);
            int buttonWidth = Math.Max(ScaleDpi(100, dpi),
                (contentWidth - 2 * buttonGap) / 3);
            btSelect.SetBounds(margin, buttonTop, buttonWidth, buttonHeight);
            btRename.SetBounds(margin + buttonWidth + buttonGap, buttonTop, buttonWidth, buttonHeight);
            btRecreate.SetBounds(margin + 2 * (buttonWidth + buttonGap), buttonTop, buttonWidth, buttonHeight);

            dgvMain.RowTemplate.Height = Math.Max(
                ScaleDpi(24, dpi),
                (int)Math.Ceiling(dgvMain.Font.GetHeight(dpi)) + ScaleDpi(6, dpi));
            dgvMain.Columns[0].Width = ScaleDpi(88, dpi);
            dgvMain.Columns[1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        }
        finally
        {
            _isApplyingLayout = false;
        }
    }

    private static bool IsDarkColor(Color color)
    {
        double luminance = 0.2126 * color.R + 0.7152 * color.G + 0.0722 * color.B;
        return luminance < 96;
    }

    public int AddRow(string text, int style = -1)
    {
        int num = -1;
        this.Invoke(() =>
        {
            if (style != -1)
            {
                this.progressBar1.Style = (ProgressBarStyle)style;
            }

            num = this.dgvMain.Rows.Add(DateTime.Now, text);
        });
        return num;
    }

    public void SetProgressBarValue(int value, int style = -1)
    {
        this.Invoke(() =>
        {
            if (style != -1)
            {
                this.progressBar1.Style = (ProgressBarStyle)style;
            }
            this.progressBar1.Value = value;
        });
    }

    public void SetColor(int rowNum, Color color)
    {
        dgvMain.Invoke(() => dgvMain.Rows[rowNum].DefaultCellStyle.BackColor = color);
    }

    public void SetFirstDisplayedScrollingRowIndex(int rowNum)
    {
        dgvMain.Invoke(() =>
        {
            if (dgvMain.FirstDisplayedScrollingRowIndex != -1)
            {
                dgvMain.FirstDisplayedScrollingRowIndex = rowNum;
            }
        });
    }

    public void CompleteForNetezza(string randName, string configDirecotry, string[] headers, bool importToExisting, string? qualifiedTableName = null)
    {
        this.Invoke(new Action(() =>
        {
            if (headers == null)
            {
                this.dgvMain.Rows.Add(DateTime.Now, @$"Problem !");
                return;
            }
            this.progressBar1.Style = ProgressBarStyle.Continuous;
            this.progressBar1.Value = 100;
            this.dgvMain.Rows.Add(DateTime.Now, @$"Completed !");
            if (configDirecotry != null)
            {
                var c1 = Directory.GetFiles($"{configDirecotry}\\data\\", $"{randName}*.nzbad");
                if (c1.Length == 1)
                {
                    int r = this.dgvMain.Rows.Add(DateTime.Now, $"errors detected, please check nzlog/nzbad in {configDirecotry}\\data");
                    this.dgvMain.Rows[r].DefaultCellStyle.BackColor = Color.Red;
                    this.dgvMain.Rows[r].DefaultCellStyle.ForeColor = Color.White;
                    this.btSelect.BackColor = Color.Red;
                    this.btSelect.ForeColor = Color.White;
                }
            }

            string fromTable = string.IsNullOrWhiteSpace(qualifiedTableName) ? randName : qualifiedTableName;
            string selectSql = ImportSelectSqlBuilder.BuildAliasedColumnSelect(fromTable, headers);

            this.btSelect.Enabled = true;
            this.btRename.Enabled = true;
            this.btRecreate.Enabled = true;
            this.TextToClipboard += $"{selectSql}{Environment.NewLine}";
            this.RenameTextToClipboard += $"ALTER TABLE {fromTable} RENAME TO XXXXXXXXXXXX;{Environment.NewLine}";
            this.fctb.Text += $"{selectSql}{Environment.NewLine}";
            if (!importToExisting && configDirecotry != null)
            {
                this.fctb.Text += $"--you can recreate with some code mod{Environment.NewLine}";
                this.fctb.Text += $"{Environment.NewLine}CREATE TABLE YOUR_NEW_NAME ({Environment.NewLine}{String.Join($"{Environment.NewLine},", headers)}{Environment.NewLine}){Environment.NewLine}DISTRIBUTE ON RANDOM;{Environment.NewLine}{Environment.NewLine}";
                this.fctb.Text += $"{Environment.NewLine}INSERT INTO YOUR_NEW_NAME SELECT * FROM {fromTable};";
                this.fctb.Text += $"{Environment.NewLine}DROP TABLE {fromTable};";
            }
        }));
    }

    public string TextToClipboard { get; set; }
    public string RenameTextToClipboard { get; set; }

    private void button_Click(object sender, EventArgs e)
    {
        if (sender == btSelect && !String.IsNullOrEmpty(TextToClipboard))
        {
            Clipboard.SetText(TextToClipboard);
        }
        else if (sender == btRename && !String.IsNullOrEmpty(RenameTextToClipboard))
        {
            Clipboard.SetText(RenameTextToClipboard);
        }
        else if (sender == btRecreate && !String.IsNullOrEmpty(this.fctb.Text))
        {
            Clipboard.SetText(this.fctb.Text);
        }

        this.Close();
    }



    public void CompleteForGeneral(string randName, bool top = false)
    {
        this.Invoke(() =>
        {
            this.progressBar1.Style = ProgressBarStyle.Continuous;
            this.progressBar1.Value = 100;
            this.dgvMain.Rows.Add(DateTime.Now, @$"Completed !");
            this.btSelect.Enabled = true;
            this.btRename.Enabled = true;
            this.btRecreate.Enabled = true;
            if (top)
            {
                this.TextToClipboard += $"SELECT TOP 50 * FROM {randName};{Environment.NewLine}";
            }
            else
            {
                this.TextToClipboard += $"SELECT  * FROM {randName} FETCH FIRST 50 ROWS ONLY;{Environment.NewLine}";
            }

            this.RenameTextToClipboard += $"-- TO DO";
            this.fctb.Text += $"SELECT * FROM {randName};{Environment.NewLine}";
            this.fctb.Text += $"--you can recreate with some code mod{Environment.NewLine}";
            this.fctb.Text += $"-- TO DO";
            this.fctb.Text += $"-- TO DO";
            this.fctb.Text += $"{Environment.NewLine}DROP TABLE {randName};";
        });
    }

    public void CompleteForGeneral(List<string> randNames, bool top = false)
    {
        this.Invoke(() =>
        {
            this.progressBar1.Style = ProgressBarStyle.Continuous;
            this.progressBar1.Value = 100;
            this.dgvMain.Rows.Add(DateTime.Now, @$"Completed !");
            this.btSelect.Enabled = true;
            this.btRename.Enabled = true;
            this.btRecreate.Enabled = true;

            foreach (var randName in randNames)
            {
                if (top)
                {
                    this.TextToClipboard += $"SELECT TOP 50 * FROM {randName};{Environment.NewLine}";
                }
                else
                {
                    this.TextToClipboard += $"SELECT  * FROM {randName} FETCH FIRST 50 ROWS ONLY;{Environment.NewLine}";
                }
            }
        });
    }
}



