using System.Data;
using AppBase.Services;

namespace AppBase.Tests.Database;

public sealed class DataFuncServiceTests
{
    [Fact]
    public void GetDataTable_creates_table_from_reader()
    {
        var service = new DataFuncService();
        var reader = new StubReader(["ID", "NAME"], [typeof(int), typeof(string)]);

        var table = service.GetDataTable(reader);

        Assert.Equal("tab1", table.TableName);
        Assert.Equal(2, table.Columns.Count);
        Assert.Equal("ID", table.Columns[0].ColumnName);
        Assert.Equal(typeof(int), table.Columns[0].DataType);
        Assert.Equal("NAME", table.Columns[1].ColumnName);
        Assert.Equal(typeof(string), table.Columns[1].DataType);
    }

    [Fact]
    public void GetDataTable_uses_custom_tab_index()
    {
        var service = new DataFuncService();
        var reader = new StubReader(["col"], [typeof(int)]);

        var table = service.GetDataTable(reader, l: 3);

        Assert.Equal("tab3", table.TableName);
    }

    [Fact]
    public void GetDataTable_handles_duplicate_column_names()
    {
        var reader = new DuplicateNameReader();
        var service = new DataFuncService();

        var table = service.GetDataTable(reader);

        Assert.Equal(2, table.Columns.Count);
        Assert.Equal("\"col\"", table.Columns[0].ColumnName);
        Assert.Equal("\"col_2\"", table.Columns[1].ColumnName);
    }

    [Fact]
    public void GetDataTable_maps_interval_type_to_string()
    {
        var reader = new StubReader(["col"], [typeof(int)], ["interval"]);
        var service = new DataFuncService();

        var table = service.GetDataTable(reader);

        Assert.Equal(typeof(string), table.Columns[0].DataType);
    }

    [Fact]
    public void GetDataTable_quotes_invalid_column_names()
    {
        var reader = new StubReader(["123invalid"], [typeof(int)]);
        var service = new DataFuncService();

        var table = service.GetDataTable(reader);

        Assert.Equal("\"123invalid\"", table.Columns[0].ColumnName);
    }

    [Fact]
    public void GetDataTable_fallback_to_string_on_type_error()
    {
        var reader = new FailingReader();
        var service = new DataFuncService();

        var table = service.GetDataTable(reader);

        Assert.Single(table.Columns);
        Assert.Equal(typeof(string), table.Columns[0].DataType);
    }

    private sealed class StubReader(string[] names, Type[] types, string[]? typeNames = null) : IDataReader
    {
        private readonly string[] _typeNames = typeNames ?? types.Select(t => t.Name).ToArray();
        private bool _eof;

        public int FieldCount => names.Length;
        public void Dispose() { }
        public void Close() { }
        public int Depth => 0;
        public bool IsClosed => _eof;
        public int RecordsAffected => -1;
        public DataTable? GetSchemaTable() => null;
        public bool NextResult() => false;
        public bool Read() { if (_eof) return false; _eof = true; return true; }
        public string GetName(int i) => names[i];
        public string GetDataTypeName(int i) => _typeNames[i];
        public Type GetFieldType(int i) => types[i];
        public object GetValue(int i) => throw new NotImplementedException();
        public int GetValues(object[] values) => throw new NotImplementedException();
        public object this[int i] => throw new NotImplementedException();
        public object this[string name] => throw new NotImplementedException();
        public bool GetBoolean(int i) => throw new NotImplementedException();
        public byte GetByte(int i) => throw new NotImplementedException();
        public long GetBytes(int i, long fieldOffset, byte[]? buffer, int buffervOffset, int length) => throw new NotImplementedException();
        public char GetChar(int i) => throw new NotImplementedException();
        public long GetChars(int i, long fieldoffset, char[]? buffer, int bufferoffset, int length) => throw new NotImplementedException();
        public IDataReader GetData(int i) => throw new NotImplementedException();
        public DateTime GetDateTime(int i) => throw new NotImplementedException();
        public decimal GetDecimal(int i) => throw new NotImplementedException();
        public double GetDouble(int i) => throw new NotImplementedException();
        public float GetFloat(int i) => throw new NotImplementedException();
        public Guid GetGuid(int i) => throw new NotImplementedException();
        public short GetInt16(int i) => throw new NotImplementedException();
        public int GetInt32(int i) => throw new NotImplementedException();
        public long GetInt64(int i) => throw new NotImplementedException();
        public string GetString(int i) => throw new NotImplementedException();
        public bool IsDBNull(int i) => throw new NotImplementedException();
        public int GetOrdinal(string name) => throw new NotImplementedException();
    }

