using System.Collections;
using System.Data;
using System.Data.Common;
using AppBase.Common;

namespace AppBase.Tests.Database;

public sealed class DBReaderWithMessagesTests
{
    [Fact]
    public void Read_delegates_to_inner_reader()
    {
        var inner = new StubDbReader(rows: 3);
        using var reader = new DBReaderWithMessages(inner);

        Assert.True(reader.Read());
        Assert.True(reader.Read());
        Assert.True(reader.Read());
        Assert.False(reader.Read());
    }

    [Fact]
    public void FieldCount_delegates_to_inner()
    {
        var inner = new StubDbReader(fieldCount: 5);
        using var reader = new DBReaderWithMessages(inner);

        Assert.Equal(5, reader.FieldCount);
    }

    [Fact]
    public void HasRows_delegates_to_inner()
    {
        var inner = new StubDbReader(hasRows: true);
        using var reader = new DBReaderWithMessages(inner);

        Assert.True(reader.HasRows);
    }

    [Fact]
    public void RecordsAffected_delegates_to_inner()
    {
        var inner = new StubDbReader(recordsAffected: 42);
        using var reader = new DBReaderWithMessages(inner);

        Assert.Equal(42, reader.RecordsAffected);
    }

    [Fact]
    public void IsClosed_delegates_to_inner()
    {
        var inner = new StubDbReader(isClosed: true);
        using var reader = new DBReaderWithMessages(inner);

        Assert.True(reader.IsClosed);
    }

    [Fact]
    public void Depth_delegates_to_inner()
    {
        var inner = new StubDbReader(depth: 3);
        using var reader = new DBReaderWithMessages(inner);

        Assert.Equal(3, reader.Depth);
    }

    [Fact]
    public void GetString_delegates_to_inner()
    {
        var inner = new StubDbReader(stringValue: "hello");
        using var reader = new DBReaderWithMessages(inner);
        reader.Read();

        Assert.Equal("hello", reader.GetString(0));
    }

    [Fact]
    public void GetInt32_delegates_to_inner()
    {
        var inner = new StubDbReader(intValue: 99);
        using var reader = new DBReaderWithMessages(inner);
        reader.Read();

        Assert.Equal(99, reader.GetInt32(0));
    }

    [Fact]
    public void GetName_delegates_to_inner()
    {
        var inner = new StubDbReader(columnName: "col1");
        using var reader = new DBReaderWithMessages(inner);

        Assert.Equal("col1", reader.GetName(0));
    }

    [Fact]
    public void GetOrdinal_delegates_to_inner()
    {
        var inner = new StubDbReader(ordinalResult: 2);
        using var reader = new DBReaderWithMessages(inner);

        Assert.Equal(2, reader.GetOrdinal("col1"));
    }

    [Fact]
    public void Action_invoked_at_interval()
    {
        long reportedLine = 0;
        int invokeCount = 0;
        var inner = new StubDbReader(rows: 10_001);
        using var reader = new DBReaderWithMessages(inner, line =>
        {
            reportedLine = line;
            invokeCount++;
        }, msInterval: 0);

        while (reader.Read()) { }

        Assert.True(invokeCount >= 1);
        Assert.True(reportedLine >= 10_000);
    }

    [Fact]
    public void No_action_no_error()
    {
        var inner = new StubDbReader(rows: 5);
        using var reader = new DBReaderWithMessages(inner, null!);

        while (reader.Read()) { }
    }

    [Fact]
    public void GetSchemaTable_delegates_to_inner()
    {
        var expected = new DataTable("schema");
        var inner = new StubDbReader(schemaTable: expected);
        using var reader = new DBReaderWithMessages(inner);

        Assert.Same(expected, reader.GetSchemaTable());
    }

    [Fact]
    public void IsDBNull_delegates_to_inner()
    {
        var inner = new StubDbReader(dbNull: true);
        using var reader = new DBReaderWithMessages(inner);
        reader.Read();

        Assert.True(reader.IsDBNull(0));
    }

