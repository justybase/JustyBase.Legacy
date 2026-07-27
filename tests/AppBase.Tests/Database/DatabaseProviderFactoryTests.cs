using AppBase.Common;
using AppBase.Common.Interfaces;
using AppBase.Data.Core.Core;
using AppBase.Data.Core.Enums;
using AppBase.Data.Core.Interfaces;
using AppBase.Services;
using System.Drawing;
using System.Reflection;

namespace AppBase.Tests.Database;

public sealed class DatabaseProviderFactoryTests
{
    [Fact]
    public void UnknownDriverReturnsProblemResultWithoutCreatingAProvider()
    {
        IGeneralDbService databaseService = CreateProxy<IGeneralDbService>();
        DatabaseProviderFactory factory = new(CreateProxy<INetezzaHelperService>());

        DatabaseProviderFactoryResult result = factory.Create(
            null!,
            null!,
            null!,
            databaseService,
            "missing",
            Color.Empty);

        Assert.Null(result.Database);
        Assert.Equal("problem", result.DatabaseName);
        Assert.Null(result.DatabaseType);
    }

    [Fact]
    public void GeneralDbServiceDelegatesProviderCreationAndUpdatesDatabaseType()
    {
        IGeneralDb expectedDatabase = CreateProxy<IGeneralDb>();
        RecordingProviderFactory providerFactory = new(expectedDatabase);
        GeneralDbService databaseService = new(CreateProxy<ILogger>(), EmptyCredentialLookup.Instance, providerFactory);

        IGeneralDb actualDatabase = databaseService.GetGeneralDb(
            null!,
            CreateProxy<ILogger>(),
            CreateProxy<IImportExportTasks>(),
            "reporting",
            out string databaseName);

        Assert.Same(expectedDatabase, actualDatabase);
        Assert.Equal("test", databaseName);
        Assert.Equal(DatabaseTypeEnum.Postgres, databaseService.RelatedDatabaseType);
        Assert.Equal("reporting", providerFactory.ConnectionName);
    }

    private sealed class EmptyCredentialLookup : IConnectionCredentialLookup
    {
        public static EmptyCredentialLookup Instance { get; } = new();

        public bool TryGet(string connectionName, out ConnectionCredential credential)
        {
            credential = null!;
            return false;
        }
    }

    private static T CreateProxy<T>() where T : class
    {
        return DispatchProxy.Create<T, NullDispatchProxy>();
    }

    private sealed class RecordingProviderFactory : IDatabaseProviderFactory
    {
        private readonly IGeneralDb _database;

        public RecordingProviderFactory(IGeneralDb database)
        {
            _database = database;
        }

        public string ConnectionName { get; private set; } = string.Empty;

        public DatabaseProviderFactoryResult Create(
            IDatabaseRuntimeContext databaseRuntimeContext,
            ILogger logger,
            IImportExportTasks importExportTasks,
            IGeneralDbService databaseService,
            string connectionName,
            Color logErrorStdColor)
        {
            ConnectionName = connectionName;
            return new DatabaseProviderFactoryResult(_database, "test", DatabaseTypeEnum.Postgres);
        }
    }

    private class NullDispatchProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod is null || targetMethod.ReturnType == typeof(void))
            {
                return null;
            }

            return targetMethod.ReturnType.IsValueType
                ? Activator.CreateInstance(targetMethod.ReturnType)
                : null;
        }
    }
}
