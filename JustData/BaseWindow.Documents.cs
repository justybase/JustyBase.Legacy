// BaseWindow document-tab hosting.
// Non-SQL documents (Preferences, History, Query Watch, Git diff) are hosted
// as regular tabs in the main document TabControl. They share the tab strip with SQL editors but are excluded from
// every editor-only path (save, close prompts, keyboard shortcuts).
using JustData.Application.QueryWatch;
using JustData.ViewModels.History;
using JustData.ViewModels.QueryWatch;
using JustyBaseLegacy.UI.Controls;
using System.Diagnostics;

namespace JustyBaseLegacy.UI
{
    /// <summary>Marks a main TabControl page as a non-editor document.</summary>
    internal sealed class TabPageDocumentTag
    {
        public required string Key { get; init; }
        public Action? Closed { get; init; }
    }

    public partial class BaseWindow
    {
        internal static bool IsDocumentTab(TabPage? page) => page?.Tag is TabPageDocumentTag;

        private TabPage? FindDocumentTab(string key) =>
            _tabControlMain.TabPages
                .Cast<TabPage>()
                .FirstOrDefault(page => page.Tag is TabPageDocumentTag tag
                    && string.Equals(tag.Key, key, StringComparison.Ordinal));

        /// <summary>
        /// Shows a non-editor document in the main tab strip, reusing an
        /// existing tab when one with the same key is already open.
        /// </summary>
        private TabPage ShowDocumentTab(string key, string title, Func<Control> contentFactory)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            ArgumentNullException.ThrowIfNull(contentFactory);

            if (FindDocumentTab(key) is { } existing)
            {
                existing.Text = title;
                _tabControlMain.SelectedTab = existing;
                return existing;
            }

            Control content = contentFactory();
            content.Dock = DockStyle.Fill;

            var tab = new TabPage(title)
            {
                Name = key,
                Padding = new Padding(0),
                UseVisualStyleBackColor = true,
                Tag = new TabPageDocumentTag { Key = key }
            };
            tab.Controls.Add(content);

            // Insert after the last document tab so SQL order stays stable.
            _tabControlMain.TabPages.Add(tab);
            _tabControlMain.SelectedTab = tab;
            return tab;
        }

        private void CloseDocumentTab(TabPage tab)
        {
            if (tab?.Tag is not TabPageDocumentTag tag)
            {
                return;
            }

            if (_tabControlMain.TabPages.Contains(tab))
            {
                _tabControlMain.TabPages.Remove(tab);
            }

            try
            {
                // Hosted forms (Preferences, History, Query Watch) release
                // view models and timers from FormClosing/FormClosed. Closing
                // explicitly keeps that cleanup path alive when the tab is
                // removed programmatically.
                foreach (Control control in tab.Controls.Cast<Control>().ToArray())
                {
                    if (control is Form hosted && !hosted.IsDisposed)
                    {
                        hosted.Close();
                    }
                }
            }
            catch (Exception exception)
            {
                Trace.WriteLine($"Hosted document close failed: {exception.GetType().Name}");
            }

            try
            {
                tag.Closed?.Invoke();
            }
            catch (Exception exception)
            {
                Trace.WriteLine($"Document tab cleanup failed: {exception.GetType().Name}");
            }

            tab.Dispose();
            ActivateLastEditorTab();
        }

        private void ActivateLastEditorTab()
        {
            if (_lastActiveEditorTabPage is { } last
                && !last.IsDisposed
                && _tabControlMain.TabPages.Contains(last))
            {
                _tabControlMain.SelectedTab = last;
                return;
            }

            TabPage? firstEditor = EditorTabPages.FirstOrDefault();
            if (firstEditor is not null)
            {
                _tabControlMain.SelectedTab = firstEditor;
            }
        }

        private PreferencesForm? _preferencesDocumentForm;

        private void ShowPreferencesDocument()
        {
            bool isReopen = FindDocumentTab("preferencesDocument") is not null;
            ShowDocumentTab("preferencesDocument", "JustyBase Settings", CreatePreferencesContent);

            // Dock documents refreshed on every activation; a TabPage only
            // fires Load once, so refresh explicitly when the tab is revisited.
            if (isReopen && _preferencesDocumentForm is { IsDisposed: false } form)
            {
                _ = form.RefreshForDocumentAsync();
            }
        }

        private Control CreatePreferencesContent()
        {
            var form = new PreferencesForm(
                RepaintPreferences,
                SaveManySqlToDisk,
                _applicationSettingsContext,
                _snippetInitializationContext,
                _settingsPersistence.SaveConfig,
                _recentFileRuntimeContext.SaveRecentFiles,
                _uiHelperService,
                _colorTheme,
                _netezzaAutocompleteState
#if AI
                , fimCatalog: _fimModelCatalog
                , fimBootstrap: _fimModelBootstrap
                , chatCatalog: _chatCatalog
                , chatBootstrap: _chatBootstrap
#endif
                );
            form.PrepareForDocumentHost();
            _preferencesDocumentForm = form;
            return form;
        }

        private void ShowHistoryDocument()
        {
            ShowDocumentTab("historyDocument", "Query History", () =>
            {
                var viewModel = new HistoryViewModel(_historyStore, _uiDispatcher);
                var form = new History(
                    viewModel,
                    o => _colorTheme.ColorForm(o),
                    f => _uiHelperService.DoubleBufDateGridView(f),
                    (a, b, c) => AddMainTab(a, b, c),
                    HistoryDatFile,
                    _applicationSettingsContext.Config.UseSpecialColoring);
                form.PrepareForDocumentHost();
                return form;
            });
        }

        private void ShowQueryWatchDocument()
        {
            ShowDocumentTab("queryWatchDocument", "Query Watch", () =>
            {
                QueryWatchContext ContextFactory() => new(
                    SelectedConnectionName,
                    SelectedDatabase,
                    (int)_generalDbService.RelatedDatabaseType);

                var viewModel = new QueryWatchViewModel(
                    _queryWatchService,
                    ContextFactory,
                    _uiDispatcher);
                var form = new QueryWatch(
                    viewModel,
                    o => _colorTheme.ColorForm(o),
                    f => _uiHelperService.DoubleBufDateGridView(f),
                    _loggerLoud);
                form.PrepareForDocumentHost();
                _ = form.RefreshNowAsync();
                return form;
            });
        }
    }
}
