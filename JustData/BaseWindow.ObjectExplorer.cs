// BaseWindow SQL object explorer (Legend) and editor integration partial.
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
using JustyBase.NetezzaSqlParser.Authoring;
using SqlTypingPerfProbe = FastColoredTextBoxNS.Helpers.SqlTypingPerfProbe;
using FastColoredTextBoxNS.Helpers;
using JustDataAdditionalForms;
using JustData.Application.Schema;
using JustData.Application.Startup;
using JustData.Application.Editor;
using JustData.ViewModels.Editor;
using JustyBase.NetezzaDriver;
using System.Drawing;
using JustyBase.NetezzaSqlParser.Linter;
using JustyBaseLegacy.UI.Helpers;
using JustyBaseLegacy.Services;
using JustyBaseLegacy.UI.Controls;
using JustyBaseLegacy.UI.DbForms;
using JustyBaseLegacy.UI.Models;
using JustyBaseLegacy.UI.Schema;
using JustyBaseLegacy.UI.Forms;
using JustyBaseLegacy.UI.ObjectExplorer;
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
        public TreeView DgvObjectExplorer
        {
            get
            {
                if (_mvvmObjectExplorerControl is null)
                {
                    if (this.InvokeRequired)
                    {
                        this.Invoke(() => InitializeObjectExplorerControl());
                    }
                    else
                    {
                        InitializeObjectExplorerControl();
                    }
                }
                return _mvvmObjectExplorerControl?.OutlineTreeView;
            }
        }

        private void InitializeObjectExplorerControl()
        {
            if (_mvvmObjectExplorerControl is not null)
                return;

            if (_tabManager is UI.DockSuiteTabManager dsm)
            {
                _mvvmObjectExplorerControl = new Controls.MvvmObjectExplorerControl(_objectExplorerViewModel);
                _mvvmObjectExplorerControl.ReferenceActivated += NavigateToOutlineReference;
                dsm.RegisterPersistentTool("Outline", _mvvmObjectExplorerControl, WeifenLuo.WinFormsUI.Docking.DockState.DockLeft);
                if (dsm.GetToolWindow("Outline") is { } outlineTool)
                {
                    outlineTool.Activated += OnOutlineToolActivated;
                    outlineTool.VisibleChanged += OnOutlineToolVisibleChanged;
                }
                tabPageLegend.Tag = "initialized";
            }
        }

        private bool IsOutlineVisible()
        {
            if (_tabManager is UI.DockSuiteTabManager dsm)
                return dsm.IsToolWindowVisible("Outline");

            return _leftTabs.SelectedTab?.Text == "Outline";
        }

        private void OnOutlineToolActivated(object? sender, EventArgs e) => RefreshVisibleOutline();

        private void OnOutlineToolVisibleChanged(object? sender, EventArgs e)
        {
            if (IsOutlineVisible())
                RefreshVisibleOutline();
        }

        private void RefreshVisibleOutline()
        {
            if (CurrentTB is not null)
                RebuildObjectExplorer(_cleanSqlText);
        }

        private void RebuildObjectExplorer(string text)
        {
            if (!_applicationSettingsContext.Config.DoLegend || !IsOutlineVisible())
                return;

            if (_mvvmObjectExplorerControl is null)
                InitializeObjectExplorerControl();
            _ = RebuildMvvmObjectExplorerAsync(text);
        }

        private async Task RebuildMvvmObjectExplorerAsync(string text)
        {
            try
            {
                if (_mvvmObjectExplorerControl is not null && !_mvvmObjectExplorerControl.IsDisposed)
                {
                    await _mvvmObjectExplorerControl.RebuildAsync(text, SelectedConnectionName);
                }
            }
            catch (OperationCanceledException)
            {
                // A newer editor change superseded this object-explorer refresh.
            }
            catch (Exception exception)
            {
                Trace.WriteLine($"Object explorer rebuild failed: {exception.GetType().Name}");
                _loggerLoud.LogError("Object explorer rebuild failed", exception);
            }
        }

        /// <summary>
        /// F4 / Ctrl+click: jump to a temp table, CTE, or DROP TABLE name in the script.
        /// Uses the same sync parse as the MVVM outline repository (no legacy control).
        /// </summary>
        private bool TryNavigateToOutlineDefinition(string clickedWord)
        {
            if (string.IsNullOrWhiteSpace(clickedWord) || CurrentTB is null)
                return false;

            foreach (var item in JustyBaseLegacy.UI.Schema.LegacySqlReferenceParser.ParseNavigableDefinitions(_cleanSqlText))
            {
                if (!item.Name.Trim().Equals(clickedWord, StringComparison.OrdinalIgnoreCase))
                    continue;

                SelectOutlineRowByName(item.Name);
                NavigateToOutlineReference(item);
                return true;
            }

            return false;
        }

        private void SelectOutlineRowByName(string name)
        {
            if (!IsOutlineVisible())
                return;

            if (_mvvmObjectExplorerControl is null)
                return;
            _mvvmObjectExplorerControl.SelectNodeByName(name);
        }

        private void NavigateToOutlineReference(SchemaReference reference)
        {
            if (CurrentTB is null)
                return;

            string name = reference.Name.Trim();
            if (name.Length == 0 || reference.Position < 0 || reference.Position >= CurrentTB.TextLength)
                return;

            CurrentTB.GoEnd();
            CurrentTB.SelectionStart = reference.Position;
            CurrentTB.SelectionLength = Math.Min(name.Length, CurrentTB.TextLength - reference.Position);
            CurrentTB.DoSelectionVisible();
            CurrentTB.Focus();
        }

        private bool OutlineContainsWord(string word) =>
            JustyBaseLegacy.UI.Schema.LegacySqlReferenceParser.Parse(_cleanSqlText)
                .Any(reference => reference.Name.Trim().Equals(word, StringComparison.OrdinalIgnoreCase));

        private SqlTextModifyDefaultSqlImplementations _sqlTextChangingDefaultSqlImplementation;
        private readonly NzSignatureHelpPopup _signaturePopup = new();

        public void FctbTextChanging(object sender, TextChangingEventArgs e)
        {
            _sqlTextChangingDefaultSqlImplementation ??= new SqlTextModifyDefaultSqlImplementations(_autocompleteClass, _applicationSettingsContext.Config);

            _sqlTextChangingDefaultSqlImplementation.TextChangingDefaultSqlImplementation(sender as FastColoredTextBox, e);
        }

        public void FastColoredNew_KeyDown(object sender, KeyEventArgs e) =>
            _ = RunUiEventAsync(nameof(FastColoredNew_KeyDown), () => FastColoredNew_KeyDownAsync(sender, e));

        private async Task FastColoredNew_KeyDownAsync(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.Control && e.KeyCode is Keys.N or Keys.T)
                {
                    OpenNewSqlDocument();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    return;
                }

                if (e.Control && e.KeyCode is Keys.F7 or Keys.F8)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    await RunSQL(
                        0,
                        e.KeyCode == Keys.F7 ? ExportOptions.xlsx : ExportOptions.csv);
                    return;
                }

                if (sender is FastColoredTextBox authoringEditor
                    && (authoringEditor.Name.StartsWith("NetezzaSQL", StringComparison.Ordinal)
                        || _generalDbService.DriverName(SelectedConnectionName) == "NetezzaSQL"))
                {
                    if (e.KeyData == Keys.F12)
                    {
                        FctbGoToDefinition(authoringEditor);
                        e.Handled = true;
                        return;
                    }

                    if (e.KeyData == (Keys.F12 | Keys.Shift))
                    {
                        FctbShowReferences(authoringEditor);
                        e.Handled = true;
                        return;
                    }

                    if (e.KeyData == Keys.F2)
                    {
                        FctbRenameSymbol(authoringEditor);
                        e.Handled = true;
                        return;
                    }

                    if (e.KeyCode == Keys.Escape || e.KeyCode == Keys.OemCloseBrackets)
                        _signaturePopup.Hide();
                }

                if (_signaturePopup.Visible)
                {
                    if (e.KeyCode == Keys.Up)
                    {
                        _signaturePopup.SelectPreviousOverload();
                        e.Handled = true;
                        return;
                    }
                    if (e.KeyCode == Keys.Down)
                    {
                        _signaturePopup.SelectNextOverload();
                        e.Handled = true;
                        return;
                    }
                }

            if (e.KeyData == (Keys.C | Keys.Alt))
            {
                var fastColoredTextBox = sender as FastColoredTextBox;
                int position = fastColoredTextBox.SelectionStart;
                (var word, var length) = fastColoredTextBox.CurrentWord(_applicationSettingsContext.Config.CurrentWordLengthLimit);

                foreach (string snippet in _netezzaAutocompleteState.MonkeySnippets)
                {
                    if (snippet.StartsWith($"@@{word} ", StringComparison.OrdinalIgnoreCase))
                    {
                        string snippet2 = snippet.Replace("\r", "");
                        int index1 = snippet2.IndexOf(' ');
                        fastColoredTextBox.SelectionStart = position - length;
                        fastColoredTextBox.SelectionLength = length;

                        string text = snippet2.Substring(index1 + 1);

                        if (text.Contains('^'))
                        {
                            int a = text.Length - text.IndexOf('^');

                            fastColoredTextBox.InsertText(text);
                            while (--a > 0)
                            {
                                fastColoredTextBox.Selection.GoLeftThroughFolded();
                            }
                            fastColoredTextBox.InsertText("\b");
                        }
                        else
                        {
                            fastColoredTextBox.InsertText(text);
                        }

                        break;
                    }
                }
                e.Handled = true;
            }
            else if (e.KeyData == (Keys.E | Keys.Control))
            {
                CurrentTB.ExpandAllFoldingBlocks();
                e.Handled = true;
            }
            else if (e.KeyData == (Keys.R | Keys.Control))
            {
                CollapseAllregion(CurrentTB);
                e.Handled = true;
            }
            else if (e.KeyData == (Keys.B | Keys.Control))
            {
                splitContainer1.Panel1Collapsed = !splitContainer1.Panel1Collapsed;
                e.Handled = true;
            }
            else if (e.KeyData == Keys.F4)
            {
                _currentMyGrid?.HideFilters();

                var range = new FastColoredTextBoxNS.Range(CurrentTB, CurrentTB.Selection.Start, CurrentTB.Selection.Start);
                string clickedWord = range.GetFragment(@"[^(\s|,|;)]").Text;

                if (_leftTabs.SelectedTab.Text != "Outline")
                {
                    RebuildObjectExplorer(_cleanSqlText);
                }

                if (TryNavigateToOutlineDefinition(clickedWord))
                    return;

                if (_generalDbService.DriverName(SelectedConnectionName) == "NetezzaSQL")
                {
                    var match = _baseTableNZ.Match(clickedWord);
                    if (!clickedWord.Contains('.'))
                    {
                        match = _baseTableNZ.Match($"{this.SelectedDatabase}..{clickedWord}");
                    }
                    if (match.Success)
                    {
                        string db = match.Groups["base"].Value;
                        string table = match.Groups["table"].Value;
                        if (_completionContext.DatabaseSchemaLookup.TryGetValue(SelectedConnectionName, out var value) && value.TryGetValue(db, out var p1))
                        {
                            if (p1.TryGetValue(table, out var p2))
                            {

                                TypeInDatabase typeInDatabase = _schemaTables.TablesByConnection.TryGetValue(SelectedConnectionName, out var btDict1)
                                        && btDict1.TryGetValue(p2.tableId, out var btInfo1)
                                    ? btInfo1.TABLE_KIND
                                    : TypeInDatabase.table;
                                if (typeInDatabase == TypeInDatabase.table || typeInDatabase == TypeInDatabase.view || typeInDatabase == TypeInDatabase.thisExternal)
                                {
                                    if (_mvvmDatabaseExplorerControl is not null)
                                    {
                                        SelectNetezzaObjectInExplorer(SelectedConnectionName, db, p2.tableId);
                                    }

                                    CurrentTB.ClearHints();
                                    CurrentTB.AddHint(range, new CustomHint(clickedWord, CurrentTB, _objectExplorerNavigationController, _ddlCodeProvider, db, table, typeInDatabase, _colorTheme), false, false, false);
                                    //var h = CurrentTB.AddHint(range, new DataGridView(), false, false, false);
                                    //h.DoVisible();
                                }
                            }
                        }
                    }
                }
                else if (
                    _generalDbService.DriverName(SelectedConnectionName) == "DB2"
                    || _generalDbService.DriverName(SelectedConnectionName) == "Oracle"
                    || _generalDbService.DriverName(SelectedConnectionName) == "MsSqlStd"
                    || _generalDbService.DriverName(SelectedConnectionName) == "MsSqlTrusted"
                    || _generalDbService.DriverName(SelectedConnectionName) == "Postgres"
                    || _generalDbService.DriverName(SelectedConnectionName) == "SQLite"
                    || _generalDbService.DriverName(SelectedConnectionName) == "MySql"
                    )
                {
                    GoToObjectNotNetezza(clickedWord);
                }
            }
            }
            catch (Exception ex)
            {
                _loggerLoud.LogError(ex.Message, ex);
            }
        }

        string _cleanSqlText = "";



        private string _empty = "";
        public void GetTextCommentRanges(FastColoredTextBox fctb)
        {
            _cleanSqlText = fctb.GetTextCommentRanges(_colorTheme.CurrentFctbColors, ref _empty, _cleanSqlText);
        }

        public void GetTextCommentRanges(string txt, ref string res)
        {
            MiscellaneousExtensions.GetTextCommentRanges(_colorTheme.CurrentFctbColors, txt, ref res);
        }

        public void FctbTextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not FastColoredTextBox fctb)
                return;

            // #region agent perf
            SqlTypingPerfProbe.Instance.EnsureInitialized();
            SqlTypingPerfLocal.Enabled = SqlTypingPerfProbe.Instance.Enabled;
            // #endregion

            string documentKey = _documentIdsByEditor.TryGetValue(fctb, out var documentId)
                ? documentId.ToString()
                : fctb.GetHashCode().ToString();

            int textLength = fctb.TextLength;
            fctb.DelayedTextChangedInterval = SqlPerformancePolicy.GetTypingDelayedMs(fctb.LinesCount, textLength);
            // #region agent perf
            SqlTypingPerfProbe.Instance.MarkDocChange(documentKey, textLength, fctb.LinesCount, e.ChangedRange.Length);
            // #endregion

            string? currentColumn = _netezzaAutocompleteState.CurrentColumn;
            // #region agent perf
            using (SqlTypingPerfProbe.Instance.Measure(
                       "host.fctb_text_changed",
                       documentKey: documentKey,
                       chars: textLength,
                       lines: fctb.LinesCount,
                       changedChars: e.ChangedRange.Length,
                       meta: "HandleTextChanged"))
            // #endregion
            {
                _cleanSqlText = _sqlTextChangingDefaultSqlImplementation.HandleTextChanged(
                    fctb,
                    e,
                    _colorTheme.CurrentFctbColors,
                    ref _empty,
                    _cleanSqlText,
                    ref currentColumn,
                    fctb.Name.StartsWith("NetezzaSQL") || _generalDbService.DriverName(SelectedConnectionName) == "NetezzaSQL");
            }

            _netezzaAutocompleteState.CurrentColumn = currentColumn;
        }

        public void FctbTextChangedDelayed(object sender, TextChangedEventArgs e)
        {
            if (sender is not FastColoredTextBox fastColored || fastColored.IsDisposed)
                return;

            // #region agent perf
            SqlTypingPerfProbe.Instance.EnsureInitialized();
            long delayedStarted = Environment.TickCount64;
            // #endregion
            try
            {
                int lineCount = fastColored.LinesCount;
                int charCount = fastColored.TextLength;
                bool isLargeDoc = SqlPerformancePolicy.IsLargeScriptDocument(lineCount, charCount);

                EditorDocumentViewModel? document = null;
                bool workspaceTextIsClean = false;
                if (_documentIdsByEditor.TryGetValue(fastColored, out var documentId))
                {
                    document = _editorWorkspaceViewModel.Documents.FirstOrDefault(item => item.Id == documentId);
                    if (isLargeDoc)
                        document?.MarkEditorDirty();
                    else
                        document?.UpdateTextFromView(fastColored.Text, lineCount);

                    workspaceTextIsClean = document is { IsDirty: false };
                }

                TabPageMainTag? tag = fastColored.FindAncestorTabPage()?.Tag as TabPageMainTag;
                if (workspaceTextIsClean && tag is not null)
                {
                    tag.NotFirstTime = true;
                    tag.IsSaved = true;
                }
                else if (tag is { NotFirstTime: false })
                {
                    tag.NotFirstTime = true;
                }
                else if (tag is not null)
                {
                    tag.IsSaved = false;
                }

                string connectionName = string.IsNullOrWhiteSpace(document?.ConnectionName)
                    ? SelectedConnectionName
                    : document.ConnectionName;
                bool isNetezza = fastColored.Name.StartsWith("NetezzaSQL", StringComparison.Ordinal)
                    || _generalDbService.DriverName(connectionName) == "NetezzaSQL";
                if (isNetezza && !isLargeDoc)
                {
                    EditorDocumentViewModel? workspaceDocument = document ?? EnsureWorkspaceDocument(fastColored);
                    if (workspaceDocument is not null)
                    {
                        RegisterDiagnosticsTarget(workspaceDocument.Id, fastColored);
                        ApplySemanticClassification(fastColored, workspaceDocument.Id.ToString());
                    }
                }

                if (ReferenceEquals(fastColored, CurrentTB))
                {
                    // #region agent perf
                    using (SqlTypingPerfProbe.Instance.Measure(
                               "host.fctb_text_changed_delayed",
                               chars: charCount,
                               lines: lineCount,
                               meta: isLargeDoc ? "large=1" : "large=0"))
                    // #endregion
                    {
                        _cleanSqlText = _sqlTextChangingDefaultSqlImplementation.HandleSqlTextModification(
                            e,
                            fastColored,
                            _colorTheme.CurrentFctbColors,
                            ref _empty,
                            _cleanSqlText);
                    }

                    if (OutlineRefreshPolicy.ShouldRefresh(
                            _applicationSettingsContext.Config.DoLegend,
                            IsOutlineVisible(),
                            isLargeDoc,
                            lineCount,
                            charCount))
                    {
                        RebuildObjectExplorer(_cleanSqlText);
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Trace.WriteLine($"Object explorer refresh failed: {exception.GetType().Name}");
            }
            finally
            {
                // #region agent perf
                SqlTypingPerfProbe.Instance.Emit(
                    "host.fctb_text_changed_delayed_total",
                    "end",
                    Environment.TickCount64 - delayedStarted,
                    chars: fastColored.TextLength,
                    lines: fastColored.LinesCount);
                // #endregion
            }
        }
        readonly Regex _baseTableNZ = RegexBaseTableNZ();

        private string prevTekst = "";
        public void FctbToolTipNeeded(object sender, ToolTipNeededEventArgs e)
        {
            if (sender is not FastColoredTextBox editor)
                return;

            if (TryGetLintIssue(editor, editor.PlaceToPosition(e.Place), out var lintIssue))
            {
                e.ToolTipTitle = $"{GetLintSeverityLabel(lintIssue.Severity)} · {lintIssue.RuleId}";
                e.ToolTipText = GetDiagnosticMessage(lintIssue);
                e.ToolTipIcon = lintIssue.Severity switch
                {
                    LintSeverity.Error => ToolTipIcon.Error,
                    LintSeverity.Warning => ToolTipIcon.Warning,
                    LintSeverity.Information => ToolTipIcon.Info,
                    _ => ToolTipIcon.None
                };
                return;
            }

            // PointToPlace maps the gutter/lightbulb to the beginning of the
            // source line (often the WHERE keyword). Prefer the line diagnostic
            // over parser hover text in that case.
            if (TryGetLintIssueOnLine(editor, e.Place.iLine, out lintIssue))
            {
                e.ToolTipTitle = $"{GetLintSeverityLabel(lintIssue.Severity)} · {lintIssue.RuleId}";
                e.ToolTipText = GetDiagnosticMessage(lintIssue);
                e.ToolTipIcon = lintIssue.Severity switch
                {
                    LintSeverity.Error => ToolTipIcon.Error,
                    LintSeverity.Warning => ToolTipIcon.Warning,
                    LintSeverity.Information => ToolTipIcon.Info,
                    _ => ToolTipIcon.None
                };
                return;
            }

            if (_generalDbService.DriverName(SelectedConnectionName) != "NetezzaSQL")
            {
                return;
            }

            EditorDocumentViewModel? hoverDocument = EnsureWorkspaceDocument(editor);
            if (hoverDocument is null)
                return;

            string documentUri = hoverDocument.Id.ToString();
            var parserHover = _legacySqlAuthoringServices.GetHover(editor.Text, editor.PlaceToPosition(e.Place), documentUri);
            if (parserHover is not null)
            {
                e.ToolTipTitle = null;
                e.ToolTipText = parserHover.Content;
                return;
            }

            var range = new FastColoredTextBoxNS.Range(sender as FastColoredTextBox, e.Place, e.Place);
            var r1 = range.GetFragment("[^(\\s|,)]");
            string hoveredWord = r1.Text;

            try
            {
                if (!string.IsNullOrEmpty(hoveredWord) && hoveredWord.Length < 300)
                {
                    var m1 = _baseTableNZ.Match(hoveredWord);
                    if (!hoveredWord.Contains('.'))
                    {
                        m1 = _baseTableNZ.Match($"{SelectedDatabase}..{hoveredWord}");
                    }

                    if (m1.Success)
                    {
                        string db = m1.Groups["base"].Value;
                        string table = m1.Groups["table"].Value;

                        if (_completionContext.DatabaseSchemaLookup.TryGetValue(SelectedConnectionName, out var value) && value.TryGetValue(db, out var p1))
                        {
                            if (p1.TryGetValue(table, out var p2))
                            {
                                string toolTipText;
                                string toolTipTitle;
                                int tableId = p2.tableId;

                                if (!_schemaTables.TablesByConnection.TryGetValue(SelectedConnectionName, out var btDict2)
                                    || !btDict2.TryGetValue(tableId, out var tabInfo))
                                {
                                    return;
                                }

                                if (prevTekst == hoveredWord)
                                {
                                    if (!_completionContext.ColumnTablesDictionary.ContainsKey(SelectedConnectionName))
                                    {
                                        return;
                                    }


                                    StringBuilder sb = new StringBuilder();
                                    for (int i = 0; i < tabInfo.COLUMN_COUNT; i++)
                                    {
                                        var colInfo = _completionContext.ColumnTablesDictionary[SelectedConnectionName][tabInfo.FIRST_COLUMN_ID + i];
                                        string name = colInfo.COLUMN_NAME;
                                        string dataType = colInfo.DATA_TYPE;
                                        string desc = colInfo.COLUMN_DESCRIPTION;
                                        if (desc != null && desc.Length >= 100)
                                        {
                                            desc = desc[0..97] + "...";
                                        }

                                        string nullIf = colInfo.IS_NULLABLE ? "" : " not null";
                                        sb.AppendLine($"{name} ({dataType}{nullIf}) - {desc}");
                                        if (i >= 30)
                                        {
                                            sb.Append($"...");
                                            break;
                                        }
                                    }

                                    toolTipTitle = $"{table} - {tabInfo.COLUMN_COUNT} cols";

                                    toolTipText = sb.ToString();
                                    prevTekst = "";
                                }
                                else
                                {
                                    toolTipTitle = table;

                                    if (tabInfo.TABLE_DESC is not null && tabInfo.TABLE_DESC.Length > 100)
                                    {
                                        StringBuilder sb = new StringBuilder();
                                        var parts = tabInfo.TABLE_DESC.Split('\n', StringSplitOptions.TrimEntries);
                                        for (int i = 0; i < parts.Length; i++)
                                        {
                                            string part = parts[i];
                                            if (part.Length < 100)
                                            {
                                                sb.AppendLine(part);
                                            }
                                            else
                                            {
                                                for (int j = 0; j < part.Length / 100; j++)
                                                {
                                                    sb.AppendLine(part[(100 * j)..(100 * (j + 1))]);
                                                }
                                                sb.AppendLine(part[(100 * (part.Length / 100))..]);
                                            }
                                        }
                                        toolTipText = sb.ToString();
                                    }
                                    else
                                    {
                                        toolTipText = tabInfo.TABLE_DESC;
                                    }

                                    prevTekst = hoveredWord;
                                }

                                if (String.IsNullOrEmpty(toolTipText))
                                {
                                    toolTipText = "(no desc)";
                                }


                                var lines = toolTipTitle.Split(Environment.NewLine);
                                if (lines.Length >= 100)
                                {
                                    StringBuilder sb = new StringBuilder();
                                    for (int i = 0; i < 100; i++)
                                    {
                                        sb.AppendLine(lines[i]);
                                    }
                                    toolTipTitle = sb.ToString();
                                }


                                if (toolTipText.Length <= 5_000)
                                {
                                    e.ToolTipText = toolTipText;
                                }
                                else
                                {
                                    e.ToolTipText = toolTipText[0..4_999] + "...";
                                }
                            }
                        }
                    }
                    else if (OutlineContainsWord(hoveredWord))
                    {
                        e.ToolTipTitle = hoveredWord;
                        prevTekst = hoveredWord;
                        e.ToolTipText = "ctrl + click - go to first reference";
                    }
                }
            }
            catch (Exception ex)
            {
                _loggerLoud.MessageBox_Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void FctbAuthoringKeyUp(object sender, KeyEventArgs e)
        {
            if (sender is not FastColoredTextBox editor
                || (!editor.Name.StartsWith("NetezzaSQL", StringComparison.Ordinal)
                    && _generalDbService.DriverName(SelectedConnectionName) != "NetezzaSQL"))
                return;

            if (e.KeyCode is not (Keys.D9 or Keys.OemOpenBrackets or Keys.Oemcomma) || editor.SelectionStart <= 0)
                return;

            EditorDocumentViewModel? signatureDocument = EnsureWorkspaceDocument(editor);
            if (signatureDocument is null)
                return;

            string documentUri = signatureDocument.Id.ToString();
            var help = _legacySqlAuthoringServices.GetSignatureHelp(editor.Text, editor.SelectionStart, documentUri);
            if (help is null)
            {
                _signaturePopup.Hide();
                return;
            }

            _signaturePopup.Show(editor, help);
        }

        private void ApplySemanticClassification(FastColoredTextBox editor, string documentUri)
        {
            if (editor.IsDisposed)
                return;

            int lineCount = editor.LinesCount;
            int charCount = editor.TextLength;
            if (SqlPerformancePolicy.ShouldSkipSemanticClassification(lineCount, charCount)
                || SqlPerformancePolicy.IsLargeScriptDocument(lineCount, charCount))
                return;

            // #region agent perf
            using (SqlTypingPerfProbe.Instance.Measure(
                       "host.semantic",
                       chars: charCount,
                       lines: lineCount,
                       meta: "Classify+Apply"))
            // #endregion
            {
                string text = editor.Text;
                var tokens = _legacySqlAuthoringServices.ClassifySemanticTokens(text, documentUri);
                if (editor.IsDisposed || !string.Equals(editor.Text, text, StringComparison.Ordinal))
                    return;

                ApplySemanticStyling(editor, tokens, _colorTheme.CurrentFctbColors);
            }
        }

        private static void ApplySemanticStyling(FastColoredTextBox editor, IReadOnlyList<SemanticTokenSpan> tokens, FctbColors colors)
        {
            if (tokens.Count == 0)
                return;

            var documentRange = editor.Range;
            documentRange.ClearStyle(colors.SemanticStyles);

            foreach (var token in tokens)
            {
                var style = FctbSemanticStyleMapper.Resolve(token.Kind, colors);
                if (style is null)
                    continue;

                if (token.Start < 0 || token.Start >= editor.TextLength)
                    continue;

                int length = Math.Max(1, Math.Min(token.Length, editor.TextLength - token.Start));
                var range = new FastColoredTextBoxNS.Range(editor)
                {
                    Start = editor.PositionToPlace(token.Start),
                    End = editor.PositionToPlace(token.Start + length)
                };
                range.SetStyle(style);
            }

            editor.Invalidate();
        }

        public void FctbGoToDefinition(FastColoredTextBox editor)
        {
            var definition = _legacySqlAuthoringServices.GetDefinition(editor.Text, editor.SelectionStart);
            if (definition is null)
            {
                MessageBox.Show(this, "No CTE or alias definition was found at the cursor.", "Go to Definition",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            SelectOccurrence(editor, definition);
        }

        public void FctbShowReferences(FastColoredTextBox editor)
        {
            var references = _legacySqlAuthoringServices.GetReferences(editor.Text, editor.SelectionStart);
            if (references.Count == 0)
            {
                MessageBox.Show(this, "No CTE or alias references were found at the cursor.", "Find References",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var choices = references.Select((occurrence, index) =>
                $"{index + 1}: line {editor.PositionToPlace(occurrence.StartAbsolute).iLine + 1} ({(occurrence.IsDefinition ? "definition" : "reference")})")
                .ToArray();
            using var dialog = new Form { Text = "References", Width = 420, Height = 260, StartPosition = FormStartPosition.CenterParent };
            var list = new ListBox { Dock = DockStyle.Fill };
            list.Items.AddRange(choices);
            list.SelectedIndex = 0;
            list.DoubleClick += (_, _) => dialog.DialogResult = DialogResult.OK;
            dialog.Controls.Add(list);
            if (dialog.ShowDialog(this) == DialogResult.OK && list.SelectedIndex >= 0)
                SelectOccurrence(editor, references[list.SelectedIndex]);
        }

        public void FctbRenameSymbol(FastColoredTextBox editor)
        {
            var symbol = _legacySqlAuthoringServices.GetSymbol(editor.Text, editor.SelectionStart);
            if (symbol is null)
            {
                MessageBox.Show(this, "Only parser-recognized CTE and alias symbols can be renamed.", "Rename",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string? newName = PromptForRename(symbol.OldName, symbol.Occurrences.Count);
            if (string.IsNullOrWhiteSpace(newName))
                return;
            if (!_legacySqlAuthoringServices.IsValidIdentifier(newName))
            {
                MessageBox.Show(this, "Enter a valid SQL identifier.", "Rename", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string renamed = _legacySqlAuthoringServices.ApplyRename(editor.Text, symbol, newName);
            if (!string.Equals(renamed, editor.Text, StringComparison.Ordinal))
                editor.Text = renamed; // one FCTB text replacement, hence one undo action.
        }

        private string? PromptForRename(string currentName, int occurrenceCount)
        {
            using var dialog = new Form { Text = $"Rename ({occurrenceCount} occurrences)", Width = 380, Height = 140, StartPosition = FormStartPosition.CenterParent };
            var input = new TextBox { Text = currentName, Left = 12, Top = 12, Width = 340 };
            var ok = new Button { Text = "Rename", DialogResult = DialogResult.OK, Left = 196, Top = 48, Width = 75 };
            var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Left = 277, Top = 48, Width = 75 };
            dialog.Controls.AddRange([input, ok, cancel]);
            dialog.AcceptButton = ok;
            dialog.CancelButton = cancel;
            return dialog.ShowDialog(this) == DialogResult.OK ? input.Text.Trim() : null;
        }

        private static void SelectOccurrence(FastColoredTextBox editor, SymbolOccurrence occurrence)
        {
            editor.SelectionStart = occurrence.StartAbsolute;
            editor.SelectionLength = occurrence.EndAbsolute - occurrence.StartAbsolute;
            editor.DoSelectionVisible();
            editor.Focus();
        }

        public void FctbNew_MouseClick(object sender, MouseEventArgs e)
        {
            _currentMyGrid?.HideFilters();

            if (e.Button != MouseButtons.Left || !ModifierKeys.HasFlag(Keys.Control))
            {
                return;
            }

            var range = new FastColoredTextBoxNS.Range(CurrentTB, CurrentTB.Selection.Start, CurrentTB.Selection.Start);
            string clickedWord = range.GetFragment("[^(\\s|,|;)]").Text;

            if (TryNavigateToOutlineDefinition(clickedWord))
                return;

            if (ModifierKeys.HasFlag(Keys.Control) && _generalDbService.DriverName(SelectedConnectionName) == "NetezzaSQL")
            {
                var m1 = _baseTableNZ.Match(clickedWord);
                if (!clickedWord.Contains('.'))
                {
                    m1 = _baseTableNZ.Match($"{this.SelectedDatabase}..{clickedWord}");
                }
                if (m1.Success)
                {
                    string db = m1.Groups["base"].Value;
                    string table = m1.Groups["table"].Value;
                    if (_completionContext.DatabaseSchemaLookup.TryGetValue(SelectedConnectionName, out var value) && value.TryGetValue(db, out var p1))
                    {
                        if (p1.TryGetValue(table, out var p2))
                        {

                            TypeInDatabase tp = _schemaTables.TablesByConnection.TryGetValue(SelectedConnectionName, out var btDict3)
                                    && btDict3.TryGetValue(p2.tableId, out var btInfo3)
                                ? btInfo3.TABLE_KIND
                                : TypeInDatabase.table;
                            if (tp == TypeInDatabase.table || tp == TypeInDatabase.view || tp == TypeInDatabase.thisExternal)
                            {
                                SelectNetezzaObjectInExplorer(SelectedConnectionName, db, p2.tableId);
                                CurrentTB.ClearHints();
                                CurrentTB.AddHint(range, new CustomHint(clickedWord, CurrentTB, _objectExplorerNavigationController, _ddlCodeProvider, db, table, tp, _colorTheme), false, false, false);
                                //var h = CurrentTB.AddHint(range, new DataGridView(), false, false, false);
                                //h.DoVisible();
                            }
                        }
                    }
                }
            }
            else if (ModifierKeys.HasFlag(Keys.Control) && (
_generalDbService.DriverName(SelectedConnectionName) == "DB2"
                || _generalDbService.DriverName(SelectedConnectionName) == "Oracle"
                || _generalDbService.DriverName(SelectedConnectionName) == "MsSqlStd"
                || _generalDbService.DriverName(SelectedConnectionName) == "MsSqlTrusted"
                || _generalDbService.DriverName(SelectedConnectionName) == "Postgres"
                || _generalDbService.DriverName(SelectedConnectionName) == "SQLite"
                || _generalDbService.DriverName(SelectedConnectionName) == "MySql"
                ))
            {
                GoToObjectNotNetezza(clickedWord);
            }
        }

        private static readonly Regex baseTableGeneralSchema = new Regex(@"(?<schema>(\w+|""[\w\.]+""))\.(?<table>\w+)");
        private static readonly Regex baseTableGeneralSchemaWithDb = new Regex(@"(?<database>\w+)\.(?<schema>(\w+|""[\w\.]+""))\.(?<table>\w+)");

        private static string NetezzaCategory(TypeInDatabase kind) => kind switch
        {
            TypeInDatabase.table => "Tables",
            TypeInDatabase.view => "Views",
            TypeInDatabase.thisExternal => "External Tables",
            TypeInDatabase.procedure => "Procedures",
            TypeInDatabase.function => "Functions",
            TypeInDatabase.sequence => "Sequences",
            TypeInDatabase.synonym => "Synonyms",
            TypeInDatabase.thisAggregate => "Aggregate",
            _ => "Tables"
        };

        private void SelectNetezzaObjectInExplorer(string connectionName, string databaseName, int tableId)
        {
            if (_mvvmDatabaseExplorerControl is null
                || !_schemaTables.TablesByConnection.TryGetValue(connectionName, out var tables)
                || !tables.TryGetValue(tableId, out var table))
                return;

            RevealDatabaseExplorer();
            _ = _mvvmDatabaseExplorerControl.SelectObjectAsync(
                connectionName,
                databaseName,
                NetezzaCategory(table.TABLE_KIND),
                table.TABLE_NAME,
                LegacySchemaTypeMapper.Map(table.TABLE_KIND));
        }

        private void RevealDatabaseExplorer()
        {
            if (_tabManager is UI.DockSuiteTabManager dsm && _mvvmDatabaseExplorerControl is not null)
            {
                dsm.ShowToolWindow("Database", _mvvmDatabaseExplorerControl,
                    WeifenLuo.WinFormsUI.Docking.DockState.DockLeft);
            }
        }

        public void GoToObjectNotNetezza(string clickedWord, string objectType = null)
        {
            if (_generalDbService.DriverName(SelectedConnectionName) == "SQLite")
            {
                clickedWord = "master." + clickedWord;
            }

            var m1 = baseTableGeneralSchemaWithDb.Match(clickedWord);
            if (!m1.Success)
            {
                m1 = baseTableGeneralSchema.Match(clickedWord);
            }


            if (m1.Success)
            {
                string schema = m1.Groups["schema"].Value;
                string table = m1.Groups["table"].Value;
                string db = "";
                if (m1.Groups.ContainsKey("database"))
                {
                    db = m1.Groups["database"].Value;
                }

                if (_leftTabs.SelectedIndex != 0)
                {
                    _leftTabs.SelectedIndex = 0;
                }

                string connectionName = SelectedConnectionName;

                if (!_connectionSessions.TryGetValue(connectionName, out var thisDb))
                    return;

                string schemaKey = schema;
                if (!string.IsNullOrWhiteSpace(db)
                    && thisDb.DatabaseType != DatabaseTypeEnum.DB2)
                {
                    schemaKey = db + "_" + schema;
                }

                if (thisDb.objectInSchema.TryGetValue(schemaKey, out var thisSchema) &&
                    thisSchema.TryGetValue(table, out var obj)) // table or view exists
                {
                    if (_mvvmDatabaseExplorerControl is not null)
                    {
                        RevealDatabaseExplorer();
                        _ = _mvvmDatabaseExplorerControl.SelectObjectAsync(
                            connectionName,
                            string.IsNullOrWhiteSpace(db) ? thisDb.DefaultDatabaseName : db,
                            schema.Replace("\"", "", StringComparison.Ordinal),
                            table,
                            LegacySchemaTypeMapper.Map(obj));
                        return;
                    }
                }
            }
        }

        internal Task NavigateDocumentationDimDateExplorerAsync()
        {
            if (!StartupArguments.IsDocumentationNavigateDimDate(Environment.GetCommandLineArgs()))
            {
                return Task.CompletedTask;
            }

            return NavigateDocumentationDimDateExplorerCoreAsync();
        }

        private async Task NavigateDocumentationDimDateExplorerCoreAsync()
        {
            if (_mvvmDatabaseExplorerControl is null
                || string.IsNullOrWhiteSpace(SelectedConnectionName))
            {
                return;
            }

            RevealDatabaseExplorer();
            string connectionName = SelectedConnectionName;
            await _mvvmDatabaseExplorerControl.InitializeAsync(connectionName).ConfigureAwait(true);

            DateTime deadline = DateTime.UtcNow.AddMinutes(3);
            while (DateTime.UtcNow < deadline)
            {
                string databaseName = ResolveDimDateDatabaseName(connectionName) ?? "JUST_DATA";
                bool selected = await _mvvmDatabaseExplorerControl.SelectObjectAsync(
                    connectionName,
                    databaseName,
                    "Tables",
                    "DIMDATE",
                    SchemaNodeKind.Table).ConfigureAwait(true);
                if (selected)
                {
                    TreeView tree = _mvvmDatabaseExplorerControl.DatabaseTreeView;
                    if (tree.SelectedNode is { } node)
                    {
                        // Show DIMDATE near the top of the viewport (avoid TopNode=Tables,
                        // which hides it under ADMIN.DIMACCOUNT_CPY_* siblings).
                        node.EnsureVisible();
                        tree.TopNode = node;
                        tree.SelectedNode = node;
                        tree.Focus();
                    }

                    return;
                }

                await Task.Delay(1000).ConfigureAwait(true);
            }
        }

        private string? ResolveDimDateDatabaseName(string connectionName)
        {
            if (!_schemaTables.TablesByConnection.TryGetValue(connectionName, out var tables))
            {
                return null;
            }

            NetezzaTableInfo? dimDate = tables.Values.FirstOrDefault(table =>
                table.TABLE_NAME.Equals("DIMDATE", StringComparison.OrdinalIgnoreCase));
            if (dimDate is null)
            {
                return null;
            }

            if (_databaseRuntimeContext.DatabaseDictionary.TryGetValue(connectionName, out var databases)
                && databases.TryGetValue(dimDate.DATABASE_ID, out var database))
            {
                return database.DatabaseName;
            }

            return "JUST_DATA";
        }
    }
}
