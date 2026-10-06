using DirectN;
using DirectN.Extensions.Com;
using System.Reflection;
using WebView2;
using WebView2.Utilities;

namespace JustyBaseLegacy.UI.Ai;

/// <summary>
/// Hosts the system WebView2 Runtime in a WinForms child window using the
/// AOT-compatible WebView2Aot bindings (no classic COM interop, no
/// Microsoft.Web.WebView2 WinForms control). Failures are reported through
/// <see cref="Failed"/> so the caller can fall back to plain text.
/// </summary>
internal sealed class WebView2AotHost : Control
{
    private readonly string _userDataFolder;
    private IComObject<ICoreWebView2Controller>? _controller;
    private IComObject<ICoreWebView2>? _webView;
    private CoreWebView2Events? _events;
    private string? _pendingHtml;
    private bool _initializing;

    public WebView2AotHost(string userDataFolder)
    {
        _userDataFolder = userDataFolder;
        Dock = DockStyle.Fill;
    }

    public bool IsReady { get; private set; }

    public event EventHandler? Ready;

    public event EventHandler<Exception>? Failed;

    public event EventHandler<string>? MessageReceived;

    public void NavigateToString(string html)
    {
        _pendingHtml = html;

        var webView = _webView;
        if (webView is null || IsDisposed)
        {
            return;
        }

        try
        {
            webView.NavigateToString(html);
        }
        catch (Exception exception)
        {
            Failed?.Invoke(this, exception);
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        _ = InitializeAsync();
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        Teardown();
        base.OnHandleDestroyed(e);
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        UpdateControllerBounds();
    }

    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        try
        {
            _controller?.MoveFocus(COREWEBVIEW2_MOVE_FOCUS_REASON.COREWEBVIEW2_MOVE_FOCUS_REASON_PROGRAMMATIC, false);
        }
        catch
        {
            // focus forwarding is best effort
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Teardown();
        }

        base.Dispose(disposing);
    }

    private async Task InitializeAsync()
    {
        if (_initializing || IsReady || IsDisposed)
        {
            return;
        }

        _initializing = true;
        try
        {
            // Extracts WebView2Loader.dll from the embedded resource on first use.
            WebView2Utilities.Initialize(Assembly.GetEntryAssembly());
            if (string.IsNullOrWhiteSpace(WebView2Utilities.GetAvailableCoreWebView2BrowserVersionString()))
            {
                throw new InvalidOperationException("Microsoft Edge WebView2 Runtime is not installed.");
            }

            Directory.CreateDirectory(_userDataFolder);
            using var environment = await WebView2.Functions
                .CreateCoreWebView2EnvironmentWithOptionsAsync(null, _userDataFolder, null)
                .ConfigureAwait(true)
                ?? throw new InvalidOperationException("Could not create the WebView2 environment.");

            _controller = await environment.CreateCoreWebView2ControllerAsync(Handle).ConfigureAwait(true)
                ?? throw new InvalidOperationException("Could not create the WebView2 controller.");
            UpdateControllerBounds();

            _webView = _controller.CoreWebView2
                ?? throw new InvalidOperationException("Could not create the WebView2 instance.");

            using (var settings = _webView.Settings)
            {
                if (settings is not null)
                {
                    settings.IsBuiltInErrorPageEnabled = false;
                    settings.AreDefaultContextMenusEnabled = false;
                    settings.IsStatusBarEnabled = false;
                    settings.AreBrowserAcceleratorKeysEnabled = false;
                }
            }

            _events = new CoreWebView2Events(_webView);
            _events.WebMessageReceived += OnWebMessageReceived;

            IsReady = true;
            if (_pendingHtml is { } html)
            {
                _webView.NavigateToString(html);
            }

            Ready?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception exception)
        {
            Teardown();
            Failed?.Invoke(this, exception);
        }
        finally
        {
            _initializing = false;
        }
    }

    private void OnWebMessageReceived(object? sender, ICoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            var payload = e.TryGetWebMessageAsString();
            if (!string.IsNullOrEmpty(payload))
            {
                MessageReceived?.Invoke(this, payload);
            }
        }
        catch
        {
            // malformed message payloads are ignored
        }
    }

    private void UpdateControllerBounds()
    {
        var controller = _controller;
        if (controller is null || IsDisposed)
        {
            return;
        }

        try
        {
            controller.Bounds = new RECT { left = 0, top = 0, right = ClientSize.Width, bottom = ClientSize.Height };
        }
        catch
        {
            // resizing during teardown can race with controller disposal
        }
    }

    private void Teardown()
    {
        IsReady = false;

        if (_events is not null)
        {
            _events.WebMessageReceived -= OnWebMessageReceived;
            _events.Dispose();
            _events = null;
        }

        Interlocked.Exchange(ref _webView, null)?.Dispose();

        var controller = Interlocked.Exchange(ref _controller, null);
        if (controller is not null)
        {
            try
            {
                controller.Object.Close();
            }
            catch
            {
                // controller may already be closed by parent window teardown
            }

            controller.Dispose();
        }
    }
}
