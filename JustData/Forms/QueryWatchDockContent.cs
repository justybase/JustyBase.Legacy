using AppBase.Common;
using JustData.ViewModels.QueryWatch;
using WeifenLuo.WinFormsUI.Docking;

namespace JustyBaseLegacy.UI.Forms;

internal sealed class QueryWatchDockContent : DockContent
{
    private readonly QueryWatch _queryWatchForm;

    public QueryWatchDockContent(
        QueryWatchViewModel viewModel,
        Action<Form> doColorize,
        Action<DataGridView> doubleBuff,
        ILogger logger)
    {
        Text = "Query Watch";
        TabText = "Query Watch";
        Name = "queryWatchDocument";
        CloseButton = true;
        CloseButtonVisible = true;
        HideOnClose = false;
        DockAreas = DockAreas.Document;

        _queryWatchForm = new QueryWatch(viewModel, doColorize, doubleBuff, logger);
        _queryWatchForm.PrepareForDocumentHost();
        _queryWatchForm.FormClosed += QueryWatchForm_FormClosed;
        Controls.Add(_queryWatchForm);
    }

    public Task RefreshNowAsync() => _queryWatchForm.RefreshNowAsync();

    protected override string GetPersistString()
    {
        return "unsaved://Query Watch";
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        if (!_queryWatchForm.Visible)
        {
            _queryWatchForm.Show();
        }
    }

    private void QueryWatchForm_FormClosed(object? sender, FormClosedEventArgs e)
    {
        if (!IsDisposed && !Disposing)
        {
            Close();
        }
    }
}
