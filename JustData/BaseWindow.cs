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
using AppBase.Services.Utilities;
using JustyBaseLegacy.UI.Fim;
using JustyBaseLegacy.UI.Sql;
using JustyBaseLegacy.UI.ImportExport;
using JustData.Application.ImportExport;
using JustData.Application.History;
using JustData.Application.QueryWatch;
using JustData.ViewModels.QueryWatch;
using DatabaseDataGridView.WinForms;
using DatabaseDataGridView.WinForms.Coloring;
using FastColoredTextBoxNS;
using FastColoredTextBoxNS.Helpers;
using JustDataAdditionalForms;
using JustyBase.NetezzaDriver;
using System.Drawing;
using JustyBase.NetezzaSqlParser.Authoring;
using JustyBase.NetezzaSqlParser.Linter;
using JustyBaseLegacy.UI.Helpers;
using JustyBaseLegacy.Services;
using JustyBaseLegacy.UI.Controls;
using JustyBaseLegacy.UI.DbForms;
using JustyBaseLegacy.UI.Extensions;
using JustyBaseLegacy.UI.Models;
using JustyBaseLegacy.UI.Login;
using JustyBaseLegacy.UI.Windowing;
using JustData.ViewModels;
using JustData.Application.Communication;
using JustData.Application;
using JustData.Application.Editor;
using JustData.Application.Files;
using JustData.Application.Login;
using JustData.Application.Variables;
using JustData.Application.Schema;
using JustData.Application.Startup;
using JustData.Application.Sql;
using JustData.ViewModels.Variables;
using JustData.ViewModels.Files;
using JustData.Application.Git;
using JustData.ViewModels.Git;
using JustyBaseLegacy.UI.Forms;
using JustData.ViewModels.Editor;
using JustData.ViewModels.Explorer;
using JustData.ViewModels.ImportExport;
using JustyBaseLegacy.UI.Schema;
using JustyBaseLegacy.UI.ObjectExplorer;
using JustyBaseLegacy.UI.Theme;
using JustyBaseLegacy.UI.Editor;
using SpreadSheetTasks;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
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
using WeifenLuo.WinFormsUI.Docking;

