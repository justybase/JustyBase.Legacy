using AppBase.Services;

namespace AppBase.Tests.ImportExport;

public sealed class CsvReaderExtendedTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "JustyBaseLegacy.Tests", Guid.NewGuid().ToString("N"));

    public CsvReaderExtendedTests() => Directory.CreateDirectory(_directory);

    // ── Empty / edge cases ──

    [Fact]
    public void Read_empty_csv_with_only_headers()
    {
        var reader = CreateAndOpen("id,name\n");
        Assert.Equal(2, reader.FieldCount);
        Assert.False(reader.Read());
        reader.Dispose();
    }

    [Fact]
    public void Read_csv_with_empty_values()
    {
        var reader = CreateAndOpen("a,b,c\n1,,3\n");
        Assert.True(reader.Read());
        // Empty value has span length 0
        Assert.True(reader.GetSpanLength(0) > 0); // "1"
        Assert.Equal(0, reader.GetSpanLength(1));  // empty
        Assert.True(reader.GetSpanLength(2) > 0); // "3"
        Assert.False(reader.IsDecimal(1)); // empty is not a decimal
        reader.Dispose();
    }

    [Fact]
    public void Read_csv_with_whitespace_values()
    {
        var reader = CreateAndOpen("a,b\n  ,x\n");
        Assert.True(reader.Read());
        // Whitespace has length > 0 (not empty)
        Assert.True(reader.GetSpanLength(0) > 0);
        reader.Dispose();
    }

    [Fact]
    public void Read_csv_with_trailing_newlines()
    {
        var reader = CreateAndOpen("id\n42\n");
        Assert.True(reader.Read());
        Assert.Equal(2, reader.GetSpanLength(0)); // "42" length = 2
        Assert.False(reader.Read()); // trailing newline does not create empty row
        reader.Dispose();
    }

    // ── Quoted values ──

    [Fact]
    public void Read_csv_with_quoted_value_containing_comma()
    {
        var reader = CreateAndOpen("name,desc\n\"hello, world\",test\n");
        Assert.True(reader.Read());
        // Quoted value with comma - length should include the full value
        Assert.Equal(12, reader.GetSpanLength(0)); // "hello, world" = 12
        Assert.False(reader.Read());
        reader.Dispose();
    }

    [Fact]
    public void Read_csv_with_escaped_quotes()
    {
        var reader = CreateAndOpen("text\n\"say \"\"hello\"\"\"\n");
        Assert.True(reader.Read());
        // Escaped quotes - length of actual value "say \"hello\""
        Assert.True(reader.GetSpanLength(0) > 0);
        reader.Dispose();
    }

    // ── Number type inference ──

    [Fact]
    public void Read_csv_infers_int()
    {
        var reader = CreateAndOpen("val\n42\n");
        Assert.True(reader.Read());
        // 42 is Int64 (no decimal point)
        Assert.False(reader.IsDecimal(0));
        Assert.Equal(2, reader.GetSpanLength(0));
        reader.Dispose();
    }

    [Fact]
    public void Read_csv_infers_decimal()
    {
        var reader = CreateAndOpen("val\n3.14\n");
        Assert.True(reader.Read());
        Assert.True(reader.IsDecimal(0));
        Assert.Equal(3.14m, reader.GetDecimal(0));
        reader.Dispose();
    }

    [Fact]
    public void Read_csv_infers_negative_decimal()
    {
        var reader = CreateAndOpen("val\n-2.5\n");
        Assert.True(reader.Read());
        Assert.True(reader.IsDecimal(0));
        Assert.Equal(-2.5m, reader.GetDecimal(0));
        reader.Dispose();
    }

    [Fact]
    public void Read_csv_infers_scientific_notation()
    {
        var reader = CreateAndOpen("val\n1.5E2\n");
        Assert.True(reader.Read());
        Assert.True(reader.IsDecimal(0));
        Assert.Equal(150m, reader.GetDecimal(0));
        reader.Dispose();
    }

    [Fact]
    public void Read_csv_large_number_beyond_int64_is_not_decimal()
    {
        // 21 digits can't fit in Int64, no decimal point → stays as string
        var reader = CreateAndOpen("val\n123456789012345678901\n");
        Assert.True(reader.Read());
        Assert.False(reader.IsDecimal(0)); // not decimal (no . or E)
        Assert.Equal(21, reader.GetSpanLength(0));
        reader.Dispose();
    }

    [Fact]
    public void Read_csv_int64_max_value()
    {
        var reader = CreateAndOpen("val\n9223372036854775807\n"); // Int64.MaxValue
        Assert.True(reader.Read());
        Assert.False(reader.IsDecimal(0)); // Int64, not decimal
        Assert.Equal(19, reader.GetSpanLength(0));
        reader.Dispose();
    }

    [Fact]
    public void Read_csv_int64_min_value()
    {
        var reader = CreateAndOpen("val\n-9223372036854775808\n"); // Int64.MinValue
        Assert.True(reader.Read());
        Assert.False(reader.IsDecimal(0)); // Int64, not decimal
        reader.Dispose();
    }

    [Fact]
    public void Read_csv_simple_int_is_not_decimal()
    {
        var reader = CreateAndOpen("val\n1000\n");
        Assert.True(reader.Read());
        // Simple int without decimal point is Int64, not decimal
        Assert.False(reader.IsDecimal(0));
        Assert.Equal(4, reader.GetSpanLength(0));
        reader.Dispose();
    }

    // ── Date type inference ──

    [Fact]
    public void Read_csv_infers_date_iso_format()
    {
        var reader = CreateAndOpen("val\n2026-01-15\n");
        Assert.True(reader.Read());
        // ISO date - should be DateTime (not decimal, not int64)
        Assert.False(reader.IsDecimal(0));
        reader.Dispose();
    }

    [Fact]
    public void Read_csv_infers_date_us_format()
    {
        var reader = CreateAndOpen("val\n01/15/2026\n");
        Assert.True(reader.Read());
        Assert.False(reader.IsDecimal(0));
        reader.Dispose();
    }

    [Fact]
    public void Read_csv_infers_datetime_with_time()
    {
        var reader = CreateAndOpen("val\n2026-01-15 14:30:00\n");
        Assert.True(reader.Read());
        Assert.False(reader.IsDecimal(0));
        reader.Dispose();
    }

    // ── Unicode ──

    [Fact]
    public void Read_csv_with_unicode_values()
    {
        var reader = CreateAndOpen("name,city\nZażółć,Wrocław\n");
        Assert.True(reader.Read());
        Assert.True(reader.GetSpanLength(0) > 0);
        Assert.True(reader.GetSpanLength(1) > 0);
        reader.Dispose();
    }

    // ── TransformValuesAutomaticly = false ──

    [Fact]
    public void Read_csv_with_transform_off_still_reads_rows()
    {
        var reader = new CsvReader();
        reader.TransformValuesAutomaticly = false;
        string path = Write("transform_off.csv", "val\n42\n3.14\nhello\n");
        reader.Open(path);

        Assert.True(reader.Read()); // "42"
        Assert.False(reader.IsDecimal(0)); // no transformation, default is false

        Assert.True(reader.Read()); // "3.14"
        Assert.False(reader.IsDecimal(0));

        Assert.True(reader.Read()); // "hello"
        Assert.False(reader.Read()); // no more rows
        reader.Dispose();
    }

    // ── Multiple rows ──

    [Fact]
    public void Read_csv_multiple_rows_all_types()
    {
        var reader = CreateAndOpen("id,amount,created,name\n1,10.5,2026-01-01,alpha\n2,20.5,2026-02-01,beta\n3,30.5,2026-03-01,gamma\n");

        Assert.True(reader.Read());
        Assert.False(reader.IsDecimal(0)); // "1" is Int64
        Assert.True(reader.IsDecimal(1));  // "10.5" is decimal
        Assert.Equal(10.5m, reader.GetDecimal(1));

        Assert.True(reader.Read());
        Assert.True(reader.IsDecimal(1));
        Assert.Equal(20.5m, reader.GetDecimal(1));

        Assert.True(reader.Read());
        Assert.True(reader.IsDecimal(1));
        Assert.Equal(30.5m, reader.GetDecimal(1));

        Assert.False(reader.Read());
        reader.Dispose();
    }

    // ── Sheet names ──

    [Fact]
    public void GetSheetNames_replaces_dots_and_slashes()
    {
        var reader = CreateAndOpen("a\n1\n", "my.data.file.csv");
        var names = reader.GetSheetNames();
        Assert.Single(names);
        Assert.Equal("my_data_file_csv", names[0]);
        reader.Dispose();
    }

    // ── Booleans as strings ──

    [Fact]
    public void Read_csv_boolean_values_are_not_numeric()
    {
        var reader = CreateAndOpen("val\ntrue\nfalse\n");
        Assert.True(reader.Read());
        Assert.False(reader.IsDecimal(0)); // not numeric
        Assert.True(reader.Read());
        // Can't get string value directly, but can verify length
        Assert.Equal(5, reader.GetSpanLength(0)); // "false" = 5 chars
        reader.Dispose();
    }

    // ── Helpers ──

    private CsvReader CreateAndOpen(string content, string fileName = "test.csv")
    {
        var reader = new CsvReader();
        string path = Write(fileName, content);
        reader.Open(path);
        return reader;
    }

    private string Write(string name, string content)
    {
        string path = Path.Combine(_directory, name);
        File.WriteAllText(path, content);
        return path;
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }
}
