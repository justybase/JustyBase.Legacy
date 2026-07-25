using System.Data;

namespace DatabaseDataGridView.WinForms.Commands;

public interface IFilterCommand
{
    Task ExecuteAsync();
}