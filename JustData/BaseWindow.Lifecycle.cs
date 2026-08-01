// BaseWindow form lifecycle (close, save state, notifications) partial.
using AppBase.Common;
using AppBase.Common.Enums;
using AppBase.Common.Interfaces;
using AppBase.Data;
using AppBase.Data.Core.Enums;
using AppBase.Data.Core.Interfaces;
using AppBase.Services;
using JustDataAdditionalForms;
using DatabaseDataGridView.WinForms;
using JustyBase.NetezzaDriver;
using JustyBaseLegacy.UI.Models;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JustyBaseLegacy.UI
{
    public partial class BaseWindow
    {
        private static readonly TimeSpan DatabaseCloseTimeout = TimeSpan.FromSeconds(3);

        private readonly NotifyIcon _notifyIcon1 = new NotifyIcon()
        {
            BalloonTipIcon = ToolTipIcon.Info,
            BalloonTipTitle = "JustData Info Message",
            Icon = JustData.Properties.Resources.icon2ico,
            Text = "JustData Info",
            Visible = true,
        };

        private bool _closeWorkflowRunning;
        private bool _allowCloseAfterAsyncPreparation;

        private async Task<bool> DoSaveTabStateAsync(bool ask = true)
        {
            await SaveManySqlToDiskAsync().ConfigureAwait(true);
            return await PromptSaveUnsavedTabsAsync(ask).ConfigureAwait(true);
        }

        /// <summary>
        /// Asks (once) whether to save tabs that have unsaved changes, and saves
        /// them on confirmation. Returns <c>true</c> when the close should be
        /// aborted (user pressed Cancel, or a save failed).
        /// </summary>
        private async Task<bool> PromptSaveUnsavedTabsAsync(bool ask = true)
        {
            var tabPages = new List<TabPage>();
            foreach (TabPage tabPage in EditorTabPages)
            {
                if (tabPage.Tag is null
                    && (_tabManager.GetEditor(tabPage) is not { TextLength: > 0 }))
                    continue;

                if (tabPage.Tag is not TabPageMainTag { IsSaved: true })
                {
                    tabPages.Add(tabPage);
                }
            }

            // No unsaved changes -> close silently, no prompt.
            if (tabPages.Count == 0)
            {
                return false;
            }

            DialogResult result = ask
                ? _loggerLoud.MessageBox_Show(this, "Save unsaved files?", "Save?", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question)
                : DialogResult.No;

            if (result == DialogResult.Yes)
            {
                foreach (TabPage tabPage in tabPages)
                {
                    if (!await SaveAsync(tabPage))
                    {
                        return true;
                    }
                }
            }
            else if (result == DialogResult.Cancel)
            {
                return true;
            }

            return false;
        }

        private async void BaseWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_allowCloseAfterAsyncPreparation)
            {
                _allowCloseAfterAsyncPreparation = false;
                CompleteClose();
                return;
            }

            // FormClosing is synchronous, while bundle persistence is async.
            // Cancel the current close attempt, await the work, then re-enter
            // Close(). The message pump remains responsive throughout the wait.
            e.Cancel = true;
            if (_closeWorkflowRunning)
            {
                return;
            }

            _closeWorkflowRunning = true;
            _notifyIcon1.Visible = false;
            try
            {
                // Ask once, and only when there are genuinely unsaved changes.
                // Tabs close on exit by definition, so a separate
                // "Close all tabs?" confirmation adds no value.
                // DoSaveTabStateAsync also persists the startup bundle so the
                // previous session can be restored on next launch.
                if (await DoSaveTabStateAsync())
                {
                    _notifyIcon1.Visible = true;
                    return;
                }

                await CloseOpenedDbConnectionsAsync();
                _allowCloseAfterAsyncPreparation = true;
                Close();
            }
            catch (Exception ex)
            {
                _allowCloseAfterAsyncPreparation = false;
                _notifyIcon1.Visible = true;
                _loggerLoud.MessageBox_Show(this, ex.Message, "Close error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _closeWorkflowRunning = false;
            }
        }

        private void CompleteClose()
        {
            _notifyIcon1.Dispose();

            if (_tabManager is DockSuiteTabManager dsm)
            {
                string layoutPath = Path.Combine(_applicationSettingsContext.ConfigDirectory, "dockLayout.xml");
                dsm.SaveLayout(layoutPath);
            }

            NetezzaLegacyCompletionHelpers.SaveSnipets(_applicationSettingsContext, _netezzaAutocompleteState);
            _connectionSessions.Clear();

            try
            {
                if (!_completionContext.SchemaRefreshed || _netezzaHelperService.SqliteInProgress)
                {
                    _applicationSettingsContext.Config.ResetSchema = true;
                }

                _recentFileRuntimeContext.SaveRecentFiles();
                _applicationSettingsContext.Config.NotFirstLaunch = true;
                _settingsPersistence.SaveConfig();
            }
            catch (Exception ex)
            {
                _loggerLoud.MessageBox_Show(this, ex.Message, "Close error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            _shellViewModel.OpenPreferencesRequested -= OpenPreferencesFromShell;
            _shellViewModel.RefreshSchemaRequested -= RefreshSchemaFromShell;
            _shellViewModel.ShutdownRequested -= Close;
            _editorWorkspaceViewModel.DocumentReloaded -= OnEditorDocumentReloaded;
            _editorWorkspaceViewModel.PropertyChanged -= OnEditorWorkspacePropertyChanged;
            if (_gitTimelineDocument is not null)
            {
                _gitTimelineDocument.PropertyChanged -= OnGitTimelineDocumentPropertyChanged;
                _gitTimelineDocument = null;
            }
            _documentExecutionLifecyclePresenter.Dispose();
            _sqlResultPresenter.Dispose();
            _loggerLoud.SetWindow(null);
        }

        private async Task CloseOpenedDbConnectionsAsync()
        {
            DbConnection[] openedDbConnections = EditorTabPages
                .Select(_tabManager.GetEditor)
                .Where(editor => editor is not null && TabConnectionCache.Default.TryGet(editor, out _))
                .Select(editor =>
                {
                    TabConnectionCache.Default.TryGet(editor!, out var data);
                    return data?.Connection;
                })
                .OfType<DbConnection>()
                .Where(connection => connection.State == System.Data.ConnectionState.Open)
                .Distinct()
                .ToArray();

            await Task.WhenAll(openedDbConnections.Select(CloseDatabaseConnectionAsync)).ConfigureAwait(true);
        }

        private static async Task CloseDatabaseConnectionAsync(DbConnection connection)
        {
            Task closeTask = Task.Run(() =>
            {
                try
                {
                    connection.Close();
                }
                catch (Exception exception)
                {
                    Trace.WriteLine($"Closing a database connection failed: {exception.GetType().Name}");
                }
            });

            try
            {
                await closeTask.WaitAsync(DatabaseCloseTimeout).ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                Trace.WriteLine($"Closing a database connection exceeded {DatabaseCloseTimeout.TotalSeconds:0.#} seconds.");
            }
        }

        private void DoMessage(string message)
        {
            _notifyIcon1.BalloonTipText = message;
            _notifyIcon1.ShowBalloonTip(2000);
        }
    }
}
