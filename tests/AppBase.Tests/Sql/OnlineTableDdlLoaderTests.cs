using AppBase.Data.Ddl;
using JustyBase.Netezza.Models;
using JustyBase.NetezzaDdl;

namespace AppBase.Tests.Sql;

public sealed class OnlineTableDdlLoaderTests
{
    [Fact]
    public void BuildInput_maps_columns_distribution_organize_and_comment()
    {
        var columns = new NetezzaSchemaColumn[]
        {
            new("ID", "INTEGER", Nullable: false),
            new("NAME", "VARCHAR(50)", Nullable: true, Description: "display name"),
        };

        var input = OnlineTableDdlLoader.BuildInput(
            "JUST_DATA",
            "ADMIN",
            "DIMACCOUNT",
            columns,
            distributeColumns: ["ID"],
            organizeColumns: ["NAME"],
            tableComment: "accounts",
            tableOwner: "ADMIN");

        Assert.Equal("JUST_DATA", input.Database);
        Assert.Equal("ADMIN", input.Schema);
        Assert.Equal("DIMACCOUNT", input.TableName);
        Assert.Equal("accounts", input.TableComment);
        Assert.Equal("ADMIN", input.TableOwner);
        Assert.Equal(2, input.Columns.Count);
        Assert.Equal(["ID"], input.DistributeColumns);
        Assert.Equal(["NAME"], input.OrganizeColumns);

        string ddl = new NetezzaDdlTextBuilder().BuildCreateTable(input);
        Assert.Contains("CREATE TABLE", ddl, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DIMACCOUNT", ddl, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DISTRIBUTE ON", ddl, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MapKeys_groups_primary_and_foreign_key_rows()
    {
        OnlineTableKeyRow[] rows =
        [
            new("PK_DIM", 'p', "ID", null, null, null, null, null, null),
            new("FK_DIM", 'f', "PARENT_ID", "JUST_DATA", "ADMIN", "PARENT", "ID", "NO ACTION", "CASCADE"),
        ];

        var keys = OnlineTableDdlLoader.MapKeys(rows);

        Assert.Equal(2, keys.Count);
        var pk = Assert.Single(keys, k => k.KeyType == 'p');
        Assert.Equal("PK_DIM", pk.KeyName);
        Assert.Equal(["ID"], pk.ColumnNames);

        var fk = Assert.Single(keys, k => k.KeyType == 'f');
        Assert.Equal("FK_DIM", fk.KeyName);
        Assert.Equal(["PARENT_ID"], fk.ColumnNames);
        Assert.Equal("JUST_DATA", fk.PkDatabase);
        Assert.Equal("ADMIN", fk.PkSchema);
        Assert.Equal("PARENT", fk.PkRelation);
        Assert.Equal(["ID"], fk.ReferencedPkColumnNames);
        Assert.Equal("CASCADE", fk.OnDelete);
    }

    [Fact]
    public void BuildInput_includes_primary_key_alter_in_ddl()
    {
        var keys = OnlineTableDdlLoader.MapKeys(
        [
            new("PK_T", 'P', "ID", null, null, null, null, null, null),
        ]);

        var input = OnlineTableDdlLoader.BuildInput(
            "DB",
            "ADMIN",
            "T",
            [new NetezzaSchemaColumn("ID", "INTEGER", Nullable: false)],
            keys: keys);

        string ddl = new NetezzaDdlTextBuilder().BuildCreateTable(input);
        Assert.Contains("PRIMARY KEY", ddl, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PK_T", ddl, StringComparison.Ordinal);
    }
}
