using AppBase.Common;
using System.Data;
using System.Diagnostics;

namespace AppBase.Services;

public sealed class DataFuncService : IDataFuncService
{
    public DataTable GetDataTable(IDataReader rdr, int l = 1)
    {
        var dtResultsForGrid = new DataTable($"tab{l}");

        for (int i = 0; i < rdr.FieldCount; i++)
        {
            string originalName = rdr.GetName(i);
            string uniqueName = originalName;
            int suffix = 2;

            string finalColumnName = uniqueName.IsGoodName() ? uniqueName : $"\"{uniqueName}\"";

            while (dtResultsForGrid.Columns.Contains(finalColumnName))
            {
                uniqueName = $"{originalName}_{suffix++}";
                finalColumnName = uniqueName.IsGoodName() ? uniqueName : $"\"{uniqueName}\"";
            }

            Type columnType;
            try
            {
                if (rdr.GetDataTypeName(i).Equals("interval", StringComparison.OrdinalIgnoreCase))
                {
                    columnType = typeof(string);
                }
                else
                {
                    columnType = rdr.GetFieldType(i);
                }
            }
            catch (Exception ex)
            {
                // Fallback to string if type resolution fails.
                // Replacing MessageBox with a debug message to remove UI dependency from the service layer.
                System.Diagnostics.Trace.WriteLine($"Failed to determine data type for column '{originalName}'. Defaulting to string. Exception: {ex.Message}");
                columnType = typeof(string);
            }

            dtResultsForGrid.Columns.Add(finalColumnName, columnType);
        }

        return dtResultsForGrid;
    }
}
