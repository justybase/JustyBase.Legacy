using AppBase.Common;
using AppBase.Common.Configuration;
using AppBase.Common.Interfaces;
using AppBase.Services;
using NSubstitute;

namespace AppBase.Tests.ImportExport;

public sealed class ImportExportTasksChooseTypesTests
{
    private readonly ImportExportTasks _sut;

    public ImportExportTasksChooseTypesTests()
    {
        var config = Substitute.For<IApplicationConfig>();
        config.DefaultNvarcharLength.Returns(255);
        var settings = Substitute.For<IApplicationSettingsContext>();
        settings.Config.Returns(config);
        _sut = new ImportExportTasks(settings);
    }

    [Theory]
    [InlineData("COL_#TEXT", "COL_#TEXT NVARCHAR(255)")]
    [InlineData("COL_#NUMERIC", "COL_#NUMERIC NUMERIC(20,8)")]
    [InlineData("COL_#INTEGER", "COL_#INTEGER INTEGER")]
    [InlineData("COL_#DATE", "COL_#DATE DATE")]
    [InlineData("COL_#TIMESTAMP", "COL_#TIMESTAMP TIMESTAMP")]
    public void ChooseTypes_honors_explicit_header_suffixes(string header, string expected)
    {
        var headers = new[] { header };

        _sut.ChooseTypes(new Dictionary<int, Dictionary<DatabaseColumnType, int[]>>(), headers);

        Assert.Equal(expected, headers[0]);
    }

    [Fact]
    public void ChooseTypes_defaults_missing_column_to_nvarchar()
    {
        var headers = new[] { "NAME" };
        var typesCount = new Dictionary<int, Dictionary<DatabaseColumnType, int[]>>();

        _sut.ChooseTypes(typesCount, headers);

        Assert.Equal("NAME NVARCHAR(260)", headers[0]);
        Assert.True(typesCount.ContainsKey(0));
        Assert.Equal(1, typesCount[0][DatabaseColumnType.nvarchar][0]);
        Assert.Equal(255, typesCount[0][DatabaseColumnType.nvarchar][1]);
    }

    [Fact]
    public void ChooseTypes_prefers_nvarchar_when_column_contains_text()
    {
        var headers = new[] { "MIXED" };
        var typesCount = new Dictionary<int, Dictionary<DatabaseColumnType, int[]>>
        {
            [0] = new()
            {
                [DatabaseColumnType.nvarchar] = [3, 10, 0],
                [DatabaseColumnType.integer] = [2, 5, 0],
                [DatabaseColumnType.numeric] = [1, 8, 2]
            }
        };

        _sut.ChooseTypes(typesCount, headers);

        Assert.Equal("MIXED NVARCHAR(15)", headers[0]);
    }

    [Fact]
    public void ChooseTypes_widens_nvarchar_for_date_or_timestamp_mix()
    {
        var headers = new[] { "MIXED" };
        var typesCount = new Dictionary<int, Dictionary<DatabaseColumnType, int[]>>
        {
            [0] = new()
            {
                [DatabaseColumnType.nvarchar] = [2, 8, 0],
                [DatabaseColumnType.date] = [1, 0, 0]
            }
        };

        _sut.ChooseTypes(typesCount, headers);

        Assert.Equal("MIXED NVARCHAR(55)", headers[0]);
    }

    [Fact]
    public void ChooseTypes_emits_numeric_when_no_nvarchar()
    {
        var headers = new[] { "AMOUNT" };
        var typesCount = new Dictionary<int, Dictionary<DatabaseColumnType, int[]>>
        {
            [0] = new()
            {
                [DatabaseColumnType.numeric] = [5, 12, 4]
            }
        };

        _sut.ChooseTypes(typesCount, headers);

        Assert.Equal("AMOUNT NUMERIC(12,4)", headers[0]);
    }

    [Fact]
    public void ChooseTypes_widens_numeric_precision_when_integer_mix_requires_it()
    {
        var headers = new[] { "AMOUNT" };
        var typesCount = new Dictionary<int, Dictionary<DatabaseColumnType, int[]>>
        {
            [0] = new()
            {
                [DatabaseColumnType.numeric] = [2, 6, 2],
                [DatabaseColumnType.integer] = [3, 10, 0]
            }
        };

        _sut.ChooseTypes(typesCount, headers);

        // integer length 10 + scale 2 = 12, then containInteger forces a >= b+16 => 18
        Assert.Equal("AMOUNT NUMERIC(18,2)", headers[0]);
    }

    [Fact]
    public void ChooseTypes_falls_back_to_nvarchar_when_numeric_and_timestamp_mix()
    {
        var headers = new[] { "CONFUSING" };
        var typesCount = new Dictionary<int, Dictionary<DatabaseColumnType, int[]>>
        {
            [0] = new()
            {
                [DatabaseColumnType.numeric] = [2, 10, 2],
                [DatabaseColumnType.timestamp] = [2, 0, 0]
            }
        };

        _sut.ChooseTypes(typesCount, headers);

        Assert.Equal("CONFUSING NVARCHAR(255)", headers[0]);
    }

    [Fact]
    public void ChooseTypes_uses_nvarchar50_when_timestamp_and_integer_mix()
    {
        var headers = new[] { "CONFUSING" };
        var typesCount = new Dictionary<int, Dictionary<DatabaseColumnType, int[]>>
        {
            [0] = new()
            {
                [DatabaseColumnType.timestamp] = [2, 0, 0],
                [DatabaseColumnType.integer] = [2, 5, 0]
            }
        };

        _sut.ChooseTypes(typesCount, headers);

        Assert.Equal("CONFUSING NVARCHAR(50)", headers[0]);
    }

    [Theory]
    [InlineData(DatabaseColumnType.integer, "ID BIGINT")]
    [InlineData(DatabaseColumnType.date, "ID DATE")]
    [InlineData(DatabaseColumnType.timestamp, "ID TIMESTAMP")]
    [InlineData(DatabaseColumnType.boolean, "ID BOOL")]
    public void ChooseTypes_uses_best_choice_for_homogeneous_column(
        DatabaseColumnType type,
        string expected)
    {
        var headers = new[] { "ID" };
        var typesCount = new Dictionary<int, Dictionary<DatabaseColumnType, int[]>>
        {
            [0] = new()
            {
                [type] = [4, 8, 2]
            }
        };

        _sut.ChooseTypes(typesCount, headers);

        Assert.Equal(expected, headers[0]);
    }

    [Fact]
    public void ChooseTypes_ignores_noinfo_when_selecting_best_choice()
    {
        var headers = new[] { "FLAG" };
        var typesCount = new Dictionary<int, Dictionary<DatabaseColumnType, int[]>>
        {
            [0] = new()
            {
                [DatabaseColumnType.noinfo] = [99, 0, 0],
                [DatabaseColumnType.boolean] = [3, 0, 0]
            }
        };

        _sut.ChooseTypes(typesCount, headers);

        Assert.Equal("FLAG BOOL", headers[0]);
    }
}
