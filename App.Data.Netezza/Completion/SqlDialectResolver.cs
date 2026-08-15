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

        string driverName = _generalDbService.DriverName(connectionName);

        if (string.Equals(driverName, "DB2", StringComparison.OrdinalIgnoreCase))
            return SqlDialect.Db2;

        if (string.Equals(driverName, "MsSqlStd", StringComparison.OrdinalIgnoreCase)
            || string.Equals(driverName, "MsSqlTrusted", StringComparison.OrdinalIgnoreCase))
            return SqlDialect.Mssql;

        if (string.Equals(driverName, "Postgres", StringComparison.OrdinalIgnoreCase))
            return SqlDialect.PostgreSql;

        return SqlDialect.Netezza;
    }
}
