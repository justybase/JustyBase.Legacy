using System.ComponentModel;
using JustData.Mvvm;

namespace JustData.UiTests;

public sealed class WindowsFormsUiDispatcherTests
{
    [Fact]
    public async Task InvokeAsync_completes_with_lifecycle_error_when_control_is_disposed()
    {
        var dispatcher = new WindowsFormsUiDispatcher(new DisposedSynchronizer());

        await Assert.ThrowsAsync<ObjectDisposedException>(() => dispatcher.InvokeAsync(() => { }));
    }

    private sealed class DisposedSynchronizer : ISynchronizeInvoke
    {
        public bool InvokeRequired => true;

        public IAsyncResult BeginInvoke(Delegate method, object?[]? args) =>
            throw new ObjectDisposedException(nameof(DisposedSynchronizer));

        public object? EndInvoke(IAsyncResult result) => null;

        public object? Invoke(Delegate method, object?[]? args) => null;
    }
}
