using AppBase.Common;
using AppBase.Common.Configuration;
using AppBase.Common.Interfaces;
using AppBase.Data;
using AppBase.Data.Completion;
using AppBase.Data.Core.Models;
using DatabaseDataGridView.WinForms;
using DatabaseDataGridView.WinForms.Coloring;
using JustData.Application.Settings;
using JustData.ViewModels.Preferences;
using JustyBaseLegacy.UI.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;


namespace JustyBaseLegacy.UI
{
    public partial class PreferencesForm : Form
    {
        private bool _documentHosted;
        private ColorSettingsModel _colorSettings;
        private readonly Action _repaintApplication;
        private readonly Action _saveManySqlToDisk;
        private IApplicationConfig _config;
        private readonly IApplicationSettingsContext _applicationSettingsContext;
        private readonly INetezzaAutocompleteState _netezzaAutocompleteState;
        private readonly ISnippetInitializationContext _snippetInitializationContext;
        private readonly Action _saveConfig;
        private readonly Action _saveRecentFiles;
        private readonly IUiHelperService _uiHelperService;
        private readonly IColorTheme _colorize;
        private readonly PreferencesViewModel _settingsViewModel;
        private SnippetSettings _pendingSnippets = new();
        private bool _specialColoringHandlerAttached;
        public PreferencesForm(Action repaintApplication, Action saveManySqlToDisk,
            IApplicationSettingsContext applicationSettingsContext,
            ISnippetInitializationContext snippetInitializationContext,
            Action saveConfig,
            Action saveRecentFiles,
            IUiHelperService uiHelperService,
            IColorTheme colorTheme,
            INetezzaAutocompleteState netezzaAutocompleteState,
            PreferencesViewModel? settingsViewModel = null)
        {
            _applicationSettingsContext = applicationSettingsContext ?? throw new ArgumentNullException(nameof(applicationSettingsContext));
            _netezzaAutocompleteState = netezzaAutocompleteState ?? throw new ArgumentNullException(nameof(netezzaAutocompleteState));
            _snippetInitializationContext = snippetInitializationContext ?? throw new ArgumentNullException(nameof(snippetInitializationContext));
            _saveConfig = saveConfig ?? throw new ArgumentNullException(nameof(saveConfig));
            _saveRecentFiles = saveRecentFiles ?? throw new ArgumentNullException(nameof(saveRecentFiles));
            _repaintApplication = repaintApplication ?? throw new ArgumentNullException(nameof(repaintApplication));
            _saveManySqlToDisk = saveManySqlToDisk ?? throw new ArgumentNullException(nameof(saveManySqlToDisk));
            _uiHelperService = uiHelperService;
            _config = _applicationSettingsContext.Config;
            _settingsViewModel = settingsViewModel ?? new PreferencesViewModel(
                new WinFormsApplicationSettingsStore(_applicationSettingsContext, _netezzaAutocompleteState),
                new WinFormsSettingsThemePreviewAdapter(_applicationSettingsContext, _repaintApplication));
            InitializeComponent();
            _colorize = colorTheme;
            BuildModernLayout();
            string lg = System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

            startupPathsDgv.Visible = true;
            _colorize.ColorForm(this);
            ApplyModernTheme();

            _uiHelperService.DoubleBufDateGridView(dgvStandard);
            _uiHelperService.DoubleBufDateGridView(dgvClassic);
            _uiHelperService.DoubleBufDateGridView(dgvQuick);
            _uiHelperService.DoubleBufDateGridView(dgvTypo);
            _uiHelperService.DoubleBufDateGridView(dgvKeywords);
            _uiHelperService.DoubleBufDateGridView(dgvColoringList1);
            _uiHelperService.DoubleBufDateGridView(dgvColoringList2);
            _uiHelperService.DoubleBufDateGridView(startupPathsDgv);

            toolTip1.SetToolTip(checkBoxSpecialColoring, "After changing state of this option remember to save [Ctrl + S] or Save menu");


            string[] files = System.IO.Directory.GetFiles(_applicationSettingsContext.ConfigDirectory);

            if (checkBoxSpecialColoring.Checked && _colorSettings is ColorSettingsModel colors)
            {
                tbColoring1.ForeColor = colors.FontkeyWordsStyle1;
                tbColoring1.BackColor = colors.BackgroundFastColored;

                tbColoring2.ForeColor = colors.FontkeyWordsStyle2;
                tbColoring2.BackColor = colors.BackgroundFastColored;
            }
            else
            {
                tbColoring1.ForeColor = Color.FromArgb(0, 0, 255);
                tbColoring2.ForeColor = Color.FromArgb(250, 0, 250);
            }
        }

        internal void PrepareForDocumentHost()
        {
            _documentHosted = true;
            TopLevel = false;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            MinimumSize = Size.Empty;
            MaximumSize = Size.Empty;
            Dock = DockStyle.Fill;
        }

        private async void PreferencesForm_Load(object sender, EventArgs e)
        {
            try
            {
                await RefreshForDocumentAsync();
            }
            catch (Exception exception)
            {
                Trace.WriteLine($"Preferences form load failed: {exception.GetType().Name}");
            }
        }

        internal async Task RefreshForDocumentAsync()
        {
            await _settingsViewModel.LoadAsync();
            _config = LegacyApplicationSettingsMapper.ToLegacy(_settingsViewModel.Draft);
            _pendingSnippets = _settingsViewModel.Draft.Snippets.Clone();
            LoadFromConfig();
            LoadSnippets();
            checkBoxSpecialColoring.Checked = _config.UseSpecialColoring;
            if (!_specialColoringHandlerAttached)
            {
                checkBoxSpecialColoring.CheckedChanged += CheckBoxSpecialColoring_CheckedChanged;
                _specialColoringHandlerAttached = true;
            }
            UpdateColorEditorAvailability();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!_settingsViewModel.IsSaved || _settingsViewModel.IsBusy)
            {
                _settingsViewModel.Cancel();
            }

            _settingsViewModel.Dispose();

            base.OnFormClosing(e);
        }

        private void CheckBoxSpecialColoring_CheckedChanged(object sender, EventArgs e)
        {

            UpdateColorEditorAvailability();
        }

        private void UpdateColorEditorAvailability()
        {
            _colorEditorSections.Enabled = checkBoxSpecialColoring.Checked;
            _editorFontButton.Enabled = checkBoxSpecialColoring.Checked;
        }

