using System.Data;
using AppBase.Common;

namespace AppBase.Tests.Models;

public sealed class ReaderFromListTests
{
    [Fact]
    public void FieldCount_matches_header_columns()
    {
        using var reader = CreateReader(
            new[] { "id", "name", "value" },
            new[] { typeof(int), typeof(string), typeof(double) },
            []);

        Assert.Equal(3, reader.FieldCount);
    }

    [Fact]
    public void HasRows_true_when_data_present()
    {
        using var reader = CreateReader(
            new[] { "col1" },
            new[] { typeof(string) },
            new[] { new object[] { "hello" } });

        Assert.True(reader.HasRows);
    }

    [Fact]
    public void HasRows_false_when_no_data()
    {
        using var reader = CreateReader(
            new[] { "col1" },
            new[] { typeof(string) },
            []);

        Assert.False(reader.HasRows);
    }

    [Fact]
    public void Read_advances_through_rows()
    {
        using var reader = CreateReader(
            new[] { "col1" },
            new[] { typeof(int) },
            new[] { new object[] { 10 }, new object[] { 20 } });

        Assert.True(reader.Read());
        Assert.Equal(10, reader.GetInt32(0));
        Assert.True(reader.Read());
        Assert.Equal(20, reader.GetInt32(0));
        Assert.False(reader.Read());
    }

    [Fact]
    public void GetString_returns_correct_value()
    {
        using var reader = CreateSingleRowReader(new[] { "name" }, new[] { typeof(string) }, "test");

        reader.Read();
        Assert.Equal("test", reader.GetString(0));
    }

    [Fact]
    public void GetInt32_returns_correct_value()
    {
        using var reader = CreateSingleRowReader(new[] { "num" }, new[] { typeof(int) }, 42);

        reader.Read();
        Assert.Equal(42, reader.GetInt32(0));
    }

    [Fact]
    public void GetBoolean_returns_correct_value()
    {
        using var reader = CreateSingleRowReader(new[] { "flag" }, new[] { typeof(bool) }, true);

        reader.Read();
        Assert.True(reader.GetBoolean(0));
    }

    [Fact]
    public void GetDouble_returns_correct_value()
    {
        using var reader = CreateSingleRowReader(new[] { "amount" }, new[] { typeof(double) }, 3.14);

        reader.Read();
        Assert.Equal(3.14, reader.GetDouble(0));
    }

    [Fact]
    public void GetDateTime_returns_correct_value()
    {
        var dt = new DateTime(2026, 7, 20);
        using var reader = CreateSingleRowReader(new[] { "created" }, new[] { typeof(DateTime) }, dt);

        reader.Read();
        Assert.Equal(dt, reader.GetDateTime(0));
    }

    [Fact]
    public void GetDecimal_returns_correct_value()
    {
        using var reader = CreateSingleRowReader(new[] { "price" }, new[] { typeof(decimal) }, 19.99m);

        reader.Read();
        Assert.Equal(19.99m, reader.GetDecimal(0));
    }

    [Fact]
    public void GetFloat_returns_correct_value()
    {
        using var reader = CreateSingleRowReader(new[] { "ratio" }, new[] { typeof(float) }, 2.5f);

        reader.Read();
        Assert.Equal(2.5f, reader.GetFloat(0));
    }

    [Fact]
    public void GetInt16_returns_correct_value()
    {
        using var reader = CreateSingleRowReader(new[] { "small" }, new[] { typeof(short) }, (short)7);

        reader.Read();
        Assert.Equal(7, reader.GetInt16(0));
    }

    [Fact]
    public void GetInt64_returns_correct_value()
    {
        using var reader = CreateSingleRowReader(new[] { "big" }, new[] { typeof(long) }, 9999999999L);

        reader.Read();
        Assert.Equal(9999999999L, reader.GetInt64(0));
    }

    [Fact]
    public void GetByte_returns_correct_value()
    {
        using var reader = CreateSingleRowReader(new[] { "b" }, new[] { typeof(byte) }, (byte)255);

        reader.Read();
        Assert.Equal(255, reader.GetByte(0));
    }

    [Fact]
    public void GetChar_returns_correct_value()
    {
        using var reader = CreateSingleRowReader(new[] { "c" }, new[] { typeof(char) }, 'X');

        reader.Read();
        Assert.Equal('X', reader.GetChar(0));
    }

    [Fact]
    public void IsDBNull_true_for_null_values()
    {
        using var reader = CreateSingleRowReader(new[] { "col" }, new[] { typeof(string) }, DBNull.Value);

        reader.Read();
        Assert.True(reader.IsDBNull(0));
    }

