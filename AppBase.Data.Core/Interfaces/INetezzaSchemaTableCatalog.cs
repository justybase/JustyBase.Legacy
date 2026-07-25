using AppBase.Data.Core.Models;

namespace AppBase.Data.Core.Interfaces;

/// <summary>
/// Injected catalog of Netezza table metadata keyed by connection name.
/// Prefer this over process-wide static catalogs so tests can
/// isolate schema state without mutating shared globals.
/// </summary>
public interface INetezzaSchemaTableCatalog
{
    Dictionary<string, Dictionary<int, NetezzaTableInfo>> TablesByConnection { get; }
}
