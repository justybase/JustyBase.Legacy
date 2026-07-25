using System.Collections;
using System.Data;
using System.Data.Common;
using System.Text;
using System.Windows.Forms;
using AppBase.Common.Configuration;
using AppBase.Common.Interfaces;
using AppBase.Services;
using NSubstitute;

namespace AppBase.Tests.ImportExport;

public sealed class ImportExportTasksClipboardAndHeadersTests
{
    private readonly ImportExportTasks _sut;

    public ImportExportTasksClipboardAndHeadersTests()
    {
        var config = Substitute.For<IApplicationConfig>();
        config.DefaultNvarcharLength.Returns(255);
        var settings = Substitute.For<IApplicationSettingsContext>();
        settings.Config.Returns(config);
        _sut = new ImportExportTasks(settings);
    }

    [Fact]
    public void GetHeaders_maps_common_clr_types_from_schema_table()
    {
        using var reader = new SchemaOnlyDbDataReader(
        [
            ("Name", typeof(string), 50, 0, true),
            ("Age", typeof(int), 10, 0, true),
            ("Amount", typeof(decimal), 18, 2, true),
            ("When", typeof(DateTime), 0, 0, true),
            ("Flag", typeof(bool), 0, 0, true),
            ("Big", typeof(long), 19, 0, true),
            ("Score", typeof(double), 15, 0, true),
            ("Other", typeof(Guid), 0, 0, true)
        ]);

        string[] headers = _sut.GetHeaders(reader);

        Assert.Equal(
        [
            "Name NVARCHAR(50)",
            "Age INTEGER",
            "Amount NUMERIC(18,2)",
            "When DATE",
            "Flag BOOL",
            "Big BIGINT",
            "Score DOUBLE",
            "Other NVARCHAR(255)"
        ], headers);
    }

    [Fact]
    public void GetHeaders_marks_non_nullable_columns()
    {
        using var reader = new SchemaOnlyDbDataReader(
        [
            ("Id", typeof(int), 10, 0, false)
        ]);

        string[] headers = _sut.GetHeaders(reader);

        Assert.Equal(["Id INTEGER NOT NULL"], headers);
    }

    [Fact]
    public void GetDataTableFromClipboard_parses_xml_spreadsheet_headers_and_typed_rows()
    {
        string xml = """
            <?xml version="1.0"?>
            <Workbook xmlns="urn:schemas-microsoft-com:office:spreadsheet"
             xmlns:ss="urn:schemas-microsoft-com:office:spreadsheet">
             <Worksheet ss:Name="Sheet1">
              <Table ss:ExpandedColumnCount="2" ss:ExpandedRowCount="2">
               <Row>
                <Cell><Data ss:Type="String">Name</Data></Cell>
                <Cell><Data ss:Type="String">Age</Data></Cell>
               </Row>
               <Row>
                <Cell><Data ss:Type="String">Alice</Data></Cell>
                <Cell><Data ss:Type="Number">30</Data></Cell>
               </Row>
              </Table>
             </Worksheet>
            </Workbook>
            """;

        var clipboard = Substitute.For<IDataObject>();
        clipboard.GetData("XML Spreadsheet").Returns(new MemoryStream(Encoding.UTF8.GetBytes(xml)));

        DataTable table = _sut.GetDataTableFromClipboard(clipboard, escapechar: '"', sep: ';', TypesFromFirstRow: true);

        Assert.Equal(2, table.Columns.Count);
        Assert.Equal("Name_1", table.Columns[0].ColumnName);
        Assert.Equal("Age_2", table.Columns[1].ColumnName);
        Assert.Equal(typeof(string), table.Columns[0].DataType);
        Assert.Equal(typeof(decimal), table.Columns[1].DataType);
        Assert.Single(table.Rows);
        Assert.Equal("Alice", table.Rows[0][0]);
        Assert.Equal(30m, table.Rows[0][1]);
    }

    /// <summary>
    /// Minimal DbDataReader that only serves GetSchemaTable for GetHeaders tests.
    /// Column ordinals match the default (non-Oracle / non-NZ) branch in ImportExportTasks.GetHeaders.
    /// </summary>
    private sealed class SchemaOnlyDbDataReader : DbDataReader
    {
        private readonly DataTable _schema;

        public SchemaOnlyDbDataReader(
            IReadOnlyList<(string Name, Type Type, short Precision, short Scale, bool AllowNull)> columns)
        {
            _schema = new DataTable();
            _schema.Columns.Add("ColumnName", typeof(string));       // 0
            _schema.Columns.Add("ColumnOrdinal", typeof(int));       // 1
            _schema.Columns.Add("ColumnSize", typeof(int));          // 2
            _schema.Columns.Add("NumericPrecision", typeof(short));  // 3
            _schema.Columns.Add("NumericScale", typeof(short));      // 4
            _schema.Columns.Add("DataType", typeof(Type));           // 5
            _schema.Columns.Add("ProviderType", typeof(int));        // 6
            _schema.Columns.Add("IsLong", typeof(bool));             // 7
            _schema.Columns.Add("AllowDBNull", typeof(bool));        // 8

            for (int i = 0; i < columns.Count; i++)
            {
                var c = columns[i];
                _schema.Rows.Add(c.Name, i, (int)c.Precision, c.Precision, c.Scale, c.Type, 0, false, c.AllowNull);
            }
        }

        public override int FieldCount => _schema.Rows.Count;
        public override DataTable GetSchemaTable() => _schema;

        public override object this[int ordinal] => throw new NotSupportedException();
        public override object this[string name] => throw new NotSupportedException();
        public override int Depth => 0;
        public override bool HasRows => false;
        public override bool IsClosed => false;
        public override int RecordsAffected => 0;
        public override bool GetBoolean(int ordinal) => throw new NotSupportedException();
        public override byte GetByte(int ordinal) => throw new NotSupportedException();
        public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length)
            => throw new NotSupportedException();
        public override char GetChar(int ordinal) => throw new NotSupportedException();
        public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length)
            => throw new NotSupportedException();
        public override string GetDataTypeName(int ordinal) => throw new NotSupportedException();
        public override DateTime GetDateTime(int ordinal) => throw new NotSupportedException();
        public override decimal GetDecimal(int ordinal) => throw new NotSupportedException();
        public override double GetDouble(int ordinal) => throw new NotSupportedException();
        public override Type GetFieldType(int ordinal) => throw new NotSupportedException();
        public override float GetFloat(int ordinal) => throw new NotSupportedException();
        public override Guid GetGuid(int ordinal) => throw new NotSupportedException();
        public override short GetInt16(int ordinal) => throw new NotSupportedException();
        public override int GetInt32(int ordinal) => throw new NotSupportedException();
        public override long GetInt64(int ordinal) => throw new NotSupportedException();
        public override string GetName(int ordinal) => throw new NotSupportedException();
        public override int GetOrdinal(string name) => throw new NotSupportedException();
        public override string GetString(int ordinal) => throw new NotSupportedException();
        public override object GetValue(int ordinal) => throw new NotSupportedException();
        public override int GetValues(object[] values) => throw new NotSupportedException();
        public override bool IsDBNull(int ordinal) => throw new NotSupportedException();
        public override bool NextResult() => false;
        public override bool Read() => false;
        public override IEnumerator GetEnumerator() => throw new NotSupportedException();
    }
}
