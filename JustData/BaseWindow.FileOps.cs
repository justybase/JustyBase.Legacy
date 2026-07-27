// BaseWindow file open/save and recent files partial.
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
using DatabaseDataGridView.WinForms;
using DatabaseDataGridView.WinForms.Coloring;
using FastColoredTextBoxNS;
using FastColoredTextBoxNS.Helpers;
using JustDataAdditionalForms;
using JustyBase.NetezzaDriver;
using JustData.Application.Editor;
using System.Drawing;
using JustyBase.NetezzaSqlParser.Linter;
using JustyBaseLegacy.UI.Helpers;
using JustyBaseLegacy.Services;
using JustyBaseLegacy.UI.Controls;
using JustyBaseLegacy.UI.DbForms;
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
        private readonly SemaphoreSlim _manySqlSaveGate = new(1, 1);

        public void Open()
        {
            _ = RunUiEventAsync(nameof(Open), OpenAsync);
        }

        public async Task OpenAsync()
        {
            if (ofdMain.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                await OpenSqlFileAsync(ofdMain.FileName);
            }
        }

        public async Task<FastColoredTextBox?> OpenSqlFileAsync(
            string fileName,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return null;

            if (!File.Exists(fileName))
                return AddMainTabCore(fileName, title: "", trescSQL: "");

            try
            {
                string documentText = await File.ReadAllTextAsync(fileName, cancellationToken);
                return AddMainTabCore(
                    fileName,
                    title: "",
                    trescSQL: documentText);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                Trace.WriteLine($"Opening SQL file failed for '{fileName}': {exception}");
                _loggerLoud.MessageBox_Show(
                    this,
                    exception.Message,
                    "Cannot open file",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return null;
            }
        }

        private async Task OpenManySQLhAsync(string manySqlPath, CancellationToken cancellationToken = default)
        {
            try
            {
                ManySqlBundle bundle = await LoadManySqlBundleWithRecoveryAsync(manySqlPath, cancellationToken);
                int firstTabIndex = EditorTabPages.Count;
                var openedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var openedContent = new HashSet<int>();

                foreach (string token in bundle.TabsOrder)
                {
                    int pathIndex = -1;
                    for (int index = 0; index < bundle.SqlPaths.Count; index++)
                    {
                        if (!openedPaths.Contains(Path.GetFullPath(bundle.SqlPaths[index]))
                            && string.Equals(bundle.SqlPaths[index], token, StringComparison.OrdinalIgnoreCase))
                        {
                            pathIndex = index;
                            break;
                        }
                    }
                    if (pathIndex >= 0 && pathIndex < bundle.SqlPaths.Count)
                    {
                        await OpenSqlFileAsync(bundle.SqlPaths[pathIndex], cancellationToken);
                        openedPaths.Add(Path.GetFullPath(bundle.SqlPaths[pathIndex]));
                        continue;
                    }

                    int contentIndex = -1;
                    for (int index = 0; index < bundle.SqlContentList.Count; index++)
                    {
                        if (!openedContent.Contains(index)
                            && string.Equals(bundle.SqlContentList[index].Title, token, StringComparison.Ordinal))
                        {
                            contentIndex = index;
                            break;
                        }
                    }
                    if (contentIndex >= 0 && contentIndex < bundle.SqlContentList.Count)
                    {
                        ManySqlContent content = bundle.SqlContentList[contentIndex];
                        AddMainTab(null, title: content.Title, trescSQL: content.Text);
                        openedContent.Add(contentIndex);
                    }
                }

                foreach (string path in bundle.SqlPaths)
                {
                    if (!openedPaths.Contains(Path.GetFullPath(path)))
                    {
                        await OpenSqlFileAsync(path, cancellationToken);
                        openedPaths.Add(Path.GetFullPath(path));
                    }
                }

                for (int index = 0; index < bundle.SqlContentList.Count; index++)
                {
                    if (!openedContent.Contains(index))
                    {
                        ManySqlContent content = bundle.SqlContentList[index];
                        AddMainTab(null, title: content.Title, trescSQL: content.Text);
                    }
                }

                if (EditorTabPages.Count == firstTabIndex)
                    AddMainTab(null, "tab");

                int selectedIndex = Math.Clamp(firstTabIndex + bundle.SelectedTabNum, 0, EditorTabPages.Count - 1);
                if (EditorTabPages.Count > 0)
                    _tabManager.SelectTab(EditorTabPages[selectedIndex]);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                Trace.WriteLine($"Opening Many SQL bundle was cancelled: {manySqlPath}");
            }
            catch (FileNotFoundException ex1)
            {
                _loggerLoud.MessageBox_Show(this, ex1.Message, "Cannot open file", MessageBoxButtons.OK, MessageBoxIcon.Error);
                recentManyFilesMenu.DropDownItems.RemoveByKey(manySqlPath);
                SyncRecentFiles();
            }
            catch (Exception ex)
            {
                _loggerLoud.MessageBox_Show(this, ex.Message, "Cannot open file", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private async Task OpenManySQLAsync()
        {
            if (manyOpenFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                AddRecentMany(manyOpenFileDialog.FileName);
                await OpenManySQLhAsync(manyOpenFileDialog.FileName);
            }
        }

        private void OpenToolStripMenuItem_Click(object sender, EventArgs e) =>
            _ = RunUiEventAsync(nameof(OpenToolStripMenuItem_Click), OpenAsync);

        private void OpenManyToolStripMenuItem_Click(object sender, EventArgs e) =>
            _ = RunUiEventAsync(nameof(OpenManyToolStripMenuItem_Click), OpenManySQLAsync);

        private void Recent_Click(object sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem item)
                _ = RunUiEventAsync(nameof(Recent_Click), () => OpenSqlFileAsync(item.Name));
        }

        private void RecentMany_Click(object sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem item)
                _ = RunUiEventAsync(nameof(RecentMany_Click), () => OpenManySQLhAsync(item.Name));
        }

        private void SaveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ActiveEditorTabPage is TabPage activeTab)
                _ = RunUiEventAsync(nameof(SaveAsToolStripMenuItem_Click), () => SaveAsync(activeTab, forceSaveAs: true));
        }

        private void SaveManyStrip_Click(object sender, EventArgs e) =>
            _ = RunUiEventAsync(nameof(SaveManyStrip_Click), SaveManyFromMenuAsync);

        private async Task SaveManyFromMenuAsync()
        {
            if (manySaveFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;

            string filePath = manySaveFileDialog.FileName;
            AddRecentMany(filePath);
            await SaveManySqlToDiskAsync(filePath);
        }

        public void SaveManySqlToDisk()
        {
            _ = RunUiEventAsync(nameof(SaveManySqlToDisk), () => SaveManySqlToDiskAsync());
        }

        public async Task SaveManySqlToDiskAsync(string? filePath = null, CancellationToken cancellationToken = default)
        {
            filePath ??= Path.Combine(_applicationSettingsContext.ConfigDirectory, "simpleStartup.manysql");
            await _manySqlSaveGate.WaitAsync(cancellationToken);
            try
            {
                IReadOnlyList<EditorDocumentId>? documentOrder = _tabManager is DockSuiteTabManager dockSuiteTabManager
                    ? dockSuiteTabManager.GetEditorDocumentOrder()
                    : null;
                if (documentOrder is { Count: > 0 })
                    _editorWorkspaceViewModel.Reorder(documentOrder);
                await _editorWorkspaceViewModel.SaveManySqlAsync(
                    filePath,
                    cancellationToken).ConfigureAwait(true);
            }
            catch (IOException ex)
            {
                MessageBox.Show(this, ex.Message, "Cannot save startup file set", MessageBoxButtons.OK, MessageBoxIcon.Error);
                if (ex.Message.Contains("because it is being used by another process", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        List<Process> processes = FileUtil.WhoIsLocking(filePath);
                        if (processes is not null && processes.Count > 0)
                        {
                            DialogResult result = MessageBox.Show(this, ex.Message, "Kill locking processes?", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                            if (result == DialogResult.Yes)
                            {
                                foreach (Process process in processes)
                                {
                                    process.Kill();
                                }
                            }
                        }
                    }
                    catch (Exception cleanupException)
                    {
                        Trace.WriteLine($"Releasing a file lock failed: {cleanupException.GetType().Name}");
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                Trace.WriteLine("Saving Many SQL bundle was cancelled.");
            }
            catch (Exception ex)
            {
                _loggerLoud.MessageBox_Show(this, ex.Message, "Cannot save startup file set", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _manySqlSaveGate.Release();
            }
        }

        private async Task<ManySqlBundle> LoadManySqlBundleWithRecoveryAsync(string path, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _manySqlBundleService.LoadAsync(path, cancellationToken);
            }
            catch (IOException ex) when (ex.Message.Contains("because it is being used by another process", StringComparison.OrdinalIgnoreCase))
            {
                List<Process> processes = FileUtil.WhoIsLocking(path);
                if (processes is null || processes.Count == 0)
                    throw;

                string processText = string.Join(';', processes.Select(process => process.ProcessName));
                DialogResult result = _loggerLoud.MessageBox_Show(
                    this,
                    $"{ex.Message}\r\nProcesses: {processText}",
                    "Kill locking processes?",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                if (result != DialogResult.Yes)
                    throw;

                foreach (Process process in processes)
                {
                    try
                    {
                        if (!process.HasExited)
                            process.Kill();
                        process.WaitForExit(1000);
                    }
                    catch (Exception cleanupException)
                    {
                        Trace.WriteLine($"Releasing a file lock failed: {cleanupException.GetType().Name}");
                    }
                }

                return await _manySqlBundleService.LoadAsync(path, cancellationToken);
            }
        }
        private async Task<bool> SaveAsync(
            TabPage page,
            CancellationToken cancellationToken = default,
            bool forceSaveAs = false)
        {
            if (page is null)
                return false;

            var saveEditorPanel = page is not null ? _tabManager.GetEditorPanel(page) : null;
            FastColoredTextBox? fctb = saveEditorPanel?.CurrentTb;

            string? filename = null;
            bool isNewDocument = forceSaveAs || page.Tag is null;
            if (isNewDocument)
            {
                if (saveEditorPanel is null && page is TabPagePicture pagePicture)
                {
                    if (pagePicture.DatabaseTypeName == "TXT")
                    {
                        //this.Invoke(() => _loggerLoud.MessageBox_Show("not implemented yet"));
                        _loggerLoud.MessageBox_Show(this, "This feature is not implemented yet.", "Not implemented", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return true;
                    }
                }
                if (fctb is null)
                    return false;

                if (saveFileDialogSQL.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                    return false;

                filename = saveFileDialogSQL.FileName;
            }
            else
            {
                filename = (page.Tag as TabPageMainTag)?.Filename;
            }

            if (string.IsNullOrWhiteSpace(filename) || fctb is null)
                return false;

            try
            {
                bool useUtf8WithoutBom = fctb.useUtf8WithoutBoom;
                bool saved;
                if (_documentIdsByTab.TryGetValue(page, out var documentId)
                    && _editorWorkspaceViewModel.Documents.FirstOrDefault(item => item.Id == documentId) is { } document)
                {
                    // Keep the workspace authoritative when a legacy editor
                    // control is saved through the old menu/shortcut surface.
                    document.UpdateTextFromView(fctb.Text);
                    saved = await _editorWorkspaceViewModel.SaveAsAsync(
                        documentId,
                        filename,
                        cancellationToken,
                        useUtf8WithoutBom);
                }
                else
                {
                    string? directory = Path.GetDirectoryName(filename);
                    if (!string.IsNullOrWhiteSpace(directory))
                        Directory.CreateDirectory(directory);
                    Encoding encoding = new UTF8Encoding(!useUtf8WithoutBom);
                    await File.WriteAllTextAsync(filename, fctb.Text, encoding, cancellationToken);
                    saved = true;
                }

                if (!saved)
                    return false;

                string previousTitle = page.Text;
                if (isNewDocument)
                {
                    page.Text = Path.GetFileName(filename);
                    page.Tag = new TabPageMainTag { Filename = filename, IsSaved = true };
                    VariablesAfterChangeTabName(previousTitle, page.Text);
                }
                else if (page.Tag is TabPageMainTag savedTag)
                {
                    savedTag.IsSaved = true;
                }

                page.Name = filename;
                fctb.IsChanged = false;
                AddRecent(filename);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return false;
            }
            catch (Exception ex)
            {
                if (_loggerLoud.MessageBox_Show(this, ex.Message, "Error", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error) == DialogResult.Retry)
                    return await SaveAsync(page, cancellationToken, forceSaveAs);
                else
                    return false;
            }

            fctb.Invalidate();
            return true;
        }
        private void AddRecent(string fileName)
        {
            if (this.recentFilesMenu.DropDownItems.Count == _applicationSettingsContext.Config.MaxRecentFilesCount)
            {
                this.recentFilesMenu.DropDownItems.RemoveAt(0);
            }
            if (!this.recentFilesMenu.DropDownItems.ContainsKey(fileName))
            {
                var rec = new ToolStripMenuItem()
                {
                    Text = fileName,
                    Name = fileName
                };


                rec.BackColor = _colorTheme.MainBack;
                rec.ForeColor = _colorTheme.MainFore;

                rec.Click += Recent_Click;
                this.recentFilesMenu.DropDownItems.Add(rec);
            }

            SyncRecentFiles();
        }

        private void SyncRecentFiles()
        {
            _recentFileRuntimeContext.RecentFiles.Clear();
            foreach (var item in recentFilesMenu.DropDownItems.OfType<ToolStripMenuItem>())
            {
                _recentFileRuntimeContext.RecentFiles.Add(item.Name);
            }

            _recentFileRuntimeContext.RecentManySqlFiles.Clear();
            foreach (var item in recentManyFilesMenu.DropDownItems.OfType<ToolStripMenuItem>())
            {
                _recentFileRuntimeContext.RecentManySqlFiles.Add(item.Name);
            }
        }

        private void AddRecentMany(string fileName)
        {
            if (this.recentManyFilesMenu.DropDownItems.Count == _applicationSettingsContext.Config.MaxRecentFilesCount)
            {
                this.recentManyFilesMenu.DropDownItems.RemoveAt(0);
            }
            if (!this.recentManyFilesMenu.DropDownItems.ContainsKey(fileName))
            {
                var rec = new ToolStripMenuItem()
                {
                    Text = fileName,
                    Name = fileName
                };

                rec.BackColor = _colorTheme.MainBack;
                rec.ForeColor = _colorTheme.MainFore;


                rec.Click += RecentMany_Click;
                this.recentManyFilesMenu.DropDownItems.Add(rec);
            }
            SyncRecentFiles();
        }

        public void tabControlMainDragOver(object sender, System.Windows.Forms.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop) || e.Data.GetDataPresent("FileContents"))
            {
                e.Effect = DragDropEffects.Link;
            }
        }

        public void tabControlMain_DragDrop(object sender, System.Windows.Forms.DragEventArgs e)
        {
            _ = RunUiEventAsync(nameof(tabControlMain_DragDrop), () => HandleTabControlMainDragDropAsync(e));
        }

        private async Task HandleTabControlMainDragDropAsync(System.Windows.Forms.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // get all files droppeds  
                if (e.Data.GetData(DataFormats.FileDrop) is string[] files && files.Any())
                {
                    foreach (var path in files)
                    {
                        if (path.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
                        {
                            await OpenSqlFileAsync(path);
                        }
                        else if (path.EndsWith(".manysql.enc", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".manysql", StringComparison.OrdinalIgnoreCase))
                        {
                            await OpenManySQLhAsync(path);
                        }
                        else if (path.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)
                            || path.EndsWith(".xlsb", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)
                            )
                        {
                            if (SelectedConnectionName is not null && path is not null)
                            {
                                await ImportViaViewModel(SelectedConnectionName, path);
                            }
                        }
                    }
                }
            }
            else if (e.Data.GetDataPresent("FileContents"))
            {
                try
                {
                    var stream = (MemoryStream)e.Data.GetData("FileContents");
                    var streamreader = new StreamReader(stream);

                    if (stream.Length < 3 * 1024 * 1024)
                    {
                        AddMainTab(null, "droped", streamreader.ReadToEnd());
                    }
                    else
                    {
                        _loggerLoud.MessageBox_Show(this, "The file is too large.", "File too large", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }

                    streamreader.Close();
                    stream.Close();
                }
                catch (Exception exception)
                {
                    Trace.WriteLine($"Opening a dropped file failed: {exception.GetType().Name}");
                }
            }
        }
        public void SaveOnTabEventHandler(object sender, EventArgs e)
        {
            if (ActiveEditorTabPage is TabPage activeTab)
                _ = RunUiEventAsync(nameof(SaveOnTabEventHandler), () => SaveAsync(activeTab));
        }

        void InitHistRecent()
        {
            string filepath = $"{_applicationSettingsContext.ConfigDirectory}\\recent.json";
            if (File.Exists(filepath))
            {
                string content = _textFileContentReader.GetContentOfTextFile(filepath);
                var recentsX = JsonSerializer.Deserialize(content, MyJsonContextStringList.Default.ListString);

                foreach (var item in recentsX)
                {
                    AddRecent(item);
                }
            }

            filepath = $"{_applicationSettingsContext.ConfigDirectory}\\recentMany.json";

            if (File.Exists(filepath))
            {
                string content = _textFileContentReader.GetContentOfTextFile(filepath);
                var recentsManyX = JsonSerializer.Deserialize(content, MyJsonContextStringList.Default.ListString);
                foreach (var item in recentsManyX)
                {
                    AddRecentMany(item);
                }
            }
        }
    }
}