        private void LoadFromConfig()
        {
            startupPathsDgv.Rows.Clear();
            ColorSettingsModel pgdColors = new ColorSettingsModel()
            {
                FontString = _config.FontName,
                FontSize = _config.FontSize,

                BackgroundFastColored = Color.FromArgb(_config.BackgroundFastColored[3], _config.BackgroundFastColored[0], _config.BackgroundFastColored[1], _config.BackgroundFastColored[2]),
                SelectionColorFastColored = Color.FromArgb(_config.SelectionColorFastColored[3], _config.SelectionColorFastColored[0], _config.SelectionColorFastColored[1], _config.SelectionColorFastColored[2]),
                DisabledColorFastColored = Color.FromArgb(_config.DisabledColorFastColored[3], _config.DisabledColorFastColored[0], _config.DisabledColorFastColored[1], _config.DisabledColorFastColored[2]),
                IndentBackColorFastColored = Color.FromArgb(_config.IndentBackColorFastColored[3], _config.IndentBackColorFastColored[0], _config.IndentBackColorFastColored[1], _config.IndentBackColorFastColored[2]),
                LineNumberColorFastColored = Color.FromArgb(_config.LineNumberColorFastColored[3], _config.LineNumberColorFastColored[0], _config.LineNumberColorFastColored[1], _config.LineNumberColorFastColored[2]),
                FoldingIndicatorColorFastColored = Color.FromArgb(_config.FoldingIndicatorColorFastColored[3], _config.FoldingIndicatorColorFastColored[0], _config.FoldingIndicatorColorFastColored[1], _config.FoldingIndicatorColorFastColored[2]),
                ForeColorFastColored = Color.FromArgb(_config.ForeColorFastColored[3], _config.ForeColorFastColored[0], _config.ForeColorFastColored[1], _config.ForeColorFastColored[2]),
                FontkeyWordsStyle1 = Color.FromArgb(_config.FontkeyWordsStyle1[3], _config.FontkeyWordsStyle1[0], _config.FontkeyWordsStyle1[1], _config.FontkeyWordsStyle1[2]),
                FontkeyWordsStyle2 = Color.FromArgb(_config.FontkeyWordsStyle2[3], _config.FontkeyWordsStyle2[0], _config.FontkeyWordsStyle2[1], _config.FontkeyWordsStyle2[2]),
                FontparamStyle = Color.FromArgb(_config.FontparamStyle[3], _config.FontparamStyle[0], _config.FontparamStyle[1], _config.FontparamStyle[2]),
                FontmyCommandsStyle = Color.FromArgb(_config.FontmyCommandsStyle[3], _config.FontmyCommandsStyle[0], _config.FontmyCommandsStyle[1], _config.FontmyCommandsStyle[2]),
                FontnumberStyle = Color.FromArgb(_config.FontnumberStyle[3], _config.FontnumberStyle[0], _config.FontnumberStyle[1], _config.FontnumberStyle[2]),
                FontcommentsStyle = Color.FromArgb(_config.FontcommentsStyle[3], _config.FontcommentsStyle[0], _config.FontcommentsStyle[1], _config.FontcommentsStyle[2]),
                FontstringsStyle = Color.FromArgb(_config.FontstringsStyle[3], _config.FontstringsStyle[0], _config.FontstringsStyle[1], _config.FontstringsStyle[2]),
                FontsameWordsStyle = Color.FromArgb(_config.FontsameWordsStyle[3], _config.FontsameWordsStyle[0], _config.FontsameWordsStyle[1], _config.FontsameWordsStyle[2]),
                DgvDefaultCellStyleBackColor = Color.FromArgb(_config.DgvDefaultCellStyleBackColor[3], _config.DgvDefaultCellStyleBackColor[0], _config.DgvDefaultCellStyleBackColor[1], _config.DgvDefaultCellStyleBackColor[2]),
                DgvAlternatingRowsDefaultCellStyleBackColor = Color.FromArgb(_config.DgvAlternatingRowsDefaultCellStyleBackColor[3], _config.DgvAlternatingRowsDefaultCellStyleBackColor[0], _config.DgvAlternatingRowsDefaultCellStyleBackColor[1], _config.DgvAlternatingRowsDefaultCellStyleBackColor[2]),
                DgvDefaultCellStyleForeColor = Color.FromArgb(_config.DgvDefaultCellStyleForeColor[3], _config.DgvDefaultCellStyleForeColor[0], _config.DgvDefaultCellStyleForeColor[1], _config.DgvDefaultCellStyleForeColor[2]),
                DgvRowHeadersDefaultCellStyleBack = Color.FromArgb(_config.DgvRowHeadersDefaultCellStyleBack[3], _config.DgvRowHeadersDefaultCellStyleBack[0], _config.DgvRowHeadersDefaultCellStyleBack[1], _config.DgvRowHeadersDefaultCellStyleBack[2]),
                DgvColumnHeadersDefaultCellStyleFore = Color.FromArgb(_config.DgvColumnHeadersDefaultCellStyleFore[3], _config.DgvColumnHeadersDefaultCellStyleFore[0], _config.DgvColumnHeadersDefaultCellStyleFore[1], _config.DgvColumnHeadersDefaultCellStyleFore[2]),
                DgvColumnHeadersDefaultCellStyleBack = Color.FromArgb(_config.DgvColumnHeadersDefaultCellStyleBack[3], _config.DgvColumnHeadersDefaultCellStyleBack[0], _config.DgvColumnHeadersDefaultCellStyleBack[1], _config.DgvColumnHeadersDefaultCellStyleBack[2]),
                DocMapBackColor = Color.FromArgb(_config.DocMapBackColor[3], _config.DocMapBackColor[0], _config.DocMapBackColor[1], _config.DocMapBackColor[2]),
                DocMapForeColor = Color.FromArgb(_config.DocMapForeColor[3], _config.DocMapForeColor[0], _config.DocMapForeColor[1], _config.DocMapForeColor[2]),
                TabColor = Color.FromArgb(_config.TabColor[3], _config.TabColor[0], _config.TabColor[1], _config.TabColor[2]),
                SelectedtabColor = Color.FromArgb(_config.SelectedtabColor[3], _config.SelectedtabColor[0], _config.SelectedtabColor[1], _config.SelectedtabColor[2]),
                TabTitleColor = Color.FromArgb(_config.TabTitleColor[3], _config.TabTitleColor[0], _config.TabTitleColor[1], _config.TabTitleColor[2]),
                StripBack = Color.FromArgb(_config.StripBack[3], _config.StripBack[0], _config.StripBack[1], _config.StripBack[2]),
                StripFore = Color.FromArgb(_config.StripFore[3], _config.StripFore[0], _config.StripFore[1], _config.StripFore[2]),
                TreeViewBackColor = Color.FromArgb(_config.TreeViewBackColor[3], _config.TreeViewBackColor[0], _config.TreeViewBackColor[1], _config.TreeViewBackColor[2]),
                TreeViewForeColor = Color.FromArgb(_config.TreeViewForeColor[3], _config.TreeViewForeColor[0], _config.TreeViewForeColor[1], _config.TreeViewForeColor[2]),
                TreeViewLineColor = Color.FromArgb(_config.TreeViewLineColor[3], _config.TreeViewLineColor[0], _config.TreeViewLineColor[1], _config.TreeViewLineColor[2]),
                TextBoxFileSearchBackColor = Color.FromArgb(_config.TextBoxFileSearchBackColor[3], _config.TextBoxFileSearchBackColor[0], _config.TextBoxFileSearchBackColor[1], _config.TextBoxFileSearchBackColor[2]),
                TextBoxFileSearchForeColor = Color.FromArgb(_config.TextBoxFileSearchForeColor[3], _config.TextBoxFileSearchForeColor[0], _config.TextBoxFileSearchForeColor[1], _config.TextBoxFileSearchForeColor[2]),
                MenuItemSelected = Color.FromArgb(_config.MenuItemSelected[3], _config.MenuItemSelected[0], _config.MenuItemSelected[1], _config.MenuItemSelected[2]),
                MenuItemSelectedGradientBegin = Color.FromArgb(_config.MenuItemSelectedGradientBegin[3], _config.MenuItemSelectedGradientBegin[0], _config.MenuItemSelectedGradientBegin[1], _config.MenuItemSelectedGradientBegin[2]),
                MenuItemSelectedGradientEnd = Color.FromArgb(_config.MenuItemSelectedGradientEnd[3], _config.MenuItemSelectedGradientEnd[0], _config.MenuItemSelectedGradientEnd[1], _config.MenuItemSelectedGradientEnd[2]),
                MenuItemBorder = Color.FromArgb(_config.MenuItemBorder[3], _config.MenuItemBorder[0], _config.MenuItemBorder[1], _config.MenuItemBorder[2]),
                MenuItemPressedGradientBegin = Color.FromArgb(_config.MenuItemPressedGradientBegin[3], _config.MenuItemPressedGradientBegin[0], _config.MenuItemPressedGradientBegin[1], _config.MenuItemPressedGradientBegin[2]),
                MenuItemPressedGradientMiddle = Color.FromArgb(_config.MenuItemPressedGradientMiddle[3], _config.MenuItemPressedGradientMiddle[0], _config.MenuItemPressedGradientMiddle[1], _config.MenuItemPressedGradientMiddle[2]),
                MenuItemPressedGradientEnd = Color.FromArgb(_config.MenuItemPressedGradientEnd[3], _config.MenuItemPressedGradientEnd[0], _config.MenuItemPressedGradientEnd[1], _config.MenuItemPressedGradientEnd[2]),
                ButtonSelectedHighlightBorder = Color.FromArgb(_config.ButtonSelectedHighlightBorder[3], _config.ButtonSelectedHighlightBorder[0], _config.ButtonSelectedHighlightBorder[1], _config.ButtonSelectedHighlightBorder[2]),
                GroupingRowColorBack = Color.FromArgb(_config.GroupingRowColorBack[3], _config.GroupingRowColorBack[0], _config.GroupingRowColorBack[1], _config.GroupingRowColorBack[2])
            };
            _colorSettings = pgdColors;
            UpdateColorEditorValues();

            checkBoxSpecialColoring.Checked = _config.UseSpecialColoring;

            cbUseXlsb.Checked = _config.UseXlsb;

            cbImportExisting.Checked = _config.ImportExisting;

            rbCtrlVAsk.Checked = _config.CtrlVmode == 0;
            rbCtrlVAuto.Checked = _config.CtrlVmode == 1;
            rbCtrlVNormal.Checked = _config.CtrlVmode == 2;

            tbCSVSep.Text = _config.SepInExportedCsv;
            tbCsvDecimalDelim.Text = _config.DecimalDelimInCsv;
            tbSepRowsInExportedCsv.Text = _config.SepRowsInExportedCsv;
            tbEncondingName.Text = _config.EncondingName;

            nlongQueryWarning.Value = _config.LongQueryWarning / 600;
            nestimatedWarning.Value = _config.EstimatedWarning / (1000 * 60);
            nEstimatedWarningInterval.Value = _config.EstimatedWarningInterval / 600;

            cbDateFormat.Text = _config.DateTimeFormat;
            if (!cbDateFormat.Items.Contains(_config.DateTimeFormat))
            {
                int n = cbDateFormat.Items.Add(_config.DateTimeFormat);
                cbDateFormat.SelectedIndex = n;
            }
            else
            {
                int n = cbDateFormat.Items.IndexOf(_config.DateTimeFormat);
                cbDateFormat.SelectedIndex = n;
            }

            cbIntFormat.Text = _config.IntegerFormat;
            if (!cbIntFormat.Items.Contains(_config.IntegerFormat))
            {
                int n = cbIntFormat.Items.Add(_config.IntegerFormat);
                cbIntFormat.SelectedIndex = n;
            }
            else
            {
                int n = cbIntFormat.Items.IndexOf(_config.IntegerFormat);
                cbIntFormat.SelectedIndex = n;
            }

            cbDecimalFormat.Text = _config.DecimalFormat;
            if (!cbDecimalFormat.Items.Contains(_config.DecimalFormat))
            {
                int n = cbDecimalFormat.Items.Add(_config.DecimalFormat);
                cbDecimalFormat.SelectedIndex = n;
            }
            else
            {
                int n = cbDecimalFormat.Items.IndexOf(_config.DecimalFormat);
                cbDecimalFormat.SelectedIndex = n;
            }



            this.rowsLimit.Value = _config.ResultRowsLimit;
            this.numResultRowsLimitWarning.Value = _config.ResultRowsLimitWarning;
            this.numCommandTimeout.Value = _config.CommandTimeout;

            cbUseSpecialTabNames.Checked = _config.UseSpecialTabNames;
            cbFirstLaunch.Checked = !_config.NotFirstLaunch;

            cbPinDataByDefault.Checked = _config.PinDataByDefault;
            cbDontShowOwner.Checked = _config.DontShowOwner;
            cbSortMethod.SelectedIndex = _config.SortMethod;

            cbBracketFolding.Checked = _config.BracketFolding;
            cbDontIndent.Checked = _config.DontUseIndent;

            cbWordWrap.Checked = (_config.WordWrap == 1);
            cbWordWrapAutoIndent.Checked = (_config.WordWrapAutoIndent == 1);


            cbAutoCompleteBrackets.Checked = _config.AutoCompleteBrackets;
            nuFileSearchTimeout.Value = _config.FileSearchTimeout;


            cbResetSchema.Checked = _config.ResetSchema;
            cbLoadSourcesOnStartup.Checked = _config.LoadSourcesOnStartup;
            cbOnlineOnlyDdls.Checked = _config.OnlineOnlyDdls;
            nuMaxSchemaParallelism.Value = _config.MaxSchemaParallelism;
            cbSimpleStarupRestore.Checked = _config.SimpleStartupRestore;


            foreach (var row in _config.StartFilesExtra)
            {
                int num = startupPathsDgv.Rows.Add();
                startupPathsDgv.Rows[num].Cells[0].Value = row.Key;
                startupPathsDgv.Rows[num].Cells[2].Value = row.Value;
            }
        }


