using AppBase.Common;
using AppBase.Common.Interfaces;
using AppBase.Data.Core.Models;
using DatabaseDataGridView.WinForms;
using DatabaseDataGridView.WinForms.Coloring;
using FastColoredTextBoxNS;
using JustData.Application.Editor;
using JustData.Application.History;
using JustData.Application;
using JustData.Application.Startup;
using JustData.Application.QueryWatch;
using JustData.ViewModels.QueryWatch;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using WeifenLuo.WinFormsUI.Docking;

using JustyBaseLegacy.UI.Forms;

namespace JustyBaseLegacy.UI;

/// <summary>
/// ITabManager implementation that owns a WeifenLuo DockPanel.
/// Editor tabs are created as <see cref="EditorDockContent"/> documents
/// inside the DockPanel instead of TabPage+TabControl.
///
/// An inner <see cref="TabManagerService"/> remains as an accepted ADR-004
/// compatibility shim for TabPage-based lookups. Collapsing it is deferred to
/// a later shell cleanup wave — it is not a second editor host.
/// </summary>
internal sealed class DockSuiteTabManager : ITabManager, IDisposable
{
    private readonly IUiDispatcher _uiDispatcher;
    private readonly TabManagerService _inner = new();
    // DockPanelSuite controls retain palette brushes while they repaint. Keep
    // both themes alive for the lifetime of the panel instead of replacing
    // them with short-lived instances during a live theme switch.
    private readonly VS2015LightTheme _lightTheme = new();
    private readonly VS2015DarkTheme _darkTheme = new();
    private DockPanel _dockPanel = CreateDockPanel();

    private static DockPanel CreateDockPanel() => new()
    {
        Dock = DockStyle.Fill,
        // DockingWindow keeps the document tab strip visible even with a single tab.
        // DockingSdi hides the strip when only one document is open.
        DocumentStyle = DocumentStyle.DockingWindow
    };

    // ── DockSuite-specific maps ───────────────────────────────
    private readonly Dictionary<TabPage, EditorDockContent> _tabToDockContent = new();
    private readonly Dictionary<TabPage, EditorDocumentId> _documentIdsByTab = new();
    private readonly Dictionary<EditorDocumentId, TabPage> _tabsByDocumentId = new();
    private bool _isLoadingLayout;
    private readonly Dictionary<EditorDockContent, TabPage> _dockContentToTab = new();
    private EditorDockContent? _lastActiveEditorContent;
    private readonly Dictionary<string, ToolDockContent> _toolWindows = new(StringComparer.Ordinal);
    private PreferencesDockContent? _preferencesContent;
    private HistoryDockContent? _historyContent;
    private QueryWatchDockContent? _queryWatchContent;
    private readonly Dictionary<TabPage, TabControl> _perTabResults = new();
    private TabControl? _mainTabControl;
    private ContextMenuStrip? _documentTabContextMenuStrip;

    public DockSuiteTabManager(IUiDispatcher uiDispatcher)
    {
        _uiDispatcher = uiDispatcher ?? throw new ArgumentNullException(nameof(uiDispatcher));
    }

    /// <summary>The WeifenLuo DockPanel hosted in the main form.</summary>
    public DockPanel DockPanel => _dockPanel;

    /// <summary>Returns true when the DockPanel is the active container (Phase 2+).</summary>
    public bool IsActive => _dockPanel.Visible;

    // ── ITabManager ───────────────────────────────────────────

    public void Initialize(TabControl mainTabControl)
    {
        _inner.Initialize(mainTabControl);
        _mainTabControl = mainTabControl;

        if (_dockPanel.Parent is null && mainTabControl?.Parent is Control parent)
        {
            _dockPanel.Name = "dockPanel";
            _dockPanel.Visible = true;          // visible in Phase 2
            mainTabControl.Visible = false;     // hidden — stays for data access
            parent.Controls.Add(_dockPanel);
            parent.Controls.SetChildIndex(_dockPanel, 0);
        }

        // ── Sync _tabControlMain.SelectedTab with DockPanel active document ──
        // When the user clicks a different editor tab in the DockPanel, the
        // hidden _tabControlMain.SelectedTab becomes stale. This handler keeps
        // them in sync so that keyboard shortcuts (Ctrl+W, Ctrl+S, etc.)
        // that read _tabControlMain.SelectedTab directly work correctly.
        //
        // The handler is stored as a field so it can be detached from a
        // retired DockPanel and reattached to a freshly created one during a
        // theme switch (see ApplyTheme).
        _dockPanel.ActiveDocumentChanged += OnActiveDocumentChanged;
    }

    /// <summary>
    /// Assigns the shared context menu shown when right-clicking a SQL document tab.
    /// </summary>
    public void SetDocumentTabContextMenu(ContextMenuStrip? menu)
    {
        _documentTabContextMenuStrip = menu;
        foreach (var dockContent in _tabToDockContent.Values)
            dockContent.TabPageContextMenuStrip = menu;
    }

