using System.Data.Common;
using System.Drawing;
using AppBase.Common;
using AppBase.Common.Interfaces;
using AppBase.Data.Core;
using AppBase.Data.Core.Enums;
using AppBase.Data.Core.Interfaces;
using NSubstitute;

namespace AppBase.Tests.Database;

public sealed class GeneralDbSqlAddCodeTests
{
    private readonly TestGeneralDb _sut = TestGeneralDb.Create();

    [Theory]
    [InlineData("column", "ALTER TABLE admin.employees ADD COLUMN <COLUMN_NAME> INT NOT NULL DEFAULT 0")]
    [InlineData("constraint", "getConstraint code")]
    [InlineData("index", "getIndex code")]
    [InlineData("partition", "getPartition code")]
    [InlineData("trigger", "getTrigger code")]
    [InlineData("unknown", "code for unknown")]
    public void GetSqlAddCode_routes_object_type_to_template(string objectType, string expected)
    {
        string sql = _sut.GetSqlAddCode(objectType, "db", "admin", "employees");

        Assert.Equal(expected, sql);
    }

    [Fact]
    public void GetSqlAddCode_uses_overridden_templates_from_derived_class()
    {
        var sut = TestGeneralDb.Create(overrideTemplates: true);

        Assert.Equal("COL:db.s.t", sut.GetSqlAddCode("column", "db", "s", "t"));
        Assert.Equal("IDX:db.s.t", sut.GetSqlAddCode("index", "db", "s", "t"));
    }

    private sealed class TestGeneralDb : GeneralDb
    {
        private readonly bool _overrideTemplates;

        public static TestGeneralDb Create(bool overrideTemplates = false)
        {
            return new TestGeneralDb(
                Substitute.For<IDatabaseRuntimeContext>(),
                Substitute.For<ILogger>(),
                Substitute.For<IImportExportTasks>(),
                Substitute.For<IGeneralDbService>(),
                overrideTemplates)
            {
                LogErrorStdColor = Color.Red
            };
        }

        private TestGeneralDb(
            IDatabaseRuntimeContext runtime,
            ILogger logger,
            IImportExportTasks importExport,
            IGeneralDbService service,
            bool overrideTemplates)
            : base(runtime, logger, importExport, service)
        {
            _overrideTemplates = overrideTemplates;
        }

        protected override string GetColumn(string db, string schema, string parentObject)
            => _overrideTemplates
                ? $"COL:{db}.{schema}.{parentObject}"
                : base.GetColumn(db, schema, parentObject);

        protected override string GetIndex(string db, string schema, string parentObject)
            => _overrideTemplates
                ? $"IDX:{db}.{schema}.{parentObject}"
                : base.GetIndex(db, schema, parentObject);

        protected override void AddToCache(string dbName, string schema, string tablename) { }

        public override void ResetDynamicCollection() { }

        public override DatabaseTypeEnum DatabaseType => DatabaseTypeEnum.Netezza;

        public override string SearchInViewsSource(string txtToSearch) => string.Empty;

        public override string SearchInProcedureSource(string txtToSearch) => string.Empty;

        public override DbConnection GetConnection(string databaseName, bool usePool = true)
            => throw new NotSupportedException();

        public override DbConnection GetConnection()
            => throw new NotSupportedException();
    }
}
