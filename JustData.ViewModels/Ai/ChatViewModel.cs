using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JustyBase.Ai.Chat;
using JustyBase.Ai.Models;
using JustyBase.Ai.Ports;
using JustyBase.Ai.Services;

namespace JustData.ViewModels.Ai;

/// <summary>Backend pair carrying both the machine id and the display name for ComboBox binding.</summary>
public sealed record ChatBackendOption(string Id, string DisplayName);

/// <summary>
/// WinForms-host chat view model over the shared <see cref="ChatSessionController"/>.
/// Pure UI-agnostic layer — the panel binds to its properties and commands.
/// </summary>
public sealed partial class ChatViewModel : ObservableObject
{
    private readonly ChatSessionController _controller;
    private readonly ICopilotChatService _chatService;
    private readonly IChatSettingsStore _settingsStore;
    private readonly IUiDispatcher _dispatcher;
    private readonly ISimpleLogger _logger;
    private readonly SemaphoreSlim _initializeGate = new(1, 1);
    private readonly SemaphoreSlim _backendSwitchGate = new(1, 1);
    private CancellationTokenSource? _codexLoginCts;
    private CancellationTokenSource? _backendSwitchCts;
    private int _modelsRefreshVersion;
    private bool _synchronizingSessionSelection;
    private bool _constructed;

    public ChatViewModel(
        ChatSessionController controller,
        ICopilotChatService chatService,
        IChatSettingsStore settingsStore,
        IUiDispatcher dispatcher,
        ISimpleLogger logger)
    {
        _controller = controller;
        _chatService = chatService;
        _settingsStore = settingsStore;
        _dispatcher = dispatcher;
        _logger = logger;

        _controller.SessionChanged += (_, _) => RaiseUi(() =>
        {
            MessagesChanged?.Invoke();
            OnPropertyChanged(nameof(CurrentSessionTitle));
            RefreshSavedSessions();
        });
        _controller.SessionsChanged += (_, _) => RaiseUi(() => RefreshSavedSessions());
        _controller.StreamingChanged += (_, streaming) => RaiseUi(() =>
        {
            IsStreaming = streaming;
            MessagesChanged?.Invoke();
        });
        _controller.UserMessageAdded += (_, _) => RaiseUi(() => MessagesChanged?.Invoke());
        _controller.AssistantMessageStarted += (_, _) => RaiseUi(() => MessagesChanged?.Invoke());
        _controller.AssistantMessageContentChanged += (_, _) => RaiseUi(() => MessagesChanged?.Invoke());
        _controller.AssistantMessageCompleted += (_, _) => RaiseUi(() => MessagesChanged?.Invoke());
        _controller.ToolConfirmationRequested += (_, _) => RaiseUi(() => MessagesChanged?.Invoke());
        _controller.StatusMessageChanged += (_, message) => RaiseUi(() => StatusMessage = message);

        foreach (var mode in ChatModeConfig.AllModes)
        {
            AvailableModes.Add(mode);
        }

        foreach (var (id, displayName) in _chatService.AvailableBackends)
        {
            AvailableBackends.Add(new ChatBackendOption(id, displayName));
        }

        var settings = _settingsStore.Settings;
        SelectedBackendId = settings.AiChatBackendId;
        SelectedModel = string.IsNullOrWhiteSpace(settings.AiChatDefaultModel)
            ? "gpt-5.6-luna"
            : settings.AiChatDefaultModel;
        SelectedReasoningEffort = string.IsNullOrWhiteSpace(settings.AiChatDefaultReasoningEffort)
            ? "low"
            : settings.AiChatDefaultReasoningEffort;
        CurrentMode = ChatModeExtensions.FromSlug(settings.AiChatDefaultMode);

        RefreshSavedSessions();
        RefreshCodexAccountState();

        // Probe account/read once at startup so an already-signed-in session is detected
        // without waiting for the user to click the Codex button.
        RefreshCodexCommand.Execute(null);

        // The embedded model list is a pure disk probe — populate the dropdown right
        // away so downloaded models are visible even before the backend connects.
        _ = RefreshModelsAsync();
        _constructed = true;
    }

