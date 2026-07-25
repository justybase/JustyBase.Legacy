using AppBase.Common;
using System.Data;
using System.Diagnostics;

namespace AppBase.Services;

public sealed class DataFuncs : IDataFuncs
{
    public static IDataFuncs Default { get; } = new DataFuncs();

    public DataTable GetDataTable(IDataReader rdr, int l = 1, Action<string>? onErrorMessage = null)
    {
        DataTable gridResultsDataTable = new DataTable();
        gridResultsDataTable.TableName = $"tab{l}";
        Dictionary<string, int> nameOccurrences = new Dictionary<string, int>();
        //determine columns

        for (int i = 0; i < rdr.FieldCount; i++)
        {
            string columnName = rdr.GetName(i);

            if (nameOccurrences.ContainsKey(columnName))
            {
                nameOccurrences[columnName] += 1;
            }
            else
            {
                nameOccurrences[columnName] = 1;
            }

            if (nameOccurrences[columnName] == 1)
            {
                try
                {
                    if (!columnName.IsGoodName())
                    {
                        columnName = $"\"{columnName}\"";
                    }
                    string dtName = rdr.GetDataTypeName(i);


                    if (dtName == "interval")
                    {
                        gridResultsDataTable.Columns.Add(columnName, typeof(string));
                    }
                    else
                    {
                        gridResultsDataTable.Columns.Add(columnName, rdr.GetFieldType(i));
                    }
                }
                catch (Exception ex)
                {
                    var message = $"to show real data please cast {columnName} to nvarchar: {ex.Message}";
                    onErrorMessage?.Invoke(message);
                    System.Diagnostics.Trace.WriteLine(message);
                    gridResultsDataTable.Columns.Add(columnName, typeof(string));
                }
            }
            else
            {
                string colName2 = $"{columnName}_{nameOccurrences[columnName]}";
                while (nameOccurrences.ContainsKey(colName2))
                {
                    colName2 += "_";
                }

                if (nameOccurrences.ContainsKey(colName2))
                {
                    nameOccurrences[colName2] += 1;
                }
                else
                {
                    nameOccurrences[colName2] = 1;
                }
                if (!colName2.IsGoodName())
                {
                    colName2 = $"\"{colName2}\"";
                }
                gridResultsDataTable.Columns.Add(colName2, rdr.GetFieldType(i));
            }
        }

        return gridResultsDataTable;
    }
}