        private void SaveLocalyMain()
        {
            var pgdColors = _colorSettings;
            if (pgdColors is null)
            {
                return;
            }

            #region editor and application colors

            _config.UseSpecialColoring = this.checkBoxSpecialColoring.Checked;
            _config.FontSize = pgdColors.FontSize;
            _config.FontName = pgdColors.FontString;

            _config.BackgroundFastColored[0] = pgdColors.BackgroundFastColored.R;
            _config.BackgroundFastColored[1] = pgdColors.BackgroundFastColored.G;
            _config.BackgroundFastColored[2] = pgdColors.BackgroundFastColored.B;
            _config.BackgroundFastColored[3] = pgdColors.BackgroundFastColored.A;

            _config.SelectionColorFastColored[0] = pgdColors.SelectionColorFastColored.R;
            _config.SelectionColorFastColored[1] = pgdColors.SelectionColorFastColored.G;
            _config.SelectionColorFastColored[2] = pgdColors.SelectionColorFastColored.B;
            _config.SelectionColorFastColored[3] = pgdColors.SelectionColorFastColored.A;

            _config.DisabledColorFastColored[0] = pgdColors.DisabledColorFastColored.R;
            _config.DisabledColorFastColored[1] = pgdColors.DisabledColorFastColored.G;
            _config.DisabledColorFastColored[2] = pgdColors.DisabledColorFastColored.B;
            _config.DisabledColorFastColored[3] = pgdColors.DisabledColorFastColored.A;

            _config.IndentBackColorFastColored[0] = pgdColors.IndentBackColorFastColored.R;
            _config.IndentBackColorFastColored[1] = pgdColors.IndentBackColorFastColored.G;
            _config.IndentBackColorFastColored[2] = pgdColors.IndentBackColorFastColored.B;
            _config.IndentBackColorFastColored[3] = pgdColors.IndentBackColorFastColored.A;

            _config.LineNumberColorFastColored[0] = pgdColors.LineNumberColorFastColored.R;
            _config.LineNumberColorFastColored[1] = pgdColors.LineNumberColorFastColored.G;
            _config.LineNumberColorFastColored[2] = pgdColors.LineNumberColorFastColored.B;
            _config.LineNumberColorFastColored[3] = pgdColors.LineNumberColorFastColored.A;

            _config.FoldingIndicatorColorFastColored[0] = pgdColors.FoldingIndicatorColorFastColored.R;
            _config.FoldingIndicatorColorFastColored[1] = pgdColors.FoldingIndicatorColorFastColored.G;
            _config.FoldingIndicatorColorFastColored[2] = pgdColors.FoldingIndicatorColorFastColored.B;
            _config.FoldingIndicatorColorFastColored[3] = pgdColors.FoldingIndicatorColorFastColored.A;

            _config.ForeColorFastColored[0] = pgdColors.ForeColorFastColored.R;
            _config.ForeColorFastColored[1] = pgdColors.ForeColorFastColored.G;
            _config.ForeColorFastColored[2] = pgdColors.ForeColorFastColored.B;
            _config.ForeColorFastColored[3] = pgdColors.ForeColorFastColored.A;

            _config.FontkeyWordsStyle1[0] = pgdColors.FontkeyWordsStyle1.R;
            _config.FontkeyWordsStyle1[1] = pgdColors.FontkeyWordsStyle1.G;
            _config.FontkeyWordsStyle1[2] = pgdColors.FontkeyWordsStyle1.B;
            _config.FontkeyWordsStyle1[3] = pgdColors.FontkeyWordsStyle1.A;

            _config.FontkeyWordsStyle2[0] = pgdColors.FontkeyWordsStyle2.R;
            _config.FontkeyWordsStyle2[1] = pgdColors.FontkeyWordsStyle2.G;
            _config.FontkeyWordsStyle2[2] = pgdColors.FontkeyWordsStyle2.B;
            _config.FontkeyWordsStyle2[3] = pgdColors.FontkeyWordsStyle2.A;


            _config.FontparamStyle[0] = pgdColors.FontparamStyle.R;
            _config.FontparamStyle[1] = pgdColors.FontparamStyle.G;
            _config.FontparamStyle[2] = pgdColors.FontparamStyle.B;
            _config.FontparamStyle[3] = pgdColors.FontparamStyle.A;

            _config.FontmyCommandsStyle[0] = pgdColors.FontmyCommandsStyle.R;
            _config.FontmyCommandsStyle[1] = pgdColors.FontmyCommandsStyle.G;
            _config.FontmyCommandsStyle[2] = pgdColors.FontmyCommandsStyle.B;
            _config.FontmyCommandsStyle[3] = pgdColors.FontmyCommandsStyle.A;

            _config.FontnumberStyle[0] = pgdColors.FontnumberStyle.R;
            _config.FontnumberStyle[1] = pgdColors.FontnumberStyle.G;
            _config.FontnumberStyle[2] = pgdColors.FontnumberStyle.B;
            _config.FontnumberStyle[3] = pgdColors.FontnumberStyle.A;

            _config.FontcommentsStyle[0] = pgdColors.FontcommentsStyle.R;
            _config.FontcommentsStyle[1] = pgdColors.FontcommentsStyle.G;
            _config.FontcommentsStyle[2] = pgdColors.FontcommentsStyle.B;
            _config.FontcommentsStyle[3] = pgdColors.FontcommentsStyle.A;

            _config.FontstringsStyle[0] = pgdColors.FontstringsStyle.R;
            _config.FontstringsStyle[1] = pgdColors.FontstringsStyle.G;
            _config.FontstringsStyle[2] = pgdColors.FontstringsStyle.B;
            _config.FontstringsStyle[3] = pgdColors.FontstringsStyle.A;

            _config.FontsameWordsStyle[0] = pgdColors.FontsameWordsStyle.R;
            _config.FontsameWordsStyle[1] = pgdColors.FontsameWordsStyle.G;
            _config.FontsameWordsStyle[2] = pgdColors.FontsameWordsStyle.B;
            _config.FontsameWordsStyle[3] = pgdColors.FontsameWordsStyle.A;

            _config.DgvDefaultCellStyleBackColor[0] = pgdColors.DgvDefaultCellStyleBackColor.R;
            _config.DgvDefaultCellStyleBackColor[1] = pgdColors.DgvDefaultCellStyleBackColor.G;
            _config.DgvDefaultCellStyleBackColor[2] = pgdColors.DgvDefaultCellStyleBackColor.B;
            _config.DgvDefaultCellStyleBackColor[3] = pgdColors.DgvDefaultCellStyleBackColor.A;

            _config.DgvAlternatingRowsDefaultCellStyleBackColor[0] = pgdColors.DgvAlternatingRowsDefaultCellStyleBackColor.R;
            _config.DgvAlternatingRowsDefaultCellStyleBackColor[1] = pgdColors.DgvAlternatingRowsDefaultCellStyleBackColor.G;
            _config.DgvAlternatingRowsDefaultCellStyleBackColor[2] = pgdColors.DgvAlternatingRowsDefaultCellStyleBackColor.B;
            _config.DgvAlternatingRowsDefaultCellStyleBackColor[3] = pgdColors.DgvAlternatingRowsDefaultCellStyleBackColor.A;

            _config.DgvDefaultCellStyleForeColor[0] = pgdColors.DgvDefaultCellStyleForeColor.R;
            _config.DgvDefaultCellStyleForeColor[1] = pgdColors.DgvDefaultCellStyleForeColor.G;
            _config.DgvDefaultCellStyleForeColor[2] = pgdColors.DgvDefaultCellStyleForeColor.B;
            _config.DgvDefaultCellStyleForeColor[3] = pgdColors.DgvDefaultCellStyleForeColor.A;

            _config.DgvRowHeadersDefaultCellStyleBack[0] = pgdColors.DgvRowHeadersDefaultCellStyleBack.R;
            _config.DgvRowHeadersDefaultCellStyleBack[1] = pgdColors.DgvRowHeadersDefaultCellStyleBack.G;
            _config.DgvRowHeadersDefaultCellStyleBack[2] = pgdColors.DgvRowHeadersDefaultCellStyleBack.B;
            _config.DgvRowHeadersDefaultCellStyleBack[3] = pgdColors.DgvRowHeadersDefaultCellStyleBack.A;

            _config.DgvColumnHeadersDefaultCellStyleFore[0] = pgdColors.DgvColumnHeadersDefaultCellStyleFore.R;
            _config.DgvColumnHeadersDefaultCellStyleFore[1] = pgdColors.DgvColumnHeadersDefaultCellStyleFore.G;
            _config.DgvColumnHeadersDefaultCellStyleFore[2] = pgdColors.DgvColumnHeadersDefaultCellStyleFore.B;
            _config.DgvColumnHeadersDefaultCellStyleFore[3] = pgdColors.DgvColumnHeadersDefaultCellStyleFore.A;

            _config.DgvColumnHeadersDefaultCellStyleBack[0] = pgdColors.DgvColumnHeadersDefaultCellStyleBack.R;
            _config.DgvColumnHeadersDefaultCellStyleBack[1] = pgdColors.DgvColumnHeadersDefaultCellStyleBack.G;
            _config.DgvColumnHeadersDefaultCellStyleBack[2] = pgdColors.DgvColumnHeadersDefaultCellStyleBack.B;
            _config.DgvColumnHeadersDefaultCellStyleBack[3] = pgdColors.DgvColumnHeadersDefaultCellStyleBack.A;

            _config.DocMapBackColor[0] = pgdColors.DocMapBackColor.R;
            _config.DocMapBackColor[1] = pgdColors.DocMapBackColor.G;
            _config.DocMapBackColor[2] = pgdColors.DocMapBackColor.B;
            _config.DocMapBackColor[3] = pgdColors.DocMapBackColor.A;

            _config.DocMapForeColor[0] = pgdColors.DocMapForeColor.R;
            _config.DocMapForeColor[1] = pgdColors.DocMapForeColor.G;
            _config.DocMapForeColor[2] = pgdColors.DocMapForeColor.B;
            _config.DocMapForeColor[3] = pgdColors.DocMapForeColor.A;

            _config.TabColor[0] = pgdColors.TabColor.R;
            _config.TabColor[1] = pgdColors.TabColor.G;
            _config.TabColor[2] = pgdColors.TabColor.B;
            _config.TabColor[3] = pgdColors.TabColor.A;

            _config.SelectedtabColor[0] = pgdColors.SelectedtabColor.R;
            _config.SelectedtabColor[1] = pgdColors.SelectedtabColor.G;
            _config.SelectedtabColor[2] = pgdColors.SelectedtabColor.B;
            _config.SelectedtabColor[3] = pgdColors.SelectedtabColor.A;

            _config.TabTitleColor[0] = pgdColors.TabTitleColor.R;
            _config.TabTitleColor[1] = pgdColors.TabTitleColor.G;
            _config.TabTitleColor[2] = pgdColors.TabTitleColor.B;
            _config.TabTitleColor[3] = pgdColors.TabTitleColor.A;

            _config.StripBack[0] = pgdColors.StripBack.R;
            _config.StripBack[1] = pgdColors.StripBack.G;
            _config.StripBack[2] = pgdColors.StripBack.B;
            _config.StripBack[3] = pgdColors.StripBack.A;

            _config.StripFore[0] = pgdColors.StripFore.R;
            _config.StripFore[1] = pgdColors.StripFore.G;
            _config.StripFore[2] = pgdColors.StripFore.B;
            _config.StripFore[3] = pgdColors.StripFore.A;

            _config.TreeViewBackColor[0] = pgdColors.TreeViewBackColor.R;
            _config.TreeViewBackColor[1] = pgdColors.TreeViewBackColor.G;
            _config.TreeViewBackColor[2] = pgdColors.TreeViewBackColor.B;
            _config.TreeViewBackColor[3] = pgdColors.TreeViewBackColor.A;

            _config.TreeViewForeColor[0] = pgdColors.TreeViewForeColor.R;
            _config.TreeViewForeColor[1] = pgdColors.TreeViewForeColor.G;
            _config.TreeViewForeColor[2] = pgdColors.TreeViewForeColor.B;
            _config.TreeViewForeColor[3] = pgdColors.TreeViewForeColor.A;

            _config.TreeViewLineColor[0] = pgdColors.TreeViewLineColor.R;
            _config.TreeViewLineColor[1] = pgdColors.TreeViewLineColor.G;
            _config.TreeViewLineColor[2] = pgdColors.TreeViewLineColor.B;
            _config.TreeViewLineColor[3] = pgdColors.TreeViewLineColor.A;

            _config.TextBoxFileSearchBackColor[0] = pgdColors.TextBoxFileSearchBackColor.R;
            _config.TextBoxFileSearchBackColor[1] = pgdColors.TextBoxFileSearchBackColor.G;
            _config.TextBoxFileSearchBackColor[2] = pgdColors.TextBoxFileSearchBackColor.B;
            _config.TextBoxFileSearchBackColor[3] = pgdColors.TextBoxFileSearchBackColor.A;

            _config.TextBoxFileSearchForeColor[0] = pgdColors.TextBoxFileSearchForeColor.R;
            _config.TextBoxFileSearchForeColor[1] = pgdColors.TextBoxFileSearchForeColor.G;
            _config.TextBoxFileSearchForeColor[2] = pgdColors.TextBoxFileSearchForeColor.B;
            _config.TextBoxFileSearchForeColor[3] = pgdColors.TextBoxFileSearchForeColor.A;

            _config.MenuItemSelected[0] = pgdColors.MenuItemSelected.R;
            _config.MenuItemSelected[1] = pgdColors.MenuItemSelected.G;
            _config.MenuItemSelected[2] = pgdColors.MenuItemSelected.B;
            _config.MenuItemSelected[3] = pgdColors.MenuItemSelected.A;

            _config.MenuItemSelectedGradientBegin[0] = pgdColors.MenuItemSelectedGradientBegin.R;
            _config.MenuItemSelectedGradientBegin[1] = pgdColors.MenuItemSelectedGradientBegin.G;
            _config.MenuItemSelectedGradientBegin[2] = pgdColors.MenuItemSelectedGradientBegin.B;
            _config.MenuItemSelectedGradientBegin[3] = pgdColors.MenuItemSelectedGradientBegin.A;

            _config.MenuItemSelectedGradientEnd[0] = pgdColors.MenuItemSelectedGradientEnd.R;
            _config.MenuItemSelectedGradientEnd[1] = pgdColors.MenuItemSelectedGradientEnd.G;
            _config.MenuItemSelectedGradientEnd[2] = pgdColors.MenuItemSelectedGradientEnd.B;
            _config.MenuItemSelectedGradientEnd[3] = pgdColors.MenuItemSelectedGradientEnd.A;

            _config.MenuItemBorder[0] = pgdColors.MenuItemBorder.R;
            _config.MenuItemBorder[1] = pgdColors.MenuItemBorder.G;
            _config.MenuItemBorder[2] = pgdColors.MenuItemBorder.B;
            _config.MenuItemBorder[3] = pgdColors.MenuItemBorder.A;

            _config.MenuItemPressedGradientBegin[0] = pgdColors.MenuItemPressedGradientBegin.R;
            _config.MenuItemPressedGradientBegin[1] = pgdColors.MenuItemPressedGradientBegin.G;
            _config.MenuItemPressedGradientBegin[2] = pgdColors.MenuItemPressedGradientBegin.B;
            _config.MenuItemPressedGradientBegin[3] = pgdColors.MenuItemPressedGradientBegin.A;

            _config.MenuItemPressedGradientMiddle[0] = pgdColors.MenuItemPressedGradientMiddle.R;
            _config.MenuItemPressedGradientMiddle[1] = pgdColors.MenuItemPressedGradientMiddle.G;
            _config.MenuItemPressedGradientMiddle[2] = pgdColors.MenuItemPressedGradientMiddle.B;
            _config.MenuItemPressedGradientMiddle[3] = pgdColors.MenuItemPressedGradientMiddle.A;

            _config.MenuItemPressedGradientEnd[0] = pgdColors.MenuItemPressedGradientEnd.R;
            _config.MenuItemPressedGradientEnd[1] = pgdColors.MenuItemPressedGradientEnd.G;
            _config.MenuItemPressedGradientEnd[2] = pgdColors.MenuItemPressedGradientEnd.B;
            _config.MenuItemPressedGradientEnd[3] = pgdColors.MenuItemPressedGradientEnd.A;

            _config.ButtonSelectedHighlightBorder[0] = pgdColors.ButtonSelectedHighlightBorder.R;
            _config.ButtonSelectedHighlightBorder[1] = pgdColors.ButtonSelectedHighlightBorder.G;
            _config.ButtonSelectedHighlightBorder[2] = pgdColors.ButtonSelectedHighlightBorder.B;
            _config.ButtonSelectedHighlightBorder[3] = pgdColors.ButtonSelectedHighlightBorder.A;

            _config.GroupingRowColorBack[0] = pgdColors.GroupingRowColorBack.R;
            _config.GroupingRowColorBack[1] = pgdColors.GroupingRowColorBack.G;
            _config.GroupingRowColorBack[2] = pgdColors.GroupingRowColorBack.B;
            _config.GroupingRowColorBack[3] = pgdColors.GroupingRowColorBack.A;
            #endregion


            _config.UseSpecialColoring = checkBoxSpecialColoring.Checked;
            _config.UseXlsb = cbUseXlsb.Checked;
            _config.ImportExisting = cbImportExisting.Checked;

            if (rbCtrlVAsk.Checked)
            {
                _config.CtrlVmode = 0;
            }
            else if (rbCtrlVAuto.Checked)
            {
                _config.CtrlVmode = 1;
            }
            else if (rbCtrlVNormal.Checked)
            {
                _config.CtrlVmode = 2;
            }

            _config.SepInExportedCsv = tbCSVSep.Text;
            _config.DecimalDelimInCsv = tbCsvDecimalDelim.Text;
            _config.SepRowsInExportedCsv = tbSepRowsInExportedCsv.Text;
            _config.EncondingName = tbEncondingName.Text;

            _config.LongQueryWarning = (int)nlongQueryWarning.Value * 600;
            _config.EstimatedWarning = (int)nestimatedWarning.Value * 1000 * 60;
            _config.EstimatedWarningInterval = (int)nEstimatedWarningInterval.Value * 600;

            _config.DateTimeFormat = cbDateFormat.Text;
            _config.IntegerFormat = cbIntFormat.Text;
            _config.DecimalFormat = cbDecimalFormat.Text;

            _config.ResultRowsLimit = (int)this.rowsLimit.Value;
            _config.ResultRowsLimitWarning = (int)this.numResultRowsLimitWarning.Value;
            _config.CommandTimeout = (int)this.numCommandTimeout.Value;

            _config.UseSpecialTabNames = cbUseSpecialTabNames.Checked;
            _config.NotFirstLaunch = !cbFirstLaunch.Checked;

            _config.PinDataByDefault = cbPinDataByDefault.Checked;
            _config.DontShowOwner = cbDontShowOwner.Checked;
            _config.SortMethod = cbSortMethod.SelectedIndex;
            _config.BracketFolding = cbBracketFolding.Checked;
            _config.DontUseIndent = cbDontIndent.Checked;

            _config.WordWrap = (cbWordWrap.Checked ? 1 : -1);
            _config.WordWrapAutoIndent = (cbWordWrapAutoIndent.Checked ? 1 : -1);

            _config.AutoCompleteBrackets = cbAutoCompleteBrackets.Checked;
            _config.FileSearchTimeout = (int)nuFileSearchTimeout.Value;


            _config.ResetSchema = cbResetSchema.Checked;
            _config.LoadSourcesOnStartup = cbLoadSourcesOnStartup.Checked;
            _config.OnlineOnlyDdls = cbOnlineOnlyDdls.Checked;
            _config.MaxSchemaParallelism = (int)this.nuMaxSchemaParallelism.Value;
            _config.SimpleStartupRestore = cbSimpleStarupRestore.Checked;

            foreach (DataGridViewRow row in startupPathsDgv.Rows)
            {
                string tempPath = (string)((DataGridViewTextBoxCell)row.Cells[0]).Value;
                if (string.IsNullOrWhiteSpace(tempPath))
                {
                    continue;
                }
                var obj = ((DataGridViewCheckBoxCell)row.Cells[2]).Value;
                bool val = false;
                if (obj is not null)
                {
                    val = (bool)obj;
                }

                _config.StartFilesExtra[(string)((DataGridViewTextBoxCell)row.Cells[0]).Value] = val;
            }
        }

