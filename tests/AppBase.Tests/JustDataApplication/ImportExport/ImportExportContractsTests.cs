using JustData.Application.Editor;
using JustData.Application.ImportExport;

namespace AppBase.Tests.JustDataApplication.ImportExport;

public sealed class ImportExportContractsTests
{
    [Fact]
    public void ImportRequest_defaults()
    {
        var docId = EditorDocumentId.New();
        var req = new ImportRequest(docId, @"C:\data.csv", ImportFormat.Csv);
        Assert.Equal(docId, req.DocumentId);
        Assert.Equal(@"C:\data.csv", req.SourcePath);
        Assert.Equal(ImportFormat.Csv, req.Format);
        Assert.Equal("", req.ConnectionName);
        Assert.Equal("", req.DatabaseName);
        Assert.Null(req.TargetTable);
        Assert.False(req.ImportToExisting);
        Assert.Equal(0, req.SkipRows);
        Assert.True(req.HasHeader);
    }

    [Fact]
    public void ImportRequest_with_all_fields()
    {
        var docId = EditorDocumentId.New();
        var req = new ImportRequest(docId, @"C:\data.xlsx", ImportFormat.Xlsx,
            "conn", "db", "target", true, 3, "Sheet1", "utf-8", ',', false);

        Assert.Equal("conn", req.ConnectionName);
        Assert.Equal("db", req.DatabaseName);
        Assert.Equal("target", req.TargetTable);
        Assert.True(req.ImportToExisting);
        Assert.Equal(3, req.SkipRows);
        Assert.Equal("Sheet1", req.SheetName);
        Assert.Equal("utf-8", req.EncodingName);
        Assert.Equal(',', req.Separator);
        Assert.False(req.HasHeader);
    }

    [Fact]
    public void ImportPreview_creates_correctly()
    {
        var preview = new ImportPreview(@"C:\data.csv", ImportFormat.Csv,
            new[] { "Sheet1" }, new[] { "col1", "col2" }, 1000, new[] { "Warning" });

        Assert.Equal(@"C:\data.csv", preview.SourcePath);
        Assert.Single(preview.SheetNames);
        Assert.Equal(2, preview.Headers.Count);
        Assert.Equal(1000, preview.EstimatedRows);
        Assert.Single(preview.Warnings!);
    }

    [Fact]
    public void ImportResult_creates_correctly()
    {
        var result = new ImportResult(100, 95, 5, new[] { "error1" }, "target_table", true);
        Assert.Equal(100, result.RowsRead);
        Assert.Equal(95, result.RowsImported);
        Assert.Equal(5, result.RowsSkipped);
        Assert.Single(result.Errors);
        Assert.Equal("target_table", result.TargetTable);
        Assert.True(result.IsPartial);
    }

    [Fact]
    public void ImportProgress_creates_with_stage()
    {
        var progress = new ImportProgress("Parsing", 50, 0, 0, null, false, null, null);
        Assert.Equal("Parsing", progress.Stage);
        Assert.Equal(50, progress.RowsRead);
        Assert.False(progress.IsCompleted);
    }

    [Fact]
    public void ImportProgress_completed()
    {
        var result = new ImportResult(100, 100, 0, [], "t", false);
        var progress = new ImportProgress("Done", 100, 100, 0, "Complete", true, result, null);
        Assert.True(progress.IsCompleted);
        Assert.NotNull(progress.Result);
        Assert.Equal(100, progress.RowsImported);
    }

    [Fact]
    public void ExportRequest_defaults()
    {
        var docId = EditorDocumentId.New();
        var req = new ExportRequest(docId, @"C:\out.csv", ExportFormat.Csv);
        Assert.Equal(docId, req.DocumentId);
        Assert.Equal(@"C:\out.csv", req.OutputPath);
        Assert.Equal(ExportFormat.Csv, req.Format);
        Assert.Null(req.ResultSetId);
        Assert.Null(req.ConnectionName);
        Assert.True(req.IncludeHeaders);
        Assert.False(req.IncludeSqlMetadata);
    }

    [Fact]
    public void ExportRequest_with_all_fields()
    {
        var docId = EditorDocumentId.New();
        var req = new ExportRequest(docId, @"C:\out.xlsx", ExportFormat.Xlsx,
            "rs1", "SELECT 1", "conn", false, true);
        Assert.Equal("rs1", req.ResultSetId);
        Assert.Equal("SELECT 1", req.SqlText);
        Assert.Equal("conn", req.ConnectionName);
        Assert.False(req.IncludeHeaders);
        Assert.True(req.IncludeSqlMetadata);
    }

    [Fact]
    public void ExportProgress_creates_correctly()
    {
        var progress = new ExportProgress("Writing", 500, null, false, null);
        Assert.Equal("Writing", progress.Stage);
        Assert.Equal(500, progress.RowsWritten);
        Assert.False(progress.IsCompleted);
    }

    [Fact]
    public void ExportProgress_with_error()
    {
        var progress = new ExportProgress("Error", 0, "Write failed", false, "Disk full");
        Assert.Equal("Write failed", progress.Message);
        Assert.Equal("Disk full", progress.ErrorMessage);
    }

    [Fact]
    public void ImportFormat_has_expected_values()
    {
        Assert.Equal(0, (int)ImportFormat.Csv);
        Assert.Equal(3, (int)ImportFormat.Clipboard);
        Assert.Equal(4, (int)ImportFormat.NetezzaXmlSpreadsheet);
    }

    [Fact]
    public void ExportFormat_has_expected_values()
    {
        Assert.Equal(0, (int)ExportFormat.Csv);
        Assert.Equal(1, (int)ExportFormat.Xlsx);
        Assert.Equal(2, (int)ExportFormat.Xlsb);
    }
}
