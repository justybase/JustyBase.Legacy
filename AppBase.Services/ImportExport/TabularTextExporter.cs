using AppBase.Common.JsonContext;
using JustyBase.ImportExport.Export;
using System.Data;
using System.Text.Json;

namespace AppBase.Services;

/// <summary>
/// Database- and UI-independent formatting used by text exports.
/// CSV escaping/writing delegates to <see cref="CsvExportWriter"/>.
/// </summary>
internal static class TabularTextExporter
{
    internal static string EscapeCsvField(string? value, char delimiter)
        => CsvExportWriter.Escape(value, delimiter);

    internal static long WriteCsv(
        TextWriter writer,
        IDataReader reader,
        char delimiter,
        string newLine,
        bool includeHeader,
        CancellationToken cancellationToken = default)
    {
        return CsvExportWriter.WriteFromDataReader(
            writer,
            reader,
            new ExportOptions(Delimiter: delimiter, NewLine: newLine, IncludeHeaders: includeHeader),
            cancellationToken: cancellationToken);
    }

    internal static long WriteJson(
        TextWriter writer,
        IDataReader reader,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(reader);

        long rowCount = 0;
        writer.Write('[');
        while (reader.Read())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (rowCount > 0)
            {
                writer.Write(',');
            }

            string[] values = new string[reader.FieldCount];
            for (int index = 0; index < values.Length; index++)
            {
                values[index] = reader.IsDBNull(index)
                    ? null
                    : Convert.ToString(reader.GetValue(index), System.Globalization.CultureInfo.InvariantCulture);
            }

            writer.Write(JsonSerializer.Serialize(values, MyJsonContextStringArray.Default.StringArray));
            rowCount++;
        }
        writer.Write(']');

        return rowCount;
    }
}
