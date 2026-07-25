using AppBase.Services;
using System.IO.Compression;
using System.Text;

namespace AppBase.Tests.ImportExport;

public sealed class CsvReaderTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "JustyBaseLegacy.Tests", Guid.NewGuid().ToString("N"));

    public CsvReaderTests() => Directory.CreateDirectory(_directory);

    [Fact]
    public void OpenAndRead_InfersNumbersDatesAndStrings()
    {
        string path = Write("values.csv", "id,amount,created,label\n42,1.25,2026-01-02,hello\n");
        CsvReader reader = new();

        reader.Open(path);

        Assert.Equal(4, reader.FieldCount);
        Assert.Equal("values_csv", Assert.Single(reader.GetSheetNames()));
        Assert.True(reader.Read());
        Assert.True(reader.IsDecimal(1));
        Assert.Equal(1.25m, reader.GetDecimal(1));
        Assert.Equal(5, reader.GetSpanLength(3));
        Assert.False(reader.Read());
        reader.Dispose();
    }

    [Fact]
    public void Open_BrotliInput_ReadsCompressedCsv()
    {
        string path = Path.Combine(_directory, "compressed.csv.br");
        using (FileStream output = File.Create(path))
        using (BrotliStream compressed = new(output, CompressionLevel.SmallestSize))
        using (StreamWriter writer = new(compressed, Encoding.UTF8))
        {
            writer.Write("name\nvalue\n");
        }

        CsvReader reader = new(isBrotli: true);
        reader.Open(path);

        Assert.True(reader.IsBrotli);
        Assert.True(reader.Read());
        Assert.Equal("compressed_csv_br", Assert.Single(reader.GetSheetNames()));
        reader.Dispose();
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
