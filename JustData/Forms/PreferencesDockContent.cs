using AppBase.Common;
using AppBase.Common.Interfaces;
using AppBase.Data.Core.Models;
using DatabaseDataGridView.WinForms;
using DatabaseDataGridView.WinForms.Coloring;
using System;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using JustyBaseLegacy.UI;

namespace JustyBaseLegacy.UI.Forms;

/// <summary>
/// Hosts PreferencesForm as a normal document in the main DockSuite area.
/// This keeps settings in the same workspace as SQL editors and avoids the
/// fixed-size constraints of a separate high-DPI dialog.
/// </summary>
internal sealed class PreferencesDockContent : DockContent
{
    private readonly PreferencesForm _preferencesForm;

    public PreferencesDockContent(
        Action repaintApplication,
        Action saveManySqlToDisk,
        IApplicationSettingsContext applicationSettingsContext,
        ISnippetInitializationContext snippetInitializationContext,
        Action saveConfig,
        Action saveRecentFiles,
        IUiHelperService uiHelperService,
        IColorTheme colorTheme,
        INetezzaAutocompleteState netezzaAutocompleteState,
        JustyBase.Ai.Fim.Download.IFimModelCatalog? fimCatalog = null,
        JustyBaseLegacy.UI.Fim.IFimModelBootstrapService? fimBootstrap = null)
    {
        Text = "JustyBase Settings";
        TabText = "JustyBase Settings";
        Name = "preferencesDocument";
        CloseButton = true;
        CloseButtonVisible = true;
        HideOnClose = false;
        DockAreas = DockAreas.Document;

        _preferencesForm = new PreferencesForm(
            repaintApplication,
            saveManySqlToDisk,
            applicationSettingsContext,
            snippetInitializationContext,
            saveConfig,
            saveRecentFiles,
            uiHelperService,
            colorTheme,
            netezzaAutocompleteState,
            fimCatalog: fimCatalog,
            fimBootstrap: fimBootstrap);
        _preferencesForm.PrepareForDocumentHost();
        _preferencesForm.FormClosed += PreferencesForm_FormClosed;
        Controls.Add(_preferencesForm);
    }

    protected override string GetPersistString()
    {
        // Settings should not be restored as a SQL/file document on the next
        // startup; it is reopened explicitly from the Preferences menu.
        return "unsaved://JustyBase Settings";
    }

    protected override async void OnShown(EventArgs e)
    {
        base.OnShown(e);
        try
        {
            await _preferencesForm.RefreshForDocumentAsync();
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.WriteLine($"Preferences document load failed: {exception.GetType().Name}");
        }
        if (!_preferencesForm.Visible)
        {
            _preferencesForm.Show();
        }
    }

    private void PreferencesForm_FormClosed(object sender, FormClosedEventArgs e)
    {
        if (!IsDisposed && !Disposing)
        {
            Close();
        }
    }
}
