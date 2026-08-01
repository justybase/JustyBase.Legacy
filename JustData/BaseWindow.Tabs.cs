// BaseWindow tab lifecycle partial.
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
using DatabaseDataGridView.WinForms;
using DatabaseDataGridView.WinForms.Coloring;
using FastColoredTextBoxNS;
using FastColoredTextBoxNS.Helpers;
using JustDataAdditionalForms;
using JustData.Application.Editor;
using JustData.ViewModels.Editor;
using JustyBase.NetezzaDriver;
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
        private void CloseTabWithConnection(TabControl tpc, TabPage tabPage, bool cleanUpRam = true)
        {
            var editorForRelease = _tabManager.GetEditor(tabPage);
            if (editorForRelease is not null)
            {
                EditorDocumentViewModel? lintDocument = EditorWorkspaceDocumentEnsure.TryGetByEditorKey(
                    _editorWorkspaceViewModel,
                    _documentIdsByEditor,
                    editorForRelease);
                if (lintDocument is not null)
                {
                    lintDocument.DiagnosticsChanged -= OnDocumentDiagnosticsChanged;
                    _lintDiagnosticsTargets.Remove(lintDocument.Id);
                    _cachedDiagnostics.Remove(lintDocument.Id);
                }
                else if (_documentIdsByEditor.TryGetValue(editorForRelease, out var orphanDocumentId))
                {
                    _lintDiagnosticsTargets.Remove(orphanDocumentId);
                    _cachedDiagnostics.Remove(orphanDocumentId);
                }

                _lintIssuesByEditor.Remove(editorForRelease);
                _lightbulbManager.ClearLightbulbs(editorForRelease);
            }

            var splitContainer = _tabManager.GetSplitContainerForTab(tabPage);
            if (splitContainer is not null)
            {
                var editorPanel = _tabManager.GetEditorPanel(tabPage);
                var fctb = editorPanel?.CurrentTb;
                if (fctb is not null && TabConnectionCache.Default.TryGet(fctb, out var tabConnectionData))
                {
                    foreach (var item in tabConnectionData.Commands)
                    {
                        if (!tabConnectionData.CloseConnectionByDefault)
                        {
                            item.Cancel();
                        }
                    }
                    if (tabConnectionData.Connection != null && tabConnectionData.Connection.State == ConnectionState.Open)
                    {
                        if (!tabConnectionData.CloseConnectionByDefault)
                        {
                            tabConnectionData.Connection.Close();
                        }
                    }
                }
            }

            if (cleanUpRam)
            {
                try
                {
                    List<CustomDataGridView> myDataGridViews = new List<CustomDataGridView>();
                    if (_tabManager.GetSplitContainerForTab(tabPage)?.Tag is ResultData resultData)
                    {
                        foreach (TabPage page in resultData.TabControlSQLResults.TabPages)
                        {
                            foreach (CustomDataGridView myDataGridView in page.Controls.OfType<CustomDataGridView>())
                            {
                                myDataGridViews.Add(myDataGridView);
                            }
                        }
                    }

                    for (int i = 0; i < myDataGridViews.Count; i++)
                    {
                        myDataGridViews[i].ClearDataGridView();
                    }
                    myDataGridViews = null;
                    ClearCurrentHelpReferences();
                }
                catch (Exception exception)
                {
                    Trace.WriteLine($"Tab cleanup failed: {exception.GetType().Name}");
                }
                finally
                {
                    //CleanRam();
                }
            }

            tpc.TabPages.Remove(tabPage);
            _tabManager.UnregisterTab(tabPage);

            if (_documentIdsByTab.Remove(tabPage, out var documentId))
            {
                if (editorForRelease is not null)
                    _documentIdsByEditor.Remove(editorForRelease);
                _editorWorkspaceViewModel.RemoveDocument(documentId);
            }
        }

        private static readonly TaskDialogButton _saveAndClose = new TaskDialogButton("save and close");
        private static readonly TaskDialogButton _closeWithoutSaving = new TaskDialogButton("close without saving");
        private readonly HashSet<TabPage> _tabCloseOperations = [];

        private readonly TaskDialogPage _td = new TaskDialogPage()
        {
            //Text = "Tab is not saved",
            Heading = "Tab is not saved, close?",
            Caption = "Close ?",
            Buttons =
            {
                _saveAndClose, _closeWithoutSaving, TaskDialogButton.Cancel
            },
            Icon = TaskDialogIcon.Warning,
            DefaultButton = TaskDialogButton.Cancel
        };

        private async Task DoClosingOfTabAsync(TabControl tpc, TabPage tabPage)
        {
            if (tabPage is null || !_tabCloseOperations.Add(tabPage))
                return;

            try
            {
                if (tabPage is not null)
                {
                    bool hasEditor = _tabManager.GetEditorPanel(tabPage) is not null;
                    bool isEmpty = !hasEditor;

                    if (isEmpty)
                    {
                        tpc.TabPages.Remove(tabPage);
                        return;
                    }

                }

                FastColoredTextBox fctb = _tabManager.GetEditor(tabPage);

                if (fctb != null && fctb.TextLength > 0)
                {
                    if (tabPage.Tag != null && !(tabPage.Tag as TabPageMainTag).IsSaved && !fctb.ReadOnly)
                    {
                        var dialogResult = TaskDialog.ShowDialog(this, _td);
                        if (dialogResult == _saveAndClose)
                        {
                            if (await SaveAsync(tabPage))
                                CloseTabWithConnection(tpc, tabPage);
                        }
                        else if (dialogResult == _closeWithoutSaving)
                        {
                            CloseTabWithConnection(tpc, tabPage);
                        }
                    }
                    else if (tabPage.Tag != null)
                    {
                        CloseTabWithConnection(tpc, tabPage);
                    }
                    else
                    {
                        if (_applicationSettingsContext.Config.CloseWaringLevel >= 2 && !fctb.ReadOnly)
                        {
                            var dialogResult = TaskDialog.ShowDialog(this, _td);

                            if (dialogResult == _saveAndClose)
                            {
                                if (await SaveAsync(tabPage))
                                    CloseTabWithConnection(tpc, tabPage);
                            }
                            else if (dialogResult == _closeWithoutSaving)
                            {
                                CloseTabWithConnection(tpc, tabPage);
                            }
                        }
                        else
                        {
                            CloseTabWithConnection(tpc, tabPage);
                        }
                    }
                }
                else if (fctb != null && fctb.TextLength == 0)
                {
                    CloseTabWithConnection(tpc, tabPage);
                }
                else
                {
                    CloseTabWithConnection(tpc, tabPage);
                }
            }
            catch (Exception exception)
            {
                Trace.WriteLine($"Closing an editor tab failed: {exception.GetType().Name}");
            }
            finally
            {
                _tabCloseOperations.Remove(tabPage);
            }
        }
        private void TabControlMain_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ActiveEditorTabPage is not TabPage selectedTab)
                return;

            if (_documentIdsByTab.TryGetValue(selectedTab, out var documentId))
            {
                _editorWorkspaceViewModel.Activate(documentId);
                if (_tabManager.GetEditorPanel(selectedTab) is IEditorPanel editorPanel
                    && _editorWorkspaceViewModel.Documents.FirstOrDefault(item => item.Id == documentId) is { } document)
                {
                    document.ConnectionName = editorPanel.SelectedConnectionName;
                    document.DatabaseName = editorPanel.SelectedDatabase;
                    document.KeepConnectionOpen = editorPanel.KeepConnectionOpen;
                    document.ContinueOnError = editorPanel.ContinueOnError;
                }
            }

            if (CurrentTB != null)
            {
                var localFastColoredTextBox = CurrentTB;
                localFastColoredTextBox.Focus();
                GetTextCommentRanges(localFastColoredTextBox);
                if (IsOutlineVisible())
                {
                    RebuildObjectExplorer(_cleanSqlText);
                }

                _sessionVariableRuntimeContext.ActualTabTitleText = selectedTab.Text;
                VariablesRefresh();
            }

            UpdateGitTimelineForActiveDocument();
        }

        //https://stackoverflow.com/questions/61845917/how-do-i-change-tabcontrol-close-button-image-on-mouse-hover

        private void UnPin(TabPage page, TabControl tcCurrent)
        {
            if (tcCurrent == null)
            {
                return;
            }
            (page.Tag as TabPageResultsTag).Docked = false;
            (page as TabPagePicture).PinImage = _normalPinImage;
            SynchronizeResultPinState(page, isPinned: false);

            int nr = tcCurrent.TabPages.IndexOf(page);
            if (nr == -1)
            {
                return;
            }
            Rectangle tabRect = tcCurrent.GetTabRect(nr);
            tcCurrent.Invalidate(tabRect);
        }

        private void Dokuj(TabPage page, TabControl tcCurrent)
        {
            if (tcCurrent == null)
            {
                return;
            }
            (page.Tag as TabPageResultsTag).Docked = true;
            (page as TabPagePicture).PinImage = _activePinImage;
            SynchronizeResultPinState(page, isPinned: true);
            tcCurrent.Invalidate();
        }

        private void SynchronizeResultPinState(TabPage page, bool isPinned)
        {
            if (page.Tag is not TabPageResultsTag { DocumentId: { } documentId } tag || tag.Key is not { } key)
                return;

            var execution = _editorWorkspaceViewModel.Documents
                .FirstOrDefault(document => document.Id == documentId)
                ?.SqlExecution;
            if (isPinned)
                execution?.PinResult(key);
            else
                execution?.UnpinResult(key);
        }

        public void AttachTabPage(TabPage page, TabControl tcCurrent)
        {
            if ((page.Tag as TabPageResultsTag).Docked)
            {
                UnPin(page, tcCurrent);
            }
            else
            {
                Dokuj(page, tcCurrent);
            }
        }

        private void OdDokujWszystkoEventHandler(object sender, EventArgs e)
        {
            if (CurrentSplitContainer?.Tag is ResultData rd && rd.TabControlSQLResults is TabControl tabContolWynikiSQLAktualne)
            {
                foreach (TabPage item in tabContolWynikiSQLAktualne.Controls)
                {
                    if ((item.Tag as TabPageResultsTag).Docked)
                    {
                        UnPin(item as TabPagePicture, tabContolWynikiSQLAktualne);
                    }
                }
            }
        }

        private void DeleteUndockedTabs()
        {
            if (CurrentSplitContainer?.Tag is not ResultData)
            {
                return;
            }

            TabControl tabContolWynikiSQLAktualne = (CurrentSplitContainer.Tag as ResultData).TabControlSQLResults;
            List<TabPage> ttp = new List<TabPage>();
            List<CustomDataGridView> myDataGridViews = new List<CustomDataGridView>();
            foreach (var item in tabContolWynikiSQLAktualne.TabPages)
            {
                var cc0 = item as TabPage;
                if ((cc0.Tag as TabPageResultsTag).Docked || IsPermanentDiagnosticsTab(cc0))
                    continue;
                ttp.Add(cc0);

                if (cc0.Controls.Count > 0 && cc0.Controls[0] is CustomDataGridView myDataGrid)
                {
                    myDataGridViews.Add(myDataGrid);
                }
            }
            foreach (TabPage item in ttp)
            {
                ForgetLegacyResultCommand(item);
                tabContolWynikiSQLAktualne.TabPages.Remove(item);
            }

            foreach (var myDataGrid in myDataGridViews)
            {
                try
                {
                    myDataGrid.ClearDataGridView();
                }
                catch (Exception exception)
                {
                    Trace.WriteLine($"Tab resource cleanup failed: {exception.GetType().Name}");
                }
            }
            ClearCurrentHelpReferences();
        }

        private void _leftTabsSelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabPageVariables.Tag is null && _leftTabs.SelectedTab == tabPageVariables)
            {
                InitializeVariablesControl();
            }
            else if (tabPageLegend.Tag is null && _leftTabs.SelectedTab == tabPageLegend)
            {
                InitializeObjectExplorerControl();
            }
            else if (tabPageFiles.Tag is null && _leftTabs.SelectedTab == tabPageFiles)
            {
                InitializeFilesControl();
            }

            if (IsOutlineVisible())
            {
                RebuildObjectExplorer(_cleanSqlText);
            }
        }

        public void RemoveTabData(TabControl tbControl, int numerek = -1)
        {
            {
                TabControl tabContolWynikiSQLAktualne = tbControl as TabControl;
                if (tabContolWynikiSQLAktualne != null && tabContolWynikiSQLAktualne.SelectedTab != null)
                {
                    if (numerek == -1)
                    {
                        //int 
                        numerek = tabContolWynikiSQLAktualne.SelectedIndex;
                    }

                    if (IsPermanentDiagnosticsTab(tabContolWynikiSQLAktualne.TabPages[numerek]))
                    {
                        return;
                    }

                    CustomDataGridView myDataGridView = null;

                    if (tabContolWynikiSQLAktualne.TabPages[numerek].Controls.Count > 0 &&
                        tabContolWynikiSQLAktualne.TabPages[numerek].Controls[0] is CustomDataGridView myDataGrid)
                    {
                        myDataGridView = myDataGrid;
                    }

                    TabPage removedPage = tabContolWynikiSQLAktualne.TabPages[numerek];
                    ForgetLegacyResultCommand(removedPage);
                    tabContolWynikiSQLAktualne.TabPages.RemoveAt(numerek);
                    //tabContolWynikiSQLAktualne.Invalidate();

                    if (tabContolWynikiSQLAktualne.TabCount > 0
                        && !tabContolWynikiSQLAktualne.TabPages.Cast<TabPage>().All(IsPermanentDiagnosticsTab)
                        && numerek > 0)
                    {
                        tabContolWynikiSQLAktualne.SelectTab(numerek - 1);
                    }
                    else if (tabContolWynikiSQLAktualne.TabCount > 0
                        && !tabContolWynikiSQLAktualne.TabPages.Cast<TabPage>().All(IsPermanentDiagnosticsTab))
                    {
                        tabContolWynikiSQLAktualne.SelectTab(numerek);
                    }

                    myDataGridView?.ClearDataGridView();

                    ClearCurrentHelpReferences();
                }
            }
            //CleanRam();
        }

        private void DeleteTabEventHandler(object sender, EventArgs e)
        {
            RemoveTabData((CurrentSplitContainer.Tag as ResultData).TabControlSQLResults);
        }

        private void CloseAllResultTabsEventHandler(object sender, EventArgs e)
        {
            {
                TabControl tabContolWynikiSQLAktualne = (CurrentSplitContainer.Tag as ResultData).TabControlSQLResults;

                List<CustomDataGridView> myDataGridViews = new List<CustomDataGridView>();
                List<TabPage> tabsToRemove = new List<TabPage>();
                for (int i = 0; i < tabContolWynikiSQLAktualne.Controls.Count; i++)
                {
                    var page = tabContolWynikiSQLAktualne.TabPages[i];
                    if (IsPermanentDiagnosticsTab(page))
                    {
                        if (page.Controls.OfType<DataGridView>().FirstOrDefault() is DataGridView diagnosticsGrid)
                        {
                            diagnosticsGrid.Rows.Clear();
                        }
                        continue;
                    }

                    tabsToRemove.Add(page);
                    if (page.Controls.Count > 0 && page.Controls[0] is CustomDataGridView myDataGrid)
                    {
                        myDataGridViews.Add(myDataGrid);
                    }
                }

                foreach (TabPage page in tabsToRemove)
                {
                    ForgetLegacyResultCommand(page);
                    tabContolWynikiSQLAktualne.TabPages.Remove(page);
                }

                foreach (var myDataGrid in myDataGridViews)
                {
                    try
                    {
                        myDataGrid.ClearDataGridView();
                    }
                    catch (Exception exception)
                    {
                        Trace.WriteLine($"Tab resource cleanup failed: {exception.GetType().Name}");
                    }
                }
                ClearCurrentHelpReferences();
            }
        }

        private void CloseOthersEventHandler(object sender, EventArgs e)
        {
            if (CurrentSplitContainer?.Tag is ResultData rd && rd.TabControlSQLResults is TabControl tabContolWynikiSQLAktualne)
            {
                List<TabPage> ttp = new List<TabPage>();
                List<CustomDataGridView> myDataGridViews = new List<CustomDataGridView>();
                foreach (var item in tabContolWynikiSQLAktualne.TabPages)
                {
                    var cc0 = item as TabPage;
                    if (tabContolWynikiSQLAktualne.SelectedTab == cc0 || IsPermanentDiagnosticsTab(cc0))
                        continue;

                    ttp.Add(cc0);

                    if (cc0.Controls.Count > 0 && cc0.Controls[0] is CustomDataGridView myDataGrid)
                    {
                        myDataGridViews.Add(myDataGrid);
                    }
                }
                foreach (TabPage item in ttp)
                {
                    tabContolWynikiSQLAktualne.TabPages.Remove(item);
                }

                foreach (var myDataGrid in myDataGridViews)
                {
                    try
                    {
                        myDataGrid.ClearDataGridView();
                    }
                    catch (Exception exception)
                    {
                        Trace.WriteLine($"Tab resource cleanup failed: {exception.GetType().Name}");
                    }
                }
                ClearCurrentHelpReferences();
            }
        }


        private void tabControlMain_ControlRemoved(object sender, ControlEventArgs e)
        {
            if (e.Control is not TabPage removedTab)
                return;

            // Save references before cleanup
            var editorPanel = _tabManager.GetEditorPanel(removedTab);
            bool hasUpper = editorPanel is not null;
            _tabManager.UnregisterTab(removedTab);

            if (EditorTabPages.Count == 1)
            {
                AddMainTab(null);
            }

            if (!hasUpper)
            {
                return;
            }

            try
            {
                var fctb = editorPanel.CurrentTb;
                fctb.CloseBindingFile();

            }
            catch (Exception ex)
            {
                _loggerLoud.MessageBox_Show(this, ex.Message, "Tab error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
