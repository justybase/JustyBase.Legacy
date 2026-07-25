using AppBase.Common;
using AppBase.Common.Interfaces;
using AppBase.Data.Core.Core;
using AppBase.Data.Core.Interfaces;
using DatabaseDataGridView.WinForms;
using DatabaseDataGridView.WinForms.Coloring;
using JustData.Application.ImportExport;
using JustDataAdditionalForms;
using JustyBaseLegacy.UI.DbForms;
using System.Text;
using System.Windows.Forms;

namespace JustyBaseLegacy.UI.ImportExport;

public interface IImportOperationService
{
    IAsyncEnumerable<ImportProgress> ImportAsync(ImportRequest request, CancellationToken cancellationToken = default);
}

/// <summary>Provider import workflow with WinForms dialogs kept as local UI adapters.</summary>
public sealed class WinFormsImportOperationService : IImportOperationService
{
    private readonly IImportExportTasks _tasks;
    private readonly IApplicationSettingsContext _settings;
    private readonly INetezzaCompletionContext _completion;
    private readonly IColorTheme _theme;
    private readonly IUiHelperService _ui;

    public WinFormsImportOperationService(IImportExportTasks tasks, IApplicationSettingsContext settings,
        INetezzaCompletionContext completion, IColorTheme theme, IUiHelperService ui)
    {
        _tasks = tasks; _settings = settings; _completion = completion; _theme = theme; _ui = ui;
    }

    public async IAsyncEnumerable<ImportProgress> ImportAsync(ImportRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ConnectionName)
            || !IGeneralDbService.GeneralDic.TryGetValue(request.ConnectionName, out IGeneralDb? connection))
        {
            yield return new ImportProgress("failed", IsCompleted: true, ErrorMessage: "The selected connection is not available.");
            yield break;
        }
        yield return new ImportProgress("starting", Message: request.Format is ImportFormat.Clipboard or ImportFormat.NetezzaXmlSpreadsheet
            ? "Reading clipboard data..." : $"Reading {request.SourcePath}...");

        ImportProgress result;
        try
        {
            string database = request.DatabaseName;
            if (request.Format is ImportFormat.Clipboard or ImportFormat.NetezzaXmlSpreadsheet)
            {
                IDataObject? clipboard = Clipboard.GetDataObject();
                if (clipboard is null) throw new InvalidOperationException("The clipboard does not contain importable data.");
                ImportProgressForm form = CreateProgress("Clipboard import");
                char separator = request.Separator ?? _settings.Config.PasteAsExternalSep[0];
                using CancellationTokenRegistration registration = cancellationToken.Register(() => _ = connection.AbortAsync("x"));
                if (request.Format == ImportFormat.NetezzaXmlSpreadsheet || clipboard.GetDataPresent("XML Spreadsheet"))
                    await connection.PerformImportXmlAsync(clipboard, '\\', separator, form, database).WaitAsync(cancellationToken);
                else
                    await connection.PerformImportFromText('\\', separator, form, database, request.ConnectionName).WaitAsync(cancellationToken);
            }
            else
            {
                ImportProgressForm form = CreateProgress($"{request.SourcePath} - import");
                using CancellationTokenRegistration registration = cancellationToken.Register(() => _ = connection.AbortAsync("x"));
                await connection.ImportFromFile(
                    path => ResolveEncoding(request.EncodingName, path),
                    count => request.TargetTable ?? SelectTableName(count),
                    sheets => string.IsNullOrWhiteSpace(request.SheetName) ? SelectSheets(sheets) : [request.SheetName],
                    _tasks, request.SourcePath, form, database,
                    string.IsNullOrWhiteSpace(request.TargetTable) ? null : [request.TargetTable],
                    string.IsNullOrWhiteSpace(request.SheetName) ? null : [request.SheetName], request.SkipRows).WaitAsync(cancellationToken);
            }
            result = new ImportProgress("completed", RowsSkipped: request.SkipRows, IsCompleted: true,
                Result: new ImportResult(0, 0, request.SkipRows, [], request.TargetTable), Message: "Import completed.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            result = new ImportProgress("cancelled", RowsSkipped: request.SkipRows, IsCompleted: true, ErrorMessage: "Import cancelled.");
        }
        catch (Exception exception)
        {
            string message = JustData.Application.Sql.SqlSensitiveDataRedactor.Redact(exception.Message);
            result = new ImportProgress("failed", RowsSkipped: request.SkipRows, IsCompleted: true, ErrorMessage: message,
                Result: new ImportResult(0, 0, request.SkipRows, [message], request.TargetTable, true));
        }
        yield return result;
    }

    private ImportProgressForm CreateProgress(string title)
    {
        var form = new ImportProgressForm(control => _theme.ColorForm(control), grid => _ui.DoubleBufDateGridView(grid)) { Text = title };
        form.FormClosed += (_, _) => form.Dispose();
        form.Show();
        return form;
    }
    private Encoding ResolveEncoding(string? requested, string path)
    {
        if (!string.IsNullOrWhiteSpace(requested)) return Encoding.GetEncoding(requested);
        using var form = new EncodingForm(path);
        return form.ShowDialog() == DialogResult.OK ? form.GetEncoding : Encoding.UTF8;
    }
    private string SelectTableName(int count)
    {
        using var form = new TableListForm(_completion, count);
        form.ShowDialog(); return form.GetSelected();
    }
    private List<string> SelectSheets(string[] sheets)
    {
        using var form = new ImportChoseTab(sheets, control => _theme.ColorForm(control), grid => _ui.DoubleBufDateGridView(grid));
        return form.ShowDialog() == DialogResult.OK ? form.SelectedTabs : [];
    }
}
