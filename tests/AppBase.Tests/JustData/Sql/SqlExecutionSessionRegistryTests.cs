using JustData.Application.Editor;
using JustyBaseLegacy.UI.Sql;

namespace AppBase.Tests.JustData.Sql;

public sealed class SqlExecutionSessionRegistryTests
{
    [Fact]
    public async Task A_document_can_only_have_one_active_session_and_can_be_reused_after_completion()
    {
        using var registry = new SqlExecutionSessionRegistry();
        EditorDocumentId documentId = EditorDocumentId.New();

        Assert.True(registry.TryStart(documentId, "main", out ISqlExecutionSession first));
        Assert.False(registry.TryStart(documentId, "main", out _));

        bool providerAbortCalled = false;
        first.SetProviderAbort(() =>
        {
            providerAbortCalled = true;
            return Task.CompletedTask;
        });
        await registry.CancelAsync(documentId);

        Assert.True(first.IsCancelling);
        Assert.True(providerAbortCalled);

        registry.Complete(documentId);
        Assert.False(registry.TryGet(documentId, out _));
        Assert.True(registry.TryStart(documentId, "main", out _));
    }

    [Fact]
    public void Cleanup_is_idempotent_for_a_closed_document()
    {
        using var registry = new SqlExecutionSessionRegistry();
        EditorDocumentId documentId = EditorDocumentId.New();
        Assert.True(registry.TryStart(documentId, "main", out _));

        registry.Cleanup(documentId);
        registry.Cleanup(documentId);

        Assert.False(registry.TryGet(documentId, out _));
    }

    [Fact]
    public async Task Cancellation_marker_is_consumed_once_and_cleared_for_the_next_session()
    {
        using var registry = new SqlExecutionSessionRegistry();
        EditorDocumentId documentId = EditorDocumentId.New();
        Assert.True(registry.TryStart(documentId, "main", out _));

        await registry.CancelAsync(documentId);
        registry.Complete(documentId);

        Assert.True(registry.TryConsumeCancellation(documentId));
        Assert.False(registry.TryConsumeCancellation(documentId));
        Assert.True(registry.TryStart(documentId, "main", out _));
        Assert.False(registry.TryConsumeCancellation(documentId));
    }
}
