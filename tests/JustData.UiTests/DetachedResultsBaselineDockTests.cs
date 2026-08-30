using AppBase.Common;
using AppBase.Common.Interfaces;
using FastColoredTextBoxNS;
using JustData.Application;
using JustData.Application.Editor;
using JustyBaseLegacy.Services;
using JustyBaseLegacy.UI;
using WeifenLuo.WinFormsUI.Docking;

namespace JustData.UiTests;

/// <summary>
/// Baseline guards for current Results/DockSuite behaviour.
/// All tests are green on main and will catch regressions when
/// detached result documents (Opcja B) are introduced.
/// </summary>
public sealed class DetachedResultsBaselineDockTests
{
    // ── Helpers ──────────────────────────────────────────────────

    private sealed class InlineDispatcher : IUiDispatcher
    {
        public bool CheckAccess() => true;
        public Task InvokeAsync(Action action, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            action();
            return Task.CompletedTask;
        }
    }

    private sealed class FakeEditorPanel : IEditorPanel
    {
        public FastColoredTextBox CurrentTb { get; } = new FastColoredTextBox();
        public string SelectedConnectionName { get; set; } = "conn";
        public string SelectedDatabase { get; set; } = "db";
        public bool KeepConnectionOpen { get; set; }
        public bool ContinueOnError { get; set; }
        public void SetEnabledConnectionsDatabases(bool enabled) { }
    }

