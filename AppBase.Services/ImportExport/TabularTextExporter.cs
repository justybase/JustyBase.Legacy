using AppBase.Common.JsonContext;
using System.Data;
using System.Globalization;
using System.Text.Json;

namespace AppBase.Services;

/// <summary>
/// Database- and UI-independent formatting used by text exports.
/// </summary>
internal static class TabularTextExporter
{
    internal static string EscapeCsvField(string? value, char delimiter)
    {
        value ??= string.Empty;
        bool requiresQuotes = value.Contains(delimiter)
            || value.Contains('"')
            || value.Contains('\r')
            || value.Contains('\n');

        if (!requiresQuotes)
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }

    internal static long WriteCsv(
        TextWriter writer,
        IDataReader reader,
        char delimiter,
        string newLine,
        bool includeHeader,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(reader);
        ValidateNewLine(newLine);

        if (includeHeader)
        {
            WriteCsvRow(writer, Enumerable.Range(0, reader.FieldCount).Select(reader.GetName), delimiter, newLine);
        }

        long rowCount = 0;
        while (reader.Read())
        {
            cancellationToken.ThrowIfCancellationRequested();
            IEnumerable<string?> values = Enumerable.Range(0, reader.FieldCount)
                .Select(index => reader.IsDBNull(index)
                    ? null
                    : Convert.ToString(reader.GetValue(index), CultureInfo.InvariantCulture));
            WriteCsvRow(writer, values, delimiter, newLine);
            rowCount++;
        }

        return rowCount;
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
                    : Convert.ToString(reader.GetValue(index), CultureInfo.InvariantCulture);
            }

            writer.Write(JsonSerializer.Serialize(values, MyJsonContextStringArray.Default.StringArray));
            rowCount++;
        }
        writer.Write(']');

        return rowCount;
    }

    private static void WriteCsvRow(
        TextWriter writer,
        IEnumerable<string?> values,
        char delimiter,
        string newLine)
    {
        bool first = true;
        foreach (string? value in values)
        {
            if (!first)
            {
                writer.Write(delimiter);
            }

            writer.Write(EscapeCsvField(value, delimiter));
            first = false;
        }
        writer.Write(newLine);
    }

    private static void ValidateNewLine(string newLine)
    {
        if (newLine is not ("\r" or "\n" or "\r\n"))
        {
            throw new ArgumentException("Newline must be CR, LF, or CRLF.", nameof(newLine));
        }
    }
}
