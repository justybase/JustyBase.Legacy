using SpreadSheetTasks;
using Sylvan.Data.Csv;
using System.Buffers;
using System.IO.Compression;
using System.Text;

namespace AppBase.Services;

public sealed class CsvReader : ExcelReaderAbstract
{
    private string _filePath;
    public string FilePath => _filePath;

    //CsvDataReaderZ dbReader;
    private CsvDataReader csvReader;
    private StreamReader _streamReader;
    private bool _isBrotli = false;
    public bool IsBrotli => _isBrotli;
    public CsvReader(bool isBrotli = false)
    {
        _isBrotli = isBrotli;
    }

    FileStream _originalFileStream;
    public override void Open(string path, bool readSharedStrings = true, bool updateMode = false, Encoding encoding = null)
    {
        if (_isBrotli)
        {
            _originalFileStream = File.OpenRead(path);
            var br = new BrotliStream(new BufferedStream(_originalFileStream), CompressionMode.Decompress);
            _streamReader = new StreamReader(br);
        }
        else
        {
            _streamReader = new StreamReader(path);
        }

        csvReader = CsvDataReader.Create(_streamReader);
        _filePath = path;

        FieldCount = csvReader.FieldCount;
        innerRow = new FieldInfo[FieldCount];
        decimalVals = new decimal[FieldCount];
        isDecimalArray = new bool[FieldCount];
        for (int i = 0; i < FieldCount; i++)
        {
            innerRow[i].type = ExcelDataType.String;
            innerRow[i].strValue = csvReader.GetName(i);
        }
    }

    public override string[] GetSheetNames()
    {
        return new string[] { Path.GetFileName(_filePath).Replace('.', '_') };
    }
    public bool TransformValuesAutomaticly { get; set; } = true;
    public override bool Read()
    {
        var innerReaderRead = csvReader.Read();
        if (innerReaderRead)
        {
            for (int i = 0; i < csvReader.FieldCount; i++)
            {
                if (TransformValuesAutomaticly)
                {
                    TransFromSpanValue(i);
                }
            }
        }
        return innerReaderRead;
    }

    private bool[] isDecimalArray = null;
    private decimal[] decimalVals = null;

    private SearchValues<char> searchValues = SearchValues.Create(",.E");
    public void TransFromSpanValue(int i)
    {
        var strVal = csvReader.GetFieldSpan(i);
        innerRow[i].type = ExcelDataType.Null;
        if (strVal.Length == 0)
        {
            innerRow[i].type = ExcelDataType.Null;
        }
        else if ((strVal[0] == '-' || Char.IsDigit(strVal[0])) && strVal.Length < 40 && strVal.ContainsAny(searchValues)
                && (decimal.TryParse(strVal, out decimal decimalRes)
                || decimal.TryParse(strVal, System.Globalization.NumberStyles.Any, ExcelReaderAbstract.invariantCultureInfo, out decimalRes)
                )
            )
        {
            innerRow[i].type = ExcelDataType.Double;
            innerRow[i].doubleValue = (double)decimalRes;//forLengthDetection in FullScanExcelReader
            isDecimalArray[i] = true;
            decimalVals[i] = decimalRes;
        }
        else if (strVal.Length < 20 && Int64.TryParse(strVal, out Int64 int64Val))
        {
            innerRow[i].type = ExcelDataType.Int64;
            innerRow[i].int64Value = int64Val;
        }
        else if (DateTime.TryParse(strVal, out DateTime datetimeVal))
        {
            innerRow[i].type = ExcelDataType.DateTime;
            innerRow[i].dtValue = datetimeVal;
        }
        else
        {
            innerRow[i].type = ExcelDataType.String;
            innerRow[i].strValue = strVal.ToString();
        }
    }

    public int GetSpanLength(int i)
    {
        return csvReader.GetFieldSpan(i).Length;
    }

    public decimal GetDecimal(int i) => decimalVals[i];
    public bool IsDecimal(int i) => isDecimalArray[i] == true;

    public override void Dispose()
    {
        csvReader?.Dispose();
        _streamReader.Dispose();
        //throw new NotImplementedException();
    }

    public override double RelativePositionInStream()
    {
        if (_streamReader.BaseStream.CanSeek)
        {
            return (double)_streamReader.BaseStream.Position / _streamReader.BaseStream.Length;
        }
        if (IsBrotli)
        {
            return (double)_originalFileStream.Position / _originalFileStream.Length;
        }
        return 0.5;
    }
}