namespace JustyBaseLegacy.UI
{
    public partial class BaseWindow : Form, IEditorHost, IWinFormsSqlResultView, ISqlResultsUiView, ISqlEditorUiPort
    {
        private void Form1_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                WindowNativeMethods.ReleaseCapture();
                WindowNativeMethods.SendMessage(Handle, WindowConstants.WM_NCLBUTTONDOWN, WindowConstants.HT_CAPTION, 0);
            }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                const int WS_MAXIMIZEBOX = 0x00010000;
                const int WS_MINIMIZEBOX = 0x00020000;
                cp.Style |= WS_MAXIMIZEBOX;
                cp.Style |= WS_MINIMIZEBOX;
                return cp;
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WindowConstants.WM_COPYDATA)
            {
                if (m.LParam != IntPtr.Zero)
                {
                    COPYDATASTRUCT data = Marshal.PtrToStructure<COPYDATASTRUCT>(m.LParam);
                    string? rawPath = data.lpData == IntPtr.Zero || data.cbData <= 0
                        ? null
                        : Marshal.PtrToStringUni(data.lpData, data.cbData / 2);
                    if (JustData.Application.Communication.ExternalOpenRequest.TryCreate(rawPath, out var request))
                    {
                        _ = _externalOpenRequestRouter.RouteAsync(request!);
                    }
                }

            }
            else
            {
                CustomProc(ref m);
            }
        }

        private void HandleExternalOpenRequest(JustData.Application.Communication.ExternalOpenRequest request)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                WindowNativeMethods.ShowWindow(Handle, WindowConstants.SW_RESTORE);
            }

            if (request.Path.EndsWith(".manysql.enc", StringComparison.OrdinalIgnoreCase)
                || request.Path.EndsWith(".manysql", StringComparison.OrdinalIgnoreCase))
            {
                _ = OpenManySQLhAsync(request.Path);
            }
            else
            {
                _ = OpenSqlFileAsync(request.Path);
            }
        }


        public FastColoredTextBox CurrentTB
        {
            get
            {
                FastColoredTextBox fctb = null;

                if (this.InvokeRequired)
                {
                    this.Invoke(() =>
                    {
                        fctb = _tabManager.CurrentEditor;
                    });
                }
                else
                {
                    fctb = _tabManager.CurrentEditor;
                }
                return fctb;
            }
        }

        SQLUpperPanel CurrentUpper
        {
            get
            {
                SQLUpperPanel upper = null;

                if (this.InvokeRequired)
                {
                    this.Invoke(() =>
                    {
                        upper = _tabManager.CurrentEditorPanel as SQLUpperPanel;
                    });
                }
                else
                {
                    upper = _tabManager.CurrentEditorPanel as SQLUpperPanel;
                }
                return upper;
            }
        }

        public IEnumerable<AutocompleteItem> ActualSuggestionList
        {
            get
            {
                if (CurrentTB == null)
                    return null;
                return (CurrentTB.Tag as TbInfo).SuggestionList;
            }
        }

        public Dictionary<string, List<string>> AdditionalTabletData
        {
            get
            {
                if (CurrentTB == null)
                    return null;
                return (CurrentTB.Tag as TbInfo).AdditionalTableData;
            }
        }

        public Dictionary<string, List<string>> AdditionalDataWith
        {
            get
            {
                if (CurrentTB == null)
                    return null;
                return (CurrentTB.Tag as TbInfo).AdditionalDataWith;
            }
        }

        SplitContainer? CurrentSplitContainer
        {
            get
            {
                if (_tabManager.CurrentSplitContainer is { } current)
                    return current;

                // DockSuite can temporarily have the Results tool as the active
                // content while the SQL editor still owns keyboard focus.  The
                // detached compatibility TabPage then is not the inner
                // TabControl's SelectedTab, so resolve the splitter through the
                // stable document identity captured for the focused editor.
                if (CurrentTB is { } editor
                    && _documentIdsByEditor.TryGetValue(editor, out EditorDocumentId documentId))
                {
                    TabPage? documentTab = _documentIdsByTab
                        .FirstOrDefault(item => item.Value == documentId)
                        .Key;
                    if (documentTab is not null
                        && _tabManager.GetSplitContainerForTab(documentTab) is { } documentSplitter)
                    {
                        return documentSplitter;
                    }
                }

                return ActiveEditorTabPage is { } activeTab
                    ? _tabManager.GetSplitContainerForTab(activeTab)
                    : null;
            }
        }

        private IReadOnlyList<TabPage> EditorTabPages => _tabManager is DockSuiteTabManager dockSuite
            ? dockSuite.GetEditorTabPages()
            : _tabControlMain.TabPages.Cast<TabPage>().ToArray();

        private TabPage? ActiveEditorTabPage => _tabManager is DockSuiteTabManager dockSuite
            ? dockSuite.GetActiveEditorTabPage()
            : _tabControlMain.SelectedTab;



        private readonly ILogger _loggerLoud;
        private readonly IImportExportTasks _importExportTasks;
        private readonly IGeneralDbService _generalDbService;
        private readonly IConnectionSessionRegistry _connectionSessions;
        private readonly INetezzaSchemaTableCatalogWriter _schemaTables;
        private readonly JustData.Application.Login.IApplicationSession _applicationSession;
        private readonly IFormatterService _formatter;
        private readonly ShellViewModel _shellViewModel;
        private readonly VariablesViewModel _variablesViewModel;
        private readonly EditorWorkspaceViewModel _editorWorkspaceViewModel;
        private readonly IManySqlBundleService _manySqlBundleService;
        private readonly IExternalOpenRequestRouter _externalOpenRequestRouter;
        private readonly DatabaseExplorerViewModel _databaseExplorerViewModel;
        private readonly ObjectExplorerViewModel _objectExplorerViewModel;
        private Controls.MvvmDatabaseExplorerControl? _mvvmDatabaseExplorerControl;
        private Controls.MvvmObjectExplorerControl? _mvvmObjectExplorerControl;
        private readonly IUiHelperService _uiHelperService;
        private readonly IWindowManagementService _windowManagementService;
        private readonly IColorTheme _colorTheme;
        private readonly IAutocompleteClass _autocompleteClass;
        private readonly INetezzaHelperService _netezzaHelperService;
        private readonly IApplicationSettingsContext _applicationSettingsContext;
        private readonly IDatabaseRuntimeContext _databaseRuntimeContext;
        private readonly IDatabaseRuntimeCatalogWriter _databaseCatalogWriter;
        private readonly INetezzaCompletionContext _completionContext;
        private readonly INetezzaCompletionRuntimeContext _completionRuntimeContext;
        private readonly INetezzaAutocompleteState _netezzaAutocompleteState;
        private readonly INetezzaDdlCodeProvider _ddlCodeProvider;
        private readonly ISessionVariableStore _sessionVariableStore;
        private readonly INumberFormattingContext _numberFormattingContext;
        private readonly ITabNameProvider _tabNameProvider;
        private readonly ITextFileContentReader _textFileContentReader;
        private readonly IInlineCommandRunner _inlineCommandRunner;
        private readonly ISessionVariableRuntimeContext _sessionVariableRuntimeContext;
        private readonly IApplicationSettingsPersistence _settingsPersistence;
        private readonly IRecentFileRuntimeContext _recentFileRuntimeContext;
        private readonly ISnippetInitializationContext _snippetInitializationContext;
        private readonly TabControlDrawingHandler _tabControlDrawingHandler;
        private readonly NetezzaSqlCompletionServices _netezzaSqlCompletionServices;
        private readonly LegacySqlAuthoringServices _legacySqlAuthoringServices;
        private readonly ISqlExecutionSessionRegistry _sqlExecutionSessionRegistry;
        private readonly IResultExportUseCase _resultExportUseCase;
        private readonly IDocumentResultGridRegistry _resultGridRegistry;
        private readonly IEditorCatalogState _editorCatalogState;
        private readonly ImportExportViewModelFactory _importExportViewModelFactory;
        private readonly LoginFormFactory _loginFormFactory;
        private readonly ISqlPreprocessingService _sqlPreprocessingService;
        private readonly ISpecialCommandService _specialCommandService;
        private readonly SqlExecutionRiskGate _sqlRiskGate;
        private readonly ISchemaDdlService _ddlService;
        private readonly ISchemaRefreshCoordinator _schemaRefreshCoordinator;
        private readonly IConnectionProfileCatalog _connectionProfileCatalog;
        private readonly EditorCatalogProjection _editorCatalogProjection;
        private readonly IFileSearchEngine _fileSearchEngine;
        private readonly ILoginDataValidator _loginDataValidator;
        private readonly ICodeActionProvider _codeActionProvider;
        private readonly IHistoryStore _historyStore;
        private readonly IQueryWatchService _queryWatchService;
        private readonly IUiDispatcher _uiDispatcher;
        private QueryWatch? _queryWatch;
        private readonly Dictionary<EditorDocumentId, (FastColoredTextBox Editor, DataGridView Grid)> _lintDiagnosticsTargets = new();
        private readonly SqlResultsUiFactory _sqlResultsUiFactory;
        private readonly WinFormsSqlResultPresenter _sqlResultPresenter;
        private readonly DocumentExecutionLifecyclePresenter _documentExecutionLifecyclePresenter;
        private readonly ObjectExplorerNavigationController _objectExplorerNavigationController;
        private readonly NetezzaSchemaRefreshController _netezzaSchemaRefreshController;
        private readonly WinFormsThemePresenter _themePresenter = new();
        private readonly Dictionary<string, Action> _specialActions;

        private readonly ITabManager _tabManager;
        private readonly Dictionary<TabPage, EditorDocumentId> _documentIdsByTab = new();
        private readonly Dictionary<FastColoredTextBox, EditorDocumentId> _documentIdsByEditor = new();
        private readonly Sql.LightbulbManager _lightbulbManager;

        private void ForgetLegacyResultCommand(TabPage tabPage)
        {
            if (tabPage.Tag is TabPageResultsTag { DocumentId: { } documentId } tag && tag.Key is { } key)
            {
                _sqlResultPresenter.RemovePendingResult(key);
                _editorWorkspaceViewModel.Documents
                    .FirstOrDefault(document => document.Id == documentId)
                    ?.SqlExecution.RemoveResult(key);
            }
        }

        // --- Shared services (injected) ---
        // --- Chrome / theme: BaseWindow.Theme.cs ---
        // --- Tabs: BaseWindow.Tabs.cs ---
        // --- SQL: BaseWindow.SqlExecution.cs ---
        // --- Lifecycle: BaseWindow.Lifecycle.cs ---
        // --- Grid results: BaseWindow.GridResults.cs ---
        // --- Editor: BaseWindow.Editor.cs ---
        // --- Object explorer: BaseWindow.ObjectExplorer.cs ---
        // --- Lifecycle: BaseWindow.Lifecycle.cs ---
        // --- Grid results: BaseWindow.GridResults.cs ---
        // --- Editor: BaseWindow.Editor.cs ---
        // --- Object explorer: BaseWindow.ObjectExplorer.cs ---
        // --- DB explorer menus: BaseWindow.DbExplorerMenus.cs ---
        // --- Schema refresh: BaseWindow.SchemaRefresh.cs ---
        // --- File ops: BaseWindow.FileOps.cs ---
        // --- Import/export: BaseWindow.ImportExport.cs ---


        public BaseWindow(
            ILogger logger,
            IImportExportTasks importExportTasks,
            IApplicationSettingsContext applicationSettingsContext,
            IColorTheme colorTheme,
            IDatabaseRuntimeContext databaseRuntimeContext,
            IDatabaseRuntimeCatalogWriter databaseCatalogWriter,
            INetezzaCompletionContext completionContext,
            INetezzaCompletionRuntimeContext completionRuntimeContext,
            INetezzaAutocompleteState netezzaAutocompleteState,
            ISessionVariableRuntimeContext sessionVariableRuntimeContext,
            IApplicationSettingsPersistence settingsPersistence,
            IRecentFileRuntimeContext recentFileRuntimeContext,
            ISnippetInitializationContext snippetInitializationContext,
            INetezzaDdlCodeProvider ddlCodeProvider,
            ISessionVariableStore sessionVariableStore,
            INumberFormattingContext numberFormattingContext,
            ITabNameProvider tabNameProvider,
            ITextFileContentReader textFileContentReader,
            IInlineCommandRunner inlineCommandRunner,
            IGeneralDbService generalDbService,
            IConnectionSessionRegistry connectionSessions,
            INetezzaSchemaTableCatalogWriter schemaTables,
            IUiHelperService uiHelperService,
            INetezzaHelperService netezzaHelperService,
            IWindowManagementService windowManagementService,
            NetezzaSqlCompletionServices netezzaSqlCompletionServices,
            LegacySqlAuthoringServices legacySqlAuthoringServices,
            ITabManager tabManager,
            JustData.Application.Login.IApplicationSession applicationSession,
            IFormatterService formatter,
            ShellViewModel shellViewModel,
            IExternalOpenRequestRouter externalOpenRequestRouter,
            VariablesViewModel variablesViewModel,
            FilesViewModel filesViewModel,
            GitViewModel gitViewModel,
            EditorWorkspaceViewModel editorWorkspaceViewModel,
            IManySqlBundleService manySqlBundleService,
            DatabaseExplorerViewModel databaseExplorerViewModel,
            ObjectExplorerViewModel objectExplorerViewModel,
            IResultExportUseCase resultExportUseCase,
            ImportExportViewModelFactory importExportViewModelFactory
            , LoginFormFactory loginFormFactory
            , IFileSearchEngine fileSearchEngine
            , ILoginDataValidator loginDataValidator
            , ICodeActionProvider codeActionProvider
            , ISqlPreprocessingService sqlPreprocessingService
            , ISpecialCommandService specialCommandService
            , SqlExecutionRiskGate sqlRiskGate
            , ISchemaDdlService ddlService
            , ISchemaRefreshCoordinator schemaRefreshCoordinator
            , IHistoryStore historyStore
            , IQueryWatchService queryWatchService
            , ISqlExecutionSessionRegistry sqlExecutionSessionRegistry
            , IDocumentResultGridRegistry resultGridRegistry
            , IEditorCatalogState editorCatalogState
            , IConnectionProfileCatalog connectionProfileCatalog
            , EditorCatalogProjection editorCatalogProjection
            , FimEditorHost? fimEditorHost = null
            , JustyBase.Ai.Fim.Download.IFimModelCatalog? fimModelCatalog = null
            , JustyBaseLegacy.UI.Fim.IFimModelBootstrapService? fimModelBootstrap = null
            , IUiDispatcher? uiDispatcher = null
            )
        {
            _tabManager = tabManager ?? throw new ArgumentNullException(nameof(tabManager));
            _loggerLoud = logger ?? throw new ArgumentNullException(nameof(logger));
            _loggerLoud.SetWindow(this);

            _importExportTasks = importExportTasks ?? throw new ArgumentNullException(nameof(importExportTasks));
            _applicationSettingsContext = applicationSettingsContext ?? throw new ArgumentNullException(nameof(applicationSettingsContext));
            _colorTheme = colorTheme ?? throw new ArgumentNullException(nameof(colorTheme));
            _databaseRuntimeContext = databaseRuntimeContext ?? throw new ArgumentNullException(nameof(databaseRuntimeContext));
            _databaseCatalogWriter = databaseCatalogWriter ?? throw new ArgumentNullException(nameof(databaseCatalogWriter));
            _completionContext = completionContext ?? throw new ArgumentNullException(nameof(completionContext));
            _completionRuntimeContext = completionRuntimeContext ?? throw new ArgumentNullException(nameof(completionRuntimeContext));
            _netezzaAutocompleteState = netezzaAutocompleteState ?? throw new ArgumentNullException(nameof(netezzaAutocompleteState));
            _ddlCodeProvider = ddlCodeProvider ?? throw new ArgumentNullException(nameof(ddlCodeProvider));
            _sessionVariableStore = sessionVariableStore ?? throw new ArgumentNullException(nameof(sessionVariableStore));
            _numberFormattingContext = numberFormattingContext ?? throw new ArgumentNullException(nameof(numberFormattingContext));
            _tabNameProvider = tabNameProvider ?? throw new ArgumentNullException(nameof(tabNameProvider));
            _textFileContentReader = textFileContentReader ?? throw new ArgumentNullException(nameof(textFileContentReader));
            _inlineCommandRunner = inlineCommandRunner ?? throw new ArgumentNullException(nameof(inlineCommandRunner));
            _sessionVariableRuntimeContext = sessionVariableRuntimeContext ?? throw new ArgumentNullException(nameof(sessionVariableRuntimeContext));
            _settingsPersistence = settingsPersistence ?? throw new ArgumentNullException(nameof(settingsPersistence));
            _recentFileRuntimeContext = recentFileRuntimeContext ?? throw new ArgumentNullException(nameof(recentFileRuntimeContext));
            _snippetInitializationContext = snippetInitializationContext ?? throw new ArgumentNullException(nameof(snippetInitializationContext));
            _generalDbService = generalDbService ?? throw new ArgumentNullException(nameof(generalDbService));
            _connectionSessions = connectionSessions ?? throw new ArgumentNullException(nameof(connectionSessions));
            _schemaTables = schemaTables ?? throw new ArgumentNullException(nameof(schemaTables));
            _uiHelperService = uiHelperService ?? throw new ArgumentNullException(nameof(uiHelperService));
            _netezzaHelperService = netezzaHelperService ?? throw new ArgumentNullException(nameof(netezzaHelperService));
            _windowManagementService = windowManagementService;
            _netezzaSqlCompletionServices = netezzaSqlCompletionServices ?? throw new ArgumentNullException(nameof(netezzaSqlCompletionServices));
            _legacySqlAuthoringServices = legacySqlAuthoringServices ?? throw new ArgumentNullException(nameof(legacySqlAuthoringServices));
            _applicationSession = applicationSession ?? throw new ArgumentNullException(nameof(applicationSession));
            _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
            _shellViewModel = shellViewModel ?? throw new ArgumentNullException(nameof(shellViewModel));
            _variablesViewModel = variablesViewModel ?? throw new ArgumentNullException(nameof(variablesViewModel));
            _filesViewModel = filesViewModel ?? throw new ArgumentNullException(nameof(filesViewModel));
            _gitViewModel = gitViewModel ?? throw new ArgumentNullException(nameof(gitViewModel));
            _editorWorkspaceViewModel = editorWorkspaceViewModel ?? throw new ArgumentNullException(nameof(editorWorkspaceViewModel));
            _manySqlBundleService = manySqlBundleService ?? throw new ArgumentNullException(nameof(manySqlBundleService));
            _editorWorkspaceViewModel.DocumentReloaded += OnEditorDocumentReloaded;
            _editorWorkspaceViewModel.PropertyChanged += OnEditorWorkspacePropertyChanged;
            WireGitTimelineToActiveDocument();
            _externalOpenRequestRouter = externalOpenRequestRouter ?? throw new ArgumentNullException(nameof(externalOpenRequestRouter));
            _databaseExplorerViewModel = databaseExplorerViewModel ?? throw new ArgumentNullException(nameof(databaseExplorerViewModel));
            _objectExplorerViewModel = objectExplorerViewModel ?? throw new ArgumentNullException(nameof(objectExplorerViewModel));
            _objectExplorerNavigationController = new ObjectExplorerNavigationController(() => _mvvmDatabaseExplorerControl);
            _netezzaSchemaRefreshController = new NetezzaSchemaRefreshController(RefreshTableListInternalAsync);
            _sqlExecutionSessionRegistry = sqlExecutionSessionRegistry ?? throw new ArgumentNullException(nameof(sqlExecutionSessionRegistry));
            _resultGridRegistry = resultGridRegistry ?? throw new ArgumentNullException(nameof(resultGridRegistry));
            _editorCatalogState = editorCatalogState ?? throw new ArgumentNullException(nameof(editorCatalogState));
            _importExportViewModelFactory = importExportViewModelFactory ?? throw new ArgumentNullException(nameof(importExportViewModelFactory));
            _loginFormFactory = loginFormFactory ?? throw new ArgumentNullException(nameof(loginFormFactory));
            _sqlPreprocessingService = sqlPreprocessingService ?? throw new ArgumentNullException(nameof(sqlPreprocessingService));
            _specialCommandService = specialCommandService ?? throw new ArgumentNullException(nameof(specialCommandService));
            _sqlRiskGate = sqlRiskGate ?? throw new ArgumentNullException(nameof(sqlRiskGate));
            _ddlService = ddlService ?? throw new ArgumentNullException(nameof(ddlService));
            _schemaRefreshCoordinator = schemaRefreshCoordinator ?? throw new ArgumentNullException(nameof(schemaRefreshCoordinator));
            _historyStore = historyStore ?? throw new ArgumentNullException(nameof(historyStore));
            _queryWatchService = queryWatchService ?? throw new ArgumentNullException(nameof(queryWatchService));
            _connectionProfileCatalog = connectionProfileCatalog ?? throw new ArgumentNullException(nameof(connectionProfileCatalog));
            _editorCatalogProjection = editorCatalogProjection ?? throw new ArgumentNullException(nameof(editorCatalogProjection));
            _fimEditorHost = fimEditorHost;
            _fimModelCatalog = fimModelCatalog;
            _fimModelBootstrap = fimModelBootstrap;
            _uiDispatcher = uiDispatcher ?? new JustData.Mvvm.WindowsFormsUiDispatcher(this);
            _fileSearchEngine = fileSearchEngine ?? throw new ArgumentNullException(nameof(fileSearchEngine));
            _loginDataValidator = loginDataValidator ?? throw new ArgumentNullException(nameof(loginDataValidator));
            _codeActionProvider = codeActionProvider ?? throw new ArgumentNullException(nameof(codeActionProvider));
            _resultExportUseCase = resultExportUseCase ?? throw new ArgumentNullException(nameof(resultExportUseCase));
            _specialActions = new Dictionary<string, Action>(StringComparer.OrdinalIgnoreCase)
            {
                ["WARN=0"] = () =>
                {
                    _applicationSettingsContext.Config.DoNotWarnFullUpdateDelete = true;
                    _settingsPersistence.SaveConfig();
                },
                ["WARN=1"] = () =>
                {
                    _applicationSettingsContext.Config.DoNotWarnFullUpdateDelete = false;
                    _settingsPersistence.SaveConfig();
                }
            };
            if (uiDispatcher is JustData.Mvvm.WindowsFormsUiDispatcher windowsFormsDispatcher)
                windowsFormsDispatcher.Attach(this);
            _externalOpenRequestRouter.SetDispatcher(uiDispatcher ?? new JustData.Mvvm.WindowsFormsUiDispatcher(this));
            _externalOpenRequestRouter.SetWorkspaceHandler(HandleExternalOpenRequest);
            _shellViewModel.OpenPreferencesRequested += OpenPreferencesFromShell;
            _shellViewModel.RefreshSchemaRequested += RefreshSchemaFromShell;
            _shellViewModel.ShutdownRequested += Close;
            _sqlResultsUiFactory = new SqlResultsUiFactory(this);
            _sqlResultPresenter = new WinFormsSqlResultPresenter(this);
            _documentExecutionLifecyclePresenter = new DocumentExecutionLifecyclePresenter(
                _editorWorkspaceViewModel, _sqlExecutionSessionRegistry, _sqlResultPresenter, _resultGridRegistry);
            _editorCatalogProjection.SeedFromProfiles(_connectionProfileCatalog);
            _lightbulbManager = new Sql.LightbulbManager(_lintIssuesByEditor, _codeActionProvider);
            _autocompleteClass = new AutocompleteClass(
                _completionContext,
                _applicationSettingsContext,
                this,
                _connectionSessions,
                _schemaTables,
                _netezzaHelperService,
                (connectionName, databaseName) =>
                    _ = OnNetezzaOneDatabaseAttachedAsync(connectionName, databaseName));
            _tabControlDrawingHandler = new TabControlDrawingHandler(_colorTheme, _applicationSettingsContext, this.Font
                , JustData.Properties.Resources.close, JustData.Properties.Resources.closeJasne
                , JustData.Properties.Resources.gray_pin, JustData.Properties.Resources.gray_pin_selected
                , JustData.Properties.Resources.Black_pin, JustData.Properties.Resources.Black_pin_selected);

            _netezzaHelperService.Initialize(_netezzaSchemaRefreshController);

            InitializeComponent();

            tabContextMenuStrip.Opening += TabContextMenuStrip_Opening;
            _tabManager.Initialize(_tabControlMain);

            if (_tabManager is DockSuiteTabManager dsm)
            {
                dsm.SetDocumentTabContextMenu(tabContextMenuStrip);
                dsm.TabCloseRequested = tabPage => _ = DoClosingOfTabAsync(_tabControlMain, tabPage);
                dsm.ActiveDocumentChanged = documentId =>
                {
                    if (_editorWorkspaceViewModel.Documents.Any(document => document.Id == documentId))
                    {
                        _editorWorkspaceViewModel.Activate(documentId);
                        TabControlMain_SelectedIndexChanged(this, EventArgs.Empty);
                        UpdateGitTimelineForActiveDocument();
                    }
                };
                dsm.DocumentOrderChanged = order => _editorWorkspaceViewModel.Reorder(order);
                dsm.ApplyTheme(_applicationSettingsContext.Config.UseSpecialColoring);
                RegisterLeftPanelTools(dsm);

                // Results live in the DockBottom Results tool (EnsureSqlResultsToolWindow /
                // ShowResultsForTab / WinFormsSqlResultPresenter) — the only results path.
                dsm.EnsureResultsToolWindow();

                // ── DockSuite layout fixes ────────────────────────────
                // 1. Hide the legacy _leftTabs (empty TabPages) so they don't
                //    leave a vertical empty strip. Collapse Panel1 fully.
                _leftTabs.Visible = false;
                splitContainer1.Panel1Collapsed = true;

                // 2. Eagerly initialize all left panels (they were lazy via
                //    _leftTabsSelectedIndexChanged in legacy mode).
                InitializeVariablesControl();
                InitializeFilesControl();
                InitializeGitControl();
                InitializeObjectExplorerControl();
                InitializeDockWindowMenu(dsm);
                UpdateGitTimelineForActiveDocument();

                // 3. The last registered tool (Legend) becomes the active
                //    tab in the DockLeft panel group. Re-activate Database.
                if (dsm.TryGetTool("Database", out var dbTool))
                {
                    dbTool.DockHandler.Activate();
                }

                // 4. Wire keyboard shortcuts to the form (KeyPreview=true).
                //    In DockSuite mode _tabControlMain is hidden and never
                //    receives focus, so F5/Ctrl+Enter/etc. would be dead.
                this.KeyDown += tabControlMain_KeyDown;
            }

            ControlBox = false;
            _titleBarCaptionButtons = new TitleBarCaptionButtonsControl(this)
            {
                Name = "titleBarCaptionButtons",
            };
            Controls.Add(_titleBarCaptionButtons);

            _numberFormattingContext.NumberWithDot.NumberDecimalSeparator = ".";
            if (_mvvmDatabaseExplorerControl?.CbWhatDb?.Items.Count > 0)
                _mvvmDatabaseExplorerControl.CbWhatDb.SelectedIndex = 0;

            _mvvmDatabaseExplorerControl?.CollapseAllNodes();

            // Extend client area!
            UpdateDwmMargins();
            SetStyle(ControlStyles.ResizeRedraw, true);

            UpdateMinimumSize();

            ApplyMenuStripTheme();

            ApplyShellLayout();
            LayoutMainSplitter();
            HandleCreated += (_, _) => RefreshTitleBarChrome();
            Shown += (_, _) =>
            {
                RefreshTitleBarChrome();
                TryOpenUiTestFileAfterShown();
            };
            Load += (_, _) =>
            {
                ApplyShellLayout();
                LayoutMainSplitter();
                RefreshTitleBarChrome();
            };

            RePaintMainWindowX();
            _colorTheme.SetStylesForFastColoring();

            this._mvvmDatabaseExplorerControl?.CbWhatDb.DrawItem += (o, e) => _uiHelperService.ColorComboBox_DrawItem(o, e, _applicationSettingsContext.Config.UseSpecialColoring, _colorTheme.GeneralBrush);


            _mvvmDatabaseExplorerControl?.TbFastSchemaSearch.ContextMenuStrip = new ContextMenuStrip();

            var itm = new ToolStripMenuItem()
            {
                Text = "Refresh sources"
            };

            itm.Click += (_, _) =>
                _ = RunUiEventAsync(nameof(LoadSourceTextCacheFromMenuAsync), LoadSourceTextCacheFromMenuAsync);
            _mvvmDatabaseExplorerControl?.TbFastSchemaSearch.ContextMenuStrip?.Items.Add(itm);

            StartsElements();
            _watchForNewImport.NotifyFilter = NotifyFilters.FileName;
            _watchForNewImport.Created += WatchForNewImport_Created;
            _watchForNewImport.Filter = "*.xlsx";
            _watchForNewImport.EnableRaisingEvents = true;
            recentXlsx.DropDownItemClicked += RecentXlsx_DropDownItemClicked;
            _regularActionTimer.Interval = 1_000 * 60 * _applicationSettingsContext.Config.RegularActionTimerMinutes;
            _regularActionTimer.Tick += RegularActionTimer_Tick;
            _regularActionTimer.Start();
        }

        private bool _uiTestOpenFileScheduled;

        private void TryOpenUiTestFileAfterShown()
        {
            if (_uiTestOpenFileScheduled)
                return;
            if (!StartupArguments.TryGetUiTestOpenFile(Environment.GetCommandLineArgs(), out string filePath))
                return;
            if (!File.Exists(filePath))
            {
                Trace.WriteLine($"[UiTest] open-file missing: {filePath}");
                return;
            }

            _uiTestOpenFileScheduled = true;
            // Defer so login/schema startup can settle before loading a huge script.
            BeginInvoke(new Action(() =>
            {
                _ = RunUiEventAsync(
                    nameof(OpenSqlFileAsync),
                    () => OpenSqlFileAsync(filePath));
            }));
        }

        private async Task LoadSourceTextCacheFromMenuAsync()
        {
            if (!_connectionSessions.TryGetValue(SelectedConnectionName, out IGeneralDb cn)
                || cn is not INetezza netezza
                || _mvvmDatabaseExplorerControl?.TbFastSchemaSearch is not { } searchBox)
            {
                return;
            }

            searchBox.Enabled = false;
            try
            {
                await netezza.LoadSourceTextCache();
            }
            finally
            {
                if (!searchBox.IsDisposed && !searchBox.Disposing)
                    searchBox.Enabled = true;
            }
        }

        private void OpenPreferencesFromShell()
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
            }
        }

        private void RepaintPreferences()
        {
            RePaintMainWindowX();
            RePaintMainWindowX2();
        }

        private void RefreshSchemaFromShell()
        {
            _ = RunUiEventAsync(nameof(RefreshSchemaFromShell), RefreshSchemaFromShellAsync);
        }

        private async Task RefreshSchemaFromShellAsync()
        {
            string connectionName = _completionContext.SelectedConnectionName;
            if (!string.IsNullOrWhiteSpace(connectionName))
                await RefreshSchemaFullOrNot(connectionName, NetezzaRefreshMode.full, disableInUi: false);
        }



        private VariablesControl _variablesControl;
        public DataGridView DgvVariables
        {
            get
            {
                if (_variablesControl is null)
                {
                    if (this.InvokeRequired)
                    {
                        this.Invoke(() => InitializeVariablesControl());
                    }
                    else
                    {
                        InitializeVariablesControl();
                    }
                }
                return _variablesControl?.DataGridView;
            }
        }

        private void InitializeVariablesControl()
        {
            if (_variablesControl == null)
            {
                _variablesControl = new Controls.VariablesControl(
                    this,
                    _variablesViewModel,
                    () => _sessionVariableRuntimeContext.ActualTabTitleText,
                    _uiHelperService,
                    _colorTheme);

                // Register as persistent DockPanel tool window
                if (_tabManager is DockSuiteTabManager dsm)
                {
                    dsm.RegisterPersistentTool("Variables", _variablesControl, DockState.DockLeft);
                }
                tabPageVariables.Tag = "initialized";
            }
        }

        private void RegularActionTimer_Tick(object sender, EventArgs e) =>
            _ = RunUiEventAsync(nameof(RegularActionTimer_Tick), SaveTabStateFromTimerAsync);

        private async Task SaveTabStateFromTimerAsync()
        {
            _regularActionTimer.Stop();
            try
            {
                await DoSaveTabStateAsync(false);
            }
            finally
            {
                if (!IsDisposed && !Disposing)
                    _regularActionTimer.Start();
            }
            //SaveHistory();
        }

        readonly System.Windows.Forms.Timer _regularActionTimer = new();


        private static bool IsPermanentDiagnosticsTab(TabPage page) =>
            page?.Tag is TabPageResultsTag tag && tag.IsPermanentDiagnostics;





        public MenuStrip MenuStrip1 { get => menuStrip1; }
        public System.ComponentModel.IContainer Components { get => components; }

        private string _selDatabaseError = "";
        public string SelectedDatabase
        {
            get
            {
                if (CurrentUpper is not null && CurrentUpper.SelectedDatabase is null)
                {
                    CurrentUpper.SelectedDatabase = _selDatabaseError;
                }
                return CurrentUpper?.SelectedDatabase ?? _selDatabaseError;
            }
            set
            {
                if (CurrentUpper is not null && CurrentUpper.SelectedDatabase is null)
                {
                    CurrentUpper.SelectedDatabase = _selDatabaseError;
                }
                if (CurrentUpper is not null)
                {
                    CurrentUpper.SelectedDatabase = value;
                }
                else
                {
                    _selDatabaseError = value;
                }
                _completionRuntimeContext.SelectedDatabase = value;
            }
        }

        private string _selConenctionError = "";
        public string SelectedConnectionName
        {
            get
            {
                if (CurrentUpper is not null && CurrentUpper.SelectedConnectionName is null)
                {
                    CurrentUpper.SelectedConnectionName = _selConenctionError;
                }
                return CurrentUpper?.SelectedConnectionName ?? _selConenctionError;
            }
            set
            {
                if (CurrentUpper is not null && CurrentUpper.SelectedConnectionName is null)
                {
                    CurrentUpper.SelectedConnectionName = _selConenctionError;
                    _completionRuntimeContext.SelectedConnectionName = _selConenctionError;
                }
                if (CurrentUpper is not null)
                {
                    CurrentUpper.SelectedConnectionName = value;
                    _completionRuntimeContext.SelectedConnectionName = value;
                }
                else
                {
                    _selConenctionError = value;
                }

                if (_mvvmDatabaseExplorerControl is not null
                    && !string.IsNullOrWhiteSpace(value)
                    && MvvmDatabaseExplorerControl.RequiresConnectionReload(
                        _databaseExplorerViewModel.ConnectionName,
                        _databaseExplorerViewModel.RootNodes.Count,
                        value))
                {
                    _ = _mvvmDatabaseExplorerControl.RefreshAsync(value);
                }
            }
        }

        private void StartsElements()
        {
            this.Enabled = false;

            _snippetInitializationContext.Initialize(JustData.Properties.Resources.snipety, JustData.Properties.Resources.special_names);
            MiscellaneousHelper.SetRegexKeyWords1(_applicationSettingsContext.Config.KeyWordsListForColoring1);
            MiscellaneousHelper.SetRegexKeyWords2(_applicationSettingsContext.Config.KeyWordsListForColoring2);
            InitHistRecent();

            var login = _applicationSession.CurrentLogin ?? throw new InvalidOperationException("A login selection is required before opening the main window.");
            _selDatabaseError = login.Profile.Database;
            _selConenctionError = login.Profile.Name;

            _editorCatalogState.AddConnection(login.Profile.Name);


            _applicationSettingsContext.Config.FastLogin = login.FastLogin;
            Text = "JustyBaseLegacy - " + login.Profile.Name;

            Enabled = true;

            // ── Check if DockSuite layout file exists (authoritative session restore) ──
            bool hasDockLayout = File.Exists(Path.Combine(_applicationSettingsContext.ConfigDirectory, "dockLayout.xml"));

            // When a layout exists, skip the startup-file logic;
            // the layout load block farther down will reopen the previous session.
            if (!hasDockLayout)
            {
                bool encryptedStartupExists = File.Exists(_applicationSettingsContext.ConfigDirectory + @"\simpleStartup.manysql.enc");
                bool plainStartupExists = File.Exists(_applicationSettingsContext.ConfigDirectory + @"\simpleStartup.manysql");
                if (StartupArguments.ShouldRestoreStartupFiles(
                    _applicationSettingsContext.Config.SimpleStartupRestore,
                    encryptedStartupExists,
                    plainStartupExists)
                    && encryptedStartupExists)
                {
                    _ = OpenManySQLhAsync(_applicationSettingsContext.ConfigDirectory + @"\simpleStartup.manysql");
                }
                else if (StartupArguments.ShouldRestoreStartupFiles(
                    _applicationSettingsContext.Config.SimpleStartupRestore,
                    encryptedStartupExists,
                    plainStartupExists)
                    && plainStartupExists)
                {
                    _ = OpenManySQLhAsync(_applicationSettingsContext.ConfigDirectory + @"\simpleStartup.manysql");
                }
                else if (_applicationSettingsContext.Config.StartFilesExtra is null || _applicationSettingsContext.Config.StartFilesExtra.Count == 0)
                {
                    AddMainTab(null);
                }
                else
                {
                    bool added = false;
                    foreach (var item in _applicationSettingsContext.Config.StartFilesExtra)
                    {
                        if (!item.Value)
                        {
                            continue;
                        }
                        string path = item.Key;
                        if (string.IsNullOrWhiteSpace(item.Key) || !File.Exists(path))
                        {
                            continue;
                        }

                        if (path.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
                        {
                            _ = OpenSqlFileAsync(item.Key);
                            added = true;
                        }
                        else if ((path.EndsWith(".manysql", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".manysql.enc", StringComparison.OrdinalIgnoreCase)))
                        {
                            _ = OpenManySQLhAsync(path);
                            added = true;
                        }
                    }
                    if (!added)
                    {
                        AddMainTab(null);
                    }
                }
            }

            string[] args = Environment.GetCommandLineArgs();
            if (args.Length >= 2)
            {
                if (args[1].EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
                {
                    _ = OpenSqlFileAsync(args[1]);
                }
                else if (args[1].EndsWith(".manysql", StringComparison.OrdinalIgnoreCase) ||
                    args[1].EndsWith(".manysql.enc", StringComparison.OrdinalIgnoreCase)
                    )
                {
                    _ = OpenManySQLhAsync(args[1]);
                }
            }

            if (_applicationSettingsContext.Config.MyFastXlsxExportList.Count > 0)
            {
                foreach (var item in _applicationSettingsContext.Config.MyFastXlsxExportList)
                {
                    ToolStripMenuItem temp1 = new ToolStripMenuItem()
                    {
                        BackColor = _colorTheme.MainBack,
                        ForeColor = _colorTheme.MainFore
                    };

                    temp1.Name = item;
                    temp1.Size = new System.Drawing.Size(155, 22);
                    temp1.Text = item;
                    temp1.Click += new System.EventHandler(this.ExportToXlsx_Click);
                }
            }

            // ── Restore DockPanel layout from previous session ──
            if (_tabManager is DockSuiteTabManager dsm)
            {
                string layoutPath = Path.Combine(_applicationSettingsContext.ConfigDirectory, "dockLayout.xml");
                _ = RestoreDockLayoutAsync(dsm, layoutPath);
            }

        }

        private async Task RestoreDockLayoutAsync(
            DockSuiteTabManager dockSuiteTabManager,
            string layoutPath)
        {
            try
            {
                var loadedDocuments = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (string path in dockSuiteTabManager.GetPersistedFilePaths(layoutPath))
                {
                    if (IsDisposed || Disposing)
                        return;

                    if (!File.Exists(path))
                        continue;

                    try
                    {
                        loadedDocuments[path] = await File.ReadAllTextAsync(path);
                    }
                    catch (Exception exception)
                    {
                        Trace.WriteLine($"Could not preload dock document '{path}': {exception.Message}");
                    }
                }

                if (IsDisposed || Disposing)
                    return;

                dockSuiteTabManager.LoadLayout(layoutPath, persistString =>
                {
                    if (persistString.StartsWith("tool:", StringComparison.OrdinalIgnoreCase))
                    {
                        string title = persistString["tool:".Length..];
                        if (title.Equals("Results", StringComparison.OrdinalIgnoreCase))
                            return dockSuiteTabManager.EnsureResultsToolWindow();

                        return dockSuiteTabManager.GetToolWindow(title);
                    }

                    if (string.IsNullOrEmpty(persistString)
                        || persistString.StartsWith("unsaved://", StringComparison.OrdinalIgnoreCase)
                        || !loadedDocuments.TryGetValue(persistString, out string? documentText))
                    {
                        return null;
                    }

                    AddMainTabCore(
                        persistString,
                        title: "",
                        trescSQL: documentText);

                    foreach (TabPage tab in EditorTabPages)
                    {
                        if (tab.Tag is TabPageMainTag mainTag && mainTag.Filename == persistString)
                            return dockSuiteTabManager.GetDockContentForTab(tab);
                    }
                    return null;
                });

                if (EditorTabPages.Count == 0)
                    AddMainTab(null);
            }
            catch (Exception exception)
            {
                Trace.WriteLine($"Asynchronous dock layout restore failed: {exception}");
                if (!IsDisposed && !Disposing && EditorTabPages.Count == 0)
                    AddMainTab(null);
            }
        }

        private FilesControl? _filesControl;
        private readonly FilesViewModel _filesViewModel;
        private GitControl? _gitControl;
        private readonly GitViewModel _gitViewModel;
        private readonly JustyBaseLegacy.UI.Fim.FimEditorHost? _fimEditorHost;
        private readonly JustyBase.Ai.Fim.Download.IFimModelCatalog? _fimModelCatalog;
        private readonly JustyBaseLegacy.UI.Fim.IFimModelBootstrapService? _fimModelBootstrap;

        private void InitializeFilesControl()
        {
            if (_filesControl != null)
                return;

            _filesControl = new FilesControl(_uiHelperService, _colorTheme,
                this, _applicationSettingsContext, this.imageListFiles, _filesViewModel, _fileSearchEngine);
            _filesControl.BorderStyle = BorderStyle.FixedSingle;

            // Register as persistent DockPanel tool window
            if (_tabManager is DockSuiteTabManager dsm)
            {
                dsm.RegisterPersistentTool("Files", _filesControl, DockState.DockLeft);
            }
            tabPageFiles.Tag = "initialized";
        }

        private void InitializeGitControl()
        {
            if (_gitControl != null)
                return;

            _gitControl = new GitControl(_gitViewModel, this, _colorTheme);
            _gitControl.BorderStyle = BorderStyle.FixedSingle;

            if (_tabManager is DockSuiteTabManager dsm)
            {
                dsm.RegisterPersistentTool("Git", _gitControl, DockState.DockLeft);
            }
        }

        private void UpdateGitTimelineForActiveDocument()
        {
            WireGitTimelineToActiveDocument();
            string? path = _editorWorkspaceViewModel.ActiveDocument?.FilePath;
            _gitViewModel.SetActiveFile(path);
        }

        private EditorDocumentViewModel? _gitTimelineDocument;
        private GitDiffDockContent? _gitDiffDockContent;
        private GitDiffControl? _gitDiffControl;

        public void ShowOrUpdateGitDiff(GitFileContents contents)
        {
            ArgumentNullException.ThrowIfNull(contents);

            bool isClear = string.IsNullOrEmpty(contents.RelativePath)
                && string.IsNullOrEmpty(contents.OldText)
                && string.IsNullOrEmpty(contents.NewText);

            if (isClear)
            {
                if (_gitDiffControl is null || _gitDiffDockContent is null || _gitDiffDockContent.IsDisposed)
                    return;

                _gitDiffDockContent.SetTitle($"diff: {contents.Title}");
                _gitDiffControl.LoadDiff(contents.Title, contents.OldText, contents.NewText);
                return;
            }

            string title = $"diff: {contents.Title}";
            if (_gitDiffControl is null || _gitDiffDockContent is null || _gitDiffDockContent.IsDisposed)
            {
                _gitDiffControl = new GitDiffControl();
                _gitDiffDockContent = new GitDiffDockContent(_gitDiffControl, title);
                _gitDiffDockContent.FormClosed += (_, _) =>
                {
                    _gitDiffDockContent = null;
                    _gitDiffControl = null;
                };

                if (_tabManager is DockSuiteTabManager dsm)
                    dsm.ShowGitDiffDocument(_gitDiffDockContent);
                else
                    _gitDiffDockContent.Show();
            }
            else
            {
                _gitDiffDockContent.SetTitle(title);
                if (_tabManager is DockSuiteTabManager dsm)
                    dsm.ShowGitDiffDocument(_gitDiffDockContent);
                else
                    _gitDiffDockContent.Activate();
            }

            _gitDiffControl.LoadDiff(contents.Title, contents.OldText, contents.NewText);
        }

        private void OnEditorWorkspacePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(EditorWorkspaceViewModel.ActiveDocument))
                UpdateGitTimelineForActiveDocument();
        }

        private void WireGitTimelineToActiveDocument()
        {
            var active = _editorWorkspaceViewModel.ActiveDocument;
            if (ReferenceEquals(_gitTimelineDocument, active))
                return;

            if (_gitTimelineDocument is not null)
                _gitTimelineDocument.PropertyChanged -= OnGitTimelineDocumentPropertyChanged;

            _gitTimelineDocument = active;

            if (_gitTimelineDocument is not null)
                _gitTimelineDocument.PropertyChanged += OnGitTimelineDocumentPropertyChanged;
        }

        private void OnGitTimelineDocumentPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(EditorDocumentViewModel.FilePath))
            {
                if (ReferenceEquals(sender, _editorWorkspaceViewModel.ActiveDocument))
                    _gitViewModel.SetActiveFile(_editorWorkspaceViewModel.ActiveDocument?.FilePath);
            }
        }

        // ── DockSuite tool windows (left-side panels as DockContent) ──

        /// <summary>
        /// Registers the left-side panels (Database Explorer, Variables, etc.)
        /// as DockPanel tool windows. Called only when DockSuiteTabManager is active.
        /// Controls are moved from _leftTabs TabPages into ToolDockContent windows
        /// docked to the left side of the DockPanel. _leftTabs TabPages remain
        /// for backward-compatible index-based lookups but become empty.
        /// </summary>
        private void RegisterLeftPanelTools(DockSuiteTabManager dsm)
        {
            // ── Database Explorer ──────────────────────────────────────
            // MVVM Database Explorer — replaces the old Designer-created control
            _mvvmDatabaseExplorerControl ??= new Controls.MvvmDatabaseExplorerControl(_databaseExplorerViewModel);
            _mvvmDatabaseExplorerControl.TreeViewImageList = imageList1;
            _mvvmDatabaseExplorerControl.ContextMenuFactory = CreateMvvmSchemaContextMenu;
            _mvvmDatabaseExplorerControl.AddConnectionRequested += (_, _) => ShowLoginForm();
            _mvvmDatabaseExplorerControl.EditConnectionRequested += (_, _) => ShowLoginForm();
            dsm.RegisterPersistentTool("Database", _mvvmDatabaseExplorerControl, DockState.DockLeft);

            // Ensure the tree is populated even if the control's Load event
            // does not fire in the DockSuite lifecycle.
            _ = _mvvmDatabaseExplorerControl.InitializeAsync();
        }

        private void InitializeDockWindowMenu(DockSuiteTabManager dsm)
        {
            var windowsMenu = new ToolStripMenuItem("Dock windows");
            foreach (string title in new[] { "Database", "Files", "Git", "Variables", "Outline", "Results" })
            {
                var item = new ToolStripMenuItem(title);
                item.Click += (_, _) =>
                {
                    if (title.Equals("Results", StringComparison.OrdinalIgnoreCase))
                    {
                        dsm.EnsureResultsToolWindow().Activate();
                    }
                    else if (dsm.GetToolWindow(title) is { } tool)
                    {
                        dsm.ShowToolWindow(title, tool.Content);
                    }
                };
                windowsMenu.DropDownItems.Add(item);
            }

            optionsToolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());
            optionsToolStripMenuItem.DropDownItems.Add(windowsMenu);
        }









        private void tabControlMain_KeyDown(object sender, KeyEventArgs e) =>
            _ = RunUiEventAsync(nameof(tabControlMain_KeyDown), () => HandleTabControlMainKeyDownAsync(e));

        private async Task HandleTabControlMainKeyDownAsync(KeyEventArgs e)
        {
            if (CurrentTB is null)
                return;
            if (e.KeyCode == Keys.F5 && ModifierKeys == Keys.Control ||
                e.KeyCode == Keys.F6 && ModifierKeys == Keys.Control
                )
            {
                await RunSQL(1);
            }
            else if (e.KeyCode == Keys.Return && ModifierKeys == Keys.Control)
            {
                await RunSQL(0);
            }
            else if (e.KeyCode == Keys.F5)
            {
                await RunSQL(0);
            }
            else if (e.KeyCode == Keys.F7)
            {
                await RunSQL(0, ExportOptions.xlsx);
            }
            else if (e.KeyCode == Keys.F8)
            {
                await RunSQL(0, ExportOptions.csv);
            }
            else if (e.KeyCode == Keys.F10)
            {
                await RunSQL(4); // run to cursor
            }
            //else if (e.KeyCode == Keys.F11)  // "infinity" mode
            //{
            //    RunSQL(3); // "infinity" mode
            //}
            else if (e.KeyCode == Keys.F2 /*&& !CurrentTB.Focused*/)
            {
                if (_preventRenameMainTab)
                {
                    _preventRenameMainTab = false;
                }
                else
                {
                    TryRenameTab();
                }
            }
        }
        bool _preventRenameMainTab = false;

        private void tabControlMain_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                for (int ix = 0; ix < EditorTabPages.Count; ++ix)
                {
                    if (_tabControlMain.GetTabRect(ix).Contains(e.Location))
                    {
                        _tabManager.SelectTab(EditorTabPages[ix]);
                        break;
                    }
                }
            }
        }


        /// <summary>
        /// Creates a document from text already available in memory.
        /// File-backed documents must use <see cref="OpenSqlFileAsync"/> so
        /// disk I/O never runs synchronously on the UI thread.
        /// </summary>
        public FastColoredTextBox AddMainTab(string fileName, string title = "", string trescSQL = "")
        {
            if (!string.IsNullOrWhiteSpace(fileName) && string.IsNullOrEmpty(trescSQL))
            {
                throw new InvalidOperationException(
                    "File-backed SQL documents must be opened with OpenSqlFileAsync.");
            }

            return AddMainTabCore(fileName, title, trescSQL);
        }

        private FastColoredTextBox AddMainTabCore(
            string fileName,
            string title,
            string trescSQL)
        {
            string conName = _completionContext.SelectedConnectionName;
            string driver = _generalDbService.DriverName(conName);

            if (!string.IsNullOrWhiteSpace(fileName)
                && _editorWorkspaceViewModel.FindByPath(fileName) is { } workspaceDocument)
            {
                _editorWorkspaceViewModel.Activate(workspaceDocument.Id);
                if (_documentIdsByTab.FirstOrDefault(item => item.Value == workspaceDocument.Id).Key is { } existingTab)
                {
                    _tabManager.SelectTab(existingTab);
                    return _tabManager.GetEditor(existingTab);
                }
            }

            for (int i = 0; i < EditorTabPages.Count; i++)
            {
                if (EditorTabPages[i].Tag is not null && EditorTabPages[i].Tag is TabPageMainTag tag)
                {
                    if (tag.Filename is not null && tag.Filename == fileName)
                    {
                        _tabManager.SelectTab(EditorTabPages[i]);
                        return _tabManager.GetEditor(EditorTabPages[i]);
                    }
                }
            }

            if (fileName != null)
            {
                if (!File.Exists(fileName))
                {
                    _loggerLoud.MessageBox_Show(this, $"{fileName} does not exist.", "File not found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    if (recentFilesMenu.DropDownItems.ContainsKey(fileName))
                    {
                        recentFilesMenu.DropDownItems.RemoveByKey(fileName);
                    }
                    _recentFileRuntimeContext.RecentFiles.Remove(fileName);

                    return null;
                }
            }
            var sqlUpper = new SQLUpperPanel(
                _generalDbService,
                _connectionSessions,
                _schemaTables,
                _applicationSettingsContext,
                _completionContext,
                value => _completionRuntimeContext.SelectedConnectionName = value,
                _sessionVariableStore,
                () => _sessionVariableRuntimeContext.ActualTabTitleText,
                _loggerLoud,
                this,
                _colorTheme,
                _netezzaSqlCompletionServices,
                _netezzaAutocompleteState,
                _codeActionProvider,
                _editorCatalogState,
                driver,
                SelectedConnectionName,
                SelectedDatabase,
                IsEnabledMode)
            {
                Dock = DockStyle.Fill
            };

            if (trescSQL != "")
            {
                sqlUpper.CurrentTb.Text = trescSQL;
            }

            TabPage tabPageDaneSQLNowe = new TabPagePicture()
            {
                CloseImage = _normalXimage,
                DatabaseTypeName = driver
            };

            if (fileName != null)
            {
                if (!File.Exists(fileName))
                {
                    _loggerLoud.MessageBox_Show(this, $"{fileName} does not exist.", "File not found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return sqlUpper.CurrentTb;
                }
                tabPageDaneSQLNowe.Text = Path.GetFileName(fileName);
                tabPageDaneSQLNowe.Name = fileName;
                AddRecent(fileName);
            }
            else if (title != "")
            {
                tabPageDaneSQLNowe.Text = title;
            }
            else
            {

                Dictionary<string, int> temp1 = new Dictionary<string, int>();
                for (int i = 0; i < EditorTabPages.Count; i++)
                {
                    temp1[EditorTabPages[i].Text] = 0;
                }
                tabPageDaneSQLNowe.Text = _tabNameProvider.GetNextName(new HashSet<string>(EditorTabPages.Select(p => p.Text)));
            }
            string tabName = tabPageDaneSQLNowe.Text;
            _sessionVariableRuntimeContext.EnsureSessionVariables(tabName);

            if (_tabManager is not DockSuiteTabManager)
                this._tabControlMain.Controls.Add(tabPageDaneSQLNowe);
            //tabControlMain.TabPages.Add(tabPageDaneSQLNowe);

            tabPageDaneSQLNowe.ContextMenuStrip = cmMain;
            SplitContainer splitContainerNowy = new SplitContainer();
            splitContainerNowy.BackColor = _colorTheme.MainBack;

            tabPageDaneSQLNowe.Controls.Add(splitContainerNowy);
            splitContainerNowy.Panel1.Controls.Add(sqlUpper);

            splitContainerNowy.Dock = DockStyle.Fill;
            splitContainerNowy.Location = new System.Drawing.Point(3, 3);
            splitContainerNowy.Name = "splitContainerNowy";
            if (/*splitContainerNowy.Height > 0 &&*/ splitContainerNowy.Orientation != System.Windows.Forms.Orientation.Horizontal)
            {
                try
                {
                    splitContainerNowy.Orientation = System.Windows.Forms.Orientation.Horizontal;
                }
                catch (SystemException)
                {
                    //throw;
                }
            }
            //splitContainerNowy.Orientation = Orientation.Horizontal;

            splitContainerNowy.SplitterDistance = (int)Math.Round(splitContainerNowy.Parent.Height * 0.8);
            tabPageDaneSQLNowe.Controls.Add(splitContainerNowy);

            if (fileName != null)
            {
                tabPageDaneSQLNowe.Tag = new TabPageMainTag() { Filename = fileName, IsSaved = true };
            }

            _tabManager.RegisterEditorTab(tabPageDaneSQLNowe, sqlUpper, splitContainerNowy);
            string documentText = trescSQL;

            var editorDocument = _editorWorkspaceViewModel.AddDocumentFromView(
                tabPageDaneSQLNowe.Text,
                documentText,
                fileName,
                sqlUpper.SelectedConnectionName,
                sqlUpper.SelectedDatabase,
                sqlUpper.KeepConnectionOpen,
                sqlUpper.ContinueOnError);
            editorDocument.DiagnosticsChanged += OnDocumentDiagnosticsChanged;
            editorDocument.SqlExecution.EventReceived += _sqlResultPresenter.Handle;
            editorDocument.SqlExecution.EventReceived += PresentProviderExecutionLog;
            _sqlResultPresenter.Attach(editorDocument.SqlExecution);
            _documentIdsByTab[tabPageDaneSQLNowe] = editorDocument.Id;
            _documentIdsByEditor[sqlUpper.CurrentTb] = editorDocument.Id;
            sqlUpper.SetDocumentId(editorDocument.Id);
            if (_tabManager is DockSuiteTabManager dockSuiteTabManager)
                dockSuiteTabManager.SetDocumentId(tabPageDaneSQLNowe, editorDocument.Id);
            EnsureResultsTabControl(splitContainerNowy);

            _tabManager.SelectTab(tabPageDaneSQLNowe);
            // The document VM starts debounced linting from the editor's
            // immediate TextChanged event. Register its presentation target
            // before any result can arrive; waiting for FCTB's delayed event
            // loses fast lint results and leaves the diagnostics grid empty.
            RegisterDiagnosticsTarget(editorDocument.Id, sqlUpper.CurrentTb);

            tabPageDaneSQLNowe.Location = new System.Drawing.Point(4, 24);
            tabPageDaneSQLNowe.Name = "tabDaneSQLNowe";
            tabPageDaneSQLNowe.Padding = new System.Windows.Forms.Padding(3);
            tabPageDaneSQLNowe.Size = new System.Drawing.Size(758, 432);
            tabPageDaneSQLNowe.TabIndex = 0;
            tabPageDaneSQLNowe.UseVisualStyleBackColor = true;


            splitContainerNowy.ForeColor = _colorTheme.MainFore;
            splitContainerNowy.BackColor = _colorTheme.MainBack;
            splitContainerNowy.BorderStyle = BorderStyle.FixedSingle;

            tabPageDaneSQLNowe.ForeColor = _colorTheme.MainFore;
            tabPageDaneSQLNowe.BackColor = _colorTheme.MainBack;
            tabPageDaneSQLNowe.BorderStyle = BorderStyle.FixedSingle;


            sqlUpper.CurrentTb.DelayedTextChangedInterval = _applicationSettingsContext.Config.DelayedTextChangedInterval;
            _applicationSettingsContext.Config.LargeScriptCharThreshold = SqlPerformancePolicy.LargeScriptCharThreshold;
            _applicationSettingsContext.Config.LargeScriptLineThreshold = SqlPerformancePolicy.LargeScriptLineThreshold;
            FastColoredTextBox.LargeScriptSyntaxSkipCharThreshold = SqlPerformancePolicy.LargeScriptCharThreshold;
            FastColoredTextBox.LargeScriptSyntaxSkipLineThreshold = SqlPerformancePolicy.LargeScriptLineThreshold;

            _sessionVariableRuntimeContext.ActualTabTitleText = ActiveEditorTabPage?.Text ?? string.Empty;

            if (!_applicationSettingsContext.Config.DoNotCollapseRegionsOnOpening /*&& !string.IsNullOrWhiteSpace(fileName)*/)
            {
                CollapseAllregion(sqlUpper.CurrentTb);
            }
            return sqlUpper.CurrentTb;
        }

        private void OnEditorDocumentReloaded(EditorDocumentViewModel document)
        {
            var editor = _documentIdsByEditor
                .FirstOrDefault(item => item.Value == document.Id)
                .Key;
            if (editor is null || editor.IsDisposed)
                return;

            editor.Text = document.Text;
            editor.IsChanged = false;

            var tab = _documentIdsByTab
                .FirstOrDefault(item => item.Value == document.Id)
                .Key;
            if (tab?.Tag is TabPageMainTag tag)
                tag.IsSaved = true;
        }

        /// <summary>
        /// Opens a blank SQL editor document.  All UI entry points for creating
        /// a document use this method so they cannot accidentally create a
        /// tab in the per-document Results control.
        /// </summary>
        public void OpenNewSqlDocument() => AddMainTab(null);








        void CloseAllTabs()
        {
            int tabCount = EditorTabPages.Count;
            if (tabCount == 0)
            {
                _loggerLoud.MessageBox_Show(this, "No tabs are open.", "Close tabs", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string message = $"Close {tabCount} tabs ?";
            const string caption = "Closing...";
            var result = _loggerLoud.MessageBox_Show(this, message, caption, MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    List<TabPage> tpX = EditorTabPages.ToList();

                    WindowNativeMethods.SendMessage(this.Handle, WindowConstants.WM_SETREDRAW, 0, 0);
                    foreach (TabPage item in tpX)
                    {
                        if (_tabManager.GetEditorPanel(item) is null)
                        {
                            _tabManager.UnregisterTab(item);
                            continue;
                        }
                        var fastColoredTextBox = _tabManager.GetEditor(item);
                        if (fastColoredTextBox is not null)
                        {
                            fastColoredTextBox.CloseBindingFile();
                        }
                        CloseTabWithConnection(_tabControlMain, item);
                    }
                    WindowNativeMethods.SendMessage(this.Handle, WindowConstants.WM_SETREDRAW, 1, 0);
                    this.Invalidate();

                    ClearCurrentHelpReferences();
                }
                catch (Exception ex)
                {
                    _loggerLoud.MessageBox_Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    //CleanRam();
                }
            }
        }

        void CloseOtherTabs()
        {
            int tabCount = EditorTabPages.Count;
            if (tabCount <= 1)
            {
                _loggerLoud.MessageBox_Show(this, "No tabs to close.", "Close tabs", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string message = $"Close {tabCount - 1} tabs ?";
            const string caption = "Really ?";
            var result = _loggerLoud.MessageBox_Show(this, message, caption, MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    List<TabPage> tpX = new List<TabPage>();
                    foreach (TabPage tabPage in EditorTabPages)
                    {
                        if (ActiveEditorTabPage != tabPage)
                        {
                            tpX.Add(tabPage);
                        }
                    }

                    WindowNativeMethods.SendMessage(this.Handle, WindowConstants.WM_SETREDRAW, 0, 0);
                    foreach (TabPage item in tpX)
                    {
                        if (_tabManager.GetEditorPanel(item) is null)
                        {
                            _tabManager.UnregisterTab(item);
                            continue;
                        }

                        var fastColoredTextBox = _tabManager.GetEditor(item);
                        if (fastColoredTextBox is not null)
                        {
                            fastColoredTextBox.CloseBindingFile();
                        }
                        CloseTabWithConnection(_tabControlMain, item);
                    }
                    WindowNativeMethods.SendMessage(this.Handle, WindowConstants.WM_SETREDRAW, 1, 0);
                    this.Invalidate();

                    ClearCurrentHelpReferences();
                }
                catch (Exception exception)
                {
                    Trace.WriteLine($"Result grid cleanup failed: {exception.GetType().Name}");
                }
                finally
                {
                    //CleanRam();
                }
            }
        }

        private void RemoveAllTabsEventHandler(object sender, EventArgs e)
        {
            CloseAllTabs();
        }

        private void CloseOtherTabsEventHandler(object sender, EventArgs e)
        {
            CloseOtherTabs();
        }

        private void TabContextMenuStrip_Opening(object? sender, CancelEventArgs e)
        {
            string? path = TryGetEditorFilePath(ActiveEditorTabPage);
            bool hasPath = !string.IsNullOrWhiteSpace(path);
            cmsOpenInExplorer.Enabled = hasPath && File.Exists(path);
            cmsCopyFullFilepath.Enabled = hasPath;
        }

        private static string? TryGetEditorFilePath(TabPage? tab)
        {
            if (tab?.Tag is TabPageMainTag tag && !string.IsNullOrWhiteSpace(tag.Filename))
                return tag.Filename;

            if (!string.IsNullOrWhiteSpace(tab?.Text) && File.Exists(tab.Text))
                return tab.Text;

            return null;
        }

        private void OpenInExplorerEvenHandler(object sender, EventArgs e)
        {
            if (TryGetEditorFilePath(ActiveEditorTabPage) is not { } path || !File.Exists(path))
                return;

            Process.Start("explorer.exe", $"/select, {path}");
        }

        private void CopyFullFilepathEventHandler(object sender, EventArgs e)
        {
            if (TryGetEditorFilePath(ActiveEditorTabPage) is { } path)
                Clipboard.SetText(path);
        }


        private void TryRenameTab()
        {
            if (ActiveEditorTabPage is not null)
            {
                if (ActiveEditorTabPage.Tag != null)
                {
                    _loggerLoud.MessageBox_Show(this, "Rename is not allowed.", "Rename", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    var tp = ActiveEditorTabPage;
                    string prevName = tp.Text;
                    var r = new Rename(tp, o => _colorTheme.ColorForm(o));
                    //r.ClientSize = new Size(200, 100);
                    r.StartPosition = FormStartPosition.CenterParent;
                    r.ShowDialog();

                    VariablesAfterChangeTabName(prevName, tp.Text);
                    if (_tabManager is DockSuiteTabManager dsm)
                        dsm.UpdateEditorTab(tp);
                }
            }
        }
        private void RenameTabEvenHandler(object sender, EventArgs e)
        {
            TryRenameTab();
        }

        void VariablesAfterChangeTabName(string prevTabName, string NewTabName)
        {
            _sessionVariableRuntimeContext.CopySessionVariables(prevTabName, NewTabName);
        }

        private void RenameResultTabEventHandler(object sender, EventArgs e)
        {
            if (CurrentSplitContainer?.Tag is ResultData resultData)
            {
                TabControl tc = resultData.TabControlSQLResults;
                if (tc.SelectedTab is null) return;
                var r = new Rename(tc.SelectedTab, o => _colorTheme.ColorForm(o))
                {
                    StartPosition = FormStartPosition.Manual
                };

                var p = MousePosition;
                p.Offset(-50, -50);
                r.Location = p;
                r.ShowDialog();
            }
        }












        private void Form1_KeyDown(object sender, KeyEventArgs e) =>
            _ = RunUiEventAsync(nameof(Form1_KeyDown), () => HandleForm1KeyDownAsync(sender, e));

        private async Task HandleForm1KeyDownAsync(object sender, KeyEventArgs e)
        {
            if ((e.KeyCode == Keys.N || e.KeyCode == Keys.T) && ModifierKeys == Keys.Control)
            {
                OpenNewSqlDocument();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.W && ModifierKeys == Keys.Control)
            {
                if (ActiveEditorTabPage is TabPage activeTab)
                    await DoClosingOfTabAsync(_tabControlMain, activeTab);
            }
            else if (e.KeyCode == Keys.S && ModifierKeys == Keys.Control)
            {
                if (ActiveEditorTabPage is TabPage activeTab)
                    await SaveAsync(activeTab);
            }
            else if (e.KeyCode == Keys.O && ModifierKeys == Keys.Control)
            {
                await OpenAsync();
            }
            else if (e.KeyCode == Keys.H && ModifierKeys == Keys.Alt)
            {
                HistoryToolStripMenuItem_Click(sender, e);
            }
            else if (e.KeyCode == Keys.F11)
            {
                NextVsiaulMode();
            }
        }


        private void TsmAbout_Click(object sender, EventArgs e)
        {
            OtherUtils.AboutMessage(this);
        }

        void AddVariable(string tabName, string name, string value)
        {
            if (name is not null)
            {
                _sessionVariableRuntimeContext.SetSessionVariable(tabName, name, value);
            }

            DgvVariables.RowCount = _sessionVariableRuntimeContext.GetSessionVariableCount(tabName)
                + _sessionVariableRuntimeContext.GlobalVariables.Count;
            DgvVariables.Invalidate();
        }

        private void VariablesRefresh()
        {
                string tabName = ActiveEditorTabPage?.Text;
            if (string.IsNullOrWhiteSpace(tabName))
            {
                return;
            }

            DgvVariables.RowCount = 0;
            if (_sessionVariableRuntimeContext.HasSessionVariables(tabName))
            {
                DgvVariables.RowCount = _sessionVariableRuntimeContext.GetSessionVariableCount(tabName)
                    + _sessionVariableRuntimeContext.GlobalVariables.Count;
                DgvVariables.Invalidate();
            }
        }

        private void AddViewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                var dial = new DbForms.ProvideName(o => _colorTheme.ColorForm(o));
                if (dial.ShowDialog() == DialogResult.OK)
                {
                    string name = dial.ProductName;

                    var dial2 = new DbForms.ProvideCode();
                    if (dial2.ShowDialog() == DialogResult.OK)
                    {
                        AddMainTab(null, $"view - {name}",
                            $"CREATE VIEW {name} AS \r\n" +
                            dial2.GetCode + "\r\n;");
                    }
                }
            }
            catch (Exception ex)
            {
                _loggerLoud.MessageBox_Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }


        private void TsmiWordWrap_Click(object sender, EventArgs e)
        {
            if (CurrentTB is not null)
            {
                CurrentTB.WordWrap = !CurrentTB.WordWrap;
                CurrentTB.WordWrapAutoIndent = !CurrentTB.WordWrapAutoIndent;
            }
        }


        [GeneratedRegex("(?<base>\\w+)\\.\\\"?(?<owner>(\\w|\\.)+)?\\\"?\\.(?<table>\\w+)", RegexOptions.Compiled)]
        private static partial Regex RegexBaseTableNZ();


        //[GeneratedRegex("\\b(create\\s+temp\\s+table|create\\s+table)\\s+(?<aliasWithaTabeli>(\\w|\\.)+?)\\b\\s*as\\b\\s*\\({0,1}", RegexOptions.IgnoreCase | RegexOptions.Compiled, "pl-PL")]
        //private static partial Regex RegexTable2();

        private void QueryWatchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int databaseType = (int)_generalDbService.RelatedDatabaseType;
            if (!_queryWatchService.IsSupported(databaseType))
            {
                OtherUtils.OnlyNzMesage(this);
                return;
            }

            QueryWatchContext ContextFactory() => new(
                SelectedConnectionName,
                SelectedDatabase,
                (int)_generalDbService.RelatedDatabaseType);

            if (_tabManager is DockSuiteTabManager dockSuite)
            {
                dockSuite.ShowQueryWatch(
                    _queryWatchService,
                    ContextFactory,
                    o => _colorTheme.ColorForm(o),
                    f => _uiHelperService.DoubleBufDateGridView(f),
                    _loggerLoud);
                return;
            }

            if (_queryWatch is null || _queryWatch.IsDisposed)
            {
                var viewModel = new QueryWatchViewModel(
                    _queryWatchService,
                    ContextFactory,
                    _uiDispatcher);
                _queryWatch = new QueryWatch(
                    viewModel,
                    o => _colorTheme.ColorForm(o),
                    f => _uiHelperService.DoubleBufDateGridView(f),
                    _loggerLoud);
            }

            _queryWatch.Show();
            _queryWatch.Focus();
            _ = _queryWatch.RefreshNowAsync();
        }


        private void ThemeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _applicationSettingsContext.Config.UseSpecialColoring = !_applicationSettingsContext.Config.UseSpecialColoring;
            ApplyApplicationColorMode();
            try
            {
                if (_tabManager is DockSuiteTabManager dsm)
                    dsm.ApplyTheme(_applicationSettingsContext.Config.UseSpecialColoring);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Theme switch failed: {ex}");
                // Revert the config flag so the UI reflects the actual theme.
                _applicationSettingsContext.Config.UseSpecialColoring = !_applicationSettingsContext.Config.UseSpecialColoring;
                ApplyApplicationColorMode();
                MessageBox.Show(
                    this,
                    $"Nie udało się zmienić motywu. Szczegóły:\n{ex.Message}",
                    "Błąd zmiany motywu",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            RePaintMainWindowX();
            RePaintMainWindowX2();
        }

        private void ApplyApplicationColorMode()
        {
            Application.SetColorMode(
                _applicationSettingsContext.Config.UseSpecialColoring
                    ? SystemColorMode.Dark
                    : SystemColorMode.Classic);
        }

        [GeneratedRegex(@"[a-zA-Z]")]
        private static partial Regex ContainsAZRegex();

        private EditorDocumentId? CurrentEditorDocumentId
        {
            get => ActiveEditorTabPage is { } activeTab
                && _documentIdsByTab.TryGetValue(activeTab, out var documentId)
                    ? documentId
                    : null;
        }

        public void runToCursorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _ = RunUiEventAsync(nameof(runToCursorToolStripMenuItem_Click), () => RunSQL(4));
        }

        public void WireEditorEvents(FastColoredTextBox editor, bool isNetezza)
        {
            editor.TextChanged += FctbTextChanged;
            editor.TextChanging += FctbTextChanging;
            editor.TextChangedDelayed += FctbTextChangedDelayed;
            editor.SelectionChangedDelayed += FctbSelectionChangedDelayed;
            editor.DragDrop += tabControlMain_DragDrop;
            editor.DragOver += tabControlMainDragOver;
            editor.ToolTipNeeded += FctbToolTipNeeded;
            editor.KeyDown += FastColoredNew_KeyDown;
            editor.KeyUp += FctbAuthoringKeyUp;
            editor.Pasting += FctbNew_Pasting;
            editor.MouseClick += FctbNew_MouseClick;
            editor.LightbulbClick += OnEditorLightbulbClick;
            _fimEditorHost?.Attach(editor);

            if (isNetezza)
            {
                var authoringMenu = new ContextMenuStrip();
                authoringMenu.Items.Add("Go to Definition\tF12", null, (_, _) => FctbGoToDefinition(editor));
                authoringMenu.Items.Add("Find References\tShift+F12", null, (_, _) => FctbShowReferences(editor));
                authoringMenu.Items.Add("Rename Symbol\tF2", null, (_, _) => FctbRenameSymbol(editor));
                editor.ContextMenuStrip = authoringMenu;
            }
        }

        /// <summary>
        /// Handles clicks on lightbulb markers in the FCTB gutter.
        /// Shows a polished context menu with icons, grouped sections, and hover tooltips.
        /// </summary>
        private void OnEditorLightbulbClick(object sender, FastColoredTextBoxNS.LightbulbClickEventArgs e)
        {
            var editor = sender as FastColoredTextBox;
            if (editor is null || editor.IsDisposed)
                return;

            var marker = e.Marker;
            if (marker is null || marker.Actions.Count == 0)
                return;

            // Collect actions by kind, preserving order.
            var quickFixes = new List<CodeAction>();
            var otherActions = new List<CodeAction>();
            string? firstTooltip = null;

            foreach (var obj in marker.Actions)
            {
                if (obj is not CodeAction action)
                    continue;

                firstTooltip ??= action.TooltipMessage;

                switch (action.Kind)
                {
                    case CodeActionKind.QuickFix:
                        quickFixes.Add(action);
                        break;
                    default:
                        otherActions.Add(action);
                        break;
                }
            }

            if (quickFixes.Count == 0 && otherActions.Count == 0)
                return;

            // Build the context menu.
            // NOT wrapped in 'using' because Show() is non-blocking;
            // the menu stays alive via the Closed event disposal.
            var menu = new ContextMenuStrip();
            menu.Closed += (_, _) => menu.Dispose();
            menu.ShowImageMargin = true;
            menu.ShowCheckMargin = false;

            // ─── Header item: shows line info + first issue message as tooltip ───
            if (firstTooltip is not null)
            {
                var header = new ToolStripMenuItem(
                    $"Line {marker.iLine + 1}: {marker.Actions.Count} issue(s)")
                {
                    Enabled = false,           // greyed out header
                    Font = new Font(menu.Font, FontStyle.Bold),
                    ToolTipText = firstTooltip
                };
                menu.Items.Add(header);
                menu.Items.Add(new ToolStripSeparator());
            }

            // ─── Quick Fixes section ───
            foreach (var action in quickFixes)
            {
                menu.Items.Add(BuildActionItem(action, editor));
            }

            // ─── Other actions section ───
            if (otherActions.Count > 0)
            {
                if (quickFixes.Count > 0)
                    menu.Items.Add(new ToolStripSeparator());

                foreach (var action in otherActions)
                {
                    menu.Items.Add(BuildActionItem(action, editor));
                }
            }

            menu.Show(editor, e.MouseArgs.Location);
        }

        /// <summary>
        /// Builds a single ToolStripMenuItem for the given code action,
        /// complete with icon, tooltip, and click handler.
        /// </summary>
        private ToolStripMenuItem BuildActionItem(CodeAction action, FastColoredTextBox editor)
        {
            var item = new ToolStripMenuItem(action.Description);

            // ── Icon ──
            item.Image = action.Kind switch
            {
                CodeActionKind.QuickFix => JustData.Properties.Resources.wrench,
                CodeActionKind.DisableRule => JustData.Properties.Resources.cross,
                CodeActionKind.FormatDocument => JustData.Properties.Resources.script_lightning,
                _ => null
            };

            // ── Tooltip (hover preview) ──
            if (action.TooltipMessage is not null)
            {
                var severityLabel = action.SeverityLabel;
                if (severityLabel is not null)
                    item.ToolTipText = $"[{severityLabel}] {action.TooltipMessage}";
                else
                    item.ToolTipText = action.TooltipMessage;
            }

            // ── Click handler ──
            var capturedAction = action;
            item.Click += (_, _) =>
            {
                try
                {
                    string sql = editor.Text;
                    string newSql = capturedAction.Apply(sql);

                    // Show preview dialog before applying, unless it's DisableRule (handled separately).
                    if (newSql != sql && capturedAction.Kind != CodeActionKind.DisableRule)
                    {
                        if (!ShowFixPreview(capturedAction, sql, newSql))
                            return; // user cancelled
                    }

                    if (newSql != sql)
                    {
                        editor.Text = newSql;
                    }

                    // For DisableRule kind, also call the infrastructure method.
                    if (capturedAction.Kind == CodeActionKind.DisableRule
                        && !string.IsNullOrWhiteSpace(capturedAction.RuleId))
                    {
                        DisableLintRule(capturedAction.RuleId);
                    }
                }
                catch (Exception ex)
                {
                    _loggerLoud?.MessageBox_Show(this,
                        $"Failed to apply fix: {ex.Message}",
                        "Code Action Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            };

            return item;
        }

        /// <summary>
        /// Shows a TaskDialog previewing the SQL change before applying a code action fix.
        /// Returns true if the user wants to apply the fix, false to cancel.
        /// </summary>
        private bool ShowFixPreview(CodeAction action, string oldSql, string newSql)
        {
            const int maxPreviewLines = 25;
            var diffText = BuildDiffPreview(oldSql, newSql, maxPreviewLines);

            var applyButton = new TaskDialogButton("Apply");
            var page = new TaskDialogPage
            {
                Heading = action.Description,
                Text = diffText,
                Icon = TaskDialogIcon.Information,
                Caption = "Code Action Preview",
                Buttons = { applyButton, TaskDialogButton.Cancel },
                DefaultButton = TaskDialogButton.Cancel,
                Footnote = new TaskDialogFootnote
                {
                    Text = "Before → After"
                }
            };

            var result = TaskDialog.ShowDialog(this, page);
            return result == applyButton;
        }

        /// <summary>
        /// Builds a compact before/after diff preview limited to a maximum number of lines.
        /// </summary>
        private static string BuildDiffPreview(string oldSql, string newSql, int maxLines)
        {
            var sb = new StringBuilder();

            sb.AppendLine("--- BEFORE ---");
            AppendTruncated(sb, oldSql, maxLines);
            sb.AppendLine();
            sb.AppendLine("--- AFTER ---");
            AppendTruncated(sb, newSql, maxLines);

            return sb.ToString();
        }

        private static void AppendTruncated(StringBuilder sb, string sql, int maxLines)
        {
            var lines = sql.Split('\n');
            int count = Math.Min(lines.Length, maxLines);
            for (int i = 0; i < count; i++)
            {
                sb.AppendLine(lines[i].TrimEnd('\r'));
            }
            if (lines.Length > maxLines)
            {
                sb.AppendLine($"... ({lines.Length - maxLines} more lines)");
            }
        }

    }
}
