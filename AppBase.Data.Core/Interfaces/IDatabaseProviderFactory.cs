using AppBase.Common;
using AppBase.Common.Interfaces;
using AppBase.Data.Core.Enums;
using AppBase.Data.Core.Core;
using System.Drawing;

namespace AppBase.Data.Core.Interfaces;

public sealed record DatabaseProviderFactoryResult(
    IGeneralDb Database,
    string DatabaseName,
    DatabaseTypeEnum? DatabaseType);

public interface IDatabaseProviderFactory
{
    DatabaseProviderFactoryResult Create(
        IDatabaseRuntimeContext databaseRuntimeContext,
        ILogger logger,
        IImportExportTasks importExportTasks,
        IGeneralDbService databaseService,
        string connectionName,
        Color logErrorStdColor);
}
