using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JustData.Application.Settings;
using JustyBase.Ai.Chat;
using JustyBase.Ai.Models;
using JustyBase.Ai.Ports;
using JustyBase.Ai.Services;

namespace JustData.ViewModels.Ai;

/// <summary>Backend pair carrying both the machine id and the display name for ComboBox binding.</summary>
public sealed record ChatBackendOption(string Id, string DisplayName)
{
    // WinForms ComboBox.DisplayMember uses reflection, which is disabled under
    // trimming/Native AOT. ToString() keeps the display name without binding.
    public override string ToString() => DisplayName;
}

/// <summary>
/// WinForms-host chat view model over the shared <see cref="ChatSessionController"/>.
/// Pure UI-agnostic layer — the panel binds to its properties and commands.
/// </summary>
public sealed partial class ChatViewModel : ObservableObject, IDisposable
{
    private readonly ChatSessionController _controller;
    private readonly ICopilotChatService _chatService;
    private readonly IChatSettingsStore _settingsStore;
    private readonly IUiDispatcher _dispatcher;
    private readonly ISimpleLogger _logger;
    private readonly IChatProviderModelSettingsStore? _providerModelSettings;
    private readonly SemaphoreSlim _initializeGate = new(1, 1);
    private readonly SemaphoreSlim _backendSwitchGate = new(1, 1);
    private CancellationTokenSource? _codexLoginCts;
    private CancellationTokenSource? _copilotLoginCts;
    private bool _copilotCodeRaised;
    private CancellationTokenSource? _backendSwitchCts;
    private int _modelsRefreshVersion;
    private int _startupAccountProbesStarted;
    private bool _constructed;
    private bool _disposed;
    private bool _suppressBackendSelection;
    private readonly Action<string, string> _copilotVerificationHandler;

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
        _providerModelSettings = settingsStore as IChatProviderModelSettingsStore;

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

        _copilotVerificationHandler = (code, url) =>
        {
            _copilotCodeRaised = true;
            RaiseUi(() => CopilotVerificationCodeAvailable?.Invoke(code, url));
        };
        SubscribeCopilotVerificationHandler();

        foreach (var mode in ChatModeConfig.AllModes)
        {
            AvailableModes.Add(mode);
        }

        foreach (var (id, displayName) in _chatService.AvailableBackends)
        {
            AvailableBackends.Add(new ChatBackendOption(id, displayName));
        }

        var settings = _settingsStore.Settings;
        SelectedBackendId = NormalizeBackendId(settings.AiChatBackendId);
        SelectedModel = GetPreferredModel(SelectedBackendId);
        SelectedReasoningEffort = string.IsNullOrWhiteSpace(settings.AiChatDefaultReasoningEffort)
            ? "low"
            : settings.AiChatDefaultReasoningEffort;
        CurrentMode = ChatModeExtensions.FromSlug(settings.AiChatDefaultMode);

        RefreshSavedSessions();
        RefreshCodexAccountState();
        RefreshCopilotAccountState();

        // Do not probe models until a provider is active. Before connection the service
        // cannot identify a provider and its local discovery path may return unrelated
        // local models for a selected Codex/Copilot provider.
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

    /// <summary>
    /// Starts the initial account probes after the host window has been constructed.
    /// The dispatcher resolves that window lazily, so doing this from the constructor
    /// can re-enter dependency injection while BaseWindow is still being created.
    /// </summary>
    public void StartStartupAccountProbes()
    {
        if (Interlocked.Exchange(ref _startupAccountProbesStarted, 1) != 0)
            return;

        RefreshCodexCommand.Execute(null);
        RefreshCopilotCommand.Execute(null);
    }

    /// <summary>Raised when the message list content needs a UI re-render.</summary>
    public event Action? MessagesChanged;
    public event Action? AttachmentsChanged;

