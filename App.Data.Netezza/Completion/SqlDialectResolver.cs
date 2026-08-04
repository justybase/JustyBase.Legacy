using AppBase.Data.Core.Interfaces;
using JustyBase.NetezzaSqlParser.Dialects;

namespace AppBase.Data.Completion;

/// <summary>
/// Resolves the SQL authoring dialect from the active legacy connection.
/// The application contract continues to carry only a connection name.
/// </summary>
public sealed class SqlDialectResolver
{
    private readonly IGeneralDbService? _generalDbService;

    public SqlDialectResolver(IGeneralDbService? generalDbService = null)
    {
        _generalDbService = generalDbService;
    }

    public SqlDialect Resolve(string? connectionName)
    {
        if (_generalDbService is null || string.IsNullOrWhiteSpace(connectionName))
            return SqlDialect.Netezza;

        return string.Equals(
            _generalDbService.DriverName(connectionName),
            "DB2",
            StringComparison.OrdinalIgnoreCase)
            ? SqlDialect.Db2
            : SqlDialect.Netezza;
    }
}
