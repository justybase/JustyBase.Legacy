using JustyBase.ImportExport.Export;
using System.Text;

namespace JustyBaseLegacy.UI.ImportExport;

internal static class CsvExportSettings
{
    public static Encoding ResolveEncoding(string? encodingName) => ExportEncodingResolver.Resolve(encodingName);

    public static string ResolveNewLine(string? value) => ExportEncodingResolver.ResolveNewLine(value);
}
