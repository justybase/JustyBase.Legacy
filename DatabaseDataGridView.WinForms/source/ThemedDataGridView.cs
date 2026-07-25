namespace DatabaseDataGridView.WinForms;

/// <summary>
/// DataGridView with .NET 10 implicit dark-mode theming for native scrollbars (Windows 11).
/// </summary>
public class ThemedDataGridView : DataGridView
{
    public event EventHandler? NewSqlTabRequested;
    private SqlFirstRenderProbeRun? _firstRenderProbeRun;
    private int _firstRenderExpectedColumnCount;

    internal void ConfigureFirstRenderProbe(SqlFirstRenderProbeRun? run, int expectedColumnCount)
    {
        _firstRenderProbeRun = run;
        _firstRenderExpectedColumnCount = expectedColumnCount;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        if (_firstRenderProbeRun is not null
            && Visible
            && RowCount > 0
            && Columns.Count == _firstRenderExpectedColumnCount)
        {
            _firstRenderProbeRun.ReportFirstPaint(Columns.Count);
            _firstRenderProbeRun = null;
        }
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        Keys key = keyData & Keys.KeyCode;
        if ((keyData & Keys.Control) != 0 && key is Keys.N or Keys.T)
        {
            NewSqlTabRequested?.Invoke(this, EventArgs.Empty);
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }
    protected override CreateParams CreateParams
    {
        get
        {
            SetStyle(ControlStyles.ApplyThemingImplicitly, true);
            return base.CreateParams;
        }
    }

    public void RecreateForThemeChange()
    {
        if (IsHandleCreated)
        {
            RecreateHandle();
        }
    }
}