    private sealed class DuplicateNameReader : IDataReader
    {
        private int _reads;
        public int FieldCount => 2;
        public void Dispose() { }
        public void Close() { }
        public int Depth => 0;
        public bool IsClosed => _reads > 0;
        public int RecordsAffected => -1;
        public DataTable? GetSchemaTable() => null;
        public bool NextResult() => false;
        public bool Read() => _reads++ == 0;
        public string GetName(int i) => "col";
        public string GetDataTypeName(int i) => "int";
        public Type GetFieldType(int i) => typeof(int);
        public object GetValue(int i) => throw new NotImplementedException();
        public int GetValues(object[] values) => throw new NotImplementedException();
        public object this[int i] => throw new NotImplementedException();
        public object this[string name] => throw new NotImplementedException();
        public bool GetBoolean(int i) => throw new NotImplementedException();
        public byte GetByte(int i) => throw new NotImplementedException();
        public long GetBytes(int i, long fieldOffset, byte[]? buffer, int buffervOffset, int length) => throw new NotImplementedException();
        public char GetChar(int i) => throw new NotImplementedException();
        public long GetChars(int i, long fieldoffset, char[]? buffer, int bufferoffset, int length) => throw new NotImplementedException();
        public IDataReader GetData(int i) => throw new NotImplementedException();
        public DateTime GetDateTime(int i) => throw new NotImplementedException();
        public decimal GetDecimal(int i) => throw new NotImplementedException();
        public double GetDouble(int i) => throw new NotImplementedException();
        public float GetFloat(int i) => throw new NotImplementedException();
        public Guid GetGuid(int i) => throw new NotImplementedException();
        public short GetInt16(int i) => throw new NotImplementedException();
        public int GetInt32(int i) => throw new NotImplementedException();
        public long GetInt64(int i) => throw new NotImplementedException();
        public string GetString(int i) => throw new NotImplementedException();
        public bool IsDBNull(int i) => throw new NotImplementedException();
        public int GetOrdinal(string name) => throw new NotImplementedException();
    }

    private sealed class FailingReader : IDataReader
    {
        public int FieldCount => 1;
        public void Dispose() { }
        public void Close() { }
        public int Depth => 0;
        public bool IsClosed => true;
        public int RecordsAffected => -1;
        public DataTable? GetSchemaTable() => null;
        public bool NextResult() => false;
        public bool Read() => false;
        public string GetName(int i) => "bad_col";
        public string GetDataTypeName(int i) => "BadType";
        public Type GetFieldType(int i) => throw new Exception("test type error");
        public object GetValue(int i) => throw new NotImplementedException();
        public int GetValues(object[] values) => throw new NotImplementedException();
        public object this[int i] => throw new NotImplementedException();
        public object this[string name] => throw new NotImplementedException();
        public bool GetBoolean(int i) => throw new NotImplementedException();
        public byte GetByte(int i) => throw new NotImplementedException();
        public long GetBytes(int i, long fieldOffset, byte[]? buffer, int buffervOffset, int length) => throw new NotImplementedException();
        public char GetChar(int i) => throw new NotImplementedException();
        public long GetChars(int i, long fieldoffset, char[]? buffer, int bufferoffset, int length) => throw new NotImplementedException();
        public IDataReader GetData(int i) => throw new NotImplementedException();
        public DateTime GetDateTime(int i) => throw new NotImplementedException();
        public decimal GetDecimal(int i) => throw new NotImplementedException();
        public double GetDouble(int i) => throw new NotImplementedException();
        public float GetFloat(int i) => throw new NotImplementedException();
        public Guid GetGuid(int i) => throw new NotImplementedException();
        public short GetInt16(int i) => throw new NotImplementedException();
        public int GetInt32(int i) => throw new NotImplementedException();
        public long GetInt64(int i) => throw new NotImplementedException();
        public string GetString(int i) => throw new NotImplementedException();
        public bool IsDBNull(int i) => throw new NotImplementedException();
        public int GetOrdinal(string name) => throw new NotImplementedException();
    }
}
