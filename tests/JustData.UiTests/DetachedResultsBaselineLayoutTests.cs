using System.Xml.Linq;
using JustData.Application;
using JustyBaseLegacy.UI;

namespace JustData.UiTests;

/// <summary>
/// Baseline layout/metadata guards – extend DockLayoutMetadataTests.
/// Green on main, catch IsLayoutCompatible/GetPersistedFilePaths regressions.
/// </summary>
public sealed class DetachedResultsBaselineLayoutTests
{
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

    [Fact]
    public void GetPersistedFilePaths_ignores_tool_unsaved_and_result_persist_strings()
    {
        DirectoryInfo tmp = Directory.CreateTempSubdirectory();
        try
        {
            string sqlPath = Path.Combine(tmp.FullName, "query.sql");
            string layoutPath = Path.Combine(tmp.FullName, "dockLayout.xml");

            new XDocument(
                new XElement("DockPanel",
                    new XElement("Contents",
                        new XElement("Content", new XAttribute("PersistString", sqlPath)),
                        new XElement("Content", new XAttribute("PersistString", "tool:Results")),
                        new XElement("Content", new XAttribute("PersistString", "tool:Database")),
                        new XElement("Content", new XAttribute("PersistString", "unsaved://query")),
                        new XElement("Content", new XAttribute("PersistString", sqlPath)))))
                .Save(layoutPath);

            using var manager = new DockSuiteTabManager(new InlineDispatcher());
            string[] paths = manager.GetPersistedFilePaths(layoutPath).ToArray();

            Assert.Single(paths);
            Assert.Equal(sqlPath, paths[0]);
        }
        finally
        {
            tmp.Delete(recursive: true);
        }
    }

    [Fact]
    public void IsLayoutCompatible_via_GetPersistedFilePaths_rejects_tool_in_document()
    {
        DirectoryInfo tmp = Directory.CreateTempSubdirectory();
        try
        {
            string layoutPath = Path.Combine(tmp.FullName, "dockLayout.xml");
            // Minimal DockPanelSuite layout: Contents + Panes with RefIDs
            // Pane Document containing tool:Database must be rejected → GetPersistedFilePaths returns []
            var doc = new XDocument(
                new XElement("DockPanel",
                    new XElement("Contents",
                        new XElement("Content", new XAttribute("ID", "0"), new XAttribute("PersistString", @"C:\a.sql")),
                        new XElement("Content", new XAttribute("ID", "1"), new XAttribute("PersistString", "tool:Database"))),
                    new XElement("Panes",
                        new XElement("Pane", new XAttribute("DockState", "Document"),
                            new XElement("Contents",
                                new XElement("Content", new XAttribute("RefID", "0")),
                                new XElement("Content", new XAttribute("RefID", "1")))))));

            doc.Save(layoutPath);

            using var manager = new DockSuiteTabManager(new InlineDispatcher());
            // Incompatible layout → GetPersistedFilePaths returns empty (IsLayoutCompatible false)
            Assert.Empty(manager.GetPersistedFilePaths(layoutPath));
        }
        finally
        {
            tmp.Delete(recursive: true);
        }
    }

    [Fact]
    public void IsLayoutCompatible_rejects_Results_outside_Document()
    {
        DirectoryInfo tmp = Directory.CreateTempSubdirectory();
        try
        {
            string layoutPath = Path.Combine(tmp.FullName, "dockLayout.xml");
            var doc = new XDocument(
                new XElement("DockPanel",
                    new XElement("Contents",
                        new XElement("Content", new XAttribute("ID", "0"), new XAttribute("PersistString", "tool:Results"))),
                    new XElement("Panes",
                        new XElement("Pane", new XAttribute("DockState", "DockLeft"),
                            new XElement("Contents",
                                new XElement("Content", new XAttribute("RefID", "0")))))));

            doc.Save(layoutPath);

            using var manager = new DockSuiteTabManager(new InlineDispatcher());
            Assert.Empty(manager.GetPersistedFilePaths(layoutPath));
        }
        finally
        {
            tmp.Delete(recursive: true);
        }
    }

    [Fact]
    public void SaveLayout_roundtrip_contains_tool_Results()
    {
        var tmp = Directory.CreateTempSubdirectory();
        try
        {
            string sqlPath = Path.Combine(tmp.FullName, "a.sql");
            File.WriteAllText(sqlPath, "select 1");
            string layoutPath = Path.Combine(tmp.FullName, "dockLayout.xml");

            // Use STA for WinForms DockPanel
            Exception? ex = null;
            var thread = new Thread(() =>
            {
                try
                {
                    var host = new Form();
                    var hidden = new TabControl();
                    host.Controls.Add(hidden);
                    host.CreateControl();
                    using var manager = new DockSuiteTabManager(new InlineDispatcher());
                    manager.Initialize(hidden);
                    manager.DockPanel.Theme = new WeifenLuo.WinFormsUI.Docking.VS2015LightTheme();
                    manager.DockPanel.CreateControl();

                    var tab = new TabPage("a.sql") { Tag = new AppBase.Common.TabPageMainTag { Filename = sqlPath } };
                    var splitter = new SplitContainer();
                    host.Controls.Add(splitter);
                    // Need fake panel – use minimal NSubstitute or stub
                    var panel = new FakePanel();
                    manager.RegisterEditorTab(tab, panel, splitter);
                    manager.SetDocumentId(tab, JustData.Application.Editor.EditorDocumentId.New());
                    manager.EnsureResultsToolWindow();
                    manager.SaveLayout(layoutPath);
                    host.Dispose();
                }
                catch (Exception e) { ex = e; }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            if (ex is not null) throw new Xunit.Sdk.XunitException(ex.ToString());

            Assert.True(File.Exists(layoutPath));
            string xml = File.ReadAllText(layoutPath);
            Assert.Contains("tool:Results", xml, StringComparison.Ordinal);
        }
        finally
        {
            tmp.Delete(recursive: true);
        }
    }

    private sealed class FakePanel : AppBase.Common.Interfaces.IEditorPanel
    {
        public FastColoredTextBoxNS.FastColoredTextBox CurrentTb { get; } = new();
        public string SelectedConnectionName { get; set; } = "c";
        public string SelectedDatabase { get; set; } = "d";
        public bool KeepConnectionOpen { get; set; }
        public bool ContinueOnError { get; set; }
        public void SetEnabledConnectionsDatabases(bool enabled) { }
    }
}
