using AppBase.Common.Interfaces;
using JustData.Application.ImportExport;
using System.Data;

namespace JustyBaseLegacy.UI.ImportExport;

/// <summary>
/// Compatibility adapter for the existing file readers. Preview is fully
/// neutral; the actual database write remains a host operation until its
/// provider/reader loop is moved behind IImportUseCase.
/// </summary>
public sealed class WinFormsImportUseCase : IImportUseCase
{
    private readonly IImportExportTasks _legacyTasks;
    private readonly IImportOperationService _operations;
    private readonly object _sync = new();
    private readonly SemaphoreSlim _operationGate = new(1, 1);

    public WinFormsImportUseCase(IImportExportTasks legacyTasks, IImportOperationService operations)
    {
        _legacyTasks = legacyTasks;
        _operations = operations;
    }

    public async Task<ImportPreview> PreviewAsync(
        ImportRequest request,
        CancellationToken cancellationToken = default)
    {
        await _operationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using DataSet dataSet = await Task.Run(
                () => _legacyTasks.ReadFileAndMakeDataSet(
                    request.SourcePath,
                    Math.Max(0, request.SkipRows),
                    onlyFirst: request.SheetName is null),
                cancellationToken).ConfigureAwait(false);

            DataTable? first = dataSet.Tables.Count == 0 ? null : dataSet.Tables[0];
            IReadOnlyList<string> headers = first is null
                ? []
                : first.Columns.Cast<DataColumn>().Select(column => column.ColumnName).ToArray();
            IReadOnlyList<string> sheets = dataSet.Tables.Cast<DataTable>()
                .Select((table, index) => string.IsNullOrWhiteSpace(table.TableName) ? $"Sheet{index + 1}" : table.TableName)
                .ToArray();
            return new ImportPreview(
                request.SourcePath,
                request.Format,
                sheets,
                headers,
                dataSet.Tables.Cast<DataTable>().Sum(table => (long)table.Rows.Count));
        }
        finally
        {
            _operationGate.Release();
        }
    }

    public async IAsyncEnumerable<ImportProgress> ImportAsync(
        ImportRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await _operationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await foreach (ImportProgress progress in _operations
                .ImportAsync(request, cancellationToken)
                .WithCancellation(cancellationToken)
                .ConfigureAwait(false))
            {
                yield return progress;
            }
        }
        finally
        {
            _operationGate.Release();
        }
    }
}
