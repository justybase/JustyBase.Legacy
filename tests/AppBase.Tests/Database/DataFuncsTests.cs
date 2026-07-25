using System.Data;
using AppBase.Services;

namespace AppBase.Tests.Database;

/// <summary>
/// Minimal IDataReader stub for testing DataFuncs.GetDataTable.
/// </summary>
internal sealed class StubDataReader : IDataReader
{
    private readonly string[] _names;
    private readonly Type[] _types;
    private readonly string[] _typeNames;
    private bool _eof;

    public StubDataReader(string[] names, Type[] types, string[]? typeNames = null)
    {
        _names = names;
        _types = types;
        _typeNames = typeNames ?? types.Select(t => t.Name).ToArray();
    }

    public int FieldCount => _names.Length;

    public void Dispose() { }
    public void Close() { }
    public int Depth => 0;
    public bool IsClosed => _eof;
    public int RecordsAffected => -1;
    public DataTable? GetSchemaTable() => null;
    public bool NextResult() => false;
    public bool Read()
    {
        if (_eof) return false;
        _eof = true;
        return true;
    }

    public string GetName(int i) => _names[i];
    public string GetDataTypeName(int i) => _typeNames[i];
    public Type GetFieldType(int i) => _types[i];
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

public sealed class DataFuncsTests
{
    [Fact]
    public void GetDataTable_creates_table_with_correct_columns()
    {
        var funcs = new DataFuncs();
        var reader = new StubDataReader(
            ["ID", "NAME", "VALUE"],
            [typeof(int), typeof(string), typeof(double)]);

        var table = funcs.GetDataTable(reader);

        Assert.Equal("tab1", table.TableName);
        Assert.Equal(3, table.Columns.Count);
        Assert.Equal("ID", table.Columns[0].ColumnName);
        Assert.Equal(typeof(int), table.Columns[0].DataType);
        Assert.Equal("NAME", table.Columns[1].ColumnName);
        Assert.Equal(typeof(string), table.Columns[1].DataType);
    }

    [Fact]
    public void GetDataTable_handles_custom_tab_name()
    {
        var funcs = new DataFuncs();
        var reader = new StubDataReader(["col"], [typeof(int)]);

        var table = funcs.GetDataTable(reader, l: 5);

        Assert.Equal("tab5", table.TableName);
    }

    [Fact]
    public void GetDataTable_interval_type_mapped_to_string()
    {
        var funcs = new DataFuncs();
        var reader = new StubDataReader(["col"], [typeof(int)], ["interval"]);

        var table = funcs.GetDataTable(reader);

        Assert.Single(table.Columns);
        Assert.Equal(typeof(string), table.Columns[0].DataType);
    }

    [Fact]
    public void GetDataTable_quotes_invalid_column_names()
    {
        var funcs = new DataFuncs();
        var reader = new StubDataReader(["123col"], [typeof(int)]);

        var table = funcs.GetDataTable(reader);

        Assert.Equal("\"123col\"", table.Columns[0].ColumnName);
    }

    [Fact]
    public void GetDataTable_fallback_on_error_and_invokes_callback()
    {
        var funcs = new DataFuncs();
        string? capturedError = null;
        // A reader that returns a type that isn't constructible - test error path
        var reader = new FailingDataReader();

        var table = funcs.GetDataTable(reader, onErrorMessage: msg => capturedError = msg);

        Assert.NotNull(capturedError);
        Assert.Contains("test error", capturedError, StringComparison.Ordinal);
    }

    [Fact]
    public void Default_singleton_works()
    {
        Assert.NotNull(DataFuncs.Default);
        Assert.IsAssignableFrom<IDataFuncs>(DataFuncs.Default);
    }
}

internal sealed class FailingDataReader : IDataReader
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
    public Type GetFieldType(int i) => throw new Exception("test error");
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
