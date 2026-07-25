using JustData.Application.Editor;

namespace JustData.Application.ImportExport;

public enum ImportFormat
{
    Csv,
    Xlsx,
    Xlsb,
    Clipboard,
    NetezzaXmlSpreadsheet
}

public sealed record ImportRequest(
    EditorDocumentId? DocumentId,
    string SourcePath,
    ImportFormat Format,
    string ConnectionName = "",
    string DatabaseName = "",
    string? TargetTable = null,
    bool ImportToExisting = false,
    int SkipRows = 0,
    string? SheetName = null,
    string? EncodingName = null,
    char? Separator = null,
    bool HasHeader = true);

public sealed record ImportPreview(
    string SourcePath,
    ImportFormat Format,
    IReadOnlyList<string> SheetNames,
    IReadOnlyList<string> Headers,
    long EstimatedRows,
    IReadOnlyList<string>? Warnings = null);

public sealed record ImportResult(
    long RowsRead,
    long RowsImported,
    long RowsSkipped,
    IReadOnlyList<string> Errors,
    string? TargetTable = null,
    bool IsPartial = false);

public sealed record ImportProgress(
    string Stage,
    long RowsRead = 0,
    long RowsImported = 0,
    long RowsSkipped = 0,
    string? Message = null,
    bool IsCompleted = false,
    ImportResult? Result = null,
    string? ErrorMessage = null);

public enum ExportFormat
{
    Csv,
    Xlsx,
    Xlsb
}

public sealed record ExportRequest(
    EditorDocumentId DocumentId,
    string OutputPath,
    ExportFormat Format,
    string? ResultSetId = null,
    string? SqlText = null,
    string? ConnectionName = null,
    bool IncludeHeaders = true,
    bool IncludeSqlMetadata = false);

public sealed record ExportProgress(
    string Stage,
    long RowsWritten = 0,
    string? Message = null,
    bool IsCompleted = false,
    string? ErrorMessage = null);

public interface IImportUseCase
{
    Task<ImportPreview> PreviewAsync(
        ImportRequest request,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<ImportProgress> ImportAsync(
        ImportRequest request,
        CancellationToken cancellationToken = default);
}

public interface IResultExportUseCase
{
    IAsyncEnumerable<ExportProgress> ExportAsync(
        ExportRequest request,
        CancellationToken cancellationToken = default);
}