    [Fact]
    public void IsDBNull_false_for_non_null_values()
    {
        using var reader = CreateSingleRowReader(new[] { "col" }, new[] { typeof(string) }, "value");

        reader.Read();
        Assert.False(reader.IsDBNull(0));
    }

    [Fact]
    public void GetDataTypeName_returns_type_name()
    {
        using var reader = CreateSingleRowReader(new[] { "col" }, new[] { typeof(int) }, 1);

        Assert.Equal("System.Int32", reader.GetDataTypeName(0));
    }

    [Fact]
    public void GetFieldType_returns_correct_type()
    {
        using var reader = CreateSingleRowReader(new[] { "col" }, new[] { typeof(DateTime) }, DateTime.Now);

        Assert.Equal(typeof(DateTime), reader.GetFieldType(0));
    }

    [Fact]
    public void GetName_returns_column_name()
    {
        using var reader = CreateSingleRowReader(new[] { "myColumn" }, new[] { typeof(int) }, 1);

        Assert.Equal("myColumn", reader.GetName(0));
    }

    [Fact]
    public void GetValue_returns_boxed_value()
    {
        using var reader = CreateSingleRowReader(new[] { "col" }, new[] { typeof(int) }, 42);

        reader.Read();
        Assert.Equal(42, reader.GetValue(0));
    }

    [Fact]
    public void GetValues_fills_array()
    {
        using var reader = CreateReader(
            new[] { "a", "b" },
            new[] { typeof(int), typeof(string) },
            new[] { new object[] { 1, "hello" } });

        reader.Read();
        var values = new object[2];
        int count = reader.GetValues(values);

        Assert.Equal(2, count);
        Assert.Equal(1, values[0]);
        Assert.Equal("hello", values[1]);
    }

    [Fact]
    public void GetValues_fillsDBNull_for_null_values()
    {
        using var reader = CreateReader(
            new[] { "a" },
            new[] { typeof(string) },
            new[] { new object[] { null! } });

        reader.Read();
        var values = new object[1];
        reader.GetValues(values);

        Assert.Equal(DBNull.Value, values[0]);
    }

    [Fact]
    public void Indexer_by_ordinal_returns_correct_value()
    {
        using var reader = CreateReader(
            new[] { "x", "y" },
            new[] { typeof(int), typeof(string) },
            new[] { new object[] { 7, "seven" } });

        reader.Read();
        Assert.Equal(7, reader[0]);
        Assert.Equal("seven", reader[1]);
    }

    [Fact]
    public void NextResult_always_returns_false()
    {
        using var reader = CreateSingleRowReader(new[] { "col" }, new[] { typeof(int) }, 1);
        Assert.False(reader.NextResult());
    }

    [Fact]
    public void IsClosed_false_after_creation()
    {
        using var reader = CreateSingleRowReader(new[] { "col" }, new[] { typeof(int) }, 1);
        Assert.False(reader.IsClosed);
    }

    [Fact]
    public void IsClosed_true_after_reading_all_rows()
    {
        using var reader = CreateReader(
            new[] { "col" },
            new[] { typeof(int) },
            new[] { new object[] { 1 } });

        reader.Read();
        reader.Read(); // past the end: _currentRowNum == _rowsCnt
        Assert.False(reader.IsClosed); // _currentRowNum (1) > _rowsCnt (1) == false
        reader.Read(); // one more: _currentRowNum (2) > _rowsCnt (1) == true
        Assert.True(reader.IsClosed);
    }

    [Fact]
    public void Memory_byte_type_is_mapped_to_string()
    {
        var header = new DataTable();
        header.Columns.Add("blob", typeof(Memory<byte>));
        var data = new List<object[]> { new object[] { Memory<byte>.Empty } };
        using var reader = new ReaderFromList(header, data);

        Assert.Equal(typeof(string), reader.GetFieldType(0));
    }

    [Fact]
    public void Empty_reader_field_count_is_zero()
    {
        using var reader = CreateReader(Array.Empty<string>(), Array.Empty<Type>(), []);
        Assert.Equal(0, reader.FieldCount);
        Assert.False(reader.HasRows);
    }

    private static ReaderFromList CreateReader(string[] names, Type[] types, object[][] rows)
    {
        var header = new DataTable();
        for (int i = 0; i < names.Length; i++)
            header.Columns.Add(names[i], types[i]);
        return new ReaderFromList(header, rows.ToList());
    }

    private static ReaderFromList CreateSingleRowReader(string[] names, Type[] types, object value)
    {
        var header = new DataTable();
        for (int i = 0; i < names.Length; i++)
            header.Columns.Add(names[i], types[i]);
        return new ReaderFromList(header, new List<object[]> { new object[] { value } });
    }
}
