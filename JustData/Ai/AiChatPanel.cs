using AppBase.Common;
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
        _viewModel.AttachmentsChanged += OnAttachmentsChanged;
        _viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(ChatViewModel.IsStreaming)
                or nameof(ChatViewModel.IsInitializing)
                or nameof(ChatViewModel.StatusMessage)
                or nameof(ChatViewModel.InputText)
                or nameof(ChatViewModel.IsConnected)
                or nameof(ChatViewModel.IsCodexSignedIn)
                or nameof(ChatViewModel.CodexAccountLabel)
                or nameof(ChatViewModel.SelectedModel)
                or nameof(ChatViewModel.AvailableModels)
                or nameof(ChatViewModel.AvailableReasoningEfforts)
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
        RefreshAttachments();
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
        var dpi = DeviceDpi;
        var headerHeight = DpiScale.Scale(70, dpi);
        var labelMargin = new Padding(DpiScale.Scale(4, dpi), DpiScale.Scale(3, dpi), DpiScale.Scale(2, dpi), DpiScale.Scale(3, dpi));
        var comboMargin = new Padding(DpiScale.Scale(2, dpi), DpiScale.Scale(2, dpi), DpiScale.Scale(6, dpi), DpiScale.Scale(2, dpi));

        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = headerHeight,
            ColumnCount = 6,
            RowCount = 2,
            Padding = new Padding(0)
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
        header.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        header.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        var backendLabel = MakeLabel("Backend:");
        backendLabel.Margin = labelMargin;
        backendLabel.Anchor = AnchorStyles.Right;
        header.Controls.Add(backendLabel, 0, 0);

        _backendCombo = MakeCombo(0, "aiChatPanelBackend");
        _backendCombo.DisplayMember = nameof(ChatBackendOption.DisplayName);
        _backendCombo.Margin = comboMargin;
        _backendCombo.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        header.Controls.Add(_backendCombo, 1, 0);

        var modelLabel = MakeLabel("Model:");
        modelLabel.Margin = labelMargin;
        modelLabel.Anchor = AnchorStyles.Right;
        header.Controls.Add(modelLabel, 2, 0);

        _modelCombo = MakeCombo(0, "aiChatPanelModel");
        _modelCombo.Margin = comboMargin;
        _modelCombo.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        header.Controls.Add(_modelCombo, 3, 0);

        var effortLabel = MakeLabel("Effort:");
        effortLabel.Margin = labelMargin;
        effortLabel.Anchor = AnchorStyles.Right;
        header.Controls.Add(effortLabel, 4, 0);

        _effortCombo = MakeCombo(0, "aiChatPanelReasoningEffort");
        _effortCombo.Margin = comboMargin;
        _effortCombo.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        header.Controls.Add(_effortCombo, 5, 0);

        var modeLabel = MakeLabel("Mode:");
        modeLabel.Margin = labelMargin;
        modeLabel.Anchor = AnchorStyles.Right;
        header.Controls.Add(modeLabel, 0, 1);

        _modeCombo = MakeCombo(0, "aiChatPanelMode");
        _modeCombo.DisplayMember = nameof(ChatModeConfig.DisplayName);
        _modeCombo.Margin = comboMargin;
        _modeCombo.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        header.Controls.Add(_modeCombo, 1, 1);

        _codexButton = new Button
        {
            Text = "Codex sign in",
            Name = "aiChatPanelCodexSignIn",
            AccessibleName = "Codex sign in",
            AutoSize = true,
            Margin = new Padding(DpiScale.Scale(4, dpi), DpiScale.Scale(2, dpi), DpiScale.Scale(4, dpi), DpiScale.Scale(2, dpi)),
            Anchor = AnchorStyles.Left
        };
        _codexButton.Click += async (_, _) =>
        {
            if (_viewModel.IsCodexSignedIn)
                await _viewModel.SignOutCodexCommand.ExecuteAsync(null);
            else
                await _viewModel.SignInCodexCommand.ExecuteAsync(null);
        };
        header.Controls.Add(_codexButton, 2, 1);

        _accountLabel = MakeLabel(string.Empty);
        _accountLabel.AutoSize = true;
        _accountLabel.Margin = labelMargin;
        _accountLabel.Anchor = AnchorStyles.Left;
        header.Controls.Add(_accountLabel, 3, 1);

        _backendCombo.SelectedIndexChanged += (_, _) =>
        {
            if (_suppressSessionSync)
                return;
            if (_backendCombo.SelectedItem is not ChatBackendOption option)
                return;
            _viewModel.SelectedBackendId = option.Id;
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
        var dpi = DeviceDpi;
        var composerHeight = DpiScale.Scale(84, dpi);
        var attachmentHeight = DpiScale.Scale(22, dpi);
        var buttonWidth = DpiScale.Scale(80, dpi);
        var buttonHeight = DpiScale.Scale(26, dpi);
        var buttonMargin = DpiScale.Scale(2, dpi);

        var composer = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = composerHeight,
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
            Name = "aiChatPanelAttachments",
            AccessibleName = "Pending chat attachments",
            Dock = DockStyle.Fill,
            AutoScroll = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Height = attachmentHeight
        };
        composer.Controls.Add(_attachmentStrip, 0, 0);
        composer.SetColumnSpan(_attachmentStrip, 3);

        _inputBox = new TextBox
        {
            Name = "aiChatPanelInput",
            AccessibleName = "Chat message",
            Multiline = true,
            Dock = DockStyle.Fill,
            ScrollBars = ScrollBars.Vertical,
            AcceptsReturn = true,
            Font = new Font("Consolas", 10F)
        };
        _inputBox.TextChanged += (_, _) =>
        {
            if (!string.Equals(_viewModel.InputText, _inputBox.Text, StringComparison.Ordinal))
                _viewModel.InputText = _inputBox.Text;
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

        var attachButton = new Button
        {
            Text = "Attach…",
            Name = "aiChatPanelAttach",
            AccessibleName = "Attach files",
            Width = buttonWidth,
            Height = buttonHeight,
            Margin = new Padding(buttonMargin)
        };
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

        _sendButton = new Button
        {
            Text = "Send",
            Name = "aiChatPanelSend",
            AccessibleName = "Send chat message",
            Width = buttonWidth,
            Height = buttonHeight,
            Margin = new Padding(buttonMargin)
        };
        _sendButton.Click += async (_, _) => await _viewModel.SendCommand.ExecuteAsync(null);
        composer.Controls.Add(_sendButton, 2, 1);

        _stopButton = new Button
        {
            Text = "Stop",
            Name = "aiChatPanelStop",
            AccessibleName = "Stop chat response",
            Width = buttonWidth,
            Height = buttonHeight,
            Margin = new Padding(buttonMargin),
            Visible = false
        };
        _stopButton.Click += async (_, _) => await _viewModel.CancelCommand.ExecuteAsync(null);
        composer.Controls.Add(_stopButton, 2, 1);

        return composer;
    }

    private Control BuildSessionsPanel()
    {
        var dpi = DeviceDpi;
        var sessionsHeight = DpiScale.Scale(36, dpi);

        var sessions = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = sessionsHeight,
            ColumnCount = 4,
            RowCount = 1,
            Padding = new Padding(0)
        };
        sessions.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        sessions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        sessions.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        sessions.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        sessions.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var sessionLabel = MakeLabel("Session:");
        sessionLabel.Anchor = AnchorStyles.Right;
        sessions.Controls.Add(sessionLabel, 0, 0);

        _sessionCombo = MakeCombo(0, "aiChatPanelSession");
        _sessionCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _sessionCombo.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _sessionCombo.Margin = new Padding(DpiScale.Scale(2, dpi), DpiScale.Scale(2, dpi), DpiScale.Scale(4, dpi), DpiScale.Scale(2, dpi));
        _sessionCombo.SelectedIndexChanged += (_, _) =>
        {
            if (_suppressSessionSync || _sessionCombo.SelectedItem is not ChatSession session)
                return;
            _viewModel.OpenSessionCommand.Execute(session);
        };
        sessions.Controls.Add(_sessionCombo, 1, 0);

        var newButton = new Button
        {
            Text = "New",
            Name = "aiChatPanelNewSession",
            AccessibleName = "New chat session",
            Width = DpiScale.Scale(60, dpi),
            Height = DpiScale.Scale(26, dpi),
            Margin = new Padding(DpiScale.Scale(4, dpi), DpiScale.Scale(2, dpi), DpiScale.Scale(2, dpi), DpiScale.Scale(2, dpi)),
            Anchor = AnchorStyles.Left
        };
        newButton.Click += (_, _) => _viewModel.NewSessionCommand.Execute(null);
        sessions.Controls.Add(newButton, 2, 0);

        var deleteButton = new Button
        {
            Text = "Delete",
            Name = "aiChatPanelDeleteSession",
            AccessibleName = "Delete chat session",
            Width = DpiScale.Scale(70, dpi),
            Height = DpiScale.Scale(26, dpi),
            Margin = new Padding(DpiScale.Scale(2, dpi), DpiScale.Scale(2, dpi), DpiScale.Scale(4, dpi), DpiScale.Scale(2, dpi)),
            Anchor = AnchorStyles.Left
        };
        deleteButton.Click += (_, _) =>
        {
            if (_sessionCombo.SelectedItem is ChatSession session)
                _viewModel.DeleteSessionCommand.Execute(session);
        };
        sessions.Controls.Add(deleteButton, 3, 0);

        return sessions;
    }

    private Control BuildStatusPanel()
    {
        var dpi = DeviceDpi;
        var statusHeight = DpiScale.Scale(24, dpi);

        _statusLabel = MakeLabel("AI idle");
        _statusLabel.Dock = DockStyle.Top;
        _statusLabel.Height = statusHeight;
        _statusLabel.AutoEllipsis = true;
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
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

    private void OnAttachmentsChanged() => RefreshAttachments();

    private void RefreshAttachments()
    {
        if (_attachmentStrip is null || IsDisposed)
            return;

        _attachmentStrip.SuspendLayout();
        try
        {
            var existingControls = _attachmentStrip.Controls.Cast<Control>().ToArray();
            _attachmentStrip.Controls.Clear();
            foreach (var control in existingControls)
            {
                control.Dispose();
            }

            for (var index = 0; index < _viewModel.PendingAttachments.Count; index++)
            {
                var attachment = _viewModel.PendingAttachments[index];
                var chip = new Panel
                {
                    Name = $"aiChatAttachment{index}",
                    AccessibleName = attachment.DisplayLabel,
                    AutoSize = true,
                    Height = DpiScale.Scale(22, DeviceDpi),
                    Margin = new Padding(2),
                    Padding = new Padding(2, 0, 2, 0),
                    BorderStyle = BorderStyle.FixedSingle
                };
                var label = new Label
                {
                    Text = attachment.DisplayLabel,
                    AutoSize = true,
                    Dock = DockStyle.Left,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(2, 1, 2, 1)
                };
                var removeButton = new Button
                {
                    Text = "×",
                    Name = $"aiChatAttachmentRemove{index}",
                    AccessibleName = $"Remove {attachment.DisplayLabel}",
                    AutoSize = false,
                    Width = DpiScale.Scale(22, DeviceDpi),
                    Height = DpiScale.Scale(20, DeviceDpi),
                    Dock = DockStyle.Right,
                    FlatStyle = FlatStyle.Flat,
                    Margin = new Padding(0)
                };
                removeButton.FlatAppearance.BorderSize = 0;
                removeButton.Click += (_, _) => _viewModel.RemoveAttachmentCommand.Execute(attachment);
                chip.Controls.Add(removeButton);
                chip.Controls.Add(label);
                _attachmentStrip.Controls.Add(chip);
            }
        }
        finally
        {
            _attachmentStrip.ResumeLayout();
        }
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

        if (_inputBox is not null
            && !string.Equals(_inputBox.Text, _viewModel.InputText, StringComparison.Ordinal))
        {
            var selectionStart = Math.Min(_inputBox.SelectionStart, _viewModel.InputText.Length);
            _inputBox.Text = _viewModel.InputText;
            _inputBox.SelectionStart = selectionStart;
        }

        // While the provider is connecting (first embedded launch), the composer must be
        // disabled so repeated Enter presses cannot queue ghost sends.
        var inputDisabled = _viewModel.IsInitializing;
        _inputBox!.Enabled = !inputDisabled;
        _sendButton!.Enabled = !inputDisabled;

        // Backends.
        if (_backendCombo!.Items.Count == 0)
        {
            _suppressSessionSync = true;
            foreach (var option in _viewModel.AvailableBackends)
                _backendCombo.Items.Add(option);
            _suppressSessionSync = false;
        }

        var backendToSelect = _viewModel.AvailableBackends
            .FirstOrDefault(b => string.Equals(b.Id, _viewModel.SelectedBackendId, StringComparison.OrdinalIgnoreCase));
        if (backendToSelect is not null && !Equals(_backendCombo.SelectedItem, backendToSelect))
        {
            _suppressSessionSync = true;
            _backendCombo.SelectedItem = backendToSelect;
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

    private Label MakeLabel(string text)
    {
        var dpi = DeviceDpi;
        return new Label
        {
            Text = text,
            AutoSize = true,
            Margin = new Padding(DpiScale.Scale(4, dpi), DpiScale.Scale(6, dpi), DpiScale.Scale(4, dpi), DpiScale.Scale(2, dpi))
        };
    }

    private ComboBox MakeCombo(int width, string automationId)
    {
        var dpi = DeviceDpi;
        return new ComboBox
        {
            Width = width,
            Name = automationId,
            AccessibleName = automationId,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Margin = new Padding(DpiScale.Scale(2, dpi), DpiScale.Scale(4, dpi), DpiScale.Scale(6, dpi), DpiScale.Scale(2, dpi))
        };
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _renderTimer?.Dispose();
            _webView?.Dispose();
            _viewModel.AttachmentsChanged -= OnAttachmentsChanged;
        }

        base.Dispose(disposing);
    }
}
