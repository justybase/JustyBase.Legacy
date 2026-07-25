using AppBase.Data;

namespace AppBase.Tests.Sql;

public sealed class NetezzaHelpersSqlTests
{
    [Theory]
    [InlineData("SALES")]
    [InlineData("Mixed_Name")]
    public void CatalogSqlBuilders_IncludeRequestedDatabase(string database)
    {
        Assert.Contains(database, NetezzaHelpers.DatabaseTablesSql(database), StringComparison.OrdinalIgnoreCase);
        Assert.Contains(database, NetezzaHelpers.GetDescSql(database), StringComparison.OrdinalIgnoreCase);
        Assert.Contains(database, NetezzaHelpers.OBJECT_COLUMNS_NZ_SQL_OF_DB(database), StringComparison.OrdinalIgnoreCase);
        Assert.Contains(database, NetezzaHelpers.SearchInNetezzaSchema(database, "customer"), StringComparison.OrdinalIgnoreCase);
        Assert.Contains(database, NetezzaHelpers.ExternalSql(database), StringComparison.OrdinalIgnoreCase);
        Assert.Contains(database, NetezzaHelpers.ProcSql(database), StringComparison.OrdinalIgnoreCase);
        Assert.Contains(database, NetezzaHelpers.SynonymSql(database), StringComparison.OrdinalIgnoreCase);
        Assert.Contains(database, NetezzaHelpers.ViewSql(database), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void OneTableSqlBuilders_IncludeRequestedTable()
    {
        Assert.Contains("orders", NetezzaHelpers.OneTableSqlOwner("orders"), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("orders", NetezzaHelpers.OneTableSqlSchema("orders", schemaOn: true), StringComparison.OrdinalIgnoreCase);
        Assert.NotEqual(NetezzaHelpers.OneTableSqlSchema("orders", schemaOn: true), NetezzaHelpers.OneTableSqlSchema("orders", schemaOn: false));
    }

    [Theory]
    [InlineData("INTEGER", "INTEGER")]
    [InlineData("VARCHAR", "VARCHAR")]
    public void NzProcReturnFix_NormalizesKnownProcedureReturnTypes(string input, string expectedFragment)
    {
        Assert.Contains(expectedFragment, NetezzaHelpers.NzProcReturnFix(input), StringComparison.OrdinalIgnoreCase);
    }
}
