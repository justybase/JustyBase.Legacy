// BaseWindow editor menu handlers and document map partial.
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
using JustData.Application.History;
using JustData.Application.Editor;
using JustData.ViewModels.Editor;
using DatabaseDataGridView.WinForms;
using DatabaseDataGridView.WinForms.Coloring;
using FastColoredTextBoxNS;
using FastColoredTextBoxNS.Helpers;
using JustDataAdditionalForms;
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
        private void CutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurrentTB.Cut();
        }

        private void CopyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurrentTB.Copy();
        }

        private void CopyRawToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                var text = CurrentTB?.Selection?.Text;
                if (!string.IsNullOrEmpty(text))
                {
                    Clipboard.SetText(text);
                }
            }
            catch (Exception ex)
            {
                _loggerLoud.LogError(ex.Message, ex);

            }
        }

        private void PasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ForceNormalPaste = true;
            CurrentTB.Paste();
            ForceNormalPaste = false;
        }


        //https://stackoverflow.com/questions/5338587/set-tabpage-header-color

        private void QuitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void FindToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurrentTB?.ShowFindDialog();
        }

        private void ReplaceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurrentTB?.ShowReplaceDialog();
        }

        private void CollapseSelectedBlockToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurrentTB.CollapseBlock(CurrentTB.Selection.Start.iLine, CurrentTB.Selection.End.iLine);
        }

        public void CollapseAllregion(FastColoredTextBox fctb)
        {
            Dictionary<int, int> colLines = new Dictionary<int, int>();
            int balance = 0;
            for (int iLine = 0; iLine < fctb.LinesCount; iLine++)
            {
                if (!String.IsNullOrEmpty(fctb[iLine].FoldingStartMarker))
                {
                    colLines[iLine] = ++balance;
                }
                if (!String.IsNullOrEmpty(fctb[iLine].FoldingEndMarker))
                {
                    --balance;
                }
            }
            var myList = colLines.ToList();
            myList.Sort((pair1, pair2) => -pair1.Value.CompareTo(pair2.Value));

            for (int i = 0; i < myList.Count; i++)
            {
                fctb.CollapseFoldingBlock(myList[i].Key);
            }
        }

        private void CollapseAllregionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CollapseAllregion(CurrentTB);
        }

        private void ExapndAllregionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurrentTB.ExpandAllFoldingBlocks();
        }

        private void IncreaseIndentSiftTabToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurrentTB.IncreaseIndent();
        }

        private void DecreaseIndentTabToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurrentTB.DecreaseIndent();
        }
        private async void ShowLoginForm()
        {
            using LoginForm loginForm = _loginFormFactory.Create();
            loginForm.SetRememberAsDefaultToFalse();
            var r = loginForm.ShowDialog();
            if (r == DialogResult.Cancel)
            {
                return;
            }
            var selection = loginForm.Result ?? throw new InvalidOperationException("A login selection is required.");
            string previousConnectionName = SelectedConnectionName;
            string newConnectionName = selection.Profile.Name;

            _applicationSession.SetLogin(selection, loginForm.Profiles);

            // Live sessions freeze ConnectionString at creation.
            // Evict so CbConnectionsSelectedIndexChanged rebuilds the provider and re-downloads schema.
            // Only treat as rename when the previous name is gone from profiles (not add/switch).
            bool previousRenamedAway = !string.IsNullOrEmpty(previousConnectionName)
                && !string.Equals(previousConnectionName, newConnectionName, StringComparison.OrdinalIgnoreCase)
                && !_connectionProfileCatalog.TryGetProfile(previousConnectionName, out _);

            if (previousRenamedAway)
            {
                _connectionSessions.Remove(previousConnectionName);
            }
            _connectionSessions.Remove(newConnectionName);

            if (previousRenamedAway)
            {
                foreach (TabPage tab in EditorTabPages)
                {
                    (_tabManager.GetEditorPanel(tab) as SQLUpperPanel)?.RemoveConnection(previousConnectionName);
                }
                _editorCatalogState.RemoveConnection(previousConnectionName);
            }

            SelectedConnectionName = newConnectionName;
            SelectedDatabase = selection.Profile.Database;
            _editorCatalogState.AddConnection(newConnectionName);

            _applicationSettingsContext.Config.FastLogin = selection.FastLogin;
            this.Text = "JustyBaseLegacy - " + newConnectionName;

            try
            {
                await CbConnectionsSelectedIndexChanged(enabled => CurrentUpper?.SetEnabledConnectionsDatabases(enabled));
            }
            catch (OperationCanceledException)
            {
                // Superseded by shutdown or a later connection switch.
            }
            catch (Exception ex)
            {
                _loggerLoud.LogError("Schema refresh after connection edit failed", ex);
                SchemaRefreshOptionEnable(true);
            }
        }
        private History _history;
        private void HistoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_tabManager is DockSuiteTabManager dockSuite)
            {
                dockSuite.ShowHistory(
                    o => _colorTheme.ColorForm(o),
                    f => _uiHelperService.DoubleBufDateGridView(f),
                    (a, b, c) => AddMainTab(a, b, c),
                    HistoryDatFile,
                    _applicationSettingsContext.Config.UseSpecialColoring,
                    _historyStore);
                return;
            }

            if (_history == null || _history.IsDisposed)
            {
                _history = new History(o => _colorTheme.ColorForm(o), f => _uiHelperService.DoubleBufDateGridView(f), (a, b, c) => AddMainTab(a, b, c), HistoryDatFile, _applicationSettingsContext.Config.UseSpecialColoring);
            }
            _history.Show();
            _history.Focus();
        }

        public void FctbSelectionChangedDelayed(object sender, EventArgs e)
        {
            if (sender is FastColoredTextBox editor
                && _documentIdsByEditor.TryGetValue(editor, out var documentId))
            {
                _editorWorkspaceViewModel.Documents
                    .FirstOrDefault(document => document.Id == documentId)
                    ?.UpdateEditorSelection(editor.SelectionStart, editor.SelectionLength, editor.SelectionStart);
            }

            if (CurrentTB is null)
            {
                return;
            }
            var tmp = CurrentTB.Selection.Start;
            this.cursorPositionTextBox.Text = $"(col:{tmp.iChar + 1} row:{(tmp.iLine + 1).ToString("N0")})";

            CurrentTB.Range.ClearStyle(_colorTheme.CurrentFctbColors.SameWordsStyle);

            if (CurrentTB.SelectionLength == 1 || CurrentTB.SelectionLength >= 2_000)
            {
                return;
            }

            if (CurrentTB.TextLength >= 800_000)
            {
                FastColoredTextBoxNS.Range fragment = CurrentTB.Selection;
                if (!CurrentTB.Selection.IsEmpty)
                {
                    if (CurrentTB.SelectionLength <= 3)
                    {
                        return;
                    }
                    fragment = CurrentTB.Selection;
                    string text = fragment.Text;
                    if (string.IsNullOrWhiteSpace(text))
                    {
                        return;
                    }
                    if (text.Contains('\n'))
                    {
                        return;
                    }
                    text = Regex.Escape(text);

                    var rangesTmp = CurrentTB.VisibleRange;
                    int fromLine = rangesTmp.FromLine;
                    int toLine = rangesTmp.ToLine;

                    for (int i = fromLine; i < toLine; i++)
                    {
                        if (CurrentTB.LineInfos[i].VisibleState == VisibleState.Visible)
                        {
                            var range = new FastColoredTextBoxNS.Range(CurrentTB, i);
                            var ranges = range.GetRanges(text, RegexOptions.IgnoreCase).ToArray();
                            foreach (var r in ranges)
                                r.SetStyle(_colorTheme.CurrentFctbColors.SameWordsStyle);
                        }
                    }
                }
            }
            else
            {
                //get fragment around caret
                FastColoredTextBoxNS.Range fragment = CurrentTB.Selection;

                if (fragment.Length > 0 && fragment.Length < 30 && string.IsNullOrWhiteSpace(CurrentTB.Selection.Text))
                {
                    return;
                }

                if (!CurrentTB.Selection.IsEmpty)
                {
                    fragment = CurrentTB.Selection;
                    string text = fragment.Text;
                    text = Regex.Escape(text);

                    var ranges = CurrentTB.Range.GetRanges(text, RegexOptions.IgnoreCase).ToArray();
                    if (ranges.Length > 1)
                        foreach (var r in ranges)
                            r.SetStyle(_colorTheme.CurrentFctbColors.SameWordsStyle);
                }
                else
                {
                    //get fragment around caret
                    fragment = CurrentTB.Selection.GetFragment(@"\w");
                    if (fragment.Length >= 2_000)
                    {
                        return;
                    }
                    string text = fragment.Text;
                    if (text.Contains('\n'))
                    {
                        return;
                    }

                    //highlight same words
                    var ranges = CurrentTB.Range.GetRanges("\\b" + text + "\\b", RegexOptions.IgnoreCase).ToArray();
                    if (ranges.Length > 1)
                        foreach (var r in ranges)
                            r.SetStyle(_colorTheme.CurrentFctbColors.SameWordsStyle);
                }

                if (sender is FastColoredTextBox signatureEditor
                    && _signaturePopup.Visible
                    && (signatureEditor.Name.StartsWith("NetezzaSQL", StringComparison.Ordinal)
                        || _generalDbService.DriverName(SelectedConnectionName) == "NetezzaSQL"))
                {
                    EditorDocumentViewModel? signatureDocument = EnsureWorkspaceDocument(signatureEditor);
                    if (signatureDocument is null)
                        return;

                    string documentUri = signatureDocument.Id.ToString();
                    var help = _legacySqlAuthoringServices.GetSignatureHelp(signatureEditor.Text, signatureEditor.SelectionStart, documentUri);
                    if (help is null)
                        _signaturePopup.Hide();
                    else
                        _signaturePopup.Update(help);
                }
            }
        }

        private void GoForwardCtrlShiftToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurrentTB.NavigateForward();
        }

        private void GoBackwardCtrlToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurrentTB.NavigateBackward();
        }

        private void AutoIndentToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurrentTB.DoAutoIndent();
        }

        const int _maxBracketSearchIterations2 = 2000;

        private static void GoLeftBracket(FastColoredTextBox tb, char leftBracket, char rightBracket)
        {
            FastColoredTextBoxNS.Range range = tb.Selection.Clone();//need to clone because we will move caret
            int counter = 0;

            int maxIterations = _maxBracketSearchIterations2;
            while (range.GoLeftThroughFolded())//move caret left
            {
                if (range.CharAfterStart == leftBracket) counter++;
                if (range.CharAfterStart == rightBracket) counter--;
                if (counter == 1)
                {
                    //found
                    tb.Selection.Start = range.Start;
                    tb.DoSelectionVisible();
                    break;
                }
                //
                maxIterations--;
                if (maxIterations <= 0) break;
            }
            tb.Invalidate();
        }

        private static void GoRightBracket(FastColoredTextBox tb, char leftBracket, char rightBracket)
        {
            var range = tb.Selection.Clone();//need clone because we will move caret
            int counter = 0;
            int maxIterations = _maxBracketSearchIterations2;
            do
            {
                if (range.CharAfterStart == leftBracket) counter++;
                if (range.CharAfterStart == rightBracket) counter--;
                if (counter == -1)
                {
                    //found
                    tb.Selection.Start = range.Start;
                    tb.Selection.GoRightThroughFolded();
                    tb.DoSelectionVisible();
                    break;
                }
                //
                maxIterations--;
                if (maxIterations <= 0) break;
            } while (range.GoRightThroughFolded());//move caret right

            tb.Invalidate();
        }

        private void GoLeftBracketToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GoLeftBracket(CurrentTB, '(', ')');
        }
        private void GoRightBracketToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GoRightBracket(CurrentTB, '(', ')');
        }


        private void MiPrint_Click(object sender, EventArgs e)
        {
            CurrentTB.Print(new PrintDialogSettings() { ShowPrintPreviewDialog = true });
        }

        private void SetSelectedAsReadonlyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurrentTB.Selection.ReadOnly = true;
        }

        private void SetSelectedAsWritableToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurrentTB.Selection.ReadOnly = false;
        }

        private void ChangeHotkeysToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (CurrentTB is null)
            {
                return;
            }

            var form = new HotkeysEditorForm(CurrentTB.HotkeysMapping, "(only for text box)");
            if (form.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                foreach (TabPage tabPage in EditorTabPages)
                {
                    var fctb = _tabManager.GetEditor(tabPage);
                    if (fctb is not null)
                        fctb.HotkeysMapping = form.GetHotkeys();
                }

                _applicationSettingsContext.Config.EditorHotkeys = CurrentTB.Hotkeys;
            }
        }

        private void CommentSelectedLinesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurrentUpper.commentSelectedLinesToolStripMenuItemClick(sender, e);
        }

        private void UncommentSelectedLinesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurrentUpper.uncommentSelectedLinesToolStripMenuItemClick(sender, e);
        }

        private void AddDollarSign_Click(object sender, EventArgs e)
        {
            string fragment = CurrentTB.Selection.GetFragment(@"(\$|\w)").Text;
            if (fragment.Length >= 2 && fragment.Length <= 35)
            {
                // CurrentTB.BeginUpdate();
                List<KeyValuePair<int, int>> myList = null;
                if (_applicationSettingsContext.Config.RestoreFoldingState)
                {
                    myList = RememberFoldingState();
                }

                if (fragment.StartsWith('$'))
                {
                    CurrentTB.Text = Regex.Replace(CurrentTB.TextFast, $"\\$\\b{fragment[1..]}\\b", $"{fragment[1..]}", RegexOptions.IgnoreCase);
                }
                else
                {
                    CurrentTB.Text = Regex.Replace(CurrentTB.TextFast, $"\\b{fragment}\\b", $"${fragment}", RegexOptions.IgnoreCase);
                }
                if (_applicationSettingsContext.Config.RestoreFoldingState)
                {
                    RestoreFoldingState(myList);
                }
            }
            else
            {
                _loggerLoud.MessageBox_Show(this, "Name must be between 2 and 35 characters.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private List<KeyValuePair<int, int>> RememberFoldingState()
        {
            Dictionary<int, int> colLines = new Dictionary<int, int>();
            int balance = 0;
            for (int iLine = 0; iLine < CurrentTB.LinesCount; iLine++)
            {
                if (!String.IsNullOrEmpty(CurrentTB[iLine].FoldingStartMarker) && CurrentTB.LineInfos[iLine].VisibleState == VisibleState.StartOfHiddenBlock)
                {
                    colLines[iLine] = ++balance;
                }
                if (!String.IsNullOrEmpty(CurrentTB[iLine].FoldingEndMarker))
                {
                    --balance;
                }
            }
            var myList = colLines.ToList();
            myList.Sort((pair1, pair2) => -pair1.Value.CompareTo(pair2.Value));
            return myList;
        }

        private void RestoreFoldingState(List<KeyValuePair<int, int>> myList)
        {
            try
            {
                foreach (var item in myList)
                {
                    if (CurrentTB[item.Key].FoldingStartMarker is not null)
                    {
                        CurrentTB.CollapseFoldingBlock(item.Key);
                    }
                }
            }
            catch (Exception exception)
            {
                Trace.WriteLine($"Editor state cleanup failed: {exception.GetType().Name}");
            }
        }
        private readonly Dictionary<FastColoredTextBox, DocumentMap> _listMapped = new Dictionary<FastColoredTextBox, DocumentMap>();

        private void Map_Click(object sender, EventArgs e)
        {
            if (CurrentTB is null)
            {
                return;
            }

            if (!_listMapped.TryGetValue(CurrentTB, out DocumentMap? map))
            {
                map = new DocumentMap
                {
                    Name = "documentMap",
                    Scale = 0.3f,
                    ScrollbarVisible = false,
                    Target = CurrentTB,
                };

                DocumentMapLayoutHelper.ConfigureMapColors(
                    map,
                    _applicationSettingsContext.Config.UseSpecialColoring,
                    _applicationSettingsContext.Config.DocMapBackColor,
                    _applicationSettingsContext.Config.DocMapForeColor);

                DocumentMapLayoutHelper.Show(CurrentTB, map);
                _listMapped.Add(CurrentTB, map);
                return;
            }

            if (map.Visible)
            {
                DocumentMapLayoutHelper.Hide(map, CurrentTB);
            }
            else
            {
                DocumentMapLayoutHelper.Show(CurrentTB, map);
            }
        }

        private void Ustaw_Click(object sender, EventArgs e)
        {
            if (_tabManager is DockSuiteTabManager dockSuite)
            {
                dockSuite.ShowPreferences(
                    RepaintPreferences,
                    SaveManySqlToDisk,
                    _applicationSettingsContext,
                    _snippetInitializationContext,
                    _settingsPersistence.SaveConfig,
                    _recentFileRuntimeContext.SaveRecentFiles,
                    _uiHelperService,
                    _colorTheme,
                    _netezzaAutocompleteState);
                return;
            }

            using var pr = new PreferencesForm(
                RepaintPreferences,
                SaveManySqlToDisk,
                _applicationSettingsContext,
                _snippetInitializationContext,
                _settingsPersistence.SaveConfig,
                _recentFileRuntimeContext.SaveRecentFiles,
                _uiHelperService,
                _colorTheme,
                _netezzaAutocompleteState);
            pr.ShowDialog();
        }



        int nr = 0;
        /// <summary>
        /// For full screen purpose
        /// </summary>
        private void NextVsiaulMode()
        {
            var firstTab = EditorTabPages.Count > 0 ? EditorTabPages[0] : null;
            var spl = firstTab is not null ? _tabManager.GetSplitContainerForTab(firstTab) : null;
            if (nr % 5 == 0)
            {
                spl.SplitterDistance = 20;
                this.FormBorderStyle = FormBorderStyle.None;
                this.WindowState = FormWindowState.Maximized;
                this.menuStrip1.Visible = false;
                _leftTabs.Visible = false;
                splitContainer1.SplitterDistance = 0;
            }
            else if (nr % 5 == 1)
            {
                spl.SplitterDistance = 20;
                this.WindowState = FormWindowState.Normal;
                this.FormBorderStyle = FormBorderStyle.None;
                this.Bounds = Screen.PrimaryScreen.Bounds;

                this.menuStrip1.Visible = false;
                _leftTabs.Visible = false; ;
                splitContainer1.SplitterDistance = 0;
            }
            else if (nr % 5 == 2)
            {
                spl.SplitterDistance = spl.Height - 20;

                this.FormBorderStyle = FormBorderStyle.Sizable;
                this.WindowState = FormWindowState.Maximized;
                this.FormBorderStyle = FormBorderStyle.None;
                this.WindowState = FormWindowState.Maximized;

                this.menuStrip1.Visible = false;
                _leftTabs.Visible = false; ;
                splitContainer1.SplitterDistance = 0;
            }
            else if (nr % 5 == 3)
            {
                spl.SplitterDistance = spl.Height - 20;
                WindowState = FormWindowState.Normal;
                this.FormBorderStyle = FormBorderStyle.None;
                this.Bounds = Screen.PrimaryScreen.Bounds;

                this.menuStrip1.Visible = false;
                _leftTabs.Visible = false;
                splitContainer1.SplitterDistance = 0;
            }
            else if (nr % 5 == 4)
            {
                WindowState = FormWindowState.Maximized;
                this.FormBorderStyle = FormBorderStyle.Sizable;
                this.menuStrip1.Visible = true;
                _leftTabs.Visible = true;

                spl.SplitterDistance = (int)Math.Round(spl.Parent.Height * 0.8);
                splitContainer1.SplitterDistance = (101 * this.splitContainer1.Width) / 867;
            }
            nr++;
        }


        private void AddAliases_Click(object sender, EventArgs e)
        {
            MiscellaneousHelper.AddSqlAliases(CurrentTB, (ActualSuggestionList as INetezzaAutocompleteSource)?.AliasHints);
        }


        private void SaveAsSnippet_Click(object sender, EventArgs e)
        {
            var fctb = CurrentTB;
            if (fctb.Selection.Length <= 3)
            {
                _loggerLoud.MessageBox_Show(this, "Selection is too short.", "Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            string snipetText = fctb.SelectedText;

            var sn = new SaveSnippet()
            {
                StartPosition = FormStartPosition.Manual
            };
            var p = MousePosition;
            var p2 = cmMain.PointToClient(p);
            p.Offset(-p2.X, -p2.Y);
            sn.Location = p;

            if (sn.ShowDialog() == DialogResult.OK)
            {
                if (sn.IsStandard())
                {
                    _netezzaAutocompleteState.AddMonkeySnippet($"@@{sn.GetName()} {snipetText}");
                }
                else if (sn.IsQuick())
                {
                    _applicationSettingsContext.Config.QuickSnippets.Add(sn.GetName(), snipetText);
                }
            }
        }


        private void MakeCodeToTempTable_Click(object sender, EventArgs e)
        {
            MiscellaneousHelper.CodeToTempTable(CurrentTB);
        }


        private void ShowDataFolderMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("explorer.exe", $"{_applicationSettingsContext.ConfigDirectory}\\data\\...");
        }

        private void ClearDataFolderMenuItem_Click(object sender, EventArgs e)
        {
            if (DialogResult.Yes == _loggerLoud.MessageBox_Show(this, "This action cannot be undone.", "Remove?", MessageBoxButtons.YesNo, MessageBoxIcon.Warning))
            {
                System.IO.DirectoryInfo di = new DirectoryInfo($"{_applicationSettingsContext.ConfigDirectory}\\data\\...");
                foreach (FileInfo file in di.GetFiles())
                {
                    try
                    {
                        file.Delete();
                    }
                    catch (Exception ex)
                    {
                        _loggerLoud.MessageBox_Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }

                }
                foreach (DirectoryInfo dir in di.GetDirectories())
                {
                    dir.Delete(true);
                }
            }
        }
    }
}