        (string, string) getNameOfMonkey(string snip)
        {
            if (!snip.Contains(' '))
            {
                MessageBox.Show("A name is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return ("", "");
            }

            int n = snip.IndexOf(' ');

            return (snip.Substring(2, n - 2), snip.Substring(n + 1).Trim());
        }

        private void LoadSnippets()
        {
            // The settings form can be opened before an editor creates the
            // autocomplete provider. Load the persisted/default snippet file
            // here as well, so a first installation is not shown as empty.
            _snippetInitializationContext.Initialize(
                JustData.Properties.Resources.snipety,
                JustData.Properties.Resources.special_names);
            LegacySnippetsProvider.EnsureSnippetsLoaded(_applicationSettingsContext, _netezzaAutocompleteState);

            dgvKeywords.Rows.Clear();
            dgvStandard.Rows.Clear();
            dgvClassic.Rows.Clear();
            dgvQuick.Rows.Clear();
            dgvColoringList1.Rows.Clear();
            dgvColoringList2.Rows.Clear();
            dgvTypo.Rows.Clear();

            if (_netezzaAutocompleteState.Keywords is not null)
            {
                for (int i = 0; i < _netezzaAutocompleteState.Keywords.Count; i++)
                {
                    dgvKeywords.Rows.Add(_netezzaAutocompleteState.Keywords[i]);
                }
            }

            if (_netezzaAutocompleteState.Snippets is not null)
            {
                for (int i = 0; i < _netezzaAutocompleteState.Snippets.Count; i++)
                {
                    dgvStandard.Rows.Add(_netezzaAutocompleteState.Snippets[i]);
                }
            }

            if (_netezzaAutocompleteState.MonkeySnippets is not null)
            {
                for (int i = 0; i < _netezzaAutocompleteState.MonkeySnippets.Count; i++)
                {
                    var x = getNameOfMonkey(_netezzaAutocompleteState.MonkeySnippets[i]);
                    dgvClassic.Rows.Add(x.Item1, x.Item2);
                }
            }

            foreach (var item in _config.QuickSnippets)
            {
                dgvQuick.Rows.Add(item.Key.ToUpper(), item.Value);
            }

            for (int i = 0; i < _config.KeyWordsListForColoring1.Count; i++)
            {
                dgvColoringList1.Rows.Add(_config.KeyWordsListForColoring1[i]);
            }

            for (int i = 0; i < _config.KeyWordsListForColoring2.Count; i++)
            {
                dgvColoringList2.Rows.Add(_config.KeyWordsListForColoring2[i]);
            }

            checkBoxTypo.Checked = _config.TypoCorrect;
            numericUpDownTypo.Value = (decimal)_config.TypoLimit;
            if (checkBoxTypo.Checked)
            {
                dgvTypo.Enabled = true;
                numericUpDownTypo.Enabled = true;
            }
            else
            {
                dgvTypo.Enabled = false;
                numericUpDownTypo.Enabled = false;
            }

            foreach (var item in _config.TypoPatternList)
            {
                dgvTypo.Rows.Add(item);
            }
        }


        private void SaveSnippetsLoccaly()
        {
            List<string> temp = [];
            for (int i = 0; i < dgvKeywords.Rows.Count; i++)
            {
                if (dgvKeywords[0, i].Value != null)
                {
                    temp.Add(dgvKeywords[0, i].Value as string);
                }
            }
            _pendingSnippets.Keywords = temp.ToList();

            temp.Clear();
            for (int i = 0; i < dgvStandard.Rows.Count; i++)
            {
                if (dgvStandard[0, i].Value != null)
                {
                    temp.Add(dgvStandard[0, i].Value as string);
                }
            }
            _pendingSnippets.Snippets = temp.ToList();


            temp.Clear();
            for (int i = 0; i < dgvClassic.Rows.Count; i++)
            {
                if (dgvClassic[0, i].Value != null && dgvClassic[1, i].Value != null)
                {
                    temp.Add($"@@{dgvClassic[0, i].Value} {dgvClassic[1, i].Value.ToString().Trim()}");
                }
            }
            _pendingSnippets.MonkeySnippets = temp.ToList();
            _config.QuickSnippets.Clear();
            for (int i = 0; i < dgvQuick.Rows.Count; i++)
            {
                if (dgvQuick[0, i].Value != null && dgvQuick[1, i].Value != null)
                {
                    _config.QuickSnippets[(dgvQuick[0, i].Value as string).ToUpper()] = dgvQuick[1, i].Value as string;
                }
            }
            _config.KeyWordsListForColoring1.Clear();
            for (int i = 0; i < dgvColoringList1.Rows.Count; i++)
            {
                if (dgvColoringList1[0, i].Value != null)
                {
                    _config.KeyWordsListForColoring1.Add(dgvColoringList1[0, i].Value as string);
                }
            }
            _config.KeyWordsListForColoring2.Clear();
            for (int i = 0; i < dgvColoringList2.Rows.Count; i++)
            {
                if (dgvColoringList2[0, i].Value != null)
                {
                    _config.KeyWordsListForColoring2.Add(dgvColoringList2[0, i].Value as string);
                }
            }
            _config.TypoPatternList.Clear();
            for (int i = 0; i < dgvTypo.Rows.Count; i++)
            {
                if (dgvTypo[0, i].Value != null)
                {
                    _config.TypoPatternList.Add(dgvTypo[0, i].Value as string);
                }
            }
            _config.TypoCorrect = checkBoxTypo.Checked;
            _config.TypoLimit = (int)numericUpDownTypo.Value;

        }

        private void SyncViewModelFromLegacyBuffer()
        {
            _settingsViewModel.ReplaceDraft(LegacyApplicationSettingsMapper.ToSnapshot(_config, _pendingSnippets).ToDraft());
        }

        private void TryNewColors()
        {
            SaveLocalyMain();
            SyncViewModelFromLegacyBuffer();
            _settingsViewModel.PreviewTheme();
            RePaint2();

            _colorize.ColorForm(this, force: true);
            ApplyModernTheme();
            this.Invalidate();
        }

        private void RePaint2()
        {
            Application.SetColorMode(
                _config.UseSpecialColoring ? SystemColorMode.Dark : SystemColorMode.Classic);
            _repaintApplication();
        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            TryNewColors();
        }

        private async Task<bool> SaveAsync()
        {
            TextVsDataGridAction(dgvStandard, tbStandard);
            TextVsDataGridAction(dgvQuick, tbQuick);
            TextVsDataGridAction(dgvClassic, tbClassic);

            SaveLocalyMain();
            SaveSnippetsLoccaly();
            SyncViewModelFromLegacyBuffer();
            await _settingsViewModel.SaveAsync();

            RePaint2();
            return _settingsViewModel.IsSaved;
        }

        private async Task ObserveAsync(Task task)
        {
            try
            {
                await task;
            }
            catch (Exception exception)
            {
                Trace.WriteLine($"Preferences save failed: {exception.GetType().Name}");
            }
        }

        class ColorSettingsModel
        {
            public string FontString { get; set; }
            public float FontSize { get; set; }


            [Category("Inside SQL editor")]
            [DisplayName("Background")]
            public Color BackgroundFastColored { get; set; }

            [Category("Inside SQL editor")]
            [DisplayName("Selection")]
            public Color SelectionColorFastColored { get; set; }

            [Category("Inside SQL editor")]
            [DisplayName("Disabled text color")]
            public Color DisabledColorFastColored { get; set; }

            [Category("Inside SQL editor")]
            [DisplayName("Indent back")]
            public Color IndentBackColorFastColored { get; set; }

            [Category("Inside SQL editor")]
            [DisplayName("Line number")]
            public Color LineNumberColorFastColored { get; set; }

            [Category("Inside SQL editor")]
            [DisplayName("Folding indicator")]
            public Color FoldingIndicatorColorFastColored { get; set; }

            [Category("Inside SQL editor")]
            [DisplayName("Font color")]
            public Color ForeColorFastColored { get; set; }


            [Category("Inside SQL editor")]
            [DisplayName("Word style 1")]
            public Color FontkeyWordsStyle1 { get; set; }

            [Category("Inside SQL editor")]
            [DisplayName("Word style 2")]
            public Color FontkeyWordsStyle2 { get; set; }

            [Category("Inside SQL editor")]
            [DisplayName("Params")]
            public Color FontparamStyle { get; set; }

            [Category("Inside SQL editor")]
            [DisplayName("Commands")]
            public Color FontmyCommandsStyle { get; set; }

            [Category("Inside SQL editor")]
            [DisplayName("Numbers")]
            public Color FontnumberStyle { get; set; }

            [Category("Inside SQL editor")]
            [DisplayName("Comments")]
            public Color FontcommentsStyle { get; set; }

            [Category("Inside SQL editor")]
            [DisplayName("Strings")]
            public Color FontstringsStyle { get; set; }

            [Category("Inside SQL editor")]
            [DisplayName("Same words style")]
            [Description("You can specify alpha chanel: ALPHA;R,G;B")]
            public Color FontsameWordsStyle { get; set; }


            [Category("Results Grid")]
            [DisplayName("Cell back color")]
            public Color DgvDefaultCellStyleBackColor { get; set; }

            [Category("Results Grid")]
            [DisplayName("Altering cell back color")]
            public Color DgvAlternatingRowsDefaultCellStyleBackColor { get; set; }

            [Category("Results Grid")]
            [DisplayName("Cell font color")]
            public Color DgvDefaultCellStyleForeColor { get; set; }

            [Category("Results Grid")]
            [DisplayName("Row header back color")]
            public Color DgvRowHeadersDefaultCellStyleBack { get; set; }

            [Category("Results Grid")]
            [DisplayName("Column header font color")]
            public Color DgvColumnHeadersDefaultCellStyleFore { get; set; }

            [Category("Column header back color")]
            public Color DgvColumnHeadersDefaultCellStyleBack { get; set; }


            [Category("Menu & similar")]
            [Description("Back Menu and others")]
            public Color StripBack { get; set; }

            [Category("Menu & similar")]
            [Description("Fore Menu and others")]
            public Color StripFore { get; set; }

            [Category("Tree Views")]
            [Description("Background")]
            [DisplayName("Tree back color")]
            public Color TreeViewBackColor { get; set; }

            [Category("Tree Views")]
            [Description("Font")]
            [DisplayName("Tree font color")]
            public Color TreeViewForeColor { get; set; }

            [Category("DB/File schama view")]
            public Color TreeViewLineColor { get; set; }

            [Category("DB/File schama view")]
            public Color TextBoxFileSearchBackColor { get; set; }

            [Category("DB/File schama view")]
            public Color TextBoxFileSearchForeColor { get; set; }


            [Description("Tabs")]
            public Color TabColor { get; set; }
            [Description("Tabs")]
            public Color SelectedtabColor { get; set; }
            [Description("Tabs")]
            public Color TabTitleColor { get; set; }

            [Category("Menu & similar")]
            public Color MenuItemSelected { get; set; }
            [Category("Menu & similar")]
            public Color MenuItemSelectedGradientBegin { get; set; }
            [Category("Menu & similar")]
            public Color MenuItemSelectedGradientEnd { get; set; }
            [Category("Menu & similar")]
            public Color MenuItemBorder { get; set; }
            [Category("Menu & similar")]
            public Color MenuItemPressedGradientBegin { get; set; }
            [Category("Menu & similar")]
            public Color MenuItemPressedGradientMiddle { get; set; }
            [Category("Menu & similar")]
            public Color MenuItemPressedGradientEnd { get; set; }
            [Category("Menu & similar")]
            public Color ButtonSelectedHighlightBorder { get; set; }
            [Category("Results Grid")]
            [DisplayName("Grouping row back")]
            public Color GroupingRowColorBack { get; set; }

            [Category("Menu & similar")]
            [DisplayName("Document map back")]
            public Color DocMapBackColor { get; set; }
            [Category("Menu & similar")]
            [DisplayName("Document map font")]
            public Color DocMapForeColor { get; set; }

        }


        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.S))
            {
                _ = ObserveAsync(SaveAsync());
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void checkBoxTypo_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxTypo.Checked)
            {
                dgvTypo.Enabled = true;
                numericUpDownTypo.Enabled = true;
            }
            else
            {
                dgvTypo.Enabled = false;
                numericUpDownTypo.Enabled = false;
            }
        }

        private void TextVsDataGridAction(DataGridView dgv, TextBox tb)
        {
            if (dgv.SelectedCells.Count == 1 /*&& dgv.SelectedCells[0].Value != null*/)
            {
                var sel = dgv.SelectedCells[0];

                if (!String.IsNullOrEmpty(tb.Text) && tb.Tag != null)
                {
                    var tg = tb.Tag as TagForDGV;
                    if (tg.Cell.Value == null || tg.Cell.Value.ToString() != tb.Text)
                    {
                        tg.Cell.Value = tb.Text;
                    }
                }

                tb.Text = sel.Value == null ? "" : sel.Value.ToString();
                tb.Tag = new TagForDGV()
                {
                    Cell = sel
                };
            }
        }

        private void dgvQuick_SelectionChanged(object sender, EventArgs e)
        {
            TextVsDataGridAction(dgvQuick, tbQuick);
        }

        private void DgvStandard_SelectionChanged(object sender, EventArgs e)
        {
            TextVsDataGridAction(dgvStandard, tbStandard);
        }

        private void DgvClassic_SelectionChanged(object sender, EventArgs e)
        {
            TextVsDataGridAction(dgvClassic, tbClassic);
        }

        private void DgvQuick_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex != -1 && e.RowIndex != -1 && dgvQuick[e.ColumnIndex, e.RowIndex].Value != null)
            {
                tbQuick.Text = dgvQuick[e.ColumnIndex, e.RowIndex].Value.ToString();
            }
        }

        private void DgvStandard_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex != -1 && e.RowIndex != -1 && dgvStandard[e.ColumnIndex, e.RowIndex].Value != null)
            {
                tbStandard.Text = dgvStandard[e.ColumnIndex, e.RowIndex].Value.ToString();
            }
        }

        private void DgvClassic_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex != -1 && e.RowIndex != -1 && dgvClassic[e.ColumnIndex, e.RowIndex].Value != null)
            {
                tbClassic.Text = dgvClassic[e.ColumnIndex, e.RowIndex].Value.ToString();
            }
        }

        private void TbStandard_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.S && ModifierKeys == Keys.Control)
            {
                TextVsDataGridAction(dgvStandard, tbStandard);
                SaveSnippetsLoccaly();
            }
        }
        private void TbQuick_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.S && ModifierKeys == Keys.Control)
            {
                TextVsDataGridAction(dgvQuick, tbQuick);
                SaveSnippetsLoccaly();
            }
        }
        private void TbClassic_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.S && ModifierKeys == Keys.Control)
            {
                TextVsDataGridAction(dgvClassic, tbClassic);
                SaveSnippetsLoccaly();
            }
        }

        private void BtStandardAdd_Click(object sender, EventArgs e)
        {
            dgvStandard.Rows.Add();
        }

        private void BtClassicAdd_Click(object sender, EventArgs e)
        {
            dgvClassic.Rows.Add();
        }

        private void BtQuickAdd_Click(object sender, EventArgs e)
        {
            dgvQuick.Rows.Add();
        }

        private void FormatsHelpBt_Click(object sender, EventArgs e)
        {
            Process.Start(new ProcessStartInfo("cmd", $"/c start https://docs.microsoft.com/en-us/dotnet/standard/base-types/standard-numeric-format-strings") { CreateNoWindow = true });
        }

        private void FiltersHelpBt_Click(object sender, EventArgs e)
        {
            Process.Start(new ProcessStartInfo("cmd", $"/c start https://docs.microsoft.com/en-us/dotnet/api/system.data.datacolumn.expression?redirectedfrom=MSDN&view=net-5.0#System_Data_DataColumn_Expression") { CreateNoWindow = true });
        }



        private void RestartToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var r = MessageBox.Show("Restart JustyBaseLegacy?", "Restart JustyBaseLegacy?", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (r == DialogResult.Yes)
            {
                Application.Restart();
                Environment.Exit(0);
            }
        }


        private async void BtSave2_Click(object sender, EventArgs e)
        {
            try
            {
                if (await SaveAsync())
                    this.Close();
            }
            catch (Exception exception)
            {
                Trace.WriteLine($"Preferences save failed: {exception.GetType().Name}");
            }
        }

        private async void BtRestart2_Click(object sender, EventArgs e)
        {
            try
            {
                await SaveAsync();
            }
            catch (Exception exception)
            {
                Trace.WriteLine($"Preferences save failed: {exception.GetType().Name}");
            }
            var r = MessageBox.Show("Restart JustyBaseLegacy?", "Restart JustyBaseLegacy?", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (r == DialogResult.Yes)
            {
                Application.Restart();
                Environment.Exit(0);
            }
        }


        private OpenFileDialog _openStartupFile = new OpenFileDialog();
        private void StartupPathsDgv_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            var senderGrid = (DataGridView)sender;

            if (senderGrid.Columns[e.ColumnIndex] is DataGridViewButtonColumn && e.RowIndex >= 0)
            {
                _openStartupFile.Filter = "Files|*.sql;*.manysql";
                _openStartupFile.Multiselect = false;
                var r = _openStartupFile.ShowDialog();
                if (r == DialogResult.OK && !String.IsNullOrEmpty(_openStartupFile.FileName) && _openStartupFile.CheckFileExists)
                {
                    senderGrid.Rows[e.RowIndex].Cells[0].Value = _openStartupFile.FileName;
                }
            }
        }

        private void CbSimpleStarup_CheckedChanged(object sender, EventArgs e)
        {
            if (cbSimpleStarupRestore.Checked)
            {
                startupPathsDgv.Enabled = false;
                startupPathsDgv.DefaultCellStyle.BackColor = SystemColors.Control;
                startupPathsDgv.DefaultCellStyle.ForeColor = SystemColors.GrayText;
                startupPathsDgv.ColumnHeadersDefaultCellStyle.BackColor = SystemColors.Control;
                startupPathsDgv.ColumnHeadersDefaultCellStyle.ForeColor = SystemColors.GrayText;
                startupPathsDgv.CurrentCell = null;
                startupPathsDgv.ReadOnly = true;
                startupPathsDgv.EnableHeadersVisualStyles = false;
            }
            else
            {
                startupPathsDgv.Enabled = true;
                startupPathsDgv.DefaultCellStyle.BackColor = SystemColors.Window;
                startupPathsDgv.DefaultCellStyle.ForeColor = SystemColors.ControlText;
                startupPathsDgv.ColumnHeadersDefaultCellStyle.BackColor = SystemColors.Window;
                startupPathsDgv.ColumnHeadersDefaultCellStyle.ForeColor = SystemColors.ControlText;
                startupPathsDgv.ReadOnly = false;
                startupPathsDgv.EnableHeadersVisualStyles = true;
            }
        }

        private OpenFileDialog _fileDialog = new OpenFileDialog()
        {
            Filter = "(*.dll)|*.dll"
        };
        private void Nzlink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var p = new Process();

            string link = "";
            if (sender == nzlink)
            {
                link = "https://www.ibm.com/docs/en/netezza?topic=server-netezza-performance-cloud-pak-data-system";
            }
            else if (sender == db2Link)
            {
                link = "https://www.ibm.com/docs/en/db2/11.5";
            }

            p.StartInfo = new ProcessStartInfo(link)
            {
                UseShellExecute = true
            };
            p.Start();
        }

        private void ReEncrypt()
        {
            _saveConfig();
            _saveRecentFiles();
            NetezzaLegacyCompletionHelpers.SaveSnipets(_applicationSettingsContext, _netezzaAutocompleteState);
            _saveManySqlToDisk();
        }
    }
    class TagForDGV
    {
        public DataGridViewCell Cell { get; set; }
    }
}
