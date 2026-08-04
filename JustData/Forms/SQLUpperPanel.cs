using AppBase.Common;
using AppBase.Common.Enums;
using AppBase.Common.Interfaces;
using AppBase.Data;
using AppBase.Data.Completion;
using AppBase.Data.Core.Core;
using AppBase.Data.Core.Interfaces;
using AppBase.Data.Core.Models;
using JustData.Application.Editor;
using DatabaseDataGridView.WinForms.Coloring;
using FastColoredTextBoxNS;
using FastColoredTextBoxNS.Helpers;
using JustyBaseLegacy.UI.Helpers;
using JustyBaseLegacy.UI.Sql;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JustyBaseLegacy.UI.DbForms
{
    public partial class SQLUpperPanel : UserControl, IEditorPanel
    {
        private const int CompletionPopupLogicalWidth = 720;
        private const int CompletionPopupLogicalMinimumWidth = 560;
        private const int CompletionPopupLogicalMaximumHeight = 242;

        private readonly IApplicationSettingsContext _applicationSettingsContext;
        private readonly ICodeActionProvider _codeActionProvider;
        private readonly INetezzaCompletionContext _completionContext;
        private readonly Action<string> _setSelectedConnectionName;
        private readonly IGeneralDbService _generalDbService;
        private readonly IConnectionSessionRegistry _connectionSessions;
        private readonly INetezzaSchemaTableCatalog _schemaTables;
        private readonly ILogger _logger;
        private readonly ISqlEditorUiPort _editorUi;
        private readonly IColorTheme _colorTheme;
        private readonly NetezzaSqlCompletionServices _netezzaSqlCompletionServices;
        private readonly INetezzaAutocompleteState _netezzaAutocompleteState;
        private readonly IEditorCatalogState _editorCatalogState;
        private TableLayoutPanel _editorLayout;
        private Bitmap? _runToolbarIcon;
        private Bitmap? _stopToolbarIcon;
        private Bitmap? _commentToolbarIcon;
        private Bitmap? _uncommentToolbarIcon;
        private Bitmap? _importToolbarIcon;
        private Bitmap? _keepConnectionToolbarIcon;
        private Bitmap? _formatToolbarIcon;
        private AutocompleteMenu? _autocompleteMenu;
        private NetezzaHybridAutocompleteSource? _autocompleteSource;
        private ImageList? _autocompleteImageList;


        public SQLUpperPanel(IGeneralDbService generalDbService,
            IConnectionSessionRegistry connectionSessions,
            INetezzaSchemaTableCatalog schemaTables,
            IApplicationSettingsContext applicationSettingsContext,
            INetezzaCompletionContext completionContext,
            Action<string> setSelectedConnectionName,
            JustData.Application.Variables.ISessionVariableStore sessionVariableStore,
            Func<string> activeDocumentTitleProvider,
            ILogger logger,
            ISqlEditorUiPort editorUi,
            IColorTheme colorTheme,
            NetezzaSqlCompletionServices netezzaSqlCompletionServices,
            INetezzaAutocompleteState netezzaAutocompleteState,
            ICodeActionProvider codeActionProvider,
            IEditorCatalogState editorCatalogState,
            string driver,
            string selectedConnection,
            string selectedDatabase,
            bool isEnabledMode)
        {
            _generalDbService = generalDbService;
            _connectionSessions = connectionSessions ?? throw new ArgumentNullException(nameof(connectionSessions));
            _schemaTables = schemaTables ?? throw new ArgumentNullException(nameof(schemaTables));
            _applicationSettingsContext = applicationSettingsContext;
            _codeActionProvider = codeActionProvider ?? throw new ArgumentNullException(nameof(codeActionProvider));
            _completionContext = completionContext;
            _setSelectedConnectionName = setSelectedConnectionName;
            _logger = logger;
            _editorUi = editorUi ?? throw new ArgumentNullException(nameof(editorUi));
            _colorTheme = colorTheme;
            _netezzaSqlCompletionServices = netezzaSqlCompletionServices;
            _netezzaAutocompleteState = netezzaAutocompleteState ?? throw new ArgumentNullException(nameof(netezzaAutocompleteState));
            _editorCatalogState = editorCatalogState ?? throw new ArgumentNullException(nameof(editorCatalogState));

            InitializeComponent();
            Disposed += (_, _) =>
            {
                DisposeToolbarIcons();
                DisposeAutocompleteIcons();
            };
            SetupEditorLayout();
            // A newly created document must start with the complete database
            // projection for its connection.  DDL documents used to appear to
            // work because they supplied one database explicitly from the
            // schema node, while a blank document only added the current
            // database and therefore had nothing else to select.
            string[] catalogDatabases = _editorCatalogState.Snapshot
                .DatabasesFor(selectedConnection)
                .Where(database => !string.IsNullOrWhiteSpace(database))
                .ToArray();
            if (!string.IsNullOrWhiteSpace(selectedDatabase)
                && !catalogDatabases.Contains(selectedDatabase, StringComparer.OrdinalIgnoreCase))
            {
                cbDatabases.Items.Add(string.Intern(selectedDatabase));
            }
            cbDatabases.Items.AddRange(catalogDatabases.Select(string.Intern).ToArray());
            if (cbDatabases.Items.Count > 0)
                cbDatabases.SelectedIndex = 0;
            cbDatabases.SelectedIndexChanged += new System.EventHandler(cbDatabases_SelectedIndexChanged);

            SetEnabledConnectionsDatabases(isEnabledMode);
            SelectedConnectionName = selectedConnection;
            SelectedDatabase = selectedDatabase;

            fastColoredTextBox1 = new FastColoredTextBox();

            fastColoredTextBox1.Dock = DockStyle.Fill;
            fastColoredTextBox1.Paddings = new System.Windows.Forms.Padding(0);
            _editorLayout.Controls.Add(fastColoredTextBox1, 0, 1);

            ApplyToolStripDpiMetrics();

            fastColoredTextBox1.AutoCompleteBracketsList = new char[] {
                    '(', ')',
                    '\"', '\"',
                    '\'', '\''
                };
            fastColoredTextBox1.AutoIndentCharsPatterns = "^\\s*[\\w\\.]+(\\s\\w+)?\\s*(?<range>=)\\s*(?<range>[^;=]+);\n^\\s*(case|default)\\s*[^:]*(" +
             "?<range>:)\\s*(?<range>[^;]+);";
            //fastColoredTextBox1.AutoScrollMinSize = new System.Drawing.Size(27, 14);
            fastColoredTextBox1.BackBrush = null;
            fastColoredTextBox1.CommentPrefix = "--";
            fastColoredTextBox1.Cursor = System.Windows.Forms.Cursors.IBeam;
            fastColoredTextBox1.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            fastColoredTextBox1.IsReplaceMode = false;

            fastColoredTextBox1.Name = $"{driver}_addedFastColored";
            fastColoredTextBox1.Paddings = new System.Windows.Forms.Padding(0);
            fastColoredTextBox1.ShowFoldingLines = true;

            fastColoredTextBox1.TabIndex = 0;
            fastColoredTextBox1.Zoom = 100;

            fastColoredTextBox1.LineInterval = _applicationSettingsContext.Config.LineInterval;
            fastColoredTextBox1.WordWrap = (_applicationSettingsContext.Config.WordWrap == 1);
            fastColoredTextBox1.WordWrapAutoIndent = (_applicationSettingsContext.Config.WordWrapAutoIndent == 1);

            fastColoredTextBox1.SelectionColor = _colorTheme.CurrentFctbColors.FctbSelectionColor;
            fastColoredTextBox1.DisabledColor = _colorTheme.CurrentFctbColors.FctbDisabledColor;
            fastColoredTextBox1.BackColor = _colorTheme.CurrentFctbColors.FctbBackColor;
            fastColoredTextBox1.IndentBackColor = _colorTheme.CurrentFctbColors.FctbIndentBackColor;
            fastColoredTextBox1.LineNumberColor = _colorTheme.CurrentFctbColors.FctbLineNumberColor;
            fastColoredTextBox1.FoldingIndicatorColor = _colorTheme.CurrentFctbColors.FctbFoldingIndicatorColor;
            fastColoredTextBox1.ForeColor = _colorTheme.CurrentFctbColors.FctbForeColor;
            fastColoredTextBox1.LeftBracket = '(';
            fastColoredTextBox1.RightBracket = ')';
            fastColoredTextBox1.AutoCompleteBrackets = _applicationSettingsContext.Config.AutoCompleteBrackets; // xx -> (xx), 'xx'
            fastColoredTextBox1.MaxBracketSearchIterations = 100_000;
            fastColoredTextBox1.LeftBracket2 = '{'; // \x0 = none
            fastColoredTextBox1.RightBracket2 = '}'; // \x0 = none
            fastColoredTextBox1.ToolTipDelay = _applicationSettingsContext.Config.ToolTipDelay;

            float fontSize = _applicationSettingsContext.Config.FontSize;

            fastColoredTextBox1.Font = new System.Drawing.Font(_applicationSettingsContext.Config.FontName, fontSize, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            ApplyFastColoredTextBoxDpiMetrics();
            MiscellaneousHelper.UpdateAdditionStyles(fastColoredTextBox1.Range, _colorTheme.CurrentFctbColors, _applicationSettingsContext.Config.BracketFolding);
            _editorUi.GetTextCommentRanges(fastColoredTextBox1);

            fastColoredTextBox1.Focus();
            fastColoredTextBox1.KeyDown += FastColored_KeyDown;
            if (!_applicationSettingsContext.Config.DontUseIndent)
            {
                fastColoredTextBox1.AutoIndentNeeded += FctbAutoIndentNeeded;
            }

            var popupMenu = new AutocompleteMenu(fastColoredTextBox1);
            _autocompleteMenu = popupMenu;
            ApplyAutocompleteImageListDpi();
            popupMenu.Opening += popupMenu_Opening;
            popupMenu.SearchPattern = @"[&\@\w\.=!<>""']";

            popupMenu.MinFragmentLength = 3;
            popupMenu.AllowTabKey = true;
            ApplyAutocompletePopupMetrics();
            popupMenu.SelectedColor = _colorTheme.CurrentFctbColors.FctbPopupMenuSelected;
            popupMenu.BackColor = fastColoredTextBox1.BackColor;
            popupMenu.ForeColor = fastColoredTextBox1.ForeColor;

            popupMenu.AppearInterval = _applicationSettingsContext.Config.PopupMenuDefaultAppearInterval;
                _netezzaSqlCompletionServices.EnsureSchemaForConnection(_completionContext, selectedConnection);
            var autocompleteSource = new NetezzaHybridAutocompleteSource(
                popupMenu,
                fastColoredTextBox1,
                _applicationSettingsContext,
                _completionContext,
                sessionVariableStore,
                activeDocumentTitleProvider,
                _generalDbService,
                _connectionSessions,
                _schemaTables,
                _netezzaSqlCompletionServices,
                _netezzaAutocompleteState,
                connectionNameProvider: () => SelectedConnectionName,
                databaseNameProvider: () => SelectedDatabase);
            _autocompleteSource = autocompleteSource;
            popupMenu.Items.SetAutocompleteItems(autocompleteSource);
            fastColoredTextBox1.Tag = new TbInfo(StringComparer.OrdinalIgnoreCase);
            (fastColoredTextBox1.Tag as TbInfo).PopupMenu = popupMenu;
            (fastColoredTextBox1.Tag as TbInfo).SuggestionList = autocompleteSource;
            (fastColoredTextBox1.Tag as TbInfo).AdditionalDataWith = new Dictionary<string, List<string>>();
            (fastColoredTextBox1.Tag as TbInfo).AdditionalTableData = new Dictionary<string, List<string>>();

            _editorUi.WireEditorEvents(fastColoredTextBox1, driver?.StartsWith("NetezzaSQL", StringComparison.OrdinalIgnoreCase) == true);
            fastColoredTextBox1.GotFocus += FastColoredTextBox1_GotFocus;

            tsbKeepConnection.Checked = (!_applicationSettingsContext.Config.CloseConnectionByDefault);
            KeepConnectionOpen = tsbKeepConnection.Checked;
        }

        private SqlIndenting _sqlIndenting;
        public void FctbAutoIndentNeeded(object sender, AutoIndentEventArgs args)
        {
            _sqlIndenting ??= new SqlIndenting();
            _sqlIndenting.SqlDefaultIndenting(args);
        }

        public FastColoredTextBox CurrentTb
        {
            get => fastColoredTextBox1;
            set => fastColoredTextBox1 = value;
        }

        private string _selectedConnectionName;
        public string SelectedConnectionName
        {
            get => _selectedConnectionName;
            set
            {
                string internedValue = string.Intern(value);
                _editorCatalogState.AddConnection(internedValue);
                foreach (var connectionName in _editorCatalogState.Snapshot.Connections)
                    if (!cbConnections.Items.Contains(connectionName)) cbConnections.Items.Add(connectionName);
                //cbConnections.Text= selectedConnectionName;
                // Assign before SelectedItem so cbConnections_SelectedIndexChanged does not
                // treat programmatic initialization (e.g. AddMainTab for DDL) as a connection switch.
                _selectedConnectionName = internedValue;
                cbConnections.SelectedItem = internedValue;
            }
        }

        public void SetEnabledConnectionsDatabases(bool enabled)
        {
            if (!cbConnections.IsDisposed)
            {
                cbConnections.Enabled = enabled;
            }
            if (!cbDatabases.IsDisposed)
            {
                cbDatabases.Enabled = enabled;
            }
        }

        private void CbConnections_DropDown(object sender, EventArgs e)
        {
            string tmp = _selectedConnectionName;
            cbConnections.Items.Clear();
            foreach (var item in _editorCatalogState.Snapshot.Connections)
            {
                cbConnections.Items.Add(item);
            }
            if (_editorCatalogState.Snapshot.Connections.Contains(tmp, StringComparer.OrdinalIgnoreCase))
            {
                SelectedConnectionName = tmp;
            }

            UpdateComboDropDownWidth(cbConnections);
        }
        private void CbDatabases_DropDown(object sender, EventArgs e)
        {
            if (!_connectionSessions.TryGetValue(SelectedConnectionName, out var selectedDatabase))
            {
                return;
            }
            if (_completionContext.DatabaseDictionary.TryGetValue(SelectedConnectionName, out var databases))
            {
                ExtendDatabasesList(databases.Values.Select(arg => string.Intern(arg.DatabaseName)));
            }
            else if (selectedDatabase.DatabaseList is { Count: > 0 })
            {
                ExtendDatabasesList(selectedDatabase.DatabaseList.Select(o => string.Intern(o)));
            }
            else if (!string.IsNullOrWhiteSpace(selectedDatabase.DefaultDatabaseName))
            {
                ExtendDatabasesList(new string[] { string.Intern(selectedDatabase.DefaultDatabaseName) });
            }
            //_baseWindowHelpers.schematBazySlownik["NZ"]
        }

        public void RemoveConnection(string name)
        {
            if (cbConnections.Items.Contains(name))
            {
                cbConnections.Items.Remove(name);
                _editorCatalogState.RemoveConnection(name);
            }
            if (cbConnections.Items.Count > 0)
            {
                SelectedConnectionName = (string)cbConnections.Items[0];
                if (_lastSelectedDartabaseNameForConnection.TryGetValue(SelectedConnectionName, out var res))
                {
                    CbDatabases_DropDown(this, null);
                    if (cbDatabases.Items.Contains(res))
                    {
                        SelectedDatabase = res;
                    }
                    //cbDatabases_DropDown(this, null);
                }
            }
        }

        private Dictionary<string, string> _lastSelectedDartabaseNameForConnection = new();
        public void ExtendDatabasesList(IEnumerable<string> databasesList)
        {
            if (IsDisposed || Disposing || cbDatabases is null || cbDatabases.IsDisposed)
                return;

            string[] databasesArray = (databasesList ?? Array.Empty<string>())
                .Where(o => o is not null)
                .Select(o => string.Intern(o))
                .ToArray();

            _editorCatalogState.ReplaceDatabases(SelectedConnectionName, databasesArray);
            cbDatabases.Items.Clear();
            cbDatabases.Items.AddRange(_editorCatalogState.Snapshot.DatabasesFor(SelectedConnectionName).ToArray());

            if (_lastSelectedDartabaseNameForConnection.TryGetValue(SelectedConnectionName, out var res)
                && databasesArray.Contains(res, StringComparer.OrdinalIgnoreCase))
            {
                SelectedDatabase = res;
            }
            else if (databasesArray.Length > 0)
            {
                cbDatabases.SelectedIndex = 0;
            }

            UpdateComboDropDownWidth(cbDatabases);
        }

        private string _selectedDatabase;
        public string SelectedDatabase
        {
            get => _selectedDatabase;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    _selectedDatabase = string.Empty;
                    return;
                }

                string internedValue = string.Intern(value);
                _editorCatalogState.AddDatabase(SelectedConnectionName, internedValue);

                _selectedDatabase = internedValue;
                cbDatabases.SelectedItem = internedValue;

                _lastSelectedDartabaseNameForConnection[SelectedConnectionName] = _selectedDatabase;
            }
        }
        private void cbDatabases_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbDatabases.SelectedItem is not string selectedDatabase
                || string.IsNullOrWhiteSpace(selectedDatabase)
                || IsDisposed
                || Disposing)
                return;

            SelectedDatabase = selectedDatabase;
            if (_connectionSessions.TryGetValue(SelectedConnectionName, out IGeneralDb db) && db is not null)
            {
                db.ResetDbName(SelectedConnectionName, SelectedDatabase);
            }
        }
        private async void cbConnections_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (IsDisposed || Disposing || cbConnections.SelectedItem is not string newConnectionName)
                return;

            if (cbConnections.Enabled && _selectedConnectionName != newConnectionName)
            {
                try
                {
                    _selectedConnectionName = newConnectionName;
                    _setSelectedConnectionName(newConnectionName);
                    _netezzaSqlCompletionServices.InvalidateSchema();
                    await _editorUi.CbConnectionsSelectedIndexChanged((o) => SetEnabledConnectionsDatabases(o));
                    if (!IsDisposed && !Disposing)
                        _netezzaSqlCompletionServices.EnsureSchemaForConnection(_completionContext, SelectedConnectionName);
                }
                catch (OperationCanceledException)
                {
                    // A connection switch can be superseded during shutdown.
                }
                catch (Exception exception)
                {
                    if (!IsDisposed && !Disposing)
                        _logger.LogError("Connection switch failed", exception);
                }
            }
        }

        private void FastColoredTextBox1_GotFocus(object sender, EventArgs e)
        {
            IReadOnlyList<string> connections = _editorCatalogState.Snapshot.Connections;
            if (!connections.Contains(SelectedConnectionName, StringComparer.OrdinalIgnoreCase) && connections.Count > 0)
            {
                SelectedConnectionName = connections[0];
            }

            _setSelectedConnectionName(SelectedConnectionName);
                    _netezzaSqlCompletionServices.EnsureSchemaForConnection(_completionContext, SelectedConnectionName);
        }

        void popupMenu_Opening(object sender, CancelEventArgs e)
        {
            //(CurrentTB.Tag as TbInfo).popupMenu.Items;

            //---block autocomplete menu for comments
            //get index of green style(used for comments)

            //_popupMenu.Items.SetAutocompleteItems(_autocompleteSource);

            var iGreenStyle = fastColoredTextBox1.GetStyleIndex(_colorTheme.CurrentFctbColors.CommentsStyle);
            if (iGreenStyle >= 0)
                if (fastColoredTextBox1.Selection.Start.iChar > 0)
                {
                    //current char (before caret)
                    var c = fastColoredTextBox1[fastColoredTextBox1.Selection.Start.iLine][fastColoredTextBox1.Selection.Start.iChar - 1];
                    //green Style
                    var greenStyleIndex = FastColoredTextBoxNS.Range.ToStyleIndex(iGreenStyle);
                    //if char contains green style then block popup menu
                    if ((c.style & greenStyleIndex) != 0)
                        e.Cancel = true;
                }
        }

        // The menu normally opens automatically; it can be forced here (Ctrl+Space).
        private void FastColored_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == (Keys.Space | Keys.Control))
            {
                //forced show (MinFragmentLength will be ignored)
                ((sender as FastColoredTextBox).Tag as TbInfo).PopupMenu.Show(true);
                e.Handled = true;
            }
        }

        private void RunToolStrip_Click(object sender, EventArgs e) =>
            _ = ObserveUiOperationAsync(nameof(RunToolStrip_Click), () => _editorUi.RunSQL());

        private void RunCtrlF5_Click(object sender, EventArgs e) =>
            _ = ObserveUiOperationAsync(nameof(RunCtrlF5_Click), () => _editorUi.RunSQL(1));

        public void RunExcel_Click(object sender, EventArgs e) =>
            _ = ObserveUiOperationAsync(nameof(RunExcel_Click), () => _editorUi.RunSQL(0, ExportOptions.xlsx));

        public void RunCSV_Click(object sender, EventArgs e) =>
            _ = ObserveUiOperationAsync(nameof(RunCSV_Click), () => _editorUi.RunSQL(0, ExportOptions.csv));

        public void runToCursorToolStripMenuItem_Click(object sender, EventArgs e) =>
            _ = ObserveUiOperationAsync(nameof(runToCursorToolStripMenuItem_Click), () => _editorUi.RunSQL(4));

        private void btStop_Click(object sender, EventArgs e)
        {
            _editorUi.Stop_Click(sender, e);
        }

        private void scriptModeToolStripMenuItem_Click(object sender, EventArgs e) =>
            _ = ObserveUiOperationAsync(nameof(scriptModeToolStripMenuItem_Click), () => _editorUi.RunSQL(0, ExportOptions.onlyLog));

        private void tsbImport_Click(object sender, EventArgs e)
        {
            _editorUi.XLSXtoolStripMenuItem_Click(sender, e);
        }

        private bool keepConnectionOpen;
        public bool KeepConnectionOpen
        {
            get => keepConnectionOpen;
            set
            {
                keepConnectionOpen = value;
                tsbKeepConnection.Checked = value;
                    _applicationSettingsContext.Config.CloseConnectionByDefault = !value;
                _editorUi.RefreshTabKeepConnectionProperty();
            }
        }

        private void tsbKeepConnection_Click(object sender, EventArgs e)
        {
            KeepConnectionOpen = !KeepConnectionOpen;
        }

        private void tsbFormatSql_Click(object sender, EventArgs e)
        {
            try
            {
                var editor = fastColoredTextBox1;
                if (editor is null || editor.IsDisposed)
                    return;

                string sql = editor.Text;
                if (string.IsNullOrWhiteSpace(sql))
                    return;

                var formatAction = _codeActionProvider.GetFormatAction();
                string newSql = formatAction.Apply(sql);
                if (newSql != sql)
                {
                    editor.Text = newSql;
                }
            }
            catch (Exception ex)
            {
                _logger.MessageBox_Show(_editorUi,
                    $"Failed to format SQL: {ex.Message}",
                    "Format Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        bool IEditorPanel.ContinueOnError { get => ContinueOnError; set => ContinueOnError = value; }
        bool IEditorPanel.KeepConnectionOpen { get => KeepConnectionOpen; set => KeepConnectionOpen = value; }
        void IEditorPanel.SetEnabledConnectionsDatabases(bool enabled) => SetEnabledConnectionsDatabases(enabled);
        FastColoredTextBox IEditorPanel.CurrentTb => CurrentTb;
        string IEditorPanel.SelectedConnectionName { get => SelectedConnectionName; set => SelectedConnectionName = value; }
        string IEditorPanel.SelectedDatabase { get => SelectedDatabase; set => SelectedDatabase = value; }

        public bool ContinueOnError { get; set; }
        private void tsbContinueOnError_Click(object sender, EventArgs e)
        {
            ContinueOnError = !ContinueOnError;
            tsbContinueOnError.Checked = ContinueOnError;
        }

        public void commentSelectedLinesToolStripMenuItemClick(object sender, EventArgs e)
        {
            fastColoredTextBox1.InsertLinePrefix(fastColoredTextBox1.CommentPrefix);
        }

        public void uncommentSelectedLinesToolStripMenuItemClick(object sender, EventArgs e)
        {
            fastColoredTextBox1.RemoveLinePrefix(fastColoredTextBox1.CommentPrefix);
        }

        private void NewToolStripButton_Click(object sender, EventArgs e)
        {
            _editorUi.OpenNewSqlDocument();
        }

        public void SetDocumentId(EditorDocumentId documentId) =>
            _autocompleteSource?.SetDocumentId(documentId);

        public void ResetAutocompleteCache() => _autocompleteSource?.ResetCache();

        private void SaveToolStripButton_Click(object sender, EventArgs e)
        {
            _editorUi.SaveOnTabEventHandler(sender, e);
        }

        private void OpenToolStripButton_Click(object sender, EventArgs e) =>
            _ = ObserveUiOperationAsync(nameof(OpenToolStripButton_Click), _editorUi.OpenAsync);

        private async Task ObserveUiOperationAsync(string operationName, Func<Task> operation)
        {
            try
            {
                await operation();
            }
            catch (OperationCanceledException)
            {
                // Closing the editor or replacing a run is an expected end.
            }
            catch (Exception exception)
            {
                _logger.LogError($"{operationName} failed", exception);
            }
        }

        private void CutToolStripButton_Click(object sender, EventArgs e)
        {
            fastColoredTextBox1.Cut();
        }

        private void CopyToolStripButton_Click(object sender, EventArgs e)
        {
            fastColoredTextBox1.Copy();
        }

        private void PasteToolStripButton_Click(object sender, EventArgs e)
        {
            _editorUi.ForceNormalPaste = true;
            fastColoredTextBox1.Paste();
            _editorUi.ForceNormalPaste = false;
        }
        private void PrintToolStripButton_Click(object sender, EventArgs e)
        {
            fastColoredTextBox1.Print(new PrintDialogSettings() { ShowPrintPreviewDialog = true });
        }

        private void SetupEditorLayout()
        {
            _editorLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            _editorLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _editorLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            Controls.Add(_editorLayout);
            _editorLayout.Controls.Add(toolStrip1, 0, 0);
            toolStrip1.Dock = DockStyle.Fill;
            toolStrip1.GripStyle = ToolStripGripStyle.Hidden;
        }

        private const int ToolStripComboLogicalWidth = 170;

        private void ApplyToolStripDpiMetrics()
        {
            int dpi = DeviceDpi;
            toolStrip1.ImageScalingSize = DpiScale.Scale(new Size(16, 16), dpi);
            ApplyExecutionToolbarIcons(dpi);
            int comboWidth = DpiScale.Scale(ToolStripComboLogicalWidth, dpi);
            cbConnections.Width = comboWidth;
            cbDatabases.Width = comboWidth;
            UpdateComboDropDownWidth(cbConnections);
            UpdateComboDropDownWidth(cbDatabases);
            toolStrip1.AutoSize = true;
        }

        private void ApplyExecutionToolbarIcons(int dpi)
        {
            DisposeToolbarIcons();

            Size iconSize = DpiScale.Scale(new Size(20, 20), dpi);
            _runToolbarIcon = SqlToolbarIconFactory.CreatePlay(iconSize);
            _stopToolbarIcon = SqlToolbarIconFactory.CreateStop(iconSize);
            _commentToolbarIcon = SqlToolbarIconFactory.CreateComment(iconSize, uncomment: false);
            _uncommentToolbarIcon = SqlToolbarIconFactory.CreateComment(iconSize, uncomment: true);
            _importToolbarIcon = SqlToolbarIconFactory.CreateImport(iconSize);
            _keepConnectionToolbarIcon = SqlToolbarIconFactory.CreateKeepConnection(iconSize);
            _formatToolbarIcon = SqlToolbarIconFactory.CreateFormat(iconSize);

            // These icons are already rendered at the target DPI. Letting ToolStrip
            // rescale the old 88x88 bitmaps made the controls look tiny and soft.
            btRunToolStrip.Image = _runToolbarIcon;
            btRunToolStrip.ImageScaling = ToolStripItemImageScaling.None;
            btStop.Image = _stopToolbarIcon;
            btStop.ImageScaling = ToolStripItemImageScaling.None;
            commentSelectedLinesToolStripMenuItem.Image = _commentToolbarIcon;
            commentSelectedLinesToolStripMenuItem.ImageScaling = ToolStripItemImageScaling.None;
            uncommentSelectedLinesToolStripMenuItem.Image = _uncommentToolbarIcon;
            uncommentSelectedLinesToolStripMenuItem.ImageScaling = ToolStripItemImageScaling.None;
            tsbImport.Image = _importToolbarIcon;
            tsbImport.ImageScaling = ToolStripItemImageScaling.None;
            tsbKeepConnection.Image = _keepConnectionToolbarIcon;
            tsbKeepConnection.ImageScaling = ToolStripItemImageScaling.None;
            tsbFormatSql.Image = _formatToolbarIcon;
            tsbFormatSql.ImageScaling = ToolStripItemImageScaling.None;
        }

        private void DisposeToolbarIcons()
        {
            _runToolbarIcon?.Dispose();
            _runToolbarIcon = null;
            _stopToolbarIcon?.Dispose();
            _stopToolbarIcon = null;
            _commentToolbarIcon?.Dispose();
            _commentToolbarIcon = null;
            _uncommentToolbarIcon?.Dispose();
            _uncommentToolbarIcon = null;
            _importToolbarIcon?.Dispose();
            _importToolbarIcon = null;
            _keepConnectionToolbarIcon?.Dispose();
            _keepConnectionToolbarIcon = null;
            _formatToolbarIcon?.Dispose();
            _formatToolbarIcon = null;
        }

        private void ApplyAutocompleteImageListDpi()
        {
            if (_autocompleteMenu == null || _autocompleteMenu.IsDisposed)
            {
                return;
            }

            ImageList next = SqlCompletionIconFactory.Create(DeviceDpi);
            ImageList? previous = _autocompleteImageList;
            _autocompleteImageList = next;
            _autocompleteMenu.ImageList = next;
            _autocompleteMenu.Items.Invalidate();
            previous?.Dispose();
        }

        private void ApplyAutocompletePopupMetrics()
        {
            if (_autocompleteMenu == null || _autocompleteMenu.IsDisposed)
            {
                return;
            }

            int dpi = DeviceDpi;
            int width = DpiScale.Scale(CompletionPopupLogicalWidth, dpi);
            int minimumWidth = DpiScale.Scale(CompletionPopupLogicalMinimumWidth, dpi);
            int height = DpiScale.Scale(CompletionPopupLogicalMaximumHeight, dpi);
            var maximumSize = new Size(width, height);

            _autocompleteMenu.MaximumSize = maximumSize;
            _autocompleteMenu.MinimumSize = new Size(minimumWidth, 0);
            _autocompleteMenu.Items.Width = width;
            _autocompleteMenu.Items.MaximumSize = maximumSize;
            _autocompleteMenu.Width = width;
        }

        private void DisposeAutocompleteIcons()
        {
            if (_autocompleteMenu != null && !_autocompleteMenu.IsDisposed)
            {
                _autocompleteMenu.ImageList = null;
            }

            _autocompleteImageList?.Dispose();
            _autocompleteImageList = null;
            _autocompleteMenu = null;
        }

        private void UpdateComboDropDownWidth(ToolStripComboBox combo)
        {
            if (combo == null || combo.IsDisposed)
            {
                return;
            }

            int dpi = combo.Owner?.DeviceDpi ?? DeviceDpi;
            Font font = combo.Font ?? Font;
            int maxWidth = combo.Width;
            int horizontalPad = DpiScale.Scale(28, dpi);

            void consider(string? text)
            {
                if (string.IsNullOrEmpty(text))
                {
                    return;
                }

                maxWidth = Math.Max(maxWidth, TextRenderer.MeasureText(text, font).Width + horizontalPad);
            }

            consider(combo.Text);
            foreach (object item in combo.Items)
            {
                consider(item?.ToString());
            }

            int minWidth = DpiScale.Scale(ToolStripComboLogicalWidth, dpi);
            int maxComboWidth = DpiScale.Scale(320, dpi);
            combo.Width = Math.Max(minWidth, Math.Min(maxWidth, maxComboWidth));
            combo.DropDownWidth = maxWidth;
        }

        private void ApplyFastColoredTextBoxDpiMetrics()
        {
            if (fastColoredTextBox1 == null)
            {
                return;
            }

            FctbDpiHelper.ApplyCharMetrics(fastColoredTextBox1);
            ApplyAutocompleteImageListDpi();
            ApplyAutocompletePopupMetrics();
        }

        protected override void OnDpiChangedAfterParent(EventArgs e)
        {
            base.OnDpiChangedAfterParent(e);
            ApplyToolStripDpiMetrics();
            ApplyFastColoredTextBoxDpiMetrics();
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            ApplyFastColoredTextBoxDpiMetrics();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            toolStrip1?.BringToFront();
        }
    }
}
