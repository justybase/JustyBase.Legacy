using AppBase.Common;
using AppBase.Common.Interfaces;
using AppBase.Data;
using AppBase.Data.Core.Core;
using AppBase.Data.Core.Enums;
using AppBase.Data.Core.Interfaces;
using System.Drawing;

#if INCLUDE_DB2
using App.Data.DB2;
#endif
#if INCLUDE_MSSQL
using AppBase.Data.MsSqlDb;
#endif
#if INCLUDE_ORACLE
using AppBase.Data.Oracle;
#endif
#if INCLUDE_POSTGRES
using AppBase.Data.Postgres;
#endif

namespace AppBase.Services;

public sealed class DatabaseProviderFactory : IDatabaseProviderFactory
{
    private readonly INetezzaHelperService? _netezzaHelperService;

    public DatabaseProviderFactory(INetezzaHelperService? netezzaHelperService = null)
    {
        _netezzaHelperService = netezzaHelperService;
    }

    public DatabaseProviderFactoryResult Create(
        IDatabaseRuntimeContext databaseRuntimeContext,
        ILogger logger,
        IImportExportTasks importExportTasks,
        IGeneralDbService databaseService,
        string connectionName,
        Color logErrorStdColor)
    {
        string driverName = databaseService.DriverName(connectionName);

        if (driverName == "NetezzaSQL")
        {
            INetezzaHelperService helper = _netezzaHelperService
                ?? throw new InvalidOperationException(
                    "INetezzaHelperService is required to create Netezza database providers.");

            IGeneralDb database = new Netezza(
                databaseRuntimeContext,
                logger,
                importExportTasks,
                databaseService,
                helper)
            {
                LogErrorStdColor = logErrorStdColor,
                ConnectionName = connectionName,
                ConnectionString = databaseService.ConnectionStringForNz(
                    databaseRuntimeContext.Config.ConnectionTimeout,
                    connectionName)
            };

            return new DatabaseProviderFactoryResult(database, "Netezza", DatabaseTypeEnum.Netezza);
        }
#if INCLUDE_DB2
        if (driverName == "DB2")
        {
            IGeneralDb database = new DB2(databaseRuntimeContext, logger, importExportTasks, databaseService)
            {
                LogErrorStdColor = logErrorStdColor,
                ConnectionName = connectionName,
                ConnectionString = databaseService.ConnectionStringForDB2(connectionName)
            };

            return new DatabaseProviderFactoryResult(database, "db2", DatabaseTypeEnum.DB2);
        }
#endif
#if INCLUDE_ORACLE
        if (driverName == "Oracle")
        {
            IGeneralDb database = new Oracle(databaseRuntimeContext, logger, importExportTasks, databaseService)
            {
                ConnectionString = databaseService.ConnectionStringForOracle(connectionName)
            };

            return new DatabaseProviderFactoryResult(database, "Oracle", DatabaseTypeEnum.Oracle);
        }
#endif
#if INCLUDE_POSTGRES
        if (driverName == "Postgres")
        {
            IGeneralDb database = new Postgres(databaseRuntimeContext, logger, importExportTasks, databaseService)
            {
                ConnectionString = databaseService.ConnectionStringForPostgreSQL(connectionName)
            };

            return new DatabaseProviderFactoryResult(database, "Postgres", DatabaseTypeEnum.Postgres);
        }
#endif
#if INCLUDE_MSSQL
        if (driverName == "MsSqlStd")
        {
            IGeneralDb database = new MsSqlDb(databaseRuntimeContext, logger, importExportTasks, databaseService)
            {
                ConnectionName = connectionName,
                ConnectionString = databaseService.ConnectionStringForMsSql(connectionName)
            };

            return new DatabaseProviderFactoryResult(database, "MsSql", DatabaseTypeEnum.MsSqlDb);
        }

        if (driverName == "MsSqlTrusted")
        {
            IGeneralDb database = new MsSqlDb(databaseRuntimeContext, logger, importExportTasks, databaseService)
            {
                ConnectionName = connectionName,
                ConnectionString = databaseService.ConnectionStringForMsSqlTrusted(connectionName)
            };

            return new DatabaseProviderFactoryResult(database, "MsSqlTrusted", DatabaseTypeEnum.MsSqlDb);
        }
#endif

        return new DatabaseProviderFactoryResult(null, "problem", null);
    }
}
