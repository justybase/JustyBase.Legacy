using System.Collections;
using System.Data;
using System.Data.Common;
using System.Diagnostics;

namespace AppBase.Common;

public class DBReaderWithMessages : DbDataReader
{
    private DbDataReader _rdr;
    private Action<long> _action;
    private int _msInterval = 30_000;

    public DBReaderWithMessages(IDataReader dataReader, Action<long> action = null, int msInterval = 30_000)
    {
        _rdr = (DbDataReader)dataReader;
        _action = action;
        _msInterval = msInterval;
    }

    public override DataTable? GetSchemaTable()
    {
        return _rdr.GetSchemaTable();
    }
    public override object this[int ordinal] => _rdr[ordinal];

    public override object this[string name] => _rdr[name];

    public override int Depth => _rdr.Depth;

    public override int FieldCount => _rdr.FieldCount;

    public override bool HasRows => _rdr.HasRows;

    public override bool IsClosed => _rdr.IsClosed;

    public override int RecordsAffected => _rdr.RecordsAffected;

    public override bool GetBoolean(int ordinal)
    {
        return _rdr.GetBoolean(ordinal);
    }

    public override byte GetByte(int ordinal)
    {
        return _rdr.GetByte(ordinal);
    }

    public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length)
    {
        return _rdr.GetBytes(ordinal, dataOffset, buffer, bufferOffset, length);
    }

    public override char GetChar(int ordinal)
    {
        return _rdr.GetChar(ordinal);
    }

    public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length)
    {
        return _rdr.GetChars(ordinal, dataOffset, buffer, bufferOffset, length);
    }

    public override string GetDataTypeName(int ordinal)
    {
        return _rdr.GetDataTypeName(ordinal);
    }

    public override DateTime GetDateTime(int ordinal)
    {
        return _rdr.GetDateTime(ordinal);
    }

    public override decimal GetDecimal(int ordinal)
    {
        return _rdr.GetDecimal(ordinal);
    }

    public override double GetDouble(int ordinal)
    {
        return _rdr.GetDouble(ordinal);
    }

    public override IEnumerator GetEnumerator()
    {
        return _rdr.GetEnumerator();
    }

    public override Type GetFieldType(int ordinal)
    {
        return _rdr.GetFieldType(ordinal);
    }

    public override float GetFloat(int ordinal)
    {
        return _rdr.GetFloat(ordinal);
    }

    public override Guid GetGuid(int ordinal)
    {
        return _rdr.GetGuid(ordinal);
    }

    public override short GetInt16(int ordinal)
    {
        return _rdr.GetInt16(ordinal);
    }

    public override int GetInt32(int ordinal)
    {
        return _rdr.GetInt32(ordinal);
    }

    public override long GetInt64(int ordinal)
    {
        return _rdr.GetInt64(ordinal);
    }

    public override string GetName(int ordinal)
    {
        return _rdr.GetName(ordinal);
    }

    public override int GetOrdinal(string name)
    {
        return _rdr.GetOrdinal(name);
    }

    public override string GetString(int ordinal)
    {
        return _rdr.GetString(ordinal);
    }

    public override object GetValue(int ordinal)
    {
        return _rdr.GetValue(ordinal);
    }

    public override int GetValues(object[] values)
    {
        return _rdr.GetValues(values);
    }

    public override bool IsDBNull(int ordinal)
    {
        return _rdr.IsDBNull(ordinal);
    }

    public override bool NextResult()
    {
        return _rdr.NextResult();
    }

    long _lineNumber = 0;

    private int messageAfterCnt = 10_000;

    Stopwatch lastMessageStopwatch;
    public override bool Read()
    {
        if (_action is not null)
        {
            lastMessageStopwatch ??= Stopwatch.StartNew();
            if (++_lineNumber % messageAfterCnt == 0 && lastMessageStopwatch.ElapsedMilliseconds >= _msInterval)
            {
                _action.Invoke(_lineNumber);
                lastMessageStopwatch.Restart();
            }
        }

        return _rdr.Read();
    }
}
