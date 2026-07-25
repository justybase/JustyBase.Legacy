using JustData.Application;
using JustData.Application.Communication;

namespace JustyBaseLegacy.UI.Windowing;

/// <summary>Dispatches validated inter-process open requests to the workspace thread.</summary>
public sealed class ExternalOpenRequestRouter : IExternalOpenRequestRouter, IDisposable
{
    private IUiDispatcher? _dispatcher;
    private Action<ExternalOpenRequest>? _workspaceHandler;
    private bool _disposed;

    public void SetDispatcher(IUiDispatcher dispatcher)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
    }

    public void SetWorkspaceHandler(Action<ExternalOpenRequest> handler)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _workspaceHandler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    public Task RouteAsync(ExternalOpenRequest request, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(request);
        Action<ExternalOpenRequest> handler = _workspaceHandler
            ?? throw new InvalidOperationException("The external-open workspace handler is not configured.");

        if (_dispatcher is null || _dispatcher.CheckAccess())
        {
            handler(request);
            return Task.CompletedTask;
        }

        return _dispatcher.InvokeAsync(() => handler(request), cancellationToken);
    }

    public void Dispose()
    {
        _disposed = true;
        _dispatcher = null;
        _workspaceHandler = null;
    }
}
