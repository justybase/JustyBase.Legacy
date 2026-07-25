using JustData.Application.Editor;
using JustData.Application.Sql;
using JustData.ViewModels.Editor;
using JustData.ViewModels.Sql;
using NSubstitute;

namespace JustData.ViewModels.Tests;

public sealed class EditorDocumentViewModelLifecycleTests
{
    private static EditorDocumentViewModel CreateDocument(
        IEditorFileWatchService? watchService = null)
    {
        return new EditorDocumentViewModel(
            EditorDocumentId.New(),
            "Test",
            "select 1",
            filePath: null,
            connectionName: "TestConn",
            databaseName: "TestDb",
            keepConnectionOpen: false,
            continueOnError: false,
            watchService ?? Substitute.For<IEditorFileWatchService>());
    }

    [Fact]
    public void Disposing_idle_document_does_not_throw()
    {
        var document = CreateDocument();
        document.Dispose();
    }

    [Fact]
    public void Dispose_can_be_called_multiple_times()
    {
        var document = CreateDocument();
        document.Dispose();
        document.Dispose();
    }

    [Fact]
    public void Dispose_clears_external_change_detected_backing_field()
    {
        var document = CreateDocument();
        var field = typeof(EditorDocumentViewModel)
            .GetField("ExternalChangeDetected", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(field);

        document.Dispose();
        Assert.Null(field.GetValue(document));
    }

    [Fact]
    public void Dispose_clears_diagnostics_changed_backing_field()
    {
        var document = CreateDocument();
        var field = typeof(EditorDocumentViewModel)
            .GetField("DiagnosticsChanged", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(field);

        document.Dispose();
        Assert.Null(field.GetValue(document));
    }

    [Fact]
    public async Task Disposed_execution_throws()
    {
        var document = CreateDocument();
        document.Dispose();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => document.Execution.RunAsync(
            new SqlExecutionRequest(EditorDocumentId.New(), "select 1")));
    }

    [Fact]
    public void Disposed_instance_throws_on_UpdateTextFromView()
    {
        var document = CreateDocument();
        document.Dispose();
        Assert.Throws<ObjectDisposedException>(() => document.UpdateTextFromView("new text"));
    }

    [Fact]
    public void Disposed_instance_throws_on_SetLoadedText()
    {
        var document = CreateDocument();
        document.Dispose();
        Assert.Throws<ObjectDisposedException>(() => document.SetLoadedText("new text"));
    }

    [Fact]
    public void Disposed_instance_throws_on_MarkSaved()
    {
        var document = CreateDocument();
        document.Dispose();
        Assert.Throws<ObjectDisposedException>(() => document.MarkSaved());
    }

    [Fact]
    public void Disposed_instance_throws_on_UpdateEditorSelection()
    {
        var document = CreateDocument();
        document.Dispose();
        Assert.Throws<ObjectDisposedException>(() =>
            document.UpdateEditorSelection(0, 10, 5));
    }

    [Fact]
    public void Disposed_instance_throws_on_BuildExecutionRequest()
    {
        var document = CreateDocument();
        document.Dispose();
        Assert.Throws<ObjectDisposedException>(() =>
            document.BuildExecutionRequest());
    }

    [Fact]
    public void Disposed_instance_throws_on_SuppressExternalChanges()
    {
        var document = CreateDocument();
        document.Dispose();
        Assert.Throws<ObjectDisposedException>(() =>
            document.SuppressExternalChanges(TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public void Disposed_instance_throws_on_SetSavedPath()
    {
        var document = CreateDocument();
        document.Dispose();
        Assert.Throws<ObjectDisposedException>(() =>
            document.SetSavedPath(@"C:\test.sql"));
    }

    [Fact]
    public void Dispose_disposes_watch_registration()
    {
        var watchService = Substitute.For<IEditorFileWatchService>();
        var watchHandle = Substitute.For<IDisposable>();
        watchService.Watch(Arg.Any<string>(), Arg.Any<Action<EditorFileChange>>())
            .Returns(watchHandle);

        var document = new EditorDocumentViewModel(
            EditorDocumentId.New(),
            "Test",
            "select 1",
            filePath: @"C:\test.sql",
            connectionName: "TestConn",
            databaseName: "TestDb",
            keepConnectionOpen: false,
            continueOnError: false,
            watchService);

        document.Dispose();
        watchHandle.Received(1).Dispose();
    }
}