    private void OnActiveDocumentChanged(object? sender, EventArgs e)
    {
        if (_mainTabControl is null || _mainTabControl.IsDisposed)
            return;

        var active = _dockPanel.ActiveDocument;
        if (active is EditorDockContent editorContent
            && _dockContentToTab.TryGetValue(editorContent, out var tabPage))
        {
            _lastActiveEditorContent = editorContent;
            // Editor TabPages are detached compatibility handles in
            // DockSuite mode. Do not require them to belong to the hidden
            // legacy TabControl before switching their results surface.
            if (_mainTabControl.TabPages.Contains(tabPage))
                _mainTabControl.SelectedTab = tabPage;

            SwapResultsForTab(tabPage);
            if (_documentIdsByTab.TryGetValue(tabPage, out var documentId))
                ActiveDocumentChanged?.Invoke(documentId);

            DocumentOrderChanged?.Invoke(GetEditorDocumentOrder());
        }
    }

    public void RegisterEditorTab(TabPage tabPage, IEditorPanel editorPanel, SplitContainer splitter)
    {
        // Always keep the inner service in sync for backward-compatible lookups.
        _inner.RegisterEditorTab(tabPage, editorPanel, splitter);

        // Create the per-document results strip before the document is shown.
        // Diagnostics and result tabs are configured after registration completes.
        GetOrCreateResultsTabControl(tabPage);

        // Create DockContent.
        string title = tabPage.Text;
        var dockContent = new EditorDockContent(splitter, editorPanel, title);

        // Set the file path so GetPersistString() returns the correct value.
        if (tabPage.Tag is TabPageMainTag tag && tag.Filename is not null)
            dockContent.FilePath = tag.Filename;

        _tabToDockContent[tabPage] = dockContent;
        _dockContentToTab[dockContent] = tabPage;
        _lastActiveEditorContent = dockContent;

        // Carry the TabPage's context menu to the DockContent so
        // right-click works inside the editor document window.
        if (tabPage.ContextMenuStrip is not null)
            dockContent.ContextMenuStrip = tabPage.ContextMenuStrip;

        if (_documentTabContextMenuStrip is not null)
            dockContent.TabPageContextMenuStrip = _documentTabContextMenuStrip;

        // Redirect DockContent close button to the existing close logic
        // so save-confirm and cleanup are handled consistently.
        dockContent.FormClosing += (_, e) =>
        {
            if (_dockContentToTab.TryGetValue(dockContent, out var innerTabPage)
                && e.CloseReason != CloseReason.MdiFormClosing)
            {
                e.Cancel = true;
                TabCloseRequested?.Invoke(innerTabPage);
            }
        };

        // When loading from a persisted layout, LoadFromXml handles positioning.
        // Only call Show() for new (interactive) tabs.
        if (!_isLoadingLayout)
        {
            ShowInSqlDocumentPane(dockContent);

            // Keep Results below the SQL document pane, leaving the left-side
            // Database tool at full height.
            DockResultsBelowDocuments();
        }
    }

    public void UnregisterTab(TabPage tabPage)
    {
        _inner.UnregisterTab(tabPage);

        // The splitter is reparented into EditorDockContent, so the legacy
        // TabPage traversal cannot dispose the per-document results control.
        if (_perTabResults.Remove(tabPage, out var resultsTabControl))
        {
            if (_resultsWindow?.TabControl == resultsTabControl)
                _resultsWindow.ResetTabControl();
            resultsTabControl.Dispose();
        }

        // Remove from maps BEFORE Close() to prevent the FormClosing handler
        // from re-entering through TabCloseRequested → DoClosingOfTab → UnregisterTab.
        if (_tabToDockContent.TryGetValue(tabPage, out var dockContent))
        {
            _tabToDockContent.Remove(tabPage);
            _dockContentToTab.Remove(dockContent);
            if (ReferenceEquals(_lastActiveEditorContent, dockContent))
                _lastActiveEditorContent = _dockContentToTab.Keys.LastOrDefault();
            dockContent.Close();   // FormClosing handler sees TryGetValue=false → no-op
        }

        if (_documentIdsByTab.Remove(tabPage, out var documentId))
            _tabsByDocumentId.Remove(documentId);
    }

    /// <summary>Associates a DockSuite document with its clean-layer identity.</summary>
    /// <summary>
    /// Projects the workspace document id onto DockSuite tab maps.
    /// Source of truth for get-or-create remains
    /// <c>EditorWorkspaceViewModel</c> + BaseWindow <c>_documentIdsByEditor</c>;
    /// this map is only for DockPane ↔ tab lookups.
    /// </summary>
    public void SetDocumentId(TabPage tabPage, EditorDocumentId documentId)
    {
        if (!_tabToDockContent.TryGetValue(tabPage, out var dockContent))
            return;

        if (_documentIdsByTab.Remove(tabPage, out var previous))
            _tabsByDocumentId.Remove(previous);

        _documentIdsByTab[tabPage] = documentId;
        _tabsByDocumentId[documentId] = tabPage;
        dockContent.DocumentId = documentId;
    }

