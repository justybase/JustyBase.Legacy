using AppBase.Services;
using System.Text.RegularExpressions;

namespace AppBase.Tests.ImportExport;

public sealed class FastNetezzaCsvImportTests
{
    [Fact]
    public void GetCodes_ProducesExternalTableImportWithConfiguredOptions()
    {
        FastNetezzaCsvImport importer = new()
        {
            ColumnDelimiter = '\t',
            DECIMALDELIM = ',',
            REMOTESOURCE = "ODBC",
            NULLVALUE = "NULL",
            ENCODING = "UTF8",
            TIMESTYLE = "YMD",
            LOGDIR = "C:\\logs",
            MAXROWS = 100,
            SocketBufSize = 4096,
            SkipRows = 1,
            IncludeHeader = true,
            Compress = true,
            GetCollumnsFun = () => ["id INTEGER", "name VARCHAR(50)"]
        };

        (string createSql, string insertSql, string fullCreate) = importer.GetCodes("target_table", "pipe_1");

        Assert.Contains("CREATE TABLE \"target_table\" (id INTEGER,name VARCHAR(50))", createSql);
        Assert.Contains("\\\\.\\pipe\\pipe_1", insertSql);
        Assert.Contains("DELIMITER '\\t'", insertSql);
        Assert.Contains("DECIMALDELIM ','", insertSql);
        Assert.Contains("SKIPROWS 1", insertSql);
        Assert.Contains("IncludeHeader True", insertSql);
        Assert.Contains("Compress True", insertSql);
        Assert.Contains("CREATE TABLE target_table AS", fullCreate);
    }

    [Fact]
    public void GetHeaders_UsesConfiguredDelegate()
    {
        FastNetezzaCsvImport importer = new() { GetCollumnsFun = () => ["first", "second"] };

        Assert.Equal(["first", "second"], importer.GetHeaders());
    }

    [Fact]
    public void ImportOptions_CanRepresentFilteringAndTransformationRules()
    {
        FastNetezzaCsvImport importer = new()
        {
            FilterRow = true,
            RxFilter = new Regex("keep"),
            TransformRow = true,
            RxTransform = new Regex("old"),
            RelaceValue = "new",
            RejectRow = true,
            RxReject = new Regex("drop")
        };

        Assert.Matches(importer.RxFilter, "keep this");
        Assert.Equal("new value", importer.RxTransform.Replace("old value", importer.RelaceValue));
        Assert.Matches(importer.RxReject, "drop this");
    }
}
