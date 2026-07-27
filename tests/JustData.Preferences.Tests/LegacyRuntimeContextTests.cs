using AppBase.Common;
using AppBase.Common.Configuration;
using AppBase.Common.Interfaces;
using JustyBaseLegacy.UI.Configuration;
using NSubstitute;

namespace JustData.Preferences.Tests;

public sealed class LegacyRuntimeContextTests
{
    [Fact]
    public void Session_variables_are_isolated_and_global_replacement_is_case_insensitive()
    {
        var context = new LegacySessionVariableContext();
        int changed = 0;
        context.Changed += (_, _) => changed++;
        context.SetSessionVariable("document-1", "&local", "42");

        Assert.Equal("42", context.GetSessionVariables("document-1")["&local"]);
        Assert.Empty(context.GetSessionVariables("missing"));

        context.SetGlobalVariable("&portfolio", "top-class");
        Assert.Equal("SELECT 'top-class'", context.ReplaceGlobalVariables("SELECT '&PORTFOLIO'"));

        context.ClearGlobalVariables();
        Assert.Equal(1, changed);
        Assert.Empty(context.GlobalVariables);
    }

    [Fact]
    public void Variable_reads_are_immutable_snapshots()
    {
        var context = new LegacySessionVariableContext();
        context.SetSessionVariable("document-1", "&local", "42");
        context.SetGlobalVariable("&portfolio", "top-class");

        IReadOnlyDictionary<string, string> sessionSnapshot = context.GetSessionVariables("document-1");
        IReadOnlyDictionary<string, string> globalSnapshot = context.GlobalVariables;
        context.SetSessionVariable("document-1", "&later", "43");
        context.SetGlobalVariable("&later", "next");

        Assert.Equal("42", sessionSnapshot["&local"]);
        Assert.DoesNotContain("&later", sessionSnapshot.Keys);
        Assert.Equal("top-class", globalSnapshot["&portfolio"]);
        Assert.DoesNotContain("&later", globalSnapshot.Keys);
    }

    [Fact]
    public void Database_catalog_reads_are_independent_snapshots()
    {
        IApplicationSettingsContext settings = Substitute.For<IApplicationSettingsContext>();
        var context = new LegacyDatabaseRuntimeContext(settings);
        IDatabaseRuntimeCatalogWriter writer = context;
        writer.SetDatabase("connection", 1, new DatabaseInfo(10, "SYSTEM", "ADMIN", "SYSTEM"));

        IReadOnlyDictionary<string, Dictionary<int, DatabaseInfo>> snapshot = context.DatabaseDictionary;
        writer.SetDatabase("connection", 2, new DatabaseInfo(11, "TEST", "ADMIN", "TEST"));
        writer.SetDatabase("later", 1, new DatabaseInfo(12, "LATER", "ADMIN", "LATER"));

        Assert.Single(snapshot["connection"]);
        Assert.DoesNotContain("later", snapshot.Keys);
    }

    [Fact]
    public async Task Ddl_provider_returns_empty_for_object_missing_from_catalog()
    {
        INetezzaHelperService helper = Substitute.For<INetezzaHelperService>();
        INetezzaCompletionContext completion = Substitute.For<INetezzaCompletionContext>();
        IDatabaseRuntimeContext database = Substitute.For<IDatabaseRuntimeContext>();
        completion.SelectedConnectionName.Returns("connection");
        completion.DatabaseSchemaLookup.Returns(new Dictionary<string, Dictionary<string, Dictionary<string, (string owner, int tableId)>>>());

        var provider = new LegacyNetezzaDdlCodeProvider(helper, completion, database);

        Assert.Equal(string.Empty, await provider.GetTableCodeByName("database", "table"));
        Assert.Equal(string.Empty, await provider.GetRecreateTableCodeByName("database", "table"));
    }

    [Fact]
    public void Snippet_context_bootstraps_directories_and_uses_fallback_tab_names()
    {
        string directory = Path.Combine(Path.GetTempPath(), "JustyBaseLegacy-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            IApplicationSettingsContext settings = Substitute.For<IApplicationSettingsContext>();
            settings.ConfigDirectory.Returns(directory);
            settings.Config.Returns(new ApplicationConfig { UseSpecialTabNames = false });
            var context = new LegacySnippetContext(settings);

            context.Initialize("{}", "[]");

            Assert.True(File.Exists(Path.Combine(directory, "snipets.json")));
            Assert.True(Directory.Exists(Path.Combine(directory, "data")));
            Assert.True(Directory.Exists(Path.Combine(directory, "backup")));
            Assert.Equal("tab1", context.GetNextName([]));
            Assert.Equal("tab2", context.GetNextName(["tab1"]));
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }
}
