using JustyBase.Ai.Models;
using JustyBase.Ai.Ports;
using JustyBaseLegacy.UI.Ai;
using JustData.ViewModels.Ai;
using WeifenLuo.WinFormsUI.Docking;

namespace JustyBaseLegacy.UI;

public partial class BaseWindow
{
    private AiChatPanel? _aiChatPanel;
    private readonly ChatViewModel? _chatViewModel;
    private readonly ISqlDiagnosticsProvider? _sqlDiagnosticsProvider;
    private readonly JustyBase.Ai.Embedded.Download.EmbeddedChatModelCatalog? _chatCatalog;

    private void InitializeAiChat(DockSuiteTabManager dsm)
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

        // Auto-connect only after the window is fully constructed — the chat dispatcher
        // resolves BaseWindow lazily, and resolving it from the constructor would be a DI cycle.
        try
        {
            BeginInvoke(() => _chatViewModel.AutoConnectIfEnabled());
        }
        catch (InvalidOperationException)
        {
            // Handle not created yet — skip auto-connect; the user can connect from the panel.
        }

        // "AI Chat" entry in the Dock windows menu.
        var windowsMenu = optionsToolStripMenuItem.DropDownItems
            .OfType<ToolStripMenuItem>()
            .FirstOrDefault(item => item.Text == "Dock windows");
        if (windowsMenu is not null)
        {
            var item = new ToolStripMenuItem("AI Chat");
            item.Click += (_, _) => ShowAiChat();
            windowsMenu.DropDownItems.Add(item);
        }
        else
        {
            var aiChatItem = new ToolStripMenuItem("AI Chat");
            aiChatItem.Click += (_, _) => ShowAiChat();
            optionsToolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());
            optionsToolStripMenuItem.DropDownItems.Add(aiChatItem);
        }
    }

    private void EnsureAiChatPanel()
    {
        if (_aiChatPanel is not null || _chatViewModel is null)
            return;

        _aiChatPanel = new AiChatPanel(_chatViewModel)
        {
            BorderStyle = BorderStyle.FixedSingle
        };

        if (_tabManager is DockSuiteTabManager dsm)
        {
            dsm.RegisterPersistentTool("AI Chat", _aiChatPanel, DockState.DockRight);
        }
    }

    public void ShowAiChat()
    {
        EnsureAiChatPanel();
        if (_tabManager is DockSuiteTabManager dsm && _aiChatPanel is not null)
        {
            dsm.ShowToolWindow("AI Chat", _aiChatPanel, DockState.DockRight);
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
