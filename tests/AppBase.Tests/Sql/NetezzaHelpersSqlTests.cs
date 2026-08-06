using AppBase.Data;

namespace AppBase.Tests.Sql;

public sealed class NetezzaHelpersSqlTests
{
    [Theory]
    [InlineData("SALES")]
    [InlineData("Mixed_Name")]
    public void CatalogSqlBuilders_IncludeRequestedDatabase(string database)
    {
        Assert.Contains(database, NetezzaHelpers.GetDescSql(database), StringComparison.OrdinalIgnoreCase);
        Assert.Contains(database, NetezzaHelpers.ExternalSql(database), StringComparison.OrdinalIgnoreCase);
        Assert.Contains(database, NetezzaHelpers.ProcSql(database), StringComparison.OrdinalIgnoreCase);
        Assert.Contains(database, NetezzaHelpers.SynonymSql(database), StringComparison.OrdinalIgnoreCase);
        Assert.Contains(database, NetezzaHelpers.ViewSql(database), StringComparison.OrdinalIgnoreCase);
        Assert.Contains(database, NetezzaHelpers.KeysSql(database), StringComparison.OrdinalIgnoreCase);
        Assert.Contains(database, NetezzaHelpers.DistributionColumnsSql(database), StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("INTEGER", "INTEGER")]
    [InlineData("VARCHAR", "VARCHAR")]
    public void NzProcReturnFix_NormalizesKnownProcedureReturnTypes(string input, string expectedFragment)
    {
        Assert.Contains(expectedFragment, NetezzaHelpers.NzProcReturnFix(input), StringComparison.OrdinalIgnoreCase);
    }
}
