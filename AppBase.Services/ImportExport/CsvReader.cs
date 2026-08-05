using JustyBase.ImportExport.Import;
using SpreadSheetTasks;
using System.Text;

namespace AppBase.Services;

/// <summary>
/// Host adapter over the shared <see cref="CsvRowReader"/>. Preserves the Legacy
/// <c>ExcelReaderAbstract</c> facade while gaining the shared typed-cell superset
/// (bool detection, <c>TreatAllColumnsAsText</c>, Pesel/Regon-as-text).
/// </summary>
public sealed class CsvReader(bool isBrotli = false) : ExcelReaderAbstract
{
    private readonly CsvRowReader _inner = new(isBrotli ? CsvCompression.Brotli : CsvCompression.None);
    private string? _filePath;
    public string FilePath => _filePath!;
    public bool IsBrotli => isBrotli;

    private decimal[]? decimalVals;
    private bool[]? isDecimalArray;

    public override void Open(string path, bool readSharedStrings = true, bool updateMode = false, Encoding encoding = null)
    {
        _inner.TreatAllColumnsAsText = TreatAllColumnsAsText;
        _inner.Open(path);
        _filePath = path;

        FieldCount = _inner.FieldCount;
        innerRow = new FieldInfo[FieldCount];
        decimalVals = new decimal[FieldCount];
        isDecimalArray = new bool[FieldCount];
        for (int i = 0; i < FieldCount; i++)
        {
            innerRow[i].type = ExcelDataType.String;
            innerRow[i].strValue = _inner.GetName(i);
        }
    }

    public override string[] GetSheetNames() => [Path.GetFileName(_filePath).Replace('.', '_')];

    public bool TransformValuesAutomaticly { get; set; } = true;

    public override bool Read()
    {
        bool innerReaderRead = _inner.Read();
        if (innerReaderRead && TransformValuesAutomaticly)
        {
            for (int i = 0; i < _inner.FieldCount; i++)
                TransFromSpanValue(i);
        }
        return innerReaderRead;
    }

    public void TransFromSpanValue(int i)
    {
        CsvCell cell = _inner.InferCell(i);
        ref var w = ref innerRow[i];
        switch (cell.Kind)
        {
            case CsvCellKind.Null:
                w.type = ExcelDataType.Null;
                break;
            case CsvCellKind.String:
                w.type = ExcelDataType.String;
                w.strValue = cell.StringValue;
                break;
            case CsvCellKind.Double:
                w.type = ExcelDataType.Double;
                w.doubleValue = (double)cell.DecimalValue;
                isDecimalArray![i] = true;
                decimalVals![i] = cell.DecimalValue;
                break;
            case CsvCellKind.Int64:
                w.type = ExcelDataType.Int64;
                w.int64Value = cell.Int64Value;
                break;
            case CsvCellKind.DateTime:
                w.type = ExcelDataType.DateTime;
                w.dtValue = cell.DateTimeValue;
                break;
            case CsvCellKind.Boolean:
                w.type = ExcelDataType.Boolean;
                w.boolValue = cell.BooleanValue;
                break;
        }
    }

    public int GetSpanLength(int i) => _inner.GetFieldLength(i);
    public decimal GetDecimal(int i) => decimalVals![i];
    public bool IsDecimal(int i) => isDecimalArray![i] == true;

    public override void Dispose() => _inner.Dispose();

    public override double RelativePositionInStream() => _inner.Position;
}