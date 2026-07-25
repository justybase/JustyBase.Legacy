using System.Text.RegularExpressions;

namespace DatabaseDataGridView.WinForms;

public class DataFilter
{
    private readonly List<object> _valuesInFilter;
    private readonly Func<string, object?, FilterType, bool, Task> _onSearch;
    private readonly string _formName;

    public DataFilter(string formName, List<object> valuesInFilter, Func<string, object?, FilterType, bool, Task> onSearch)
    {
        _formName = formName;
        _valuesInFilter = valuesInFilter;
        _onSearch = onSearch;
    }

    private static (FilterType filterType, string value) ParseFilterOperator(string inputTxt)
    {
        if (inputTxt.StartsWith(">=") && inputTxt.Length > 2) return (FilterType.greaterOrEqual, inputTxt[2..]);
        if (inputTxt.StartsWith("<=") && inputTxt.Length > 2) return (FilterType.lessOrEqual, inputTxt[2..]);
        if (inputTxt.StartsWith("!=") && inputTxt.Length > 2) return (FilterType.notEqual, inputTxt[2..]);
        if (inputTxt.StartsWith("<>") && inputTxt.Length > 2) return (FilterType.notEqual, inputTxt[2..]);
        if (inputTxt.StartsWith('=') && inputTxt.Length > 1) return (FilterType.equals, inputTxt[1..]);
        if (inputTxt.StartsWith('>') && inputTxt.Length > 1) return (FilterType.greater, inputTxt[1..]);
        if (inputTxt.StartsWith('<') && inputTxt.Length > 1) return (FilterType.less, inputTxt[1..]);
        return (FilterType.equals, inputTxt);
    }

    public void ApplyFilter(string inputTxt, bool forceEqual)
    {
        if (inputTxt.StartsWith("match ") && inputTxt.Length > 6)
        {
            try
            {
                var rx = new Regex(inputTxt[6..], RegexOptions.IgnoreCase);
                _onSearch?.Invoke(_formName, rx, FilterType.regex, false);
                return;
            }
            catch (Exception) { /* ignore invalid regex */ }
        }

        if (_valuesInFilter.Count == 0) return;

        var firstValueType = _valuesInFilter[0].GetType();

        if (firstValueType == typeof(string))
        {
            _onSearch?.Invoke(_formName, inputTxt, forceEqual ? FilterType.equals : FilterType.like, false);
            return;
        }

        var (filterType, valueStr) = ParseFilterOperator(inputTxt);
        if (forceEqual)
        {
            filterType = FilterType.equals;
            valueStr = inputTxt;
        }

        switch (Type.GetTypeCode(firstValueType))
        {
            case TypeCode.Int32:
                if (int.TryParse(valueStr, out var intVal)) _onSearch?.Invoke(_formName, intVal, filterType, false);
                break;
            case TypeCode.Int64:
                if (long.TryParse(valueStr, out var longVal)) _onSearch?.Invoke(_formName, longVal, filterType, false);
                break;
            case TypeCode.SByte:
                if (sbyte.TryParse(valueStr, out var sbyteVal)) _onSearch?.Invoke(_formName, sbyteVal, filterType, false);
                break;
            case TypeCode.Decimal:
                if (decimal.TryParse(valueStr, out var decimalVal)) _onSearch?.Invoke(_formName, decimalVal, filterType, false);
                break;
            case TypeCode.Double:
                if (double.TryParse(valueStr, out var doubleVal)) _onSearch?.Invoke(_formName, doubleVal, filterType, false);
                break;
            case TypeCode.Single:
                if (Single.TryParse(valueStr, out var singleVal)) _onSearch?.Invoke(_formName, singleVal, filterType, false);
                break;
            case TypeCode.Byte:
                if (byte.TryParse(valueStr, out var byteVal)) _onSearch?.Invoke(_formName, byteVal, filterType, false);
                break;
            case TypeCode.Int16:
                if (Int16.TryParse(valueStr, out var int16Val)) _onSearch?.Invoke(_formName, int16Val, filterType, false);
                break;
            case TypeCode.Boolean:
                if (bool.TryParse(valueStr, out var boolVal)) _onSearch?.Invoke(_formName, boolVal, filterType, false);
                break;
            case TypeCode.DateTime:
                if (DateTime.TryParse(valueStr, out var dateTimeVal)) _onSearch?.Invoke(_formName, dateTimeVal, filterType, false);
                break;
        }
    }
}
