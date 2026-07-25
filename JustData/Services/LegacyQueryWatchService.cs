using AppBase.Common;
using AppBase.Common.Interfaces;
using AppBase.Data.Core.Core;
using AppBase.Data.Core.Enums;
using AppBase.Data.Core.Interfaces;
using JustData.Application.QueryWatch;
using System.Data.Common;

namespace JustyBaseLegacy.UI.Services;

public sealed class LegacyQueryWatchService : IQueryWatchService
{
    private const string DropSessionColumn = "DROP_SESSION_SQL";

    private readonly IGeneralDbService _generalDbService;
    private readonly IDatabaseRuntimeContext _databaseRuntimeContext;
    private readonly ILogger _logger;
    private readonly IImportExportTasks _importExportTasks;

    public LegacyQueryWatchService(
        IGeneralDbService generalDbService,
        IDatabaseRuntimeContext databaseRuntimeContext,
        ILogger logger,
        IImportExportTasks importExportTasks)
    {
        _generalDbService = generalDbService ?? throw new ArgumentNullException(nameof(generalDbService));
        _databaseRuntimeContext = databaseRuntimeContext ?? throw new ArgumentNullException(nameof(databaseRuntimeContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _importExportTasks = importExportTasks ?? throw new ArgumentNullException(nameof(importExportTasks));
    }

    public bool IsSupported(int databaseType) =>
        databaseType is (int)DatabaseTypeEnum.DB2
            or (int)DatabaseTypeEnum.Postgres
            or (int)DatabaseTypeEnum.Netezza;

    public async Task<IReadOnlyList<QueryWatchRow>> RefreshAsync(
        QueryWatchContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        EnsureSupported(context);

        string sql = IGeneralDbService.ActiveQuerySql((DatabaseTypeEnum)context.DatabaseType);
        if (string.IsNullOrWhiteSpace(sql))
        {
            throw new InvalidOperationException("No active-query monitor SQL is defined for this database type.");
        }

        return await Task.Run(
            () => ExecuteRefresh(context, sql, cancellationToken),
            cancellationToken).ConfigureAwait(false);
    }

    public async Task DropSessionAsync(
        string dropSql,
        QueryWatchContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (string.IsNullOrWhiteSpace(dropSql))
        {
            throw new ArgumentException("Drop session SQL is required.", nameof(dropSql));
        }

        EnsureSupported(context);

        await Task.Run(
            () => ExecuteNonQuery(context, dropSql.Trim(), cancellationToken),
            cancellationToken).ConfigureAwait(false);
    }

    private IReadOnlyList<QueryWatchRow> ExecuteRefresh(
        QueryWatchContext context,
        string sql,
        CancellationToken cancellationToken)
    {
        using DbConnection connection = OpenConnection(context);
        using DbCommand command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = 30;

        using DbDataReader reader = command.ExecuteReader();
        cancellationToken.ThrowIfCancellationRequested();

        int fieldCount = reader.FieldCount;
        var columnNames = new string[fieldCount];
        int dropColumnIndex = -1;
        for (int i = 0; i < fieldCount; i++)
        {
            string name = reader.GetName(i);
            columnNames[i] = name;
            if (dropColumnIndex < 0
                && string.Equals(name, DropSessionColumn, StringComparison.OrdinalIgnoreCase))
            {
                dropColumnIndex = i;
            }
        }

        var rows = new List<QueryWatchRow>();
        while (reader.Read())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            string? dropSql = null;
            for (int i = 0; i < fieldCount; i++)
            {
                object? value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                if (i == dropColumnIndex)
                {
                    dropSql = value?.ToString();
                    continue;
                }

                values[columnNames[i]] = value;
            }

            rows.Add(new QueryWatchRow(values, dropSql));
        }

        return rows;
    }

    private void ExecuteNonQuery(
        QueryWatchContext context,
        string sql,
        CancellationToken cancellationToken)
    {
        using DbConnection connection = OpenConnection(context);
        using DbCommand command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = 30;
        cancellationToken.ThrowIfCancellationRequested();
        command.ExecuteNonQuery();
    }

    private DbConnection OpenConnection(QueryWatchContext context)
    {
        IGeneralDb? database = _generalDbService.GetGeneralDb(
            _databaseRuntimeContext,
            _logger,
            _importExportTasks,
            context.ConnectionName,
            out _);

        if (database is null)
        {
            throw new InvalidOperationException(
                $"Unable to resolve database connection '{context.ConnectionName}'.");
        }

        DbConnection connection = string.IsNullOrWhiteSpace(context.DatabaseName)
            ? database.GetConnection()
            : database.GetConnection(context.DatabaseName);

        connection.Open();
        return connection;
    }

    private void EnsureSupported(QueryWatchContext context)
    {
        if (!IsSupported(context.DatabaseType))
        {
            throw new NotSupportedException(
                "Query Watch is available for Netezza, PostgreSQL, and DB2 connections.");
        }
    }
}
