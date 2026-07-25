using JustData.Application.Editor;
using JustData.Application.Sql;
using JustData.ViewModels.Editor;
using NSubstitute;

namespace JustData.ViewModels.Tests;

public sealed class EditorDocumentViewModelFunctionalTests
{
    [Fact]
    public void UpdateTextFromView_marks_dirty_and_updates_text()
    {
        using var document = CreateDocument(text: "select 1");

        document.UpdateTextFromView("select 2");

        Assert.Equal("select 2", document.Text);
        Assert.True(document.IsDirty);
    }

    [Fact]
    public void UpdateTextFromView_same_text_does_not_mark_dirty()
    {
        using var document = CreateDocument(text: "select 1");
        var changed = new List<string>();
        document.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);

        document.UpdateTextFromView("select 1");

        Assert.False(document.IsDirty);
        Assert.DoesNotContain(nameof(EditorDocumentViewModel.Text), changed);
    }

    [Fact]
    public void UpdateTextFromView_null_treats_as_empty()
    {
        using var document = CreateDocument(text: "select 1");

        document.UpdateTextFromView(null!);

        Assert.Equal(string.Empty, document.Text);
        Assert.True(document.IsDirty);
    }

    [Fact]
    public void SetLoadedText_clears_dirty_and_updates_text()
    {
        using var document = CreateDocument(text: "select 1");
        document.UpdateTextFromView("select 2");
        Assert.True(document.IsDirty);

        document.SetLoadedText("select 3");

        Assert.Equal("select 3", document.Text);
        Assert.False(document.IsDirty);
        Assert.False(document.ExternalChangePending);
    }

    [Fact]
    public void MarkSaved_clears_dirty_and_external_change_pending()
    {
        using var document = CreateDocument(text: "select 1");
        document.UpdateTextFromView("select 2");
        Assert.True(document.IsDirty);

        document.MarkSaved();

        Assert.False(document.IsDirty);
        Assert.False(document.ExternalChangePending);
    }

    [Fact]
    public void UpdateEditorSelection_clamps_negative_values_to_zero()
    {
        using var document = CreateDocument();

        document.UpdateEditorSelection(-5, -3, -1);

        Assert.Equal(0, document.SelectionStart);
        Assert.Equal(0, document.SelectionLength);
        Assert.Equal(0, document.CaretOffset);
    }

    [Fact]
    public void UpdateEditorSelection_stores_positive_values()
    {
        using var document = CreateDocument();

        document.UpdateEditorSelection(10, 5, 15);

        Assert.Equal(10, document.SelectionStart);
        Assert.Equal(5, document.SelectionLength);
        Assert.Equal(15, document.CaretOffset);
    }

    [Fact]
    public void BuildExecutionRequest_populates_request_from_document_state()
    {
        using var document = CreateDocument(
            text: "select * from orders",
            connectionName: "prod",
            databaseName: "sales",
            keepConnectionOpen: true,
            continueOnError: true);
        document.UpdateEditorSelection(7, 3, 10);

        var request = document.BuildExecutionRequest(
            SqlExecutionMode.Script,
            SqlOutputMode.Csv,
            "/tmp/out.csv");

        Assert.Equal(document.Id, request.DocumentId);
        Assert.Equal("select * from orders", request.SqlText);
        Assert.Equal("prod", request.ConnectionName);
        Assert.Equal("sales", request.DatabaseName);
        Assert.Equal(SqlExecutionMode.Script, request.Mode);
        Assert.Equal(SqlOutputMode.Csv, request.OutputMode);
        Assert.Equal(7, request.SelectionStart);
        Assert.Equal(3, request.SelectionLength);
        Assert.Equal(10, request.CaretOffset);
        Assert.True(request.KeepConnectionOpen);
        Assert.True(request.ContinueOnError);
        Assert.Equal("/tmp/out.csv", request.OutputPath);
    }

    [Fact]
    public void ToSnapshot_creates_snapshot_with_current_state()
    {
        using var document = CreateDocument(
            text: "select 1",
            title: "test.sql",
            filePath: @"/tmp/test.sql",
            connectionName: "conn",
            databaseName: "db");
        document.UpdateTextFromView("select 2");

        var snapshot = document.ToSnapshot();

        Assert.Equal(document.Id, snapshot.Id);
        Assert.Equal("test.sql", snapshot.Title);
        Assert.Equal("select 2", snapshot.Text);
        Assert.Equal(Path.GetFullPath(@"/tmp/test.sql"), snapshot.FilePath);
        Assert.True(snapshot.IsDirty);
        Assert.Equal("conn", snapshot.ConnectionName);
        Assert.Equal("db", snapshot.DatabaseName);
    }

    [Fact]
    public void SetSavedPath_updates_file_path_and_title()
    {
        string oldPath = Path.Combine(Path.GetTempPath(), "old.sql");
        string newPath = Path.Combine(Path.GetTempPath(), "new.sql");
        var watchService = Substitute.For<IEditorFileWatchService>();
        watchService.Watch(Arg.Any<string>(), Arg.Any<Action<EditorFileChange>>())
            .Returns(Substitute.For<IDisposable>());

        using var document = new EditorDocumentViewModel(
            EditorDocumentId.New(), "old.sql", "select 1", oldPath,
            "", "", false, false, watchService);

        document.SetSavedPath(newPath);

        Assert.Equal(Path.GetFullPath(newPath), document.FilePath);
        Assert.Equal("new.sql", document.Title);
    }

    [Fact]
    public void SetSavedPath_same_path_only_updates_title()
    {
        string path = Path.Combine(Path.GetTempPath(), "test.sql");
        var watchService = Substitute.For<IEditorFileWatchService>();
        watchService.Watch(Arg.Any<string>(), Arg.Any<Action<EditorFileChange>>())
            .Returns(Substitute.For<IDisposable>());

        using var document = new EditorDocumentViewModel(
            EditorDocumentId.New(), "original.sql", "select 1", path,
            "", "", false, false, watchService);

        document.SetSavedPath(path);

        Assert.Equal(path, document.FilePath);
        Assert.Equal("test.sql", document.Title);
    }

    [Fact]
    public void SuppressExternalChanges_does_not_throw_for_negative_duration()
    {
        using var document = CreateDocument();
        document.SuppressExternalChanges(TimeSpan.Zero);
        document.SuppressExternalChanges(TimeSpan.FromSeconds(-1));
    }

    [Fact]
    public void Constructor_initializes_sql_execution_and_authoring()
    {
        using var document = CreateDocument();

        Assert.NotNull(document.SqlExecution);
        Assert.NotNull(document.SqlAuthoring);
        Assert.Same(document.SqlExecution, document.Execution);
        Assert.Same(document.SqlAuthoring, document.Authoring);
    }

    [Fact]
    public void Constructor_uses_file_name_as_title_when_file_path_provided()
    {
        var watchService = Substitute.For<IEditorFileWatchService>();
        watchService.Watch(Arg.Any<string>(), Arg.Any<Action<EditorFileChange>>())
            .Returns(Substitute.For<IDisposable>());

        using var document = new EditorDocumentViewModel(
            EditorDocumentId.New(), "tab", "select 1",
            @"/tmp/myquery.sql", "", "", false, false, watchService);

        Assert.Equal("myquery.sql", document.Title);
    }

    [Fact]
    public void Constructor_uses_tab_title_when_no_file_path()
    {
        using var document = CreateDocument(title: "My Query");

        Assert.Equal("My Query", document.Title);
    }

    [Fact]
    public void Constructor_defaults_title_to_tab_when_whitespace()
    {
        using var document = CreateDocument(title: "  ");

        Assert.Equal("tab", document.Title);
    }

    [Fact]
    public void ExternalChangePending_starts_false()
    {
        using var document = CreateDocument();

        Assert.False(document.ExternalChangePending);
    }

    private static EditorDocumentViewModel CreateDocument(
        string title = "tab",
        string text = "",
        string? filePath = null,
        string connectionName = "",
        string databaseName = "",
        bool keepConnectionOpen = false,
        bool continueOnError = false)
    {
        var watchService = Substitute.For<IEditorFileWatchService>();
        return new EditorDocumentViewModel(
            EditorDocumentId.New(), title, text, filePath,
            connectionName, databaseName, keepConnectionOpen, continueOnError,
            watchService);
    }
}
