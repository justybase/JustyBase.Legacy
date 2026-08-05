using AppBase.Common.Interfaces;
using AppBase.Common;
using DatabaseDataGridView.WinForms;
using JustData.Application.ImportExport;
using JustData.Application.Sql;
using System.Data;
using SpreadSheetTasks;
using ExportEncodingResolver = JustyBase.ImportExport.Export.ExportEncodingResolver;

namespace JustyBaseLegacy.UI.ImportExport;

/// <summary>Stateless bridge for grid/query export operations.</summary>
public sealed class WinFormsResultExportUseCase : IResultExportUseCase
{
    private readonly IDocumentResultGridRegistry _grids;
    private readonly IImportExportTasks _tasks;
    private readonly IApplicationSettingsContext _settings;

    public WinFormsResultExportUseCase(
        IDocumentResultGridRegistry grids,
        IImportExportTasks tasks,
        IApplicationSettingsContext settings)
    {
        _grids = grids;
        _tasks = tasks;
        _settings = settings;
    }

    public async IAsyncEnumerable<ExportProgress> ExportAsync(
        ExportRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ResultSetId)
            || !_grids.TryGet(new ResultSetKey(request.DocumentId, request.ResultSetId), out CustomDataGridView? grid)
            || grid is null || grid.IsDisposed || grid.CurrentDataTable is null)
        {
            yield return new ExportProgress(
                "failed",
                Message: "The selected result set is no longer available.",
                IsCompleted: true,
                ErrorMessage: "The selected result set is no longer available.");
            yield break;
        }
        if (string.IsNullOrWhiteSpace(request.OutputPath))
        {
            yield return new ExportProgress("failed", IsCompleted: true, ErrorMessage: "An output path is required.");
            yield break;
        }
        yield return new ExportProgress("starting", Message: $"Exporting to {request.OutputPath}...");
        long rows = 0;
        ExportProgress terminal;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (request.Format == ExportFormat.Csv)
            {
                using IDataReader reader = new ReaderFromList(grid.CurrentDataTable, grid.RowsList);
                await Task.Run(() => _tasks.ExportCSVReader(
                    ExportEncodingResolver.Resolve(_settings.Config.EncondingName), reader, request.OutputPath,
                    _settings.Config.SepInExportedCsv[0].ToString(), false,
                    ExportEncodingResolver.ResolveNewLine(_settings.Config.SepRowsInExportedCsv),
                    count => { cancellationToken.ThrowIfCancellationRequested(); rows = count; }, request.IncludeHeaders), cancellationToken);
            }
            else
            {
                await Task.Run(() =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    using IDataReader reader = new ReaderFromList(grid.CurrentDataTable, grid.RowsList);
                    using ExcelWriter writer = request.Format == ExportFormat.Xlsb ? new XlsbWriter(request.OutputPath) : new XlsxWriter(request.OutputPath);
                    writer.AddSheet("Sheet"); writer.WriteSheet(reader, doAutofilter: true);
                    rows = grid.RowsList.Count;
                }, cancellationToken);
            }
            terminal = new ExportProgress("completed", rows, "Export completed.", true);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            terminal = new ExportProgress("cancelled", rows, "Export cancelled.", true, "Export cancelled.");
        }
        catch (Exception exception)
        {
            string message = JustData.Application.Sql.SqlSensitiveDataRedactor.Redact(exception.Message);
            terminal = new ExportProgress("failed", rows, IsCompleted: true, ErrorMessage: message);
        }
        yield return terminal;
    }
}