    /// <summary>
    /// Returns editor IDs in the order currently displayed by DockSuite.
    /// The workspace collection tracks creation/lifecycle order, while the
    /// DockPane content collections track user tab reordering.
    /// </summary>
    public IReadOnlyList<EditorDocumentId> GetEditorDocumentOrder()
    {
        var order = new List<EditorDocumentId>();
        foreach (var pane in _dockPanel.Panes.Where(pane => pane.DockState == DockState.Document))
        {
            foreach (var content in pane.Contents.OfType<EditorDockContent>())
            {
                if (content.DocumentId is { } documentId && !order.Contains(documentId))
                    order.Add(documentId);
            }
        }

        return order;
    }

    /// <summary>
    /// Returns compatibility handles for editor tabs in DockSuite order.
    /// The handles are detached from the legacy hidden TabControl.
    /// </summary>
    public IReadOnlyList<TabPage> GetEditorTabPages()
    {
        var pages = new List<TabPage>();
        foreach (var pane in _dockPanel.Panes.Where(pane => pane.DockState == DockState.Document))
        {
            foreach (var content in pane.Contents.OfType<EditorDockContent>())
            {
                if (_dockContentToTab.TryGetValue(content, out var tabPage))
                    pages.Add(tabPage);
            }
        }

        return pages;
    }

    /// <summary>Returns the compatibility handle for DockSuite's active editor.</summary>
    public TabPage? GetActiveEditorTabPage()
    {
        EditorDockContent? editorContent = ResolveCurrentEditorContent();
        return editorContent is not null
            && _dockContentToTab.TryGetValue(editorContent, out var tabPage)
            ? tabPage
            : null;
    }

    public FastColoredTextBox? CurrentEditor =>
        ResolveCurrentEditorContent()?.Fctb;

    public IEditorPanel? CurrentEditorPanel =>
        ResolveCurrentEditorContent()?.EditorPanel;

    public SplitContainer? CurrentSplitContainer =>
        ResolveCurrentEditorContent()?.SplitContainer;

    public SplitContainer? GetSplitContainerForTab(TabPage tabPage)
    {
        if (tabPage is not null && _inner.GetSplitContainerForTab(tabPage) is { } splitter)
            return splitter;
        return null;
    }

    public IEditorPanel? GetEditorPanel(TabPage tabPage)
    {
        if (tabPage is not null && _inner.GetEditorPanel(tabPage) is { } panel)
            return panel;
        return null;
    }

    public FastColoredTextBox? GetEditor(TabPage tabPage)
    {
        return GetEditorPanel(tabPage)?.CurrentTb;
    }

