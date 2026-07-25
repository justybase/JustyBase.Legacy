using AppBase.Common;
using JustyBaseLegacy.UI.Controls;

namespace JustyBaseLegacy.UI.ObjectExplorer;

/// <summary>UI adapter used by editor hints to select objects in the MVVM explorer.</summary>
public sealed class ObjectExplorerNavigationController : IObjectExplorerNavigationHost
{
    private readonly Func<MvvmDatabaseExplorerControl?> _explorer;

    public ObjectExplorerNavigationController(Func<MvvmDatabaseExplorerControl?> explorer)
    {
        _explorer = explorer ?? throw new ArgumentNullException(nameof(explorer));
    }

    public void ExpandBaseToTable(string database, string table, string tableOrView, string connectionName)
    {
        _ = _explorer()?.SelectObjectAsync(
            connectionName,
            database,
            tableOrView,
            table,
            cancellationToken: CancellationToken.None);
    }
}
