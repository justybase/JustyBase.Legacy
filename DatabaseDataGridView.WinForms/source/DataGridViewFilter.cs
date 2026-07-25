using System.Data;
using System.Text.RegularExpressions;

namespace DatabaseDataGridView.WinForms
{
    public class DataGridViewFilter
    {
        private readonly DataTable _currentDataTable;
        private readonly List<object[]> _originalDataList;
        private TypeCode[] _typesOfFields = [];

        public DataGridViewFilter(DataTable currentDataTable, List<object[]> originalDataList)
        {
            _currentDataTable = currentDataTable;
            _originalDataList = originalDataList;
            InitializeFieldTypes();
        }

        private void InitializeFieldTypes()
        {
            if (_typesOfFields.Length != _currentDataTable.Columns.Count)
            {
                var tmpList = new List<TypeCode>();
                for (int j = 0; j < _currentDataTable.Columns.Count; j++)
                {
                    var dt = _currentDataTable.Columns[j].DataType;
                    if (dt == typeof(Memory<byte>))
                    {
                        tmpList.Add(Type.GetTypeCode(typeof(string)));
                    }
                    else
                    {
                        tmpList.Add(Type.GetTypeCode(_currentDataTable.Columns[j].DataType));
                    }
                }
                _typesOfFields = tmpList.ToArray();
            }
        }

