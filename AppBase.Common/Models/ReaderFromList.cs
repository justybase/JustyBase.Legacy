using System.Collections;
using System.Data;
using System.Data.Common;

namespace AppBase.Common;

public sealed class ReaderFromList : DbDataReader
{
    private readonly List<object[]> _dataList;
    private readonly DataTable _headerDataTable;
    public ReaderFromList(DataTable headerDataTable, List<object[]> dataList)
    {
        _dataList = dataList;
        _headerDataTable = headerDataTable;
        _fieldCount = headerDataTable.Columns.Count;
        _rowsCnt = dataList.Count;

        _typeNames = new string[_fieldCount];
        _types = new Type[_fieldCount];
        _headers = new string[_fieldCount];
        for (int i = 0; i < _fieldCount; i++)
        {
            _types[i] = _headerDataTable.Columns[i].DataType;
            _typeNames[i] = _headerDataTable.Columns[i].DataType.ToString();
            if (_types[i] == typeof(Memory<byte>))
            {
                _types[i] = typeof(string);
                _typeNames[i] = typeof(string).ToString();
            }
            _headers[i] = _headerDataTable.Columns[i].ColumnName;
        }
    }

    private readonly int _fieldCount;
    private readonly int _rowsCnt;
    private int _currentRowNum = -1;
    private object[] CurrentRow => _dataList[_currentRowNum];
    private readonly string[] _typeNames;
    private readonly Type[] _types;
    private readonly string[] _headers;

    public override object this[int ordinal] => _dataList[_currentRowNum][ordinal];

    public override object this[string name] => throw new NotImplementedException();

    public override int Depth => throw new NotImplementedException();

    public override int FieldCount => _fieldCount;

    public override bool HasRows => this._rowsCnt > 0;

    public override bool IsClosed => _currentRowNum > this._rowsCnt;

    public override int RecordsAffected => throw new NotImplementedException();

    public override bool GetBoolean(int ordinal)
    {
        return (bool)CurrentRow[ordinal];
    }

    public override byte GetByte(int ordinal)
    {
        return (byte)CurrentRow[ordinal];
    }

    public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length)
    {
        throw new NotImplementedException();
    }

    public override char GetChar(int ordinal)
    {
        return (char)CurrentRow[ordinal];
    }

    public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length)
    {
        throw new NotImplementedException();
    }

    public override string GetDataTypeName(int ordinal)
    {
        return _typeNames[ordinal];
    }

    public override DateTime GetDateTime(int ordinal)
    {
        return (DateTime)CurrentRow[ordinal];
    }

    public override decimal GetDecimal(int ordinal)
    {
        return (Decimal)CurrentRow[ordinal];
    }

    public override double GetDouble(int ordinal)
    {
        return (Double)CurrentRow[ordinal];
    }

    public override Type GetFieldType(int ordinal)
    {
        return _types[ordinal];
    }

    public override float GetFloat(int ordinal)
    {
        return (float)CurrentRow[ordinal];
    }

    public override Guid GetGuid(int ordinal)
    {
        return (Guid)CurrentRow[ordinal];
    }

    public override short GetInt16(int ordinal)
    {
        return (short)CurrentRow[ordinal];
    }

    public override int GetInt32(int ordinal)
    {
        return (int)CurrentRow[ordinal];
    }

    public override long GetInt64(int ordinal)
    {
        return (long)CurrentRow[ordinal];
    }

    public override string GetName(int ordinal)
    {
        return _headers[ordinal];
    }

    public override int GetOrdinal(string name)
    {
        throw new NotSupportedException();
    }

    public override string GetString(int ordinal)
    {
        return (string)CurrentRow[ordinal];
    }

    public override object GetValue(int ordinal)
    {
        return CurrentRow[ordinal];
    }

    public override int GetValues(object[] values)
    {
        for (int i = 0; i < _fieldCount; i++)
        {
            values[i] = CurrentRow[i] ?? DBNull.Value;
        }
        return _fieldCount;
    }

    public override bool IsDBNull(int ordinal)
    {
        var val = CurrentRow[ordinal];
        return val == null || val == DBNull.Value;
    }

    public override bool NextResult() => false;


    public override bool Read()
    {

        return ++_currentRowNum < _rowsCnt;
    }

    public override IEnumerator GetEnumerator()
    {
        throw new NotImplementedException();
    }
}
