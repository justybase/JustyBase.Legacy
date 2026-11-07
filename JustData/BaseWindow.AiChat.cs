using AppBase.Common;
using JustyBase.Ai.Models;
using JustyBase.Ai.Ports;
using JustyBaseLegacy.UI.Ai;
using JustData.ViewModels.Ai;
using JustyBaseLegacy.UI.Helpers;
using System.Windows.Forms;

namespace JustyBaseLegacy.UI;

public partial class BaseWindow
{
    private AiChatPanel? _aiChatPanel;
    private readonly ChatViewModel? _chatViewModel;
    private readonly ISqlDiagnosticsProvider? _sqlDiagnosticsProvider;
    private readonly JustyBase.Ai.Embedded.Download.EmbeddedChatModelCatalog? _chatCatalog;
    private readonly JustyBase.Ai.Chat.IEmbeddedChatModelBootstrapService? _chatBootstrap;

    private void InitializeAiChat()
    {
        if (_chatViewModel is null)
            return;

        _chatViewModel.AttachHostProviders(
            GetAiChatCurrentSql,
            GetAiChatEditorContext,
            UpdateAiChatSqlBuffer,
            GetAiChatActiveSqlContext);

        if (_sqlDiagnosticsProvider is LegacySqlDiagnosticsProvider legacyDiagnostics)
        {
            legacyDiagnostics.SetActiveEditorIssuesProvider(GetActiveEditorLintIssues);
        }

        // Start account probes and auto-connect only after the window has been shown.
        // The chat dispatcher resolves BaseWindow lazily, so starting provider work
        // during DI construction can re-enter the in-flight BaseWindow resolution.
        Shown += StartAiChatAfterFirstShow;

        // "AI Chat" entry in the Panels menu.
        var panelsMenu = optionsToolStripMenuItem.DropDownItems
            .OfType<ToolStripMenuItem>()
            .FirstOrDefault(item => item.Text == "Panels");
        if (panelsMenu is not null)
        {
            var item = new ToolStripMenuItem("AI Chat");
            item.Click += (_, _) => ShowAiChat();
            panelsMenu.DropDownItems.Add(item);

            var hideItem = new ToolStripMenuItem("Hide AI Chat");
            hideItem.Click += (_, _) => HideAiChat();
            panelsMenu.DropDownItems.Add(hideItem);
        }
        else
        {
            var aiChatItem = new ToolStripMenuItem("AI Chat");
            aiChatItem.Click += (_, _) => ShowAiChat();
            optionsToolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());
            optionsToolStripMenuItem.DropDownItems.Add(aiChatItem);
        }
    }

    private void StartAiChatAfterFirstShow(object? sender, EventArgs e)
    {
        Shown -= StartAiChatAfterFirstShow;
        _chatViewModel?.StartStartupAccountProbes();
        _chatViewModel?.AutoConnectIfEnabled();
    }

    private SplitContainer? _rightPanelSplit;
    private TabControl? _rightTabs;
    private TabPage? _aiChatTab;

    private void EnsureAiChatPanel()
    {
        if (_aiChatPanel is not null || _chatViewModel is null)
            return;

        _aiChatPanel = new AiChatPanel(_chatViewModel)
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None
        };

        SplitContainer rightSplit = EnsureRightPanel();
        _aiChatTab = new TabPage("AI Chat")
        {
            Name = "tabAiChat",
            Padding = new Padding(0)
        };
        _aiChatTab.Controls.Add(_aiChatPanel);
        _rightTabs!.TabPages.Add(_aiChatTab);
        rightSplit.Panel2Collapsed = false;
    }

    private SplitContainer EnsureRightPanel()
    {
        if (_rightPanelSplit is not null)
            return _rightPanelSplit;

        _rightPanelSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            Panel2Collapsed = true,
            SplitterWidth = Math.Max(3, DpiScale.Scale(4, DeviceDpi)),
            FixedPanel = FixedPanel.Panel2
        };

        splitContainer1.Panel2.Controls.Remove(_tabControlMain);
        _rightPanelSplit.Panel1.Controls.Add(_tabControlMain);

        _rightTabs = new TabControl
        {
            Dock = DockStyle.Fill,
            Name = "tabControlRight",
            Multiline = false
        };
        _rightPanelSplit.Panel2.Controls.Add(_rightTabs);
        splitContainer1.Panel2.Controls.Add(_rightPanelSplit);
        return _rightPanelSplit;
    }

    public void ShowAiChat()
    {
        EnsureAiChatPanel();
        if (_rightPanelSplit is null || _aiChatTab is null)
            return;

        _rightPanelSplit.Panel2Collapsed = false;
        _rightTabs!.SelectedTab = _aiChatTab;

        ApplyAiChatWidth();
        if (IsHandleCreated)
        {
            BeginInvoke(new Action(ApplyAiChatWidth));
        }
    }

    /// <summary>Collapses the right-hand AI Chat panel.</summary>
    public void HideAiChat()
    {
        if (_rightPanelSplit is not null && !_rightPanelSplit.IsDisposed)
        {
            _rightPanelSplit.Panel2Collapsed = true;
        }
    }

    private void ApplyAiChatWidth()
    {
        if (_rightPanelSplit is null || _rightPanelSplit.IsDisposed || _rightPanelSplit.Panel2Collapsed)
            return;

        int width = _rightPanelSplit.Width;
        if (width <= 0)
            return;

        int desiredWidth = Math.Min(
            DpiScale.Scale(420, DeviceDpi),
            Math.Max(DpiScale.Scale(280, DeviceDpi), width / 3));
        try
        {
            _rightPanelSplit.SplitterDistance = Math.Max(
                DpiScale.Scale(320, DeviceDpi),
                width - desiredWidth - _rightPanelSplit.SplitterWidth);
        }
        catch (InvalidOperationException)
        {
            // The splitter is laid out asynchronously on first show.
        }
        catch (ArgumentOutOfRangeException)
        {
            // Same as above: clamp to the available width once layout settles.
        }
    }

    /// <summary>"Fix in AI Chat" entry point (editor context menu / diagnostics).</summary>
    public async Task SendCurrentSqlToAiChatAsync()
    {
        if (!_applicationSettingsContext.Config.EnableAiChat)
        {
            _loggerLoud.MessageBox_Show(this, "AI Chat is disabled. Enable it in Preferences → AI Chat.");
            return;
        }

        if (_chatViewModel is null)
            return;

        ShowAiChat();

        _chatViewModel.NewSessionCommand.Execute(null);
        var sqlFixMode = _chatViewModel.AvailableModes.FirstOrDefault(mode => mode.Mode == ChatMode.SqlFix);
        if (sqlFixMode is not null)
        {
            _chatViewModel.CurrentMode = sqlFixMode.Mode;
        }

        _chatViewModel.InputText = "Fix current SQL";
        await Task.Delay(50);
        await _chatViewModel.SendCommand.ExecuteAsync(null);
    }

    private string? GetAiChatCurrentSql()
        => CurrentTB is { } editor ? editor.Text : null;

    private (string FullText, string SelectedText, int SelectionStart, int SelectionLength, int CaretOffset)? GetAiChatEditorContext()
    {
        if (CurrentTB is not { } editor)
            return null;

        var selectedText = editor.SelectedText ?? string.Empty;
        return (editor.Text ?? string.Empty, selectedText, editor.SelectionStart, selectedText.Length, editor.SelectionStart);
    }

    private bool UpdateAiChatSqlBuffer(string updatedSql)
    {
        if (CurrentTB is not { } editor)
            return false;

        editor.Text = updatedSql ?? string.Empty;
        _editorWorkspaceViewModel.ActiveDocument?.MarkEditorDirty();
        return true;
    }

    private (string ConnectionName, string DatabaseName)? GetAiChatActiveSqlContext()
    {
        var connectionName = SelectedConnectionName;
        if (string.IsNullOrWhiteSpace(connectionName))
            return null;

        return (connectionName, CurrentUpper?.SelectedDatabase ?? string.Empty);
    }

    private IReadOnlyList<JustyBase.NetezzaSqlParser.Linter.LintIssue>? GetActiveEditorLintIssues()
    {
        var editor = CurrentTB;
        if (editor is null)
            return null;

        return _lintIssuesByEditor.GetValueOrDefault(editor);
    }
}
