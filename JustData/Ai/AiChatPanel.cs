using JustData.ViewModels.Ai;
using JustyBase.Ai.Models;
using Microsoft.Web.WebView2.WinForms;
using System.Text;

namespace JustyBaseLegacy.UI.Ai;

/// <summary>
/// WinForms AI chat panel: conversation (WebView2 + Markdig), provider/backend
/// selectors, composer with attachments, tool-confirmation cards and session list.
/// </summary>
public sealed class AiChatPanel : UserControl
{
    private readonly ChatViewModel _viewModel;
    private readonly System.Windows.Forms.Timer _renderTimer;
    private readonly WebView2 _webView = new();
    private readonly TextBox _fallbackView = new()
    {
        Multiline = true,
        ReadOnly = true,
        Dock = DockStyle.Fill,
        ScrollBars = ScrollBars.Both,
        WordWrap = false,
        Font = new Font("Consolas", 9F),
        Visible = false
    };

    private ComboBox? _backendCombo;
    private ComboBox? _modelCombo;
    private ComboBox? _effortCombo;
    private ComboBox? _modeCombo;
    private TextBox? _inputBox;
    private Button? _sendButton;
    private Button? _stopButton;
    private Label? _statusLabel;
    private Label? _accountLabel;
    private ComboBox? _sessionCombo;
    private FlowLayoutPanel? _attachmentStrip;
    private Button? _codexButton;
    private bool _webViewReady;
    private bool _renderDirty;
    private bool _suppressSessionSync;

