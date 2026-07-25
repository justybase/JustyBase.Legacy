using System.Text;

namespace JustyBaseLegacy.UI.ImportExport;

internal static class CsvExportSettings
{
    public static Encoding ResolveEncoding(string? encodingName)
    {
        string configured = string.IsNullOrWhiteSpace(encodingName) ? "utf-8" : encodingName.Trim();
        if (configured.Equals("utf-8", StringComparison.OrdinalIgnoreCase)
            || configured.Equals("utf8", StringComparison.OrdinalIgnoreCase))
            return Encoding.UTF8;
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return int.TryParse(configured, out int codePage) ? Encoding.GetEncoding(codePage) : Encoding.GetEncoding(configured);
    }

    public static string ResolveNewLine(string? value) => string.IsNullOrEmpty(value)
        ? Environment.NewLine
        : value.Replace("\\r", "\r", StringComparison.Ordinal).Replace("\\n", "\n", StringComparison.Ordinal);
}
