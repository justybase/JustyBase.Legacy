using System.Xml.Linq;
using JustData.Application;
using JustyBaseLegacy.UI;

namespace JustData.UiTests;

public sealed class DockLayoutMetadataTests
{
    [Fact]
    public void GetPersistedFilePaths_ignores_tools_unsaved_documents_and_duplicates()
    {
        DirectoryInfo temporaryDirectory = Directory.CreateTempSubdirectory();
        try
        {
            string sqlPath = Path.Combine(temporaryDirectory.FullName, "query.sql");
            string layoutPath = Path.Combine(temporaryDirectory.FullName, "dockLayout.xml");

            new XDocument(
                new XElement("DockPanel",
                    new XElement("Contents",
                        new XElement("Content", new XAttribute("PersistString", sqlPath)),
                        new XElement("Content", new XAttribute("PersistString", sqlPath)),
                        new XElement("Content", new XAttribute("PersistString", "tool:Results")),
                        new XElement("Content", new XAttribute("PersistString", "unsaved://query")))))
                .Save(layoutPath);

            using var manager = new DockSuiteTabManager(new InlineDispatcher());

            Assert.Equal([sqlPath], manager.GetPersistedFilePaths(layoutPath));
        }
        finally
        {
            temporaryDirectory.Delete(recursive: true);
        }
    }

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
}