        public List<object[]> Filter(string fullText, bool fullLikeMode, Dictionary<int, (object? filterValue, FilterType filterType)> standardFilterDict,
            bool addRootGroupRowsOnly, List<object[]> groupByRows, int groupingLvlIndex, List<int> groupByColumnNums)
        {
            var newList = new List<object[]>();

            if (addRootGroupRowsOnly)
            {
                foreach (var item in groupByRows)
                {
                    if (((int?)item[groupingLvlIndex] ?? 0) == groupByColumnNums.Count)
                    {
                        newList.Add(item);
                    }
                }
                return newList;
            }

            if (standardFilterDict.Count == 0 && string.IsNullOrWhiteSpace(fullText))
            {
                return new List<object[]>(_originalDataList);
            }

            bool isTextEmpty = string.IsNullOrWhiteSpace(fullText);
            var filterParsers = CreateFilterParsers(fullText);

            SpinLock spinLock = new SpinLock();
            int cnt = _originalDataList.Count;
            int fieldCnt = _currentDataTable.Columns.Count;

            var processChunk = (int start, int end) =>
            {
                bool advancedMode = fullText.StartsWith("##");
                DataTable? dt1 = null;
                if (advancedMode)
                {
                    dt1 = _currentDataTable.Clone();
                    int columnCnt = _currentDataTable.Columns.Count;
                    for (int i = start; i < end; i++)
                    {
                        dt1.Rows.Add(_originalDataList[i][0..columnCnt]);
                    }
                    try
                    {
                        dt1.Columns.Add("Expression125", typeof(bool), fullText[2..]);
                    }
                    catch (Exception)
                    {
                        advancedMode = false;
                    }
                }

                for (int i = start; i < end; i++)
                {
                    object[] row = _originalDataList[i];
                    bool matchFound = false;

                    if (advancedMode && dt1 != null)
                    {
                        matchFound = (dt1.Rows[i - start]["Expression125"] as bool?) == true;
                    }
                    else if (!isTextEmpty)
                    {
                        int maxField = Math.Min(fieldCnt, _typesOfFields.Length);
                        for (int j = 0; j < maxField; j++)
                        {
                            var itm = row.Length > j ? row[j] : null;
                            if (itm is null || itm == DBNull.Value) continue;

                            if (fullLikeMode)
                            {
                                if (itm.ToString()?.Contains(fullText, StringComparison.OrdinalIgnoreCase) == true)
                                {
                                    matchFound = true;
                                    break;
                                }
                            }
                            else
                            {
                                if (CheckValueMatch(itm, _typesOfFields[j], filterParsers))
                                {
                                    matchFound = true;
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        matchFound = true; // No text filter, so it's a match
                    }


                    if (matchFound)
                    {
                        bool isFullOk = true;
                        if (standardFilterDict.Count > 0)
                        {
                            foreach (var item in standardFilterDict)
                            {
                                if (!CheckOk(row[item.Key], item.Value))
                                {
                                    isFullOk = false;
                                    break;
                                }
                            }
                        }

                        if (isFullOk)
                        {
                            bool lockTaken = false;
                            spinLock.Enter(ref lockTaken);
                            newList.Add(row);
                            if (lockTaken) spinLock.Exit();
                        }
                    }
                }
            };

            int processorCount = Environment.ProcessorCount;
            var tasks = new Task[processorCount];
            int chunkSize = cnt / processorCount;

            for (int i = 0; i < processorCount; i++)
            {
                int start = i * chunkSize;
                int end = (i == processorCount - 1) ? cnt : start + chunkSize;
                tasks[i] = Task.Run(() => processChunk(start, end));
            }

            Task.WaitAll(tasks);

            return newList;
        }

        private Dictionary<TypeCode, object> CreateFilterParsers(string text)
        {
            var parsers = new Dictionary<TypeCode, object>();
            if (int.TryParse(text, out var parsedInt)) parsers[TypeCode.Int32] = parsedInt;
            if (long.TryParse(text, out var parsedLong)) parsers[TypeCode.Int64] = parsedLong;
            if (decimal.TryParse(text, out var parsedDecimal)) parsers[TypeCode.Decimal] = parsedDecimal;
            if (DateTime.TryParse(text, out var parsedDateTime)) parsers[TypeCode.DateTime] = parsedDateTime;
            if (float.TryParse(text, out var parsedSingle)) parsers[TypeCode.Single] = parsedSingle;
            if (double.TryParse(text, out var parsedDouble)) parsers[TypeCode.Double] = parsedDouble;
            if (bool.TryParse(text, out var parsedBool)) parsers[TypeCode.Boolean] = parsedBool;
            if (byte.TryParse(text, out var parsedByte)) parsers[TypeCode.Byte] = parsedByte;
            if (sbyte.TryParse(text, out var parsedSByte)) parsers[TypeCode.SByte] = parsedSByte;
            if (short.TryParse(text, out var parsedInt16)) parsers[TypeCode.Int16] = parsedInt16;
            if (ushort.TryParse(text, out var parsedUInt16)) parsers[TypeCode.UInt16] = parsedUInt16;
            if (uint.TryParse(text, out var parsedUInt32)) parsers[TypeCode.UInt32] = parsedUInt32;
            if (ulong.TryParse(text, out var parsedUInt64)) parsers[TypeCode.UInt64] = parsedUInt64;
            if (char.TryParse(text, out var parsedChar)) parsers[TypeCode.Char] = parsedChar;
            parsers[TypeCode.String] = text;
            return parsers;
        }

        private bool CheckValueMatch(object value, TypeCode typeCode, Dictionary<TypeCode, object> parsers)
        {
            if (!parsers.ContainsKey(typeCode)) return false;

            object parsedValue = parsers[typeCode];
            if (typeCode == TypeCode.String)
            {
                return Convert.ToString(value)?.Contains(Convert.ToString(parsedValue) ?? string.Empty, StringComparison.OrdinalIgnoreCase) == true;
            }
            return value.Equals(parsedValue);
        }

        private static bool CheckOk(object? objectVal, (object? filterValue, FilterType filterType) value1)
        {
            if (value1.filterType == FilterType.isNull) return objectVal is null || objectVal is DBNull;
            if (value1.filterType == FilterType.isNotNull) return objectVal is not null && objectVal is not DBNull;
            if (objectVal is null || objectVal is DBNull) return false;

            if (value1.filterType == FilterType.inn)
            {
                return value1.filterValue switch
                {
                    HashSet<int> intSet => intSet.Contains((int)objectVal),
                    HashSet<long> longSet => longSet.Contains((long)objectVal),
                    HashSet<decimal> decimalSet => decimalSet.Contains((decimal)objectVal),
                    HashSet<string> stringSet => stringSet.Contains((string)objectVal),
                    HashSet<DateTime> dateTimeSet => dateTimeSet.Contains((DateTime)objectVal),
                    HashSet<bool> boolSet => boolSet.Contains((bool)objectVal),
                    _ => false,
                };
            }

            if (value1.filterType == FilterType.regex && value1.filterValue is Regex rx && objectVal is string stringVal2)
            {
                return rx.IsMatch(stringVal2);
            }

            if (value1.filterValue is null || objectVal.GetType() != value1.filterValue.GetType()) return false;

            if (value1.filterType == FilterType.equals) return value1.filterValue.Equals(objectVal);

            if (value1.filterType == FilterType.like && value1.filterValue is string stringVal)
            {
                return ((string)objectVal).Contains(stringVal, StringComparison.OrdinalIgnoreCase);
            }

            if (value1.filterValue is IComparable com)
            {
                int comparison = com.CompareTo(objectVal);
                return value1.filterType switch
                {
                    FilterType.greater => comparison < 0,
                    FilterType.greaterOrEqual => comparison <= 0,
                    FilterType.less => comparison > 0,
                    FilterType.lessOrEqual => comparison >= 0,
                    FilterType.notEqual => comparison != 0,
                    _ => false,
                };
            }

            return false;
        }
    }
}
