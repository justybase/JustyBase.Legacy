using AppBase.Services;
using System.Data;
using System.Text.Json;

namespace AppBase.Tests.ImportExport;

public sealed class TabularTextExporterTests
{
    [Theory]
    [InlineData(null, ";", "")]
    [InlineData("plain", ";", "plain")]
    [InlineData("a;b", ";", "\"a;b\"")]
    [InlineData("a\"b", ";", "\"a\"\"b\"")]
    [InlineData("a\r\nb", ";", "\"a\r\nb\"")]
    [InlineData("zażółć", ";", "zażółć")]
    public void EscapeCsvField_UsesStandardCsvQuoting(string? value, string delimiter, string expected)
    {
        Assert.Equal(expected, TabularTextExporter.EscapeCsvField(value, delimiter[0]));
    }

    [Fact]
    public void WriteCsv_HandlesHeadersSeparatorsNullAndUnicode()
    {
        using IDataReader reader = CreateReader();
        using StringWriter writer = new();

        long count = TabularTextExporter.WriteCsv(writer, reader, ';', "\n", includeHeader: true);

        Assert.Equal(2, count);
        Assert.Equal("name;note;missing\nzażółć;\"a;\"\"b\";\nline2;\"x\r\ny\";value\n", writer.ToString());
    }

    [Fact]
    public void WriteJson_ProducesValidArrayAndPreservesNulls()
    {
        using IDataReader reader = CreateReader();
        using StringWriter writer = new();

        Assert.Equal(2, TabularTextExporter.WriteJson(writer, reader));

        using JsonDocument document = JsonDocument.Parse(writer.ToString());
        Assert.Equal(JsonValueKind.Array, document.RootElement.ValueKind);
        Assert.Equal(2, document.RootElement.GetArrayLength());
        Assert.Equal(JsonValueKind.Null, document.RootElement[0][2].ValueKind);
        Assert.Equal("zażółć", document.RootElement[0][0].GetString());
    }

    [Fact]
    public void WriteCsv_HonorsCancellation()
    {
        using IDataReader reader = CreateReader();
        using StringWriter writer = new();
        using CancellationTokenSource source = new();
        source.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            TabularTextExporter.WriteCsv(writer, reader, ',', "\r\n", includeHeader: false, source.Token));
    }

    private static IDataReader CreateReader()
    {
        DataTable table = new();
        table.Columns.Add("name", typeof(string));
        table.Columns.Add("note", typeof(string));
        table.Columns.Add("missing", typeof(string));
        table.Rows.Add("zażółć", "a;\"b", DBNull.Value);
        table.Rows.Add("line2", "x\r\ny", "value");
        return table.CreateDataReader();
    }
}