    /// <summary>
    /// Connects when auto-connect is enabled. Must be invoked AFTER the host window is fully
    /// constructed (the dispatcher resolves the window lazily, so calling this from the
    /// constructor while BaseWindow is still being built would hit a DI circular dependency).
    /// </summary>
    public void AutoConnectIfEnabled()
    {
        if (!_settingsStore.Settings.AiChatAutoConnect)
            return;

        _ = ConnectAsync();
    }

    /// <summary>Raised when the message list content needs a UI re-render.</summary>
    public event Action? MessagesChanged;
    public event Action? AttachmentsChanged;

    public List<ChatMessage> Messages => _controller.Messages;

    public ChatSession CurrentSession => _controller.CurrentSession;

    public string CurrentSessionTitle => string.IsNullOrWhiteSpace(CurrentSession.Title) ? "New Chat" : CurrentSession.Title;

    public IReadOnlyList<ChatSession> SavedSessions => _controller.SavedSessions;

    [ObservableProperty]
    public partial string InputText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsConnected { get; set; }

    [ObservableProperty]
    public partial bool IsStreaming { get; set; }

    /// <summary>True while the AI provider is connecting for the first time (slow embedded model launch).</summary>
    [ObservableProperty]
    public partial bool IsInitializing { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "Initializing...";

    [ObservableProperty]
    public partial string CodexAccountLabel { get; set; } = "Not signed in";

    [ObservableProperty]
    public partial bool IsCodexSignedIn { get; set; }

    [ObservableProperty]
    public partial ChatMode CurrentMode { get; set; } = ChatMode.Expert;

    public List<ChatModeConfig> AvailableModes { get; } = [];

    [ObservableProperty]
    public partial string SelectedBackendId { get; set; } = "codex";

    public List<ChatBackendOption> AvailableBackends { get; } = [];

    [ObservableProperty]
    public partial string SelectedModel { get; set; } = string.Empty;

    public List<string> AvailableModels { get; } = [];

    [ObservableProperty]
    public partial string SelectedReasoningEffort { get; set; } = "low";

    public List<string> AvailableReasoningEfforts { get; } = [];

    [ObservableProperty]
    public partial bool IsCodexBackend { get; set; }

    [ObservableProperty]
    public partial bool IsEmbeddedBackend { get; set; }

    /// <summary>Reasoning effort is available for Codex and the embedded llama-server (Qwen3-style thinking).</summary>
    public bool ShowReasoningEffort => IsCodexBackend || IsEmbeddedBackend;

    public List<ChatAttachment> PendingAttachments { get; } = [];

    [ObservableProperty]
    public partial bool HasPendingAttachments { get; set; }

    public void AttachHostProviders(
        Func<string?> currentSqlProvider,
        Func<(string FullText, string SelectedText, int SelectionStart, int SelectionLength, int CaretOffset)?> sqlEditorContextProvider,
        Func<string, bool> sqlEditorBufferUpdater,
        Func<(string ConnectionName, string DatabaseName)?> activeSqlContextProvider)
    {
        _controller.AttachHostProviders(
            currentSqlProvider,
            sqlEditorContextProvider,
            sqlEditorBufferUpdater,
            activeSqlContextProvider);
    }

    [RelayCommand]
    private async Task ConnectAsync()
    {
        RaiseUi(() =>
        {
            IsInitializing = true;
            StatusMessage = "Preparing the AI provider — first launch of the embedded model may take a few seconds…";
        });

        try
        {
            var settings = _settingsStore.Settings;
            var configured = settings.AiChatBackendId;
            var hasConfigured = !string.IsNullOrWhiteSpace(configured)
                && _chatService.AvailableBackends.Any(b => b.Id.Equals(configured, StringComparison.OrdinalIgnoreCase));

            var success = hasConfigured
                ? await _chatService.SwitchBackendAsync(configured)
                : await _chatService.InitializeAsync();

            RaiseUi(() =>
            {
                IsConnected = success;
                IsCodexBackend = success
                    && string.Equals(_chatService.ActiveBackendId, "codex", StringComparison.OrdinalIgnoreCase);
                IsEmbeddedBackend = success
                    && string.Equals(_chatService.ActiveBackendId, "embedded", StringComparison.OrdinalIgnoreCase);
                StatusMessage = success
                    ? $"Connected to {GetBackendDisplayName(_chatService.ActiveBackendId)}"
                    : $"Not connected: {_chatService.ConnectionError}";
            });
            RefreshCodexAccountState();

            if (success)
            {
                await RefreshModelsAsync();
            }
        }
        finally
        {
            RaiseUi(() => IsInitializing = false);
        }
    }

    [RelayCommand]
    private async Task SwitchBackendAsync(string backendId)
    {
        if (string.IsNullOrWhiteSpace(backendId))
            return;

        var switchCts = new CancellationTokenSource();
        var previousSwitchCts = Interlocked.Exchange(ref _backendSwitchCts, switchCts);
        previousSwitchCts?.Cancel();

        await _backendSwitchGate.WaitAsync();
        try
        {
            if (switchCts.IsCancellationRequested)
                return;

            RaiseUi(() => StatusMessage = "Switching backend...");
            var success = await _chatService.SwitchBackendAsync(backendId);
            if (switchCts.IsCancellationRequested)
                return;

            if (success)
            {
                IsConnected = true;
                IsCodexBackend = string.Equals(backendId, "codex", StringComparison.OrdinalIgnoreCase);
                IsEmbeddedBackend = string.Equals(backendId, "embedded", StringComparison.OrdinalIgnoreCase);
                StatusMessage = $"Connected to {GetBackendDisplayName(backendId)}";

                await RefreshModelsAsync();
                _settingsStore.Update(s => s.AiChatBackendId = backendId);
            }
            else
            {
                IsConnected = false;
                IsCodexBackend = false;
                IsEmbeddedBackend = false;
                StatusMessage = $"Failed: {_chatService.ConnectionError}";
                ClearModelLists();
            }

            RaiseUi(RefreshCodexAccountState);
        }
        catch (OperationCanceledException) when (switchCts.IsCancellationRequested)
        {
            // A newer backend selection superseded this switch.
        }
        catch (Exception ex)
        {
            _logger.TrackError(ex, isCrash: false);
            if (!switchCts.IsCancellationRequested)
            {
                IsConnected = false;
                IsCodexBackend = false;
                IsEmbeddedBackend = false;
                StatusMessage = $"Failed: {ex.Message}";
                ClearModelLists();
            }
        }
        finally
        {
            _backendSwitchGate.Release();
            if (ReferenceEquals(_backendSwitchCts, switchCts))
                _backendSwitchCts = null;
            switchCts.Dispose();
        }
    }

    partial void OnSelectedBackendIdChanged(string value)
    {
        if (!_constructed || string.IsNullOrWhiteSpace(value))
            return;

        _ = SwitchBackendCommand.ExecuteAsync(value);
    }

    [RelayCommand]
    private async Task SendAsync()
    {
        if (IsStreaming || IsInitializing)
            return;

        if (string.IsNullOrWhiteSpace(InputText) && PendingAttachments.Count == 0)
            return;

        if (!IsConnected && !await EnsureConnectedAsync())
        {
            StatusMessage = $"AI provider not connected: {_chatService.ConnectionError}";
            return;
        }

        var prompt = InputText;
        var attachments = PendingAttachments.ToList();
        PendingAttachments.Clear();
        HasPendingAttachments = false;
        AttachmentsChanged?.Invoke();

        var reasoningEffort = ShowReasoningEffort && !string.IsNullOrWhiteSpace(SelectedReasoningEffort)
            ? SelectedReasoningEffort
            : null;

        await _controller.SendMessageAsync(prompt, attachments, SelectedModel, reasoningEffort);

        if (!string.IsNullOrWhiteSpace(prompt))
        {
            InputText = string.Empty;
        }
    }

    [RelayCommand]
    private async Task CancelAsync()
        => await _controller.CancelStreamingAsync();

    [RelayCommand]
    private void NewSession()
    {
        _controller.NewSession();
        RaiseUi(() => MessagesChanged?.Invoke());
    }

    [RelayCommand]
    private void OpenSession(ChatSession? session)
    {
        _controller.OpenSession(session);
        RaiseUi(() => MessagesChanged?.Invoke());
    }

    [RelayCommand]
    private void DeleteSession(ChatSession? session)
    {
        _controller.DeleteSession(session);
        RaiseUi(() => MessagesChanged?.Invoke());
    }

    [RelayCommand]
    private void ConfirmTool(bool allow)
        => _controller.ConfirmTool(allow);

    public void AddAttachment(string? path, bool isDirectory)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(path);
        }
        catch
        {
            return;
        }

        bool exists = isDirectory ? Directory.Exists(fullPath) : File.Exists(fullPath);
        if (!exists)
        {
            StatusMessage = $"Path not found: {fullPath}";
            return;
        }

        if (PendingAttachments.Any(a => a.Path.Equals(fullPath, StringComparison.OrdinalIgnoreCase)))
            return;

        PendingAttachments.Add(new ChatAttachment
        {
            Path = fullPath,
            DisplayName = isDirectory
                ? new DirectoryInfo(fullPath).Name
                : Path.GetFileName(fullPath),
            IsDirectory = isDirectory
        });
        HasPendingAttachments = PendingAttachments.Count > 0;
        AttachmentsChanged?.Invoke();
    }

    [RelayCommand]
    private void RemoveAttachment(ChatAttachment? attachment)
    {
        if (attachment is null)
            return;

        PendingAttachments.Remove(attachment);
        HasPendingAttachments = PendingAttachments.Count > 0;
        AttachmentsChanged?.Invoke();
    }

    [RelayCommand]
    private async Task SignInCodexAsync()
    {
        StatusMessage = "Opening ChatGPT sign-in in your browser...";
        var started = await _chatService.StartCodexLoginAsync();
        if (!started)
        {
            StatusMessage = $"Codex sign-in failed: {_chatService.ConnectionError ?? "app-server unavailable"}";
            return;
        }

        _codexLoginCts?.Cancel();
        _codexLoginCts?.Dispose();
        _codexLoginCts = new CancellationTokenSource();
        var cts = _codexLoginCts;

        StatusMessage = "Finish sign-in in the browser. Waiting for confirmation...";
        for (var attempt = 0; attempt < 120 && !cts.IsCancellationRequested; attempt++)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cts.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            CodexAccountInfo? account = null;
            try
            {
                account = await _chatService.ReadCodexAccountAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.TrackError(ex, isCrash: false);
            }

            RaiseUi(RefreshCodexAccountState);
            if (account?.IsAuthenticated == true)
            {
                StatusMessage = "Signed in to ChatGPT.";
                return;
            }
        }

        if (cts.IsCancellationRequested)
        {
            StatusMessage = "Codex sign-in was cancelled.";
        }
        else
        {
            StatusMessage = "Sign-in window opened. Finish sign-in, then click Codex sign in again to refresh the account.";
        }
    }

    [RelayCommand]
    private async Task SignOutCodexAsync()
    {
        _codexLoginCts?.Cancel();
        var loggedOut = await _chatService.LogoutCodexAsync();
        RefreshCodexAccountState();
        StatusMessage = loggedOut ? "Signed out from Codex." : "Could not sign out from Codex.";
        if (string.Equals(_chatService.ActiveBackendId, "codex", StringComparison.OrdinalIgnoreCase))
        {
            IsConnected = false;
            IsCodexBackend = false;
        }
    }

    [RelayCommand]
    private void RefreshCodex()
    {
        _ = _chatService.ReadCodexAccountAsync().ContinueWith(_ =>
        {
            RaiseUi(RefreshCodexAccountState);
        }, TaskScheduler.Default);
    }

    private string GetBackendDisplayName(string? backendId)
    {
        if (string.IsNullOrWhiteSpace(backendId))
            return backendId ?? string.Empty;

        var match = _chatService.AvailableBackends
            .FirstOrDefault(b => b.Id.Equals(backendId, StringComparison.OrdinalIgnoreCase));
        return string.IsNullOrWhiteSpace(match.DisplayName) ? backendId : match.DisplayName;
    }

    private async Task<bool> EnsureConnectedAsync()
    {
        if (IsConnected)
            return true;

        // Serialize lazy initialization so repeated sends during a slow first launch
        // (e.g. starting the embedded llama-server) do not spawn parallel connects.
        await _initializeGate.WaitAsync();
        try
        {
            if (IsConnected)
                return true;

            await ConnectAsync();
            return IsConnected;
        }
        finally
        {
            _initializeGate.Release();
        }
    }

    private async Task RefreshModelsAsync()
    {
        var refreshVersion = Interlocked.Increment(ref _modelsRefreshVersion);
        var activeBackendId = _chatService.ActiveBackendId;
        AvailableModels.Clear();
        AvailableReasoningEfforts.Clear();
        try
        {
            var models = await _chatService.GetAvailableModelsAsync();

            // An older probe may finish after a backend switch. Never let its result
            // put the previous backend's models back into the combo.
            if (refreshVersion != Volatile.Read(ref _modelsRefreshVersion)
                || !string.Equals(activeBackendId, _chatService.ActiveBackendId, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            foreach (var model in models)
            {
                if (!AvailableModels.Contains(model, StringComparer.OrdinalIgnoreCase))
                    AvailableModels.Add(model);
            }

            if (AvailableModels.Count == 0 && !string.IsNullOrWhiteSpace(SelectedModel))
            {
                AvailableModels.Add(SelectedModel);
            }

            if (AvailableModels.Count > 0
                && !AvailableModels.Contains(SelectedModel, StringComparer.OrdinalIgnoreCase))
            {
                SelectedModel = AvailableModels[0];
            }

            if (ShowReasoningEffort)
            {
                var efforts = await _chatService.GetAvailableReasoningEffortsAsync(SelectedModel);

                if (refreshVersion != Volatile.Read(ref _modelsRefreshVersion)
                    || !string.Equals(activeBackendId, _chatService.ActiveBackendId, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                AvailableReasoningEfforts.Clear();
                foreach (var effort in efforts)
                    AvailableReasoningEfforts.Add(effort);

                if (AvailableReasoningEfforts.Count == 0 && !string.IsNullOrWhiteSpace(SelectedReasoningEffort))
                    AvailableReasoningEfforts.Add(SelectedReasoningEffort);

                if (AvailableReasoningEfforts.Count > 0
                    && !AvailableReasoningEfforts.Contains(SelectedReasoningEffort, StringComparer.OrdinalIgnoreCase))
                {
                    SelectedReasoningEffort = AvailableReasoningEfforts[0];
                }
            }
        }
        catch (Exception ex)
        {
            _logger.TrackError(ex, isCrash: false);
            StatusMessage = $"Failed to load models: {ex.Message}";
        }

        RaiseUi(() => OnPropertyChanged(nameof(AvailableModels)));
    }

    private void ClearModelLists()
    {
        Interlocked.Increment(ref _modelsRefreshVersion);
        AvailableModels.Clear();
        AvailableReasoningEfforts.Clear();
        RaiseUi(() =>
        {
            OnPropertyChanged(nameof(AvailableModels));
            OnPropertyChanged(nameof(AvailableReasoningEfforts));
        });
    }

    private void RefreshSavedSessions()
    {
        _synchronizingSessionSelection = true;
        try
        {
            OnPropertyChanged(nameof(SavedSessions));
            OnPropertyChanged(nameof(CurrentSessionTitle));
        }
        finally
        {
            _synchronizingSessionSelection = false;
        }
    }

    private void RefreshCodexAccountState()
    {
        var account = _chatService.CodexAccount;
        IsCodexSignedIn = account?.IsAuthenticated == true;
        CodexAccountLabel = account?.IsAuthenticated == true
            ? "Signed in"
            : "Not signed in";
    }

    private void RaiseUi(Action action)
    {
        if (_dispatcher.CheckAccess())
        {
            action();
        }
        else
        {
            _ = _dispatcher.InvokeAsync(action);
        }
    }
}
