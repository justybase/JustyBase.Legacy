namespace AppBase.Common;

/// <summary>Opens an object in the schema explorer from a presentation hint.</summary>
public interface IObjectExplorerNavigationHost
{
    void ExpandBaseToTable(string database, string table, string tableOrView, string connectionName);
}