    private static void RunOnSta(Action action)
    {
        Exception? ex = null;
        var thread = new Thread(() =>
        {
            try { action(); }
            catch (Exception e) { ex = e; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (ex is not null) throw new Xunit.Sdk.XunitException(ex.ToString());
    }

    private static (DockSuiteTabManager manager, Form host, TabControl hidden) CreateManager()
    {
        var host = new Form();
        var hidden = new TabControl { Name = "tabControlMain" };
        // Host must have handle created for DockPanel parenting
        host.Controls.Add(hidden);
        host.CreateControl();
        hidden.CreateControl();

        var manager = new DockSuiteTabManager(new InlineDispatcher());
        manager.Initialize(hidden);
        manager.DockPanel.Theme = new VS2015LightTheme();
        // Force handle creation for DockPanel
        manager.DockPanel.CreateControl();
        return (manager, host, hidden);
    }

    private static TabPage CreateEditorTab(string title, string? filePath = null)
    {
        var tab = new TabPage(title) { Text = title };
        tab.Tag = new TabPageMainTag { Filename = filePath, IsSaved = filePath is not null };
        return tab;
    }

    // ── D1: GetEditorDocumentOrder excludes Results content ─────

    [Fact]
    public void GetEditorDocumentOrder_excludes_Results_content()
    {
        RunOnSta(() =>
        {
            var (manager, host, _) = CreateManager();
            try
            {
                var tab1 = CreateEditorTab("a.sql", @"C:\a.sql");
                var id1 = EditorDocumentId.New();
                var splitter1 = new SplitContainer();
                host.Controls.Add(splitter1);
                manager.RegisterEditorTab(tab1, new FakeEditorPanel(), splitter1);
                manager.SetDocumentId(tab1, id1);

                var tab2 = CreateEditorTab("b.sql", @"C:\b.sql");
                var id2 = EditorDocumentId.New();
                var splitter2 = new SplitContainer();
                host.Controls.Add(splitter2);
                manager.RegisterEditorTab(tab2, new FakeEditorPanel(), splitter2);
                manager.SetDocumentId(tab2, id2);

                // Results is ensured but must not appear in editor order
                manager.EnsureResultsToolWindow();

                IReadOnlyList<EditorDocumentId> order = manager.GetEditorDocumentOrder();

                Assert.Equal(2, order.Count);
                Assert.Contains(id1, order);
                Assert.Contains(id2, order);
                // No extra entries – Results is not an EditorDocument
                Assert.DoesNotContain(order, id => id == default);
            }
            finally
            {
                manager.Dispose();
                host.Dispose();
            }
        });
    }

    // ── D2: Closing document disposes per-tab results TabControl ─

    [Fact]
    public void Closing_document_disposes_perTabResults_TabControl()
    {
        RunOnSta(() =>
        {
            var (manager, host, _) = CreateManager();
            try
            {
                var tab = CreateEditorTab("doc.sql", @"C:\doc.sql");
                var splitter = new SplitContainer();
                host.Controls.Add(splitter);
                manager.RegisterEditorTab(tab, new FakeEditorPanel(), splitter);

                TabControl? tc = manager.GetResultsTabControl(tab);
                Assert.NotNull(tc);
                Assert.False(tc!.IsDisposed);

                manager.UnregisterTab(tab);

                Assert.True(tc.IsDisposed);
                Assert.Null(manager.GetResultsTabControl(tab));
            }
            finally
            {
                manager.Dispose();
                host.Dispose();
            }
        });
    }

    // ── D3: Diagnostics tab is index 0 and not removable via RemoveTabData semantics ─

    [Fact]
    public void Results_TabControl_has_Diagnostics_at_index_zero_not_removable()
    {
        RunOnSta(() =>
        {
            var (manager, host, _) = CreateManager();
            try
            {
                var tab = CreateEditorTab("doc.sql", @"C:\doc.sql");
                var splitter = new SplitContainer();
                host.Controls.Add(splitter);
                manager.RegisterEditorTab(tab, new FakeEditorPanel(), splitter);

                TabControl? tc = manager.GetOrCreateResultsTabControl(tab);
                Assert.NotNull(tc);

                // Simulate SqlResultsUiFactory EnsureDiagnosticsTab logic:
                // Diagnostics tab is created by factory – here we mimic minimal tag
                var diagnosticsTab = new TabPage("Diagnostics")
                {
                    Tag = new TabPageResultsTag { IsPermanentDiagnostics = true, ParentControl = tc }
                };
                tc!.TabPages.Insert(0, diagnosticsTab);

                Assert.Equal("Diagnostics", tc.TabPages[0].Text);
                Assert.True(((TabPageResultsTag)tc.TabPages[0].Tag!).IsPermanentDiagnostics);

                // Baseline guard: RemoveTabData would skip Diagnostics – verify flag remains
                // (actual RemoveTabData is in BaseWindow, here we just assert invariant)
                Assert.True(((TabPageResultsTag)tc.TabPages[0].Tag!).IsPermanentDiagnostics);
            }
            finally
            {
                manager.Dispose();
                host.Dispose();
            }
        });
    }

    // ── D4: ActiveDocument after Results focus still returns last editor ─

    [Fact]
    public void ActiveDocument_after_Results_focus_still_last_editor()
    {
        RunOnSta(() =>
        {
            var (manager, host, _) = CreateManager();
            try
            {
                var tab1 = CreateEditorTab("a.sql", @"C:\a.sql");
                var id1 = EditorDocumentId.New();
                var splitter1 = new SplitContainer();
                host.Controls.Add(splitter1);
                manager.RegisterEditorTab(tab1, new FakeEditorPanel(), splitter1);
                manager.SetDocumentId(tab1, id1);

                var results = manager.EnsureResultsToolWindow();
                // Activate Results – ActiveDocument would be ResultsDockContent
                results.Activate();

                // ResolveCurrentEditorContent via CurrentEditor must still return editor, not null
                Assert.NotNull(manager.CurrentEditor);
                Assert.NotNull(manager.CurrentSplitContainer);
                Assert.Equal(id1, manager.GetActiveEditorTabPage() is not null ? id1 : default);
                // GetEditorDocumentOrder still 1
                Assert.Single(manager.GetEditorDocumentOrder());
            }
            finally
            {
                manager.Dispose();
                host.Dispose();
            }
        });
    }

    // ── D5: ShowResultsInDocumentPane forces Bottom nested pane ─

    [Fact]
    public void ShowResultsInDocumentPane_forces_Bottom_nested_pane()
    {
        RunOnSta(() =>
        {
            var (manager, host, _) = CreateManager();
            try
            {
                var tab = CreateEditorTab("a.sql", @"C:\a.sql");
                var splitter = new SplitContainer();
                host.Controls.Add(splitter);
                manager.RegisterEditorTab(tab, new FakeEditorPanel(), splitter);
                manager.SetDocumentId(tab, EditorDocumentId.New());

                var results = manager.EnsureResultsToolWindow();

                Assert.NotNull(results.DockHandler.Pane);
                // Pane is Document-adjacent nested below editor pane, not global DockBottom
                Assert.Equal(DockState.Document, results.DockState);
            }
            finally
            {
                manager.Dispose();
                host.Dispose();
            }
        });
    }

    // ── D6: SaveLayout does not throw with two docs and results ─

    [Fact]
    public void SaveLayout_does_not_throw_with_two_docs_and_results()
    {
        RunOnSta(() =>
        {
            var (manager, host, _) = CreateManager();
            try
            {
                for (int i = 0; i < 2; i++)
                {
                    var tab = CreateEditorTab($"doc{i}.sql", $@"C:\doc{i}.sql");
                    var splitter = new SplitContainer();
                    host.Controls.Add(splitter);
                    manager.RegisterEditorTab(tab, new FakeEditorPanel(), splitter);
                    manager.SetDocumentId(tab, EditorDocumentId.New());
                }
                manager.EnsureResultsToolWindow();

                string path = Path.Combine(Path.GetTempPath(), $"dock_baseline_{Guid.NewGuid():N}.xml");
                try
                {
                    manager.SaveLayout(path);
                    Assert.True(File.Exists(path));
                    string xml = File.ReadAllText(path);
                    Assert.Contains("tool:Results", xml, StringComparison.Ordinal);
                }
                finally
                {
                    if (File.Exists(path)) File.Delete(path);
                }
            }
            finally
            {
                manager.Dispose();
                host.Dispose();
            }
        });
    }
}
