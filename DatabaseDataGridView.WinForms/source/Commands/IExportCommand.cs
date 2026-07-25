using System.Data;

namespace DatabaseDataGridView.WinForms.Commands;

public interface IExportCommand
{
    Task ExecuteAsync();
}