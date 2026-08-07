using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JustyBase.Ai.Chat;
using JustyBase.Ai.Models;
using JustyBase.Ai.Ports;
using JustyBase.Ai.Services;

namespace JustData.ViewModels.Ai;

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
    private bool _synchronizingSessionSelection;

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
        _controller.AssistantMessageCompleted += (_, _) => RaiseUi(() => MessagesChanged?.Invoke());
        _controller.ToolConfirmationRequested += (_, _) => RaiseUi(() => MessagesChanged?.Invoke());
        _controller.StatusMessageChanged += (_, message) => RaiseUi(() => StatusMessage = message);

        foreach (var mode in ChatModeConfig.AllModes)
        {
            AvailableModes.Add(mode);
        }

        foreach (var (_, name) in _chatService.AvailableBackends)
        {
            AvailableBackends.Add(name);
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

    public List<string> AvailableBackends { get; } = [];

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
        RaiseUi(() => StatusMessage = "Connecting to AI provider...");
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
            StatusMessage = success ? "Connected" : $"Not connected: {_chatService.ConnectionError}";
        });
        RefreshCodexAccountState();

        if (success)
        {
            await RefreshModelsAsync();
        }
    }

    [RelayCommand]
    private async Task SwitchBackendAsync(string backendId)
    {
        if (string.IsNullOrWhiteSpace(backendId))
            return;

        RaiseUi(() => StatusMessage = "Switching backend...");
        var success = await _chatService.SwitchBackendAsync(backendId);
        RaiseUi(() =>
        {
            if (success)
            {
                IsConnected = true;
                IsCodexBackend = string.Equals(backendId, "codex", StringComparison.OrdinalIgnoreCase);
                IsEmbeddedBackend = string.Equals(backendId, "embedded", StringComparison.OrdinalIgnoreCase);
                StatusMessage = "Connected";
            }
            else
            {
                IsConnected = false;
                IsCodexBackend = false;
                IsEmbeddedBackend = false;
                StatusMessage = $"Failed: {_chatService.ConnectionError}";
            }
        });

        RefreshCodexAccountState();
        _settingsStore.Update(s => s.AiChatBackendId = backendId);

        if (success)
        {
            await RefreshModelsAsync();
        }
    }

    partial void OnSelectedBackendIdChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        _ = SwitchBackendCommand.ExecuteAsync(value);
    }

    [RelayCommand]
    private async Task SendAsync()
    {
        if (IsStreaming)
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
    }

    [RelayCommand]
    private void RemoveAttachment(ChatAttachment? attachment)
    {
        if (attachment is null)
            return;

        PendingAttachments.Remove(attachment);
        HasPendingAttachments = PendingAttachments.Count > 0;
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

        StatusMessage = "Finish sign-in in the browser, then click Sign in again to refresh the account.";
        RefreshCodexAccountState();
    }

    [RelayCommand]
    private async Task SignOutCodexAsync()
    {
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

    private async Task<bool> EnsureConnectedAsync()
    {
        if (IsConnected)
            return true;

        await ConnectAsync();
        return IsConnected;
    }

    private async Task RefreshModelsAsync()
    {
        AvailableModels.Clear();
        AvailableReasoningEfforts.Clear();
        try
        {
            var models = await _chatService.GetAvailableModelsAsync();
            foreach (var model in models)
            {
                if (!AvailableModels.Contains(model, StringComparer.OrdinalIgnoreCase))
                    AvailableModels.Add(model);
            }

            if (AvailableModels.Count == 0 && !string.IsNullOrWhiteSpace(SelectedModel))
            {
                AvailableModels.Add(SelectedModel);
            }

            if (ShowReasoningEffort)
            {
                var efforts = await _chatService.GetAvailableReasoningEffortsAsync(SelectedModel);
                AvailableReasoningEfforts.Clear();
                foreach (var effort in efforts)
                    AvailableReasoningEfforts.Add(effort);

                if (AvailableReasoningEfforts.Count == 0 && !string.IsNullOrWhiteSpace(SelectedReasoningEffort))
                    AvailableReasoningEfforts.Add(SelectedReasoningEffort);
            }
        }
        catch (Exception ex)
        {
            _logger.TrackError(ex, isCrash: false);
            StatusMessage = $"Failed to load models: {ex.Message}";
        }

        RaiseUi(() => OnPropertyChanged(nameof(AvailableModels)));
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
            ? string.IsNullOrWhiteSpace(account.Email)
                ? $"Signed in ({account.Plan ?? "ChatGPT"})"
                : account.Email
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
