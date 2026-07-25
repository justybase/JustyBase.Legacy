using AppBase.Common;
using AppBase.Common.Interfaces;
using DatabaseDataGridView.WinForms;
using DatabaseDataGridView.WinForms.Coloring;
using JustData.Application.History;
using JustData.Application;
using JustData.ViewModels.History;
using WeifenLuo.WinFormsUI.Docking;

namespace JustyBaseLegacy.UI.Forms;

internal sealed class HistoryDockContent : DockContent
{
    private readonly History _historyForm;

    public HistoryDockContent(
        Action<Form> doColorize,
        Action<DataGridView> doubleBuff,
        Action<string, string, string> addTabAction,
        string historyDatFile,
        bool useSpecialColoring,
        IHistoryStore historyStore,
        IUiDispatcher uiDispatcher)
    {
        Text = "Query History";
        TabText = "Query History";
        Name = "historyDocument";
        CloseButton = true;
        CloseButtonVisible = true;
        HideOnClose = false;
        DockAreas = DockAreas.Document;

        var viewModel = new HistoryViewModel(historyStore, uiDispatcher);
        _historyForm = new History(
            viewModel,
            doColorize,
            doubleBuff,
            addTabAction,
            historyDatFile,
            useSpecialColoring);
        _historyForm.PrepareForDocumentHost();
        _historyForm.FormClosed += HistoryForm_FormClosed;
        Controls.Add(_historyForm);
    }

    protected override string GetPersistString()
    {
        return "unsaved://Query History";
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        if (!_historyForm.Visible)
        {
            _historyForm.Show();
        }
    }

    private void HistoryForm_FormClosed(object sender, FormClosedEventArgs e)
    {
        if (!IsDisposed && !Disposing)
        {
            Close();
        }
    }
}
