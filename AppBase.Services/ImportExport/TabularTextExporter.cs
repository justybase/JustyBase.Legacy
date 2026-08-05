using JustyBase.ImportExport.Export;
using System.Data;

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
        return JsonExportWriter.WriteFromDataReader(writer, reader, cancellationToken);
    }
}