    /// <summary>
    /// Raised once per Copilot sign-in attempt when the device-flow verification code is
    /// known. The host surfaces it in a dialog (the code is not visible anywhere else).
    /// </summary>
    public event Action<string, string>? CopilotVerificationCodeAvailable;

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

    /// <summary>True while the model list for the active provider is being refreshed.</summary>
    [ObservableProperty]
    public partial bool IsModelLoading { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "Initializing...";

    [ObservableProperty]
    public partial string CodexAccountLabel { get; set; } = "Not signed in";

    [ObservableProperty]
    public partial bool IsCodexSignedIn { get; set; }

    [ObservableProperty]
    public partial string CopilotAccountLabel { get; set; } = "Not signed in";

    [ObservableProperty]
    public partial bool IsCopilotSignedIn { get; set; }

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
    public partial bool IsCopilotBackend { get; set; }

    [ObservableProperty]
    public partial bool IsEmbeddedBackend { get; set; }

    /// <summary>Provider whose last switch failed and may still need authentication.</summary>
    [ObservableProperty]
    public partial string? FailedBackendId { get; set; }

    /// <summary>Reasoning effort is available for Codex, Copilot and the embedded llama-server (Qwen3-style thinking).</summary>
    public bool ShowReasoningEffort => IsCodexBackend || IsCopilotBackend || IsEmbeddedBackend;

    /// <summary>True only when the selected model belongs to the active provider.</summary>
    public bool IsModelReady
        => IsConnected
            && !IsInitializing
            && !IsModelLoading
            && !string.IsNullOrWhiteSpace(_chatService.ActiveBackendId)
            && AvailableModels.Contains(SelectedModel, StringComparer.OrdinalIgnoreCase);

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
            IsModelLoading = true;
            AvailableModels.Clear();
            AvailableReasoningEfforts.Clear();
            SelectedModel = string.Empty;
            OnPropertyChanged(nameof(AvailableModels));
            OnPropertyChanged(nameof(AvailableReasoningEfforts));
            OnPropertyChanged(nameof(IsModelReady));
            StatusMessage = "Preparing the AI provider — first launch of the embedded model may take a few seconds…";
        });

        // A manual provider switch can happen while auto-connect is still probing a
        // backend. Keep both paths serialized so the service cannot finish on one
        // provider while the UI is already showing another one.
        await _backendSwitchGate.WaitAsync();
        try
        {
            var settings = _settingsStore.Settings;
            var configured = settings.AiChatBackendId;
            var hasConfigured = !string.IsNullOrWhiteSpace(configured)
                && _chatService.AvailableBackends.Any(b => b.Id.Equals(configured, StringComparison.OrdinalIgnoreCase));

            var success = hasConfigured
                ? await _chatService.SwitchBackendAsync(configured)
                : await _chatService.InitializeAsync();

            var actualBackendId = success
                ? _chatService.ActiveBackendId
                : _chatService.IsConnected ? _chatService.ActiveBackendId : null;
            var connected = success || (actualBackendId is not null && _chatService.IsConnected);

            RaiseUi(() =>
            {
                IsConnected = connected;
                ApplyActiveBackendState(connected ? actualBackendId : null);
                StatusMessage = success
                    ? $"Connected to {GetBackendDisplayName(actualBackendId)}"
                    : $"Not connected: {_chatService.ConnectionError}";
            });
            RefreshCodexAccountState();
            RefreshCopilotAccountState();

            if (connected && actualBackendId is not null)
            {
                SetSelectedBackendSilently(actualBackendId);
                _settingsStore.Update(s =>
                {
                    s.AiChatBackendId = actualBackendId;
                    s.EnableEmbeddedChatAi = string.Equals(actualBackendId, "embedded", StringComparison.OrdinalIgnoreCase);
                });
                await RefreshModelsAsync(actualBackendId);
            }
            else
            {
                ClearModelLists();
            }
        }
        finally
        {
            RaiseUi(() =>
            {
                IsInitializing = false;
                if (!IsModelLoading)
                    OnPropertyChanged(nameof(IsModelReady));
            });
            _backendSwitchGate.Release();
        }
    }

    [RelayCommand]
    private async Task SwitchBackendAsync(string backendId)
    {
        if (string.IsNullOrWhiteSpace(backendId))
            return;

        var requestedBackendId = NormalizeBackendId(backendId);

        var switchCts = new CancellationTokenSource();
        var previousSwitchCts = Interlocked.Exchange(ref _backendSwitchCts, switchCts);
        previousSwitchCts?.Cancel();

        await _backendSwitchGate.WaitAsync();
        try
        {
            if (switchCts.IsCancellationRequested)
                return;

            FailedBackendId = null;

            // EmbeddedChatBackend uses the same provider decision as its activation gate.
            // Persist it before the switch so selecting Embedded from the chat header can
            // start the local server even when the legacy flag was previously false.
            _settingsStore.Update(settings =>
            {
                settings.AiChatBackendId = requestedBackendId;
                settings.EnableEmbeddedChatAi = string.Equals(requestedBackendId, "embedded", StringComparison.OrdinalIgnoreCase);
            });

            RaiseUi(() =>
            {
                IsModelLoading = true;
                SelectedModel = string.Empty;
                AvailableModels.Clear();
                AvailableReasoningEfforts.Clear();
                OnPropertyChanged(nameof(AvailableModels));
                OnPropertyChanged(nameof(AvailableReasoningEfforts));
                OnPropertyChanged(nameof(IsModelReady));
                StatusMessage = "Switching provider...";
            });
            var success = await _chatService.SwitchBackendAsync(requestedBackendId);
            if (switchCts.IsCancellationRequested)
                return;

            if (success)
            {
                FailedBackendId = null;
                var activeBackendId = _chatService.ActiveBackendId;
                IsConnected = true;
                ApplyActiveBackendState(activeBackendId);
                SetSelectedBackendSilently(activeBackendId);
                StatusMessage = $"Connected to {GetBackendDisplayName(activeBackendId)}";

                await RefreshModelsAsync(activeBackendId);
                _settingsStore.Update(s =>
                {
                    s.AiChatBackendId = activeBackendId ?? requestedBackendId;
                    s.EnableEmbeddedChatAi = string.Equals(s.AiChatBackendId, "embedded", StringComparison.OrdinalIgnoreCase);
                });
            }
            else
            {
                // LocalChatService preserves its previous active backend when a switch
                // fails (for example, Codex is not signed in). Mirror that actual state
                // instead of displaying a provider/model combination that never became active.
                var activeBackendId = _chatService.IsConnected ? _chatService.ActiveBackendId : null;
                IsConnected = activeBackendId is not null && _chatService.IsConnected;
                ApplyActiveBackendState(activeBackendId);
                SetSelectedBackendSilently(activeBackendId);
                FailedBackendId = requestedBackendId;
                if (activeBackendId is not null)
                {
                    _settingsStore.Update(s =>
                    {
                        s.AiChatBackendId = activeBackendId;
                        s.EnableEmbeddedChatAi = string.Equals(activeBackendId, "embedded", StringComparison.OrdinalIgnoreCase);
                    });
                }
                StatusMessage = $"Failed: {_chatService.ConnectionError}";
                if (activeBackendId is not null && IsConnected)
                    await RefreshModelsAsync(activeBackendId);
                else
                    ClearModelLists();
            }

            RaiseUi(RefreshCodexAccountState);
            RaiseUi(RefreshCopilotAccountState);
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
                var activeBackendId = _chatService.IsConnected ? _chatService.ActiveBackendId : null;
                IsConnected = activeBackendId is not null && _chatService.IsConnected;
                ApplyActiveBackendState(activeBackendId);
                SetSelectedBackendSilently(activeBackendId);
                FailedBackendId = requestedBackendId;
                if (activeBackendId is not null)
                {
                    _settingsStore.Update(s =>
                    {
                        s.AiChatBackendId = activeBackendId;
                        s.EnableEmbeddedChatAi = string.Equals(activeBackendId, "embedded", StringComparison.OrdinalIgnoreCase);
                    });
                }
                StatusMessage = $"Failed: {ex.Message}";
                if (activeBackendId is not null && IsConnected)
                    await RefreshModelsAsync(activeBackendId);
                else
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
        if (!_constructed || _suppressBackendSelection || string.IsNullOrWhiteSpace(value))
            return;

        ClearModelLists();
        _ = SwitchBackendCommand.ExecuteAsync(value);
    }

    partial void OnSelectedModelChanged(string value)
    {
        OnPropertyChanged(nameof(IsModelReady));
        if (!_constructed || string.IsNullOrWhiteSpace(value))
            return;

        var providerId = _chatService.ActiveBackendId;
        if (string.IsNullOrWhiteSpace(providerId)
            || !AvailableModels.Contains(value, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        _providerModelSettings?.SetModelForProvider(providerId, value);
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

        SyncEmbeddedModelSelectionFromSettings();

        // The embedded provider owns first-use download/bootstrapping. Its
        // model list may still describe the previous discovery result when
        // Preferences changes EmbeddedChatModelId while this panel is open, so
        // let the provider validate and prepare the selected catalog model
        // before applying the ordinary downloaded-model membership check.
        if (IsEmbeddedBackend)
        {
            if (!await EnsureEmbeddedModelReadyAsync())
                return;
        }
        else if (!IsModelReady)
        {
            StatusMessage = IsModelLoading
                ? "Loading models for the selected provider..."
                : "Select a valid model for the selected provider before sending.";
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

    private void SyncEmbeddedModelSelectionFromSettings()
    {
        if (!IsEmbeddedBackend || _providerModelSettings is null)
            return;

        var configuredModel = _providerModelSettings.GetModelForProvider("embedded");
        if (!string.IsNullOrWhiteSpace(configuredModel)
            && !string.Equals(SelectedModel, configuredModel, StringComparison.OrdinalIgnoreCase))
        {
            // Preferences can be saved while the chat panel remains open. Keep the
            // request model aligned with the model that the embedded server will load.
            SelectedModel = configuredModel;
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
                if (string.Equals(_chatService.ActiveBackendId, "codex", StringComparison.OrdinalIgnoreCase))
                    await RefreshModelsAsync("codex");
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
        if (IsStreaming)
        {
            StatusMessage = "Stop the current response before signing out from Codex.";
            return;
        }

        _codexLoginCts?.Cancel();
        var loggedOut = await _chatService.LogoutCodexAsync();
        if (loggedOut)
            _controller.ForgetCodexThreadBindings();
        RefreshCodexAccountState();
        StatusMessage = loggedOut ? "Signed out from Codex." : "Could not sign out from Codex.";
        if (string.Equals(_chatService.ActiveBackendId, "codex", StringComparison.OrdinalIgnoreCase))
        {
            IsConnected = false;
            IsCodexBackend = false;
            ClearModelLists();
        }
    }

    [RelayCommand]
    private async Task SignInCopilotAsync()
    {
        StatusMessage = "Opening GitHub Copilot sign-in in your browser...";
        var started = await _chatService.StartCopilotLoginAsync();
        if (!started)
        {
            StatusMessage = $"Copilot sign-in failed: {_chatService.ConnectionError ?? "copilot CLI unavailable"}";
            return;
        }

        _copilotLoginCts?.Cancel();
        _copilotLoginCts?.Dispose();
        _copilotLoginCts = new CancellationTokenSource();
        var cts = _copilotLoginCts;
        _copilotCodeRaised = false;

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

            var verificationCode = _chatService.CopilotLoginVerificationCode;
            if (!_copilotCodeRaised && !string.IsNullOrWhiteSpace(verificationCode))
            {
                _copilotCodeRaised = true;
                var code = verificationCode;
                var verificationUrl = _chatService.CopilotLoginVerificationUrl;
                var url = string.IsNullOrWhiteSpace(verificationUrl)
                    ? "https://github.com/login/device"
                    : verificationUrl;
                CopilotVerificationCodeAvailable?.Invoke(code, url);
                StatusMessage = $"Waiting for you to approve {code} in the browser...";
            }

            CopilotAccountInfo? account = null;
            try
            {
                account = await _chatService.ReadCopilotAccountAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.TrackError(ex, isCrash: false);
            }

            RaiseUi(RefreshCopilotAccountState);
            if (account?.IsAuthenticated == true)
            {
                StatusMessage = "Signed in to GitHub Copilot.";
                if (string.Equals(_chatService.ActiveBackendId, "copilot", StringComparison.OrdinalIgnoreCase))
                    await RefreshModelsAsync("copilot");
                return;
            }
        }

        if (cts.IsCancellationRequested)
        {
            StatusMessage = "Copilot sign-in was cancelled.";
        }
        else
        {
            StatusMessage = "Sign-in window opened. Finish sign-in, then click Copilot sign in again to refresh the account.";
        }
    }

    [RelayCommand]
    private async Task SignOutCopilotAsync()
    {
        if (IsStreaming)
        {
            StatusMessage = "Stop the current response before signing out from Copilot.";
            return;
        }

        _copilotLoginCts?.Cancel();
        var loggedOut = await _chatService.LogoutCopilotAsync();
        if (loggedOut)
            _controller.ForgetCopilotSessionBindings();
        RefreshCopilotAccountState();
        StatusMessage = loggedOut
            ? "Signed out from Copilot. Local chat history was kept; Copilot session links were cleared."
            : "Could not sign out from Copilot.";
        if (string.Equals(_chatService.ActiveBackendId, "copilot", StringComparison.OrdinalIgnoreCase))
        {
            IsConnected = false;
            IsCopilotBackend = false;
            ClearModelLists();
        }
    }

    [RelayCommand]
    private void RefreshCopilot()
    {
        _ = Task.Run(() => _chatService.ReadCopilotAccountAsync())
            .ContinueWith(_ => RaiseUi(RefreshCopilotAccountState), TaskScheduler.Default);
    }

    [RelayCommand]
    private void RefreshCodex()
    {
        _ = Task.Run(() => _chatService.ReadCodexAccountAsync())
            .ContinueWith(_ => RaiseUi(RefreshCodexAccountState), TaskScheduler.Default);
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

    private async Task<bool> EnsureEmbeddedModelReadyAsync()
    {
        RaiseUi(() =>
        {
            IsModelLoading = true;
            OnPropertyChanged(nameof(IsModelReady));
            StatusMessage = "Loading the selected embedded model...";
        });

        try
        {
            var success = await _chatService.SwitchBackendAsync("embedded");
            var active = NormalizeBackendId(_chatService.ActiveBackendId);
            if (!success || !string.Equals(active, "embedded", StringComparison.OrdinalIgnoreCase))
            {
                StatusMessage = $"Embedded model could not be loaded: {_chatService.ConnectionError}";
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.TrackError(ex, isCrash: false);
            StatusMessage = $"Embedded model could not be loaded: {ex.Message}";
            return false;
        }
        finally
        {
            RaiseUi(() =>
            {
                IsModelLoading = false;
                OnPropertyChanged(nameof(IsModelReady));
            });
        }
    }

    private string GetPreferredModel(string? providerId)
    {
        var normalized = NormalizeBackendId(providerId);
        var configured = _providerModelSettings?.GetModelForProvider(normalized);
        if (!string.IsNullOrWhiteSpace(configured))
            return configured;

        return normalized.Equals("embedded", StringComparison.OrdinalIgnoreCase)
            ? _settingsStore.Settings.EmbeddedChatModelId
            : _settingsStore.Settings.AiChatDefaultModel;
    }

    private bool SupportsReasoning(string? providerId)
        => string.Equals(providerId, "codex", StringComparison.OrdinalIgnoreCase)
            || string.Equals(providerId, "copilot", StringComparison.OrdinalIgnoreCase)
            || string.Equals(providerId, "embedded", StringComparison.OrdinalIgnoreCase);

    private bool IsCurrentModelRefresh(int refreshVersion, string activeBackendId)
        => refreshVersion == Volatile.Read(ref _modelsRefreshVersion)
            && string.Equals(activeBackendId, NormalizeBackendId(_chatService.ActiveBackendId), StringComparison.OrdinalIgnoreCase);

    private static string NormalizeBackendId(string? backendId)
    {
        if (string.IsNullOrWhiteSpace(backendId))
            return string.Empty;

        return backendId.Trim().ToLowerInvariant() switch
        {
            "ollama" or "lmstudio" or "openai compatible" => "openai-compatible",
            "embedded (local)" => "embedded",
            "codex (chatgpt)" => "codex",
            "github copilot" => "copilot",
            _ => backendId.Trim()
        };
    }

    private void SetSelectedBackendSilently(string? backendId)
    {
        if (string.IsNullOrWhiteSpace(backendId))
            return;

        _suppressBackendSelection = true;
        try
        {
            SelectedBackendId = NormalizeBackendId(backendId);
        }
        finally
        {
            _suppressBackendSelection = false;
        }
    }

    private void ApplyActiveBackendState(string? backendId)
    {
        IsCodexBackend = IsConnected && string.Equals(backendId, "codex", StringComparison.OrdinalIgnoreCase);
        IsCopilotBackend = IsConnected && string.Equals(backendId, "copilot", StringComparison.OrdinalIgnoreCase);
        IsEmbeddedBackend = IsConnected && string.Equals(backendId, "embedded", StringComparison.OrdinalIgnoreCase);
        OnPropertyChanged(nameof(IsModelReady));
    }

    private async Task RefreshModelsAsync(string? expectedBackendId = null)
    {
        var activeBackendId = NormalizeBackendId(expectedBackendId ?? _chatService.ActiveBackendId);
        if (string.IsNullOrWhiteSpace(activeBackendId))
        {
            ClearModelLists();
            return;
        }

        var refreshVersion = Interlocked.Increment(ref _modelsRefreshVersion);
        RaiseUi(() =>
        {
            IsModelLoading = true;
            AvailableModels.Clear();
            AvailableReasoningEfforts.Clear();
            SelectedModel = string.Empty;
            OnPropertyChanged(nameof(AvailableModels));
            OnPropertyChanged(nameof(AvailableReasoningEfforts));
            OnPropertyChanged(nameof(IsModelReady));
        });

        try
        {
            var models = (await _chatService.GetAvailableModelsAsync().ConfigureAwait(false))
                .Where(model => !string.IsNullOrWhiteSpace(model))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (!IsCurrentModelRefresh(refreshVersion, activeBackendId))
                return;

            var preferredModel = GetPreferredModel(activeBackendId);
            var selectedModel = models.FirstOrDefault(model =>
                string.Equals(model, preferredModel, StringComparison.OrdinalIgnoreCase))
                ?? models.FirstOrDefault()
                ?? string.Empty;

            List<string> efforts = [];
            if (!string.IsNullOrWhiteSpace(selectedModel) && SupportsReasoning(activeBackendId))
            {
                try
                {
                    efforts = (await _chatService.GetAvailableReasoningEffortsAsync(selectedModel).ConfigureAwait(false))
                        .Where(effort => !string.IsNullOrWhiteSpace(effort))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();
                }
                catch (Exception ex)
                {
                    // Reasoning controls are optional; retain the usable model list if this probe fails.
                    _logger.TrackError(ex, isCrash: false);
                }
            }

            if (!IsCurrentModelRefresh(refreshVersion, activeBackendId))
                return;

            await _dispatcher.InvokeAsync(() =>
            {
                if (!IsCurrentModelRefresh(refreshVersion, activeBackendId))
                    return;

                AvailableModels.Clear();
                AvailableModels.AddRange(models);
                AvailableReasoningEfforts.Clear();
                AvailableReasoningEfforts.AddRange(efforts);
                SelectedModel = selectedModel;

                if (AvailableReasoningEfforts.Count > 0
                    && !AvailableReasoningEfforts.Contains(SelectedReasoningEffort, StringComparer.OrdinalIgnoreCase))
                {
                    SelectedReasoningEffort = AvailableReasoningEfforts[0];
                }

                if (!string.IsNullOrWhiteSpace(selectedModel))
                    _providerModelSettings?.SetModelForProvider(activeBackendId, selectedModel);

                OnPropertyChanged(nameof(AvailableModels));
                OnPropertyChanged(nameof(AvailableReasoningEfforts));
                OnPropertyChanged(nameof(IsModelReady));
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.TrackError(ex, isCrash: false);
            RaiseUi(() => StatusMessage = $"Failed to load models: {ex.Message}");
        }
        finally
        {
            if (refreshVersion == Volatile.Read(ref _modelsRefreshVersion))
            {
                RaiseUi(() =>
                {
                    IsModelLoading = false;
                    OnPropertyChanged(nameof(IsModelReady));
                });
            }
        }
    }

    private void ClearModelLists()
    {
        Interlocked.Increment(ref _modelsRefreshVersion);
        AvailableModels.Clear();
        AvailableReasoningEfforts.Clear();
        SelectedModel = string.Empty;
        IsModelLoading = false;
        RaiseUi(() =>
        {
            OnPropertyChanged(nameof(AvailableModels));
            OnPropertyChanged(nameof(AvailableReasoningEfforts));
            OnPropertyChanged(nameof(IsModelReady));
        });
    }

    private void RefreshSavedSessions()
    {
        OnPropertyChanged(nameof(SavedSessions));
        OnPropertyChanged(nameof(CurrentSessionTitle));
    }

    private void RefreshCodexAccountState()
    {
        var account = _chatService.CodexAccount;
        IsCodexSignedIn = account?.IsAuthenticated == true;
        CodexAccountLabel = account?.IsAuthenticated == true
            ? "Signed in"
            : "Not signed in";
    }

    private void RefreshCopilotAccountState()
    {
        var account = _chatService.CopilotAccount;
        IsCopilotSignedIn = account?.IsAuthenticated == true;
        CopilotAccountLabel = IsCopilotSignedIn
            ? "Signed in"
            : "Not signed in";
    }

    private void SubscribeCopilotVerificationHandler()
        => _chatService.CopilotLoginVerificationCodeAvailable += _copilotVerificationHandler;

    private void UnsubscribeCopilotVerificationHandler()
        => _chatService.CopilotLoginVerificationCodeAvailable -= _copilotVerificationHandler;

    private void RaiseUi(Action action)
    {
        if (_disposed)
            return;

        if (_dispatcher.CheckAccess())
        {
            action();
        }
        else
        {
            _ = _dispatcher.InvokeAsync(action);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _codexLoginCts?.Cancel();
        _copilotLoginCts?.Cancel();
        _backendSwitchCts?.Cancel();
        UnsubscribeCopilotVerificationHandler();
        _codexLoginCts?.Dispose();
        _copilotLoginCts?.Dispose();
        _backendSwitchCts?.Dispose();
        // The gates may still be held by an async startup/backend-switch operation.
        // They are intentionally left for the pending operation to release; disposing
        // them here would turn a normal window close into ObjectDisposedException.
    }
}
