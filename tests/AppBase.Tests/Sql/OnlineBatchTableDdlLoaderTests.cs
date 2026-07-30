using AppBase.Data.Ddl;
using JustyBase.Netezza.Models;
using JustyBase.NetezzaDdl;
using JustyBase.NetezzaDdl.Models;

namespace AppBase.Tests.Sql;

public sealed class OnlineBatchTableDdlLoaderTests
{
    [Fact]
    public void BuildTableInputs_emits_create_table_for_each_entry()
    {
        var columns = new Dictionary<string, List<NetezzaSchemaColumn>>(StringComparer.OrdinalIgnoreCase)
        {
            ["ADMIN.T1"] = [new("ID", "INTEGER", Nullable: false)],
            ["ADMIN.T2"] = [new("NAME", "VARCHAR(20)", Nullable: true)],
        };
        var distribution = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["ADMIN.T1"] = ["ID"],
        };
        var keys = new Dictionary<string, IReadOnlyList<NetezzaKeyDdl>>(StringComparer.OrdinalIgnoreCase)
        {
            ["ADMIN.T1"] = OnlineTableDdlLoader.MapKeys(
            [
                new("PK_T1", 'p', "ID", null, null, null, null, null, null),
            ]),
        };

        var inputs = OnlineBatchTableDdlLoader.BuildTableInputs(
            "JUST_DATA",
            columns,
            distribution,
            keysByTable: keys,
            commentsByTable: new Dictionary<string, string> { ["ADMIN.T2"] = "names" });

        Assert.Equal(2, inputs.Count);
        string sql = new NetezzaBatchDdlBuilder().Build(new NetezzaBatchDdlInput(Tables: inputs));
        Assert.Contains("-- TABLE JUST_DATA.ADMIN.T1", sql, StringComparison.Ordinal);
        Assert.Contains("-- TABLE JUST_DATA.ADMIN.T2", sql, StringComparison.Ordinal);
        Assert.Contains("DISTRIBUTE ON", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PRIMARY KEY", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("COMMENT ON TABLE", sql, StringComparison.OrdinalIgnoreCase);
    }
}