    public AiChatPanel(ChatViewModel viewModel)
    {
        _viewModel = viewModel;
        Dock = DockStyle.Fill;
        Padding = new Padding(4);
        BackColor = Color.White;

        BuildLayout();

        _viewModel.MessagesChanged += OnMessagesChanged;
        _viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(ChatViewModel.IsStreaming)
                or nameof(ChatViewModel.StatusMessage)
                or nameof(ChatViewModel.IsConnected)
                or nameof(ChatViewModel.IsCodexSignedIn)
                or nameof(ChatViewModel.CodexAccountLabel)
                or nameof(ChatViewModel.SavedSessions)
                or nameof(ChatViewModel.CurrentSessionTitle)
                or nameof(ChatViewModel.IsCodexBackend)
                or nameof(ChatViewModel.IsEmbeddedBackend))
            {
                SyncHeaderUi();
            }
        };

        _renderTimer = new System.Windows.Forms.Timer { Interval = 80 };
        _renderTimer.Tick += (_, _) =>
        {
            if (_renderDirty)
            {
                _renderDirty = false;
                RenderConversation();
            }
        };

        HookWebView();
        SyncHeaderUi();
        OnMessagesChanged();
        RefreshSessions();
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(0)
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        root.Controls.Add(BuildHeaderPanel(), 0, 0);

        _webView.Dock = DockStyle.Fill;
        _fallbackView.Dock = DockStyle.Fill;
        var conversationHost = new Panel { Dock = DockStyle.Fill };
        _webView.Parent = conversationHost;
        _fallbackView.Parent = conversationHost;
        root.Controls.Add(conversationHost, 0, 1);

        root.Controls.Add(BuildComposerPanel(), 0, 2);
        root.Controls.Add(BuildSessionsPanel(), 0, 3);
        root.Controls.Add(BuildStatusPanel(), 0, 4);

        Controls.Add(root);
    }

    private Control BuildHeaderPanel()
    {
        var header = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 62,
            WrapContents = true,
            Padding = new Padding(0),
            FlowDirection = FlowDirection.LeftToRight
        };

        header.Controls.Add(MakeLabel("Backend:"));
        _backendCombo = MakeCombo(150);
        header.Controls.Add(_backendCombo);

        header.Controls.Add(MakeLabel("Model:"));
        _modelCombo = MakeCombo(180);
        header.Controls.Add(_modelCombo);

        header.Controls.Add(MakeLabel("Effort:"));
        _effortCombo = MakeCombo(90);
        header.Controls.Add(_effortCombo);

        header.Controls.Add(MakeLabel("Mode:"));
        _modeCombo = MakeCombo(110);
        header.Controls.Add(_modeCombo);

        _codexButton = new Button { Text = "Codex sign in", AutoSize = true, Margin = new Padding(4, 3, 4, 3) };
        _codexButton.Click += async (_, _) =>
        {
            if (_viewModel.IsCodexSignedIn)
                await _viewModel.SignOutCodexCommand.ExecuteAsync(null);
            else
                await _viewModel.SignInCodexCommand.ExecuteAsync(null);
        };
        header.Controls.Add(_codexButton);

        _accountLabel = MakeLabel(string.Empty);
        _accountLabel.AutoSize = true;
        header.Controls.Add(_accountLabel);

        _backendCombo.SelectedIndexChanged += (_, _) =>
        {
            if (_suppressSessionSync)
                return;
            if (_backendCombo.SelectedItem is not string id)
                return;
            _viewModel.SelectedBackendId = id;
        };
        _modelCombo.SelectedIndexChanged += (_, _) =>
        {
            if (_modelCombo.SelectedItem is string model)
                _viewModel.SelectedModel = model;
        };
        _effortCombo.SelectedIndexChanged += (_, _) =>
        {
            if (_effortCombo.SelectedItem is string effort)
                _viewModel.SelectedReasoningEffort = effort;
        };
        _modeCombo.SelectedIndexChanged += (_, _) =>
        {
            if (_modeCombo.SelectedItem is ChatModeConfig mode)
                _viewModel.CurrentMode = mode.Mode;
        };

        return header;
    }

    private Control BuildComposerPanel()
    {
        var composer = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 84,
            ColumnCount = 3,
            RowCount = 2
        };
        composer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        composer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        composer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        composer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        composer.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _attachmentStrip = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Height = 22
        };
        composer.Controls.Add(_attachmentStrip, 0, 0);
        composer.SetColumnSpan(_attachmentStrip, 3);

        _inputBox = new TextBox
        {
            Multiline = true,
            Dock = DockStyle.Fill,
            ScrollBars = ScrollBars.Vertical,
            AcceptsReturn = true,
            Font = new Font("Consolas", 10F)
        };
        _inputBox.KeyDown += async (_, e) =>
        {
            // Bare Enter sends the message; Ctrl+Enter and Shift+Enter keep the default newline.
            if (e.KeyCode == Keys.Enter && !e.Control && !e.Shift)
            {
                e.SuppressKeyPress = true;
                await _viewModel.SendCommand.ExecuteAsync(null);
            }
        };
        composer.Controls.Add(_inputBox, 0, 1);

        var attachButton = new Button { Text = "Attach…", Width = 80, Height = 26, Margin = new Padding(2) };
        attachButton.Click += (_, _) =>
        {
            using var dialog = new OpenFileDialog { Multiselect = true, Title = "Attach files as references" };
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                foreach (var file in dialog.FileNames)
                    _viewModel.AddAttachment(file, isDirectory: false);
            }
        };
        composer.Controls.Add(attachButton, 1, 1);

        _sendButton = new Button { Text = "Send", Width = 80, Height = 26, Margin = new Padding(2) };
        _sendButton.Click += async (_, _) => await _viewModel.SendCommand.ExecuteAsync(null);
        composer.Controls.Add(_sendButton, 2, 1);

        _stopButton = new Button { Text = "Stop", Width = 80, Height = 26, Margin = new Padding(2), Visible = false };
        _stopButton.Click += async (_, _) => await _viewModel.CancelCommand.ExecuteAsync(null);
        composer.Controls.Add(_stopButton, 2, 1);

        return composer;
    }

    private Control BuildSessionsPanel()
    {
        var sessions = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 30,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0)
        };

        sessions.Controls.Add(MakeLabel("Session:"));
        _sessionCombo = MakeCombo(300);
        _sessionCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _sessionCombo.SelectedIndexChanged += (_, _) =>
        {
            if (_suppressSessionSync || _sessionCombo.SelectedItem is not ChatSession session)
                return;
            _viewModel.OpenSessionCommand.Execute(session);
        };
        sessions.Controls.Add(_sessionCombo);

        var newButton = new Button { Text = "New", Width = 60, Height = 24, Margin = new Padding(4, 2, 2, 2) };
        newButton.Click += (_, _) => _viewModel.NewSessionCommand.Execute(null);
        sessions.Controls.Add(newButton);

        var deleteButton = new Button { Text = "Delete", Width = 70, Height = 24, Margin = new Padding(2) };
        deleteButton.Click += (_, _) =>
        {
            if (_sessionCombo.SelectedItem is ChatSession session)
                _viewModel.DeleteSessionCommand.Execute(session);
        };
        sessions.Controls.Add(deleteButton);

        return sessions;
    }

    private Control BuildStatusPanel()
    {
        _statusLabel = MakeLabel("AI idle");
        _statusLabel.Dock = DockStyle.Top;
        _statusLabel.Height = 20;
        _statusLabel.AutoEllipsis = true;
        return _statusLabel;
    }

    private void HookWebView()
    {
        try
        {
            _ = InitializeWebViewAsync();
        }
        catch
        {
            ShowFallback();
        }
    }

    private async Task InitializeWebViewAsync()
    {
        try
        {
            await _webView.EnsureCoreWebView2Async();
            _webView.CoreWebView2.WebMessageReceived += (_, e) =>
            {
                var payload = e.TryGetWebMessageAsString();
                if (payload == "approve")
                {
                    _viewModel.ConfirmToolCommand.Execute(true);
                }
                else if (payload == "deny")
                {
                    _viewModel.ConfirmToolCommand.Execute(false);
                }
            };
            _webViewReady = true;
            OnMessagesChanged();
        }
        catch
        {
            ShowFallback();
        }
    }

    private void ShowFallback()
    {
        _webView.Visible = false;
        _fallbackView.Visible = true;
    }

    private void OnMessagesChanged()
    {
        _renderDirty = true;
        _renderTimer.Stop();
        _renderTimer.Start();
        SyncHeaderUi();
        RefreshSessions();
    }

    private void RenderConversation()
    {
        var html = ChatHtmlRenderer.Render(_viewModel.Messages);
        if (_webViewReady)
        {
            try
            {
                _webView.NavigateToString(html);
            }
            catch
            {
                ShowFallback();
            }
        }

        if (_fallbackView.Visible)
        {
            var sb = new StringBuilder();
            foreach (var message in _viewModel.Messages)
            {
                var role = message.Role.Equals("user", StringComparison.OrdinalIgnoreCase) ? "User" : "Assistant";
                if (message.Role.Equals("tool-confirmation", StringComparison.OrdinalIgnoreCase))
                    role = $"Tool ({message.ToolName})";
                sb.Append('[').Append(role).Append(']').Append(' ').AppendLine(message.Content);
                sb.AppendLine();
            }

            _fallbackView.Text = sb.ToString();
        }
    }

    private void SyncHeaderUi()
    {
        if (IsDisposed)
            return;

        _sendButton!.Visible = !_viewModel.IsStreaming;
        _stopButton!.Visible = _viewModel.IsStreaming;
        _statusLabel!.Text = _viewModel.StatusMessage;
        _accountLabel!.Text = _viewModel.CodexAccountLabel;
        _codexButton!.Text = _viewModel.IsCodexSignedIn ? "Codex sign out" : "Codex sign in";

        // Backends.
        if (_backendCombo!.Items.Count == 0)
        {
            _suppressSessionSync = true;
            foreach (var id in _viewModel.AvailableBackends)
                _backendCombo.Items.Add(id);
            _backendCombo.SelectedItem = _viewModel.SelectedBackendId;
            _suppressSessionSync = false;
        }
        else if (!Equals(_backendCombo.SelectedItem, _viewModel.SelectedBackendId))
        {
            _suppressSessionSync = true;
            _backendCombo.SelectedItem = _viewModel.SelectedBackendId;
            _suppressSessionSync = false;
        }

        // Models (refresh list, keep selection).
        var modelChanged = _modelCombo!.Items.Cast<string>().SequenceEqual(_viewModel.AvailableModels) == false;
        if (modelChanged)
        {
            _suppressSessionSync = true;
            _modelCombo.Items.Clear();
            foreach (var model in _viewModel.AvailableModels)
                _modelCombo.Items.Add(model);
            if (!string.IsNullOrWhiteSpace(_viewModel.SelectedModel))
                _modelCombo.SelectedItem = _viewModel.SelectedModel;
            _suppressSessionSync = false;
        }

        _effortCombo!.Enabled = _viewModel.ShowReasoningEffort;
        if (_viewModel.ShowReasoningEffort)
        {
            var effortChanged = !_effortCombo.Items.Cast<string>().SequenceEqual(_viewModel.AvailableReasoningEfforts);
            if (effortChanged)
            {
                _suppressSessionSync = true;
                _effortCombo.Items.Clear();
                foreach (var effort in _viewModel.AvailableReasoningEfforts)
                    _effortCombo.Items.Add(effort);
                if (!string.IsNullOrWhiteSpace(_viewModel.SelectedReasoningEffort))
                    _effortCombo.SelectedItem = _viewModel.SelectedReasoningEffort;
                _suppressSessionSync = false;
            }
        }
        else
        {
            _effortCombo.Items.Clear();
        }

        if (_modeCombo!.Items.Count == 0)
        {
            foreach (var mode in _viewModel.AvailableModes)
                _modeCombo.Items.Add(mode);
        }

        _modeCombo.SelectedItem = _viewModel.AvailableModes
            .FirstOrDefault(m => m.Mode == _viewModel.CurrentMode);
    }

    private void RefreshSessions()
    {
        if (_sessionCombo is null || IsDisposed)
            return;

        var currentId = _viewModel.CurrentSession.SessionId;
        _suppressSessionSync = true;
        _sessionCombo.Items.Clear();
        foreach (var session in _viewModel.SavedSessions)
        {
            _sessionCombo.Items.Add(session);
        }

        var active = _viewModel.SavedSessions.FirstOrDefault(s => s.SessionId == currentId);
        if (active is not null)
        {
            _sessionCombo.SelectedItem = active;
        }
        else if (_viewModel.Messages.Count > 0)
        {
            // Unsaved in-progress session — show it so the title remains visible.
            _sessionCombo.Items.Insert(0, _viewModel.CurrentSession);
            _sessionCombo.SelectedIndex = 0;
        }

        _suppressSessionSync = false;
    }

    private static Label MakeLabel(string text)
        => new() { Text = text, AutoSize = true, Margin = new Padding(4, 6, 4, 2) };

    private static ComboBox MakeCombo(int width)
        => new() { Width = width, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(2, 4, 6, 2) };

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _renderTimer?.Dispose();
            _webView?.Dispose();
        }

        base.Dispose(disposing);
    }
}