    /// <summary>
    /// Switches the DockPanelSuite theme by rebuilding the <see cref="DockPanel"/>
    /// rather than reassigning <c>DockPanel.Theme</c> on a live panel.
    /// </summary>
    /// <remarks>
    /// DockPanelSuite 3.1.1 throws <c>Before applying themes all panes must be closed</c>
    /// from <c>ThemeBase.ApplyTo</c> whenever <c>DockPanel.Panes.Count > 0</c>. The former
    /// implementation tried to satisfy this by calling <see cref="DockContent.Hide()"/> on
    /// every content, but <c>Hide()</c> leaves <c>DockPane</c> instances registered with the
    /// panel until asynchronous cleanup runs, and synchronous handlers
    /// (<see cref="DockPanel.ActiveDocumentChanged"/>, persisted BeginInvoke callbacks) can
    /// re-<c>Show</c> contents during the same message — leaving <c>Panes.Count > 0</c> at
    /// the moment the theme is reassigned and crashing the application.
    ///
    /// Instead, a fresh <see cref="DockPanel"/> is constructed with the target theme already
    /// applied <em>before</em> any content is shown, sidestepping the precondition entirely.
    /// The existing <see cref="EditorDockContent"/>/<see cref="ToolDockContent"/>/
    /// <see cref="ResultsDockContent"/>/<see cref="PreferencesDockContent"/>/
    /// <see cref="HistoryDockContent"/> instances (and the user controls they host) are
    /// re-shown on the new panel, preserving tab order, dock states, and editor state. The
    /// retired panel is disposed last, after every content has been reparented.
    /// </remarks>
    public void ApplyTheme(bool dark)
    {
        // Nothing to do when the panel is not yet sited and holds no content:
        // this is the startup path (BaseWindow calls ApplyTheme before any tab is
        // registered). Just set the theme on the still-empty panel.
        if (_dockPanel.Contents.Count == 0 && _dockPanel.Parent is null)
        {
            _dockPanel.Theme = dark ? _darkTheme : _lightTheme;
            return;
        }

        var oldPanel = _dockPanel;
        var parent = oldPanel.Parent;

        // Snapshot editor documents in their current visual order (tab order
        // is owned by the DockPane content collection, which is lost once we
        // dispose the panel). EditorDockContent that is hidden/disposed is
        // dropped — it cannot be restored.
        var editorSequence = oldPanel.Panes
            .Where(pane => pane.DockState == DockState.Document)
            .SelectMany(pane => pane.Contents.OfType<EditorDockContent>())
            .Where(content => !content.IsDisposed)
            .ToArray();

        // Snapshot tool window dock states (Hide()/re-show resets Hidden →
        // Unknown, so we capture the user-visible state here).
        var toolStates = _toolWindows
            .Where(kv => !kv.Value.IsDisposed)
            .ToDictionary(kv => kv.Key, kv => kv.Value.DockState);

        var resultsContent = _resultsWindow is not null && !_resultsWindow.IsDisposed
            ? _resultsWindow
            : null;

        var preferencesContent = _preferencesContent is not null && !_preferencesContent.IsDisposed
            ? _preferencesContent
            : null;

        var historyContent = _historyContent is not null && !_historyContent.IsDisposed
            ? _historyContent
            : null;

        // Detach the ActiveDocumentChanged handler from the retiring panel so
        // its events don't reach our state during teardown.
        oldPanel.ActiveDocumentChanged -= OnActiveDocumentChanged;

        // Remove the old panel from its parent before doing reparent work so the
        // user does not see a half-finished transition on the wrong surface.
        if (parent is not null)
            parent.Controls.Remove(oldPanel);

        try
        {
            var newPanel = CreateDockPanel();
            newPanel.SuspendLayout();
            // Apply the target theme on the empty panel — no panes exist yet, so
            // ThemeBase.ApplyTo cannot throw its "all panes must be closed" guard.
            newPanel.Theme = dark ? _darkTheme : _lightTheme;

            // Swap the field so OnActiveDocumentChanged and every other consumer
            // (ShowToolWindow, ShowInSqlDocumentPane, QueueResultsDocking, …)
            // targets the new panel for the remainder of this method.
            _dockPanel = newPanel;
            newPanel.ActiveDocumentChanged += OnActiveDocumentChanged;

            if (parent is not null)
            {
                parent.Controls.Add(newPanel);
                parent.Controls.SetChildIndex(newPanel, 0);
                newPanel.Visible = true;
            }

            // Re-show persisted tool windows (Database, Files, Variables, …) in
            // their prior dock states. DockContent.Show(dockPanel, state) reparents
            // the form onto the supplied DockPanel — the wrapped user control
            // (MvvmDatabaseExplorerControl etc.) is untouched.
            foreach (var kv in _toolWindows)
            {
                if (kv.Value.IsDisposed)
                    continue;
                DockState state = toolStates.TryGetValue(kv.Key, out var s) && s != DockState.Unknown
                    ? s
                    : DockState.DockLeft;
                kv.Value.Show(newPanel, state);
            }

            // Re-show editor documents in the captured visual order.
            // Showing each on the new DockPanel assembles them into a single
            // document tab strip automatically.
            foreach (var editor in editorSequence)
            {
                editor.Show(newPanel, DockState.Document);
            }

            // Re-show Preferences/History as document tabs (they were Documents
            // before and must remain so after the switch).
            if (preferencesContent is not null)
                preferencesContent.Show(newPanel, DockState.Document);
            if (historyContent is not null)
                historyContent.Show(newPanel, DockState.Document);

            // Re-show Results nested below the SQL document pane, matching the
            // pre-switch layout.
            if (resultsContent is not null)
            {
                var documentPane = newPanel.Panes
                    .FirstOrDefault(pane => pane.DockState == DockState.Document);
                if (documentPane is not null)
                    resultsContent.Show(documentPane, DockAlignment.Bottom, 0.25);
                else
                    resultsContent.Show(newPanel, DockState.Document);
            }

            newPanel.ResumeLayout(true);

            // Re-nest Results after the layout settles (handles edge cases where
            // the new document pane materializes only after ResumeLayout).
            QueueResultsDocking();
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"ApplyTheme rebuild failed: {ex}");
            // Best-effort rollback: reattach the handler and put the old panel
            // back in place. The old theme remains in effect on the old panel.
            _dockPanel = oldPanel;
            oldPanel.ActiveDocumentChanged += OnActiveDocumentChanged;
            if (parent is not null)
            {
                parent.Controls.Add(oldPanel);
                parent.Controls.SetChildIndex(oldPanel, 0);
                oldPanel.Visible = true;
            }
            throw;
        }

