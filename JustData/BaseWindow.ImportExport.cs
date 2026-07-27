// BaseWindow import/export partial (file import, clipboard, grid XLSX export).
using AppBase.Common;
using AppBase.Common.Enums;
using AppBase.Common.Interfaces;
using AppBase.Common.JsonContext;
using AppBase.Common.Models;
using AppBase.Common.WindowManagement;
using AppBase.Data;
using AppBase.Data.Completion;
using AppBase.Data.Core.Core;
using AppBase.Data.Core.Enums;
using AppBase.Data.Core.Interfaces;
using AppBase.Data.Core.Models;
using AppBase.Services;
using AppBase.Services.Helpers;
using AppBase.Services.Sql;
using JustyBaseLegacy.UI.Sql;
using JustyBaseLegacy.UI.ImportExport;
using DatabaseDataGridView.WinForms;
using DatabaseDataGridView.WinForms.Coloring;
using FastColoredTextBoxNS;
using FastColoredTextBoxNS.Helpers;
using JustDataAdditionalForms;
using JustData.Application.Editor;
using JustData.Application.ImportExport;
using JustData.Application.Sql;
using JustData.ViewModels.ImportExport;
using JustyBase.NetezzaDriver;
using System.Drawing;
using JustyBase.NetezzaSqlParser.Linter;
using JustyBaseLegacy.UI.Helpers;
using JustyBaseLegacy.Services;
using JustyBaseLegacy.UI.Controls;
using JustyBaseLegacy.UI.Extensions;
using JustyBaseLegacy.UI.Models;
using SpreadSheetTasks;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;