    [Fact]
    public void NextResult_delegates_to_inner()
    {
        var inner = new StubDbReader(nextResult: true);
        using var reader = new DBReaderWithMessages(inner);

        Assert.True(reader.NextResult());
    }

    [Fact]
    public void GetEnumerator_delegates_to_inner()
    {
        var inner = new StubDbReader();
        using var reader = new DBReaderWithMessages(inner);

        Assert.NotNull(reader.GetEnumerator());
    }

    [Fact]
    public void Indexer_by_ordinal_delegates_to_inner()
    {
        var inner = new StubDbReader(intValue: 42);
        using var reader = new DBReaderWithMessages(inner);
        reader.Read();

        Assert.Equal(42, reader[0]);
    }

    private sealed class StubDbReader : DbDataReader
    {
        private readonly int _rows;
        private int _readCount;
        private readonly bool _hasRows;
        private readonly int _recordsAffected;
        private readonly bool _isClosed;
        private readonly int _depth;
        private readonly int _fieldCount;
        private readonly string _stringValue;
        private readonly int _intValue;
        private readonly string _columnName;
        private readonly int _ordinalResult;
        private readonly DataTable? _schemaTable;
        private readonly bool _dbNull;
        private readonly bool _nextResult;

        public StubDbReader(
            int rows = 0, bool hasRows = false, int recordsAffected = -1,
            bool isClosed = false, int depth = 0, int fieldCount = 1,
            string stringValue = "", int intValue = 0,
            string columnName = "", int ordinalResult = 0,
            DataTable? schemaTable = null, bool dbNull = false,
            bool nextResult = false)
        {
            _rows = rows;
            _hasRows = hasRows;
            _recordsAffected = recordsAffected;
            _isClosed = isClosed;
            _depth = depth;
            _fieldCount = fieldCount;
            _stringValue = stringValue;
            _intValue = intValue;
            _columnName = columnName;
            _ordinalResult = ordinalResult;
            _schemaTable = schemaTable;
            _dbNull = dbNull;
            _nextResult = nextResult;
        }

        public override int FieldCount => _fieldCount;
        public override bool HasRows => _hasRows;
        public override int RecordsAffected => _recordsAffected;
        public override bool IsClosed => _isClosed || _readCount >= _rows;
        public override int Depth => _depth;
        public override object this[int ordinal] => _intValue;
        public override object this[string name] => _intValue;

        public override bool Read() => _readCount++ < _rows;
        public override bool NextResult() => _nextResult;

        // NextResultAsync is non-virtual in .NET 10, so not overridden here.
        public override bool IsDBNull(int ordinal) => _dbNull;
        public override string GetString(int ordinal) => _stringValue;
        public override int GetInt32(int ordinal) => _intValue;
        public override string GetName(int ordinal) => _columnName;
        public override int GetOrdinal(string name) => _ordinalResult;
        public override DataTable? GetSchemaTable() => _schemaTable;
        public override IEnumerator GetEnumerator() => new object[0].GetEnumerator();
        public override bool GetBoolean(int ordinal) => throw new NotImplementedException();
        public override byte GetByte(int ordinal) => throw new NotImplementedException();
        public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) => throw new NotImplementedException();
        public override char GetChar(int ordinal) => throw new NotImplementedException();
        public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length) => throw new NotImplementedException();
        public override string GetDataTypeName(int ordinal) => "int";
        public override DateTime GetDateTime(int ordinal) => throw new NotImplementedException();
        public override decimal GetDecimal(int ordinal) => throw new NotImplementedException();
        public override double GetDouble(int ordinal) => throw new NotImplementedException();
        public override Type GetFieldType(int ordinal) => typeof(int);
        public override float GetFloat(int ordinal) => throw new NotImplementedException();
        public override Guid GetGuid(int ordinal) => throw new NotImplementedException();
        public override short GetInt16(int ordinal) => throw new NotImplementedException();
        public override long GetInt64(int ordinal) => throw new NotImplementedException();
        public override object GetValue(int ordinal) => _intValue;
        public override int GetValues(object[] values) => throw new NotImplementedException();
    }
}
