namespace AppBase.Common.Interfaces;

/// <summary>
/// Provides catalog-driven DDL text for the object hint actions.
/// </summary>
public interface INetezzaDdlCodeProvider
{
    Task<string> GetTableCodeByName(string database, string name, string? connectionName = null);
    Task<string> GetExternaTableCodeByName(string database, string name, string connectionName);
    Task<string> GetRecreateTableCodeByName(string database, string name, string? connectionName = null);
}
