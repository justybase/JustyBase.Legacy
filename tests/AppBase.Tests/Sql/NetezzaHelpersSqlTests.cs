using AppBase.Data;
using JustyBase.NetezzaCatalogSql;

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

    [Fact]
    public void SourceSearchBuilders_escape_sql_like_wildcards_and_quotes()
    {
        string viewSql = NetezzaCatalogSql.GetLegacyViewSourceSearchSql("sales", "proc'name_%");
        string procedureSql = NetezzaCatalogSql.GetLegacyProcedureSourceSearchSql("sales", "proc'name_%");

        Assert.Contains("PROC''NAME~_~%", viewSql, StringComparison.Ordinal);
        Assert.Contains("PROC''NAME~_~%", procedureSql, StringComparison.Ordinal);
        Assert.Contains("ESCAPE '~'", viewSql, StringComparison.Ordinal);
        Assert.Contains("DATABASE = 'SALES'", procedureSql, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("INTEGER", "INTEGER")]
    [InlineData("VARCHAR", "VARCHAR")]
    public void NzProcReturnFix_NormalizesKnownProcedureReturnTypes(string input, string expectedFragment)
    {
        Assert.Contains(expectedFragment, NetezzaHelpers.NzProcReturnFix(input), StringComparison.OrdinalIgnoreCase);
    }
}