namespace JustyBaseLegacy.UI
{
    public partial class BaseWindow
    {
        readonly FileSystemWatcher _watchForNewImport = new FileSystemWatcher(Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
        private async void RecentXlsx_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            try
            {
                if (!File.Exists(e.ClickedItem.Text))
                {
                    recentXlsx.DropDownItems.Remove(e.ClickedItem);
                    _loggerLoud.MessageBox_Show(this, "The file does not exist.", "File not found", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    await ImportViaViewModel(SelectedConnectionName, e.ClickedItem.Text);
                }
            }
            catch (Exception ex)
            {
                _loggerLoud.LogError(ex.Message, ex);
            }
        }

        private void WatchForNewImport_Created(object sender, FileSystemEventArgs e)
        {
            string path = e.FullPath;
            if (path.Contains("~$"))
            {
                path = path.Replace("~$", "");
            }
            if (this.InvokeRequired)
            {
                this.Invoke(() =>
                {
                    recentXlsx.DropDownItems.Add(path);
                    if (recentXlsx.DropDownItems.Count > 10)
                    {
                        recentXlsx.DropDownItems.RemoveAt(0);
                    }
                });
            }
            else
            {
                recentXlsx.DropDownItems.Add(path);
                if (recentXlsx.DropDownItems.Count > 10)
                {
                    recentXlsx.DropDownItems.RemoveAt(0);
                }
            }
        }
        public bool ForceNormalPaste { get; set; } = false;
        public void FctbNew_Pasting(object sender, TextChangingEventArgs e)
        {
            string cli;
            try
            {
                cli = Clipboard.GetText();
            }
            catch (Exception exception)
            {
                _loggerLoud.LogError("Reading the clipboard failed", exception);
                e.Cancel = true;
                return;
            }

            int newLinesCns = 0;
            int len = cli.Length > 16_384 ? 16_384 : cli.Length;
            for (int i = 0; i < len; i++)
            {
                if (cli[i] == '\n')
                {
                    newLinesCns++;
                }
            }

            if (!cli.StartsWith("\"", StringComparison.OrdinalIgnoreCase) //sql from excel
                && newLinesCns >= 2)//at least one row
            {
                var clipboard = Clipboard.GetDataObject();

                if (clipboard.GetDataPresent("XML Spreadsheet") && (_applicationSettingsContext.Config.CtrlVmode != 2 && !ForceNormalPaste))
                {
                    DialogResult r = DialogResult.Yes;
                    if (_applicationSettingsContext.Config.CtrlVmode == 0) // 0 = ask, 1 = auto, 2 = normal
                    {
                        r = _loggerLoud.MessageBox_Show(this, "Import clipboard data to new table ?" +
                            "\nto change this behavior go to settings/general", "Import ?", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    }

                    if (r == DialogResult.Yes)
                    {
                        e.Cancel = true;
                        _ = RunUiEventAsync(
                            nameof(FctbNew_Pasting),
                            () => ImportClipboardViaViewModel(JustData.Application.ImportExport.ImportFormat.NetezzaXmlSpreadsheet));
                        return;
                    }
                }
            }

            if (Clipboard.ContainsFileDropList())
            {
                e.Cancel = true;
                string[] files = Clipboard.GetFileDropList()
                    .Cast<string>()
                    .Where(path => !string.IsNullOrWhiteSpace(path)
                        && (path.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)
                        || path.EndsWith(".xlsb", StringComparison.OrdinalIgnoreCase)
                        || path.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)))
                    .ToArray();
                _ = RunUiEventAsync(nameof(FctbNew_Pasting), () => ImportClipboardFilesAsync(files));
                return;
            }
            else if (cli.Length > 20_000_000)
            {
                _loggerLoud.MessageBox_Show(this, "Text is too long.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                e.Cancel = true;
                return;
            }
            else if (cli.Length > 20_000)
            {
                return;
            }

            /*if (cli.Contains((char)160))
            {
                CurrentTB.InsertText(cli.Replace((char)160, ' '));
                //CurrentTB.InsertText($"{Environment.NewLine}--non-breaking space (U+00A0, ASCII 160) removed!");
                e.Cancel = true;
            }
            else*/
            if (!string.IsNullOrWhiteSpace(cli))
            {
                CurrentTB.InsertText(cli);
                e.Cancel = true;
            }
            else
            {
                e.Cancel = true;
            }
        }

        private async Task ImportClipboardFilesAsync(IReadOnlyList<string> files)
        {
            foreach (string path in files)
                await ImportViaViewModel(SelectedConnectionName, path);
        }
        private void pasteAsSelect_Click(object sender, EventArgs e)
        {
            string clip = Clipboard.GetText();

            if (clip.EndsWith("\r\n"))
            {
                clip = clip[0..(clip.Length - 2)];
            }

            char escapechar = '\\';
            if (clip == null)
            {
                _loggerLoud.MessageBox_Show(this, "Nothing in the clipboard.", "Clipboard", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string[] lines = _generalDbService.ClipToLines(_applicationSettingsContext.Config.PasteAsExternalSep[0], ref clip, escapechar);
            if (lines is null)
            {
                return;
            }

            var headers = lines[0].Split(_applicationSettingsContext.Config.PasteAsExternalSep[0]).Select(arg => arg.Trim()).ToArray();

            if (lines.Length * headers.Length > 10_000)
            {
                _loggerLoud.MessageBox_Show(this, "Maximum 10,000 cells allowed.", "Import", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            StringBuilder sb = new StringBuilder();

            for (int i = 1; i < lines.Length; i++)
            {
                var v1 = lines[i].SqlSplit(_applicationSettingsContext.Config.PasteAsExternalSep[0]);

                if (lines[i] == "")
                {
                    break;
                }

                if (v1.Length != headers.Length)
                {
                    _loggerLoud.MessageBox_Show(this, $"Row {i + 1} has too few or too many tab characters.", "Import", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                if (i == 1)
                {
                    sb.Append("SELECT");
                }
                else
                {
                    sb.Append("UNION ALL SELECT");
                }
                for (int j = 0; j < v1.Length; j++)
                {
                    var val = _generalDbService.PrepareValue(out DatabaseColumnType nz, v1[j]);
                    if (nz == DatabaseColumnType.integer && v1[j].Trim().Length == 11 && headers[j].Contains("PESEL", StringComparison.OrdinalIgnoreCase))
                    {
                        nz = DatabaseColumnType.nvarchar;
                        val = $"'{v1[j].Trim()}'";
                    }
                    sb.Append($" {(val == "" ? "null" : val)} AS {headers[j].NormalizeName(_applicationSettingsContext.Config.KeyWordsListForColoring1).Trim()}");
                    if (j != v1.Length - 1)
                    {
                        sb.Append(',');
                    }
                }
                sb.AppendLine();
            }
            CurrentTB.InsertText(sb.ToString());
        }

        private async void ImportFromClipboard_Click(object sender, EventArgs e)
        {
            try
            {
                IDataObject? clipboard = Clipboard.GetDataObject();
                bool isXmlSpreadsheet = clipboard?.GetDataPresent("XML Spreadsheet") == true;
                bool isNetezza = _generalDbService.DriverName(SelectedConnectionName) == "NetezzaSQL";
                if (isXmlSpreadsheet || isNetezza)
                {
                    await ImportClipboardViaViewModel(
                        isXmlSpreadsheet
                            ? JustData.Application.ImportExport.ImportFormat.NetezzaXmlSpreadsheet
                            : JustData.Application.ImportExport.ImportFormat.Clipboard);
                }
            }

            catch (Exception ex)
            {
                _loggerLoud.MessageBox_Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return;
        }

        private void PasteIn(object sender, EventArgs e)
        {
            string clip = Clipboard.GetText();
            if (clip == null)
            {
                _loggerLoud.MessageBox_Show(this, "Nothing in the clipboard.", "Clipboard", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            clip = clip.Trim();
            string[] lines = clip.Split(Environment.NewLine);

            if (lines.Length > 1_048_577)
            {
                _loggerLoud.MessageBox_Show(this, "Maximum 1,048,576 cells allowed.", "Import", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var tempCol = lines.Where(arg => arg != "").Select(arg => arg.Trim());

            CurrentTB.InsertText($"--pasted {tempCol.Distinct().Count()} unique from {lines.Length}{Environment.NewLine}");

            if (sender == inRaw)
            {
                CurrentTB.InsertText($"({String.Join(",\n", tempCol.Distinct())})");
            }
            else if (sender == inText)
            {
                CurrentTB.InsertText("(");
                CurrentTB.InsertText(String.Join(",\n", tempCol.Distinct().Select(arg => $"'{arg}'")));
                CurrentTB.InsertText(")");
            }
        }
        private async Task ExcelExport(string xlsxPath, DataTable dt, List<object[]> list, string forcedSql)
        {
            try
            {
                if (_currentMyGrid is CustomDataGridView selectedGrid
                    && _resultExportUseCase is not null)
                {
                    EditorDocumentId documentId = (selectedGrid.Parent as TabPage)?.Tag is TabPageResultsTag tag
                        && tag.DocumentId is { } taggedDocumentId
                        ? taggedDocumentId
                        : CurrentEditorDocumentId ?? throw new InvalidOperationException("No active editor document is available.");
                    if (!_resultGridRegistry.TryFind(documentId, selectedGrid, out ResultSetKey? resultKey)
                        || resultKey is not { } key)
                    {
                        throw new InvalidOperationException("The selected result set is no longer available.");
                    }
                    ExportFormat format = Path.GetExtension(xlsxPath).Equals(".xlsb", StringComparison.OrdinalIgnoreCase)
                        ? ExportFormat.Xlsb
                        : ExportFormat.Xlsx;
                    string connectionName = _editorWorkspaceViewModel.Documents
                        .FirstOrDefault(document => document.Id == documentId)
                        ?.ConnectionName ?? SelectedConnectionName;
                    ExportRequest request = new(
                        documentId,
                        xlsxPath,
                        format,
                        key.ResultSetId,
                        selectedGrid.AttachedSQL,
                        connectionName,
                        IncludeSqlMetadata: true);
                    using ImportExportViewModel operation = _importExportViewModelFactory.Create();
                    await operation.ExportAsync(request);
                    if (!string.IsNullOrWhiteSpace(operation.ErrorMessage))
                    {
                        throw new InvalidOperationException(operation.ErrorMessage);
                    }
                    return;
                }

                await Task.Run(() => _importExportTasks.SaveAsXlsx(xlsxPath, dt ?? _dtDoEksportu, list ?? _dtDoEksportuRows, _currentMyGrid?.AttachedSQL ?? forcedSql));
            }
            catch (Exception ex)
            {
                _loggerLoud.MessageBox_Show(this, ex.Message, "Excel export error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void ExportToXlsx_Click(object sender, EventArgs e)
        {
            string xlsxPath;

            string ext = "xlsx";
            if (_applicationSettingsContext.Config.UseXlsb)
            {
                ext = "xlsb";
            }

            xlsxPath = $"{(sender as ToolStripMenuItem).Name}\\{StringExtension.RandomName()}.{ext}";

            try
            {
                await ExcelExport(xlsxPath, null, null, null);
            }
            catch (Exception ex)
            {
                _loggerLoud.MessageBox_Show(this, ex.Message, "Excel export error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private async void ExportToCsvClick(object sender, EventArgs e)
        {
            try
            {
                await RunSQL(exportOption: ExportOptions.csv);
            }
            catch (Exception ex)
            {
                _loggerLoud.LogError(ex.Message, ex);
            }
        }

        private async void ExportToXlsxInlineClick(object sender, EventArgs e)
        {
            try
            {
                await RunSQL(exportOption: ExportOptions.xlsx);
            }
            catch (Exception ex)
            {
                _loggerLoud.LogError(ex.Message, ex);
            }
        }
        public async void XLSXtoolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                string connectionName = SelectedConnectionName;
                openFileXlsx.Multiselect = true;
                DialogResult r = openFileXlsx.ShowDialog();
                if (r != DialogResult.OK)
                {
                    return;
                }

                foreach (string filePath in openFileXlsx.FileNames)
                {
                    if (!File.Exists(filePath))
                    {
                        _loggerLoud.MessageBox_Show(this, $"The file does not exist: {filePath}", "File not found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        continue;
                    }
                    await ImportViaViewModel(connectionName, filePath);
                }
            }
            catch (Exception ex)
            {
                _loggerLoud.LogError(ex.Message, ex);
            }
        }

        private async Task ImportViaViewModel(string connectionName, string filePath)
        {
            using ImportExportViewModel operation = _importExportViewModelFactory.Create();
            ImportRequest request = new(
                CurrentEditorDocumentId,
                filePath,
                GetImportFormat(filePath),
                connectionName,
                SelectedDatabase);
            await operation.ImportAsync(request);
            if (!string.IsNullOrWhiteSpace(operation.ErrorMessage))
            {
                _loggerLoud.MessageBox_Show(this, operation.ErrorMessage, "Import",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private async Task ImportClipboardViaViewModel(ImportFormat format)
        {
            using ImportExportViewModel operation = _importExportViewModelFactory.Create();
            ImportRequest request = new(
                CurrentEditorDocumentId,
                "clipboard",
                format,
                SelectedConnectionName,
                SelectedDatabase,
                Separator: _applicationSettingsContext.Config.PasteAsExternalSep[0]);
            await operation.ImportAsync(request);
            if (!string.IsNullOrWhiteSpace(operation.ErrorMessage))
            {
                _loggerLoud.MessageBox_Show(this, operation.ErrorMessage, "Import",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private static ImportFormat GetImportFormat(string filePath) =>
            Path.GetExtension(filePath).ToLowerInvariant() switch
            {
                ".xlsx" => ImportFormat.Xlsx,
                ".xlsb" => ImportFormat.Xlsb,
                _ => ImportFormat.Csv
            };
    }
}
