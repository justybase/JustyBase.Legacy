using System.Data;

namespace DatabaseDataGridView.WinForms.Commands;

public class CopyAsTextCommand : IExportCommand
{
    private readonly DataGridView _dataGridView;

    public CopyAsTextCommand(DataGridView dataGridView)
    {
        _dataGridView = dataGridView;
    }

    public async Task ExecuteAsync()
    {
        await Task.Run(() =>
        {
            _dataGridView.Invoke(() =>
            {
                _dataGridView.SelectAll();
                var prevCopyMode = _dataGridView.ClipboardCopyMode;
                _dataGridView.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;
                _dataGridView.RowHeadersVisible = false;
                DataObject? dataObj = _dataGridView.GetClipboardContent();
                if (dataObj is not null)
                {
                    Clipboard.SetDataObject(dataObj);
                }
                _dataGridView.ClipboardCopyMode = prevCopyMode;
                _dataGridView.RowHeadersVisible = true;
            });
        });
    }
}
