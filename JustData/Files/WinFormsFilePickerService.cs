using JustData.Application.Files;
using System.Windows.Forms;

namespace JustyBaseLegacy.UI.Files;

public sealed class WinFormsFilePickerService : IFilePickerService
{
    public string? PickFolder()
    {
        using var dialog = new FolderBrowserDialog();
        return dialog.ShowDialog() == DialogResult.OK ? dialog.SelectedPath : null;
    }
}