        // Dispose the retired panel only after every live content has been
        // reparented onto the new one; disposing earlier would cascade-dispose
        // the DockContent forms and their hosted user controls.
        try
        {
            oldPanel.Dispose();
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"Failed to dispose retired DockPanel: {ex.Message}");
        }
    }

    // ── Layout persistence ────────────────────────────────────

    /// <summary>
    /// Persists the current DockPanel layout (open documents, dock states,
    /// positions) to an XML file via <see cref="DockPanel.SaveAsXml"/>.
    /// </summary>
    public void SaveLayout(string path)
    {
        try
        {
            _dockPanel.SaveAsXml(path);
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"Failed to save dock layout: {ex.Message}");
        }
    }

    /// <summary>
    /// Restores a previously saved DockPanel layout. For each persisted document
    /// the <paramref name="resolver"/> callback is invoked with the persist string
    /// (file path for file-backed tabs; <c>"unsaved://…"</c> for others). The
    /// callback should open the file and return the matching <see cref="EditorDockContent"/>.
    /// Unsaved or missing files are silently skipped.
    /// </summary>
    public void LoadLayout(string path, Func<string, DockContent?> resolver)
    {
        if (!File.Exists(path))
            return;

        // Layouts with Results as a global DockBottom cannot express the
        // required layout. Reject only those stale layouts before DockSuite
        // mutates the live DockPanel; a clean startup then creates Results
        // directly below the first document pane.
        if (!IsLayoutCompatible(path))
        {
            Trace.WriteLine($"Ignoring incompatible dock layout: {path}");
            return;
        }

        // The main window registers DockSuite tool windows before session
        // restore. LoadFromXml only supports a fresh panel, so restore the
        // persisted editor documents through the resolver in this case.
        if (_dockPanel.Panes.Count > 0)
        {
            RestoreDocumentsWithoutReinitializing(path, resolver);
            return;
        }

        _isLoadingLayout = true;
        try
        {
            _dockPanel.LoadFromXml(path, persistString => resolver(persistString ?? string.Empty));

            // Normalize persisted Results placement after DockSuite restores
            // all panes from XML.
            DockResultsBelowDocuments();
            QueueResultsDocking();
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"Failed to load dock layout: {ex.Message}");
        }
        finally
        {
            _isLoadingLayout = false;
        }
    }

    /// <summary>
    /// Returns the file-backed documents referenced by a persisted layout.
    /// Layout XML is metadata; editor contents are deliberately loaded by the
    /// host through an asynchronous file service before <see cref="LoadLayout"/>
    /// invokes DockSuite's synchronous resolver callback.
    /// </summary>
    public IReadOnlyList<string> GetPersistedFilePaths(string path)
    {
        if (!File.Exists(path) || !IsLayoutCompatible(path))
            return [];

        try
        {
            var document = XDocument.Load(path);
            return document
                .Descendants("Content")
                .Select(content => content.Attribute("PersistString")?.Value ?? string.Empty)
                .Where(persistString => !string.IsNullOrWhiteSpace(persistString)
                    && !persistString.StartsWith("tool:", StringComparison.OrdinalIgnoreCase)
                    && !persistString.StartsWith("unsaved://", StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"Failed to inspect dock layout '{path}': {ex.Message}");
            return [];
        }
    }

    private static bool IsLayoutCompatible(string path)
    {
        try
        {
            var document = XDocument.Load(path);
            var contentNames = document.Root?
                .Element("Contents")?
                .Elements("Content")
                .Where(e => e.Attribute("ID") is not null)
                .ToDictionary(
                    e => e.Attribute("ID")!.Value,
                    e => e.Attribute("PersistString")?.Value ?? string.Empty)
                ?? new Dictionary<string, string>();

            foreach (var pane in document.Root?.Element("Panes")?.Elements("Pane")
                         ?? Enumerable.Empty<XElement>())
            {
                bool isDocumentPane = string.Equals(
                    pane.Attribute("DockState")?.Value,
                    nameof(DockState.Document),
                    StringComparison.OrdinalIgnoreCase);

                foreach (var content in pane.Element("Contents")?.Elements("Content")
                             ?? Enumerable.Empty<XElement>())
                {
                    var refId = content.Attribute("RefID")?.Value;
                    if (refId is null || !contentNames.TryGetValue(refId, out var persistString))
                        continue;

                    if (persistString.Equals("tool:Results", StringComparison.OrdinalIgnoreCase)
                        && !isDocumentPane)
                        return false;

                    if (isDocumentPane
                        && persistString.StartsWith("tool:", StringComparison.OrdinalIgnoreCase)
                        && !persistString.Equals("tool:Results", StringComparison.OrdinalIgnoreCase))
                        return false;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"Ignoring unreadable dock layout '{path}': {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Shows a Git diff document in the SQL document pane (reuse-friendly host).
    /// </summary>
    public void ShowGitDiffDocument(GitDiffDockContent content)
    {
        ArgumentNullException.ThrowIfNull(content);

        var sqlPane = _tabToDockContent.Values
            .Select(c => c.DockHandler.Pane)
            .FirstOrDefault(pane => pane is not null && pane.DockState == DockState.Document);

        if (sqlPane is null)
            content.Show(_dockPanel, DockState.Document);
        else
            content.Show(sqlPane, sqlPane.ActiveContent);

        content.Activate();
    }

    /// <summary>Returns the EditorDockContent associated with the given TabPage, if any.</summary>
    public EditorDockContent? GetDockContentForTab(TabPage tabPage)
    {
        return tabPage is not null && _tabToDockContent.TryGetValue(tabPage, out var dc) ? dc : null;
    }

    // ── Tool windows ──────────────────────────────────────────

    /// <summary>
    /// Registers (or reuses) a tool window hosting <paramref name="content"/>
    /// with the given <paramref name="title"/> and shows it at the specified
    /// <paramref name="state"/>. If a tool with the same title already exists,
    /// the existing one is brought to front.
    /// </summary>
    public void ShowToolWindow(string title, Control content, DockState state = DockState.DockLeft)
    {
        if (_toolWindows.TryGetValue(title, out var existing))
        {
            existing.Show(_dockPanel, state);
            existing.Activate();
            return;
        }

        var tool = new ToolDockContent(content, title);
        _toolWindows[title] = tool;
        tool.Show(_dockPanel, state);
    }

    /// <summary>Hides (but does not dispose) the tool window with the given title.</summary>
    public void HideToolWindow(string title)
    {
        if (_toolWindows.TryGetValue(title, out var tool))
            tool.Hide();
    }

    /// <summary>Returns the tool window DockContent for the given title, or null.</summary>
    public ToolDockContent? GetToolWindow(string title)
    {
        return _toolWindows.TryGetValue(title, out var tool) ? tool : null;
    }

    /// <summary>Returns true if a tool with the given title is registered and visible.</summary>
    public bool IsToolWindowVisible(string title)
    {
        return _toolWindows.TryGetValue(title, out var tool) && tool.Visible;
    }

    /// <summary>
    /// Registers a set of tool windows that should be persisted and restored
    /// with the DockPanel layout. Call this once during initialization.
    /// </summary>
    public void RegisterPersistentTool(string title, Control content, DockState defaultState = DockState.DockLeft)
    {
        if (_toolWindows.ContainsKey(title))
            return;

        var tool = new ToolDockContent(content, title);
        _toolWindows[title] = tool;
        tool.Show(_dockPanel, defaultState);
    }

    public bool TryGetTool(string title, out ToolDockContent? tool) =>
        _toolWindows.TryGetValue(title, out tool);

    /// <summary>Shows Preferences as a document alongside SQL editor tabs.</summary>
    public void ShowPreferences(
        Action repaintApplication,
        Action saveManySqlToDisk,
        IApplicationSettingsContext applicationSettingsContext,
        ISnippetInitializationContext snippetInitializationContext,
        Action saveConfig,
        Action saveRecentFiles,
        IUiHelperService uiHelperService,
        IColorTheme colorTheme,
        INetezzaAutocompleteState netezzaAutocompleteState)
    {
        if (_preferencesContent is not null && !_preferencesContent.IsDisposed)
        {
            _preferencesContent.Show(_dockPanel, DockState.Document);
            _preferencesContent.Activate();
            return;
        }

        _preferencesContent = new PreferencesDockContent(
            repaintApplication,
            saveManySqlToDisk,
            applicationSettingsContext,
            snippetInitializationContext,
            saveConfig,
            saveRecentFiles,
            uiHelperService,
            colorTheme,
            netezzaAutocompleteState);
        _preferencesContent.Show(_dockPanel, DockState.Document);
        _preferencesContent.Activate();
    }

    /// <summary>Shows Query History as a document alongside SQL editor tabs.</summary>
    public void ShowHistory(
        Action<Form> doColorize,
        Action<DataGridView> doubleBuff,
        Action<string, string, string> addTabAction,
        string historyDatFile,
        bool useSpecialColoring,
        IHistoryStore historyStore)
    {
        if (_historyContent is not null && !_historyContent.IsDisposed)
        {
            _historyContent.Show(_dockPanel, DockState.Document);
            _historyContent.Activate();
            return;
        }

        _historyContent = new HistoryDockContent(
            doColorize,
            doubleBuff,
            addTabAction,
            historyDatFile,
            useSpecialColoring,
            historyStore,
            _uiDispatcher);
        _historyContent.Show(_dockPanel, DockState.Document);
        _historyContent.Activate();
    }

    /// <summary>Shows Query Watch as a document alongside SQL editor tabs.</summary>
    public void ShowQueryWatch(
        IQueryWatchService queryWatchService,
        Func<QueryWatchContext> contextFactory,
        Action<Form> doColorize,
        Action<DataGridView> doubleBuff,
        ILogger logger)
    {
        if (_queryWatchContent is not null && !_queryWatchContent.IsDisposed)
        {
            _queryWatchContent.Show(_dockPanel, DockState.Document);
            _queryWatchContent.Activate();
            _ = _queryWatchContent.RefreshNowAsync();
            return;
        }

        var viewModel = new QueryWatchViewModel(queryWatchService, contextFactory, _uiDispatcher);
        _queryWatchContent = new QueryWatchDockContent(
            viewModel,
            doColorize,
            doubleBuff,
            logger);
        _queryWatchContent.Show(_dockPanel, DockState.Document);
        _queryWatchContent.Activate();
    }

    // ── Results tool window ───────────────────────────────────

    private ResultsDockContent? _resultsWindow;

    /// <summary>
    /// Ensures the Results tool window (DockBottom) exists and returns it.
    /// Creates one if needed. Safe to call multiple times.
    /// </summary>
    public ResultsDockContent EnsureResultsToolWindow()
    {
        if (_resultsWindow is not null && !_resultsWindow.IsDisposed)
        {
            ShowResultsInDocumentPane();
            return _resultsWindow;
        }

        _resultsWindow = new ResultsDockContent();
        ShowResultsInDocumentPane();
        return _resultsWindow;
    }

    /// <summary>
    /// Makes the Results tool visible and binds it to the supplied editor
    /// document. Execution events can arrive while Results itself is active,
    /// in which case ActiveDocumentChanged cannot infer the owning editor.
    /// </summary>
    public ResultsDockContent ShowResultsForTab(TabPage tabPage)
    {
        ResultsDockContent results = EnsureResultsToolWindow();
        SwapResultsForTab(tabPage);
        ForceResultsBelowSqlDocuments();
        results.Activate();
        return results;
    }

    public void DockResultsBelowDocuments()
    {
        if (_resultsWindow is null || _resultsWindow.IsDisposed)
            return;

        if (_resultsWindow.DockState == DockState.Document)
            return;

        ShowResultsInDocumentPane();
    }

    /// <summary>
    /// Forces Results under the SQL document pane. Used once for documentation screenshots
    /// (normal startup can leave Results as a sibling Document tab when the pane was not ready).
    /// </summary>
    public void ForceResultsBelowSqlDocuments(double proportion = 0.44)
    {
        if (_resultsWindow is null || _resultsWindow.IsDisposed)
        {
            _resultsWindow = new ResultsDockContent();
        }

        DockPane? documentPane = FindSqlDocumentPane();
        if (documentPane is null)
        {
            return;
        }

        _resultsWindow.Show(documentPane, DockAlignment.Bottom, proportion);
    }

    private void ShowResultsInDocumentPane()
    {
        if (_resultsWindow is null || _resultsWindow.IsDisposed
            || (_resultsWindow.DockState == DockState.Document && _resultsWindow.Visible))
            return;

        DockPane? documentPane = FindSqlDocumentPane();
        if (documentPane is null)
            return;

        // Dock relative to the SQL document pane rather than DockPanel itself.
        // A global DockBottom pane spans below the Database tool, which is not
        // the intended SQL/Results workspace layout.
        // Documentation screenshots open a taller results pane once at first Show.
        double proportion = StartupArguments.IsDocumentationShowcaseLayout(Environment.GetCommandLineArgs())
            ? 0.56
            : 0.25;
        _resultsWindow.Show(documentPane, DockAlignment.Bottom, proportion);
    }

    private DockPane? FindSqlDocumentPane() =>
        _dockPanel.Panes.FirstOrDefault(pane =>
            pane.DockState == DockState.Document
            && pane.Contents.OfType<EditorDockContent>().Any());

    private void QueueResultsDocking()
    {
        if (_dockPanel.IsDisposed || !_dockPanel.IsHandleCreated)
            return;

        _dockPanel.BeginInvoke((MethodInvoker)DockResultsBelowDocuments);
    }

    /// <summary>Returns the existing Results tool window, or null.</summary>
    public ResultsDockContent? GetResultsToolWindow() =>
        _resultsWindow is not null && !_resultsWindow.IsDisposed ? _resultsWindow : null;

    // ── DockSuite-specific helpers ────────────────────────────

    /// <summary>Callback invoked when the user clicks the close button on a DockContent tab.</summary>
    public Action<TabPage>? TabCloseRequested { get; set; }

    /// <summary>Raised when DockSuite activates a clean-layer editor document.</summary>
    public Action<EditorDocumentId>? ActiveDocumentChanged { get; set; }

    /// <summary>
    /// Reports the current DockSuite document ordering. DockPanelSuite does
    /// not expose a dedicated tab-reorder event, but activation is raised for
    /// both clicks and the completion of a document-tab drag.
    /// </summary>
    public Action<IReadOnlyList<EditorDocumentId>>? DocumentOrderChanged { get; set; }

    /// <summary>
    /// Event raised after theme change to request reopening of document tabs.
    /// The list contains persist strings (file paths or "unsaved://..." for unsaved tabs).
    /// </summary>
    public Action<IReadOnlyList<string>>? ReopenTabsRequested { get; set; }

    /// <summary>
    /// Looks up the TabPage associated with a DockContent.
    /// Needed when legacy code expects a TabPage reference.
    /// </summary>
    public TabPage? GetTabPageForDockContent(DockContent dockContent)
    {
        if (dockContent is EditorDockContent editorContent
            && _dockContentToTab.TryGetValue(editorContent, out var tabPage))
        {
            return tabPage;
        }
        return null;
    }

    /// <summary>
    /// Selects the DockContent corresponding to the given TabPage.
    /// </summary>
    public void SelectTab(TabPage tabPage)
    {
        if (_tabToDockContent.TryGetValue(tabPage, out var dockContent))
        {
            _lastActiveEditorContent = dockContent;
            if (_mainTabControl is not null && !_mainTabControl.IsDisposed
                && _mainTabControl.TabPages.Contains(tabPage))
            {
                _mainTabControl.SelectedTab = tabPage;
            }
            ShowInSqlDocumentPane(dockContent);
            dockContent.Activate();
        }
    }

    private EditorDockContent? ResolveCurrentEditorContent()
    {
        if (_dockPanel.ActiveDocument is EditorDockContent activeEditor
            && _dockContentToTab.ContainsKey(activeEditor))
        {
            _lastActiveEditorContent = activeEditor;
            return activeEditor;
        }

        // ResultsDockContent intentionally participates in DockState.Document
        // so it can be nested below SQL documents. While Results is active,
        // DockPanel.ActiveDocument therefore no longer identifies the owning
        // editor. Keep the last real editor as the document context for F5,
        // Stop, result creation, and other document-scoped commands.
        return _lastActiveEditorContent is not null
            && !_lastActiveEditorContent.IsDisposed
            && _dockContentToTab.ContainsKey(_lastActiveEditorContent)
            ? _lastActiveEditorContent
            : null;
    }

    /// <summary>
    /// Shows an editor in the existing SQL document pane. Results is also
    /// nested in a document-state pane to obtain the SQL-over-Results layout,
    /// so using DockPanel + Document would otherwise add a new SQL editor as
    /// a tab in the Results pane when Results is active.
    /// </summary>
    private void ShowInSqlDocumentPane(EditorDockContent dockContent)
    {
        var sqlPane = _tabToDockContent.Values
            .Where(content => content != dockContent)
            .Select(content => content.DockHandler.Pane)
            .FirstOrDefault(pane => pane is not null && pane.DockState == DockState.Document);

        if (sqlPane is null)
        {
            dockContent.Show(_dockPanel, DockState.Document);
            return;
        }

        dockContent.Show(sqlPane, sqlPane.ActiveContent);
    }

    private static void RestoreDocumentsWithoutReinitializing(string path, Func<string, DockContent?> resolver)
    {
        try
        {
            var document = XDocument.Load(path);
            foreach (var content in document.Descendants("Content"))
            {
                string persistString = content.Attribute("PersistString")?.Value ?? string.Empty;
                if (string.IsNullOrWhiteSpace(persistString)
                    || persistString.StartsWith("tool:", StringComparison.OrdinalIgnoreCase)
                    || persistString.StartsWith("unsaved://", StringComparison.OrdinalIgnoreCase))
                    continue;

                resolver(persistString);
            }
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"Failed to restore documents from dock layout: {ex.Message}");
        }
    }

    /// <summary>Synchronizes DockSuite metadata after a tab title or file path changes.</summary>
    public void UpdateEditorTab(TabPage tabPage)
    {
        if (!_tabToDockContent.TryGetValue(tabPage, out var dockContent))
            return;

        dockContent.SetTitle(tabPage.Text);
        dockContent.FilePath = tabPage.Tag is TabPageMainTag tag ? tag.Filename : null;
    }

    /// <summary>Returns the result strip associated with a document.</summary>
    public TabControl? GetResultsTabControl(TabPage tabPage) =>
        tabPage is not null && _perTabResults.TryGetValue(tabPage, out var tc) ? tc : null;

    // ── Per-tab results support ───────────────────────────────

    private static TabControl CreatePerTabResultsTabControl() => new()
    {
        Dock = DockStyle.Fill,
        Multiline = true,
        Padding = new Point(12, 4)
    };

    /// <summary>
    /// Returns the per-tab results TabControl for the given TabPage,
    /// creating one if it doesn't exist yet.
    /// </summary>
    public TabControl GetOrCreateResultsTabControl(TabPage tabPage)
    {
        if (tabPage is not null && _perTabResults.TryGetValue(tabPage, out var tc))
            return tc;

        tc = CreatePerTabResultsTabControl();
        if (tabPage is not null)
            _perTabResults[tabPage] = tc;
        return tc;
    }

    /// <summary>
    /// Finds the TabPage that owns the given SplitContainer.
    /// </summary>
    public TabPage? FindTabForSplitContainer(SplitContainer splitter)
    {
        if (splitter is null)
            return null;

        foreach (var kv in _tabToDockContent)
        {
            if (kv.Value.SplitContainer == splitter)
                return kv.Key;
        }

        // Fallback: check inner service
        foreach (var kv in _perTabResults)
        {
            if (_inner.GetSplitContainerForTab(kv.Key) == splitter)
                return kv.Key;
        }

        return null;
    }

    /// <summary>
    /// Swaps the ResultsDockContent to show the per-tab TabControl
    /// for the given TabPage. Called on ActiveDocumentChanged.
    /// </summary>
    private void SwapResultsForTab(TabPage tabPage)
    {
        if (_resultsWindow is null || _resultsWindow.IsDisposed)
            return;

        if (tabPage is not null && _perTabResults.TryGetValue(tabPage, out var perTabTc))
        {
            _resultsWindow.SwapTabControl(perTabTc);
        }
    }

    public void Dispose()
    {
        TabCloseRequested = null;
        ActiveDocumentChanged = null;
        DocumentOrderChanged = null;
        ReopenTabsRequested = null;

        foreach (TabControl results in _perTabResults.Values.ToArray())
            results.Dispose();
        _perTabResults.Clear();

        // DockPanel owns the DockContent controls. Disposing it also releases
        // tool windows and the hidden compatibility host when the scoped shell
        // ends, preventing a second shell from retaining the previous UI graph.
        if (!_dockPanel.IsDisposed)
            _dockPanel.Dispose();

        _tabToDockContent.Clear();
        _dockContentToTab.Clear();
        _documentIdsByTab.Clear();
        _tabsByDocumentId.Clear();
        _toolWindows.Clear();
        _preferencesContent = null;
        _historyContent = null;
        _resultsWindow = null;
        _lastActiveEditorContent = null;
        _mainTabControl = null;
    }
}
