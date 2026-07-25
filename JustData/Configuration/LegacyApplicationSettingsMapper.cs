using AppBase.Common;
using AppBase.Common.Configuration;
using JustData.Application.Settings;

namespace JustyBaseLegacy.UI.Configuration;

public static class LegacyApplicationSettingsMapper
{
    public static ApplicationSettingsSnapshot ToSnapshot(IApplicationConfig config, SnippetSettings? snippets = null)
    {
        ArgumentNullException.ThrowIfNull(config);
        var draft = new ApplicationSettingsDraft();
        MapAppearanceToDraft(config, draft.Appearance);
        MapEditorToDraft(config, draft.Editor);
        MapSqlResultsToDraft(config, draft.SqlResults);
        MapImportExportToDraft(config, draft.ImportExport);
        MapFilesStartupToDraft(config, draft.FilesStartup);
        MapLintToDraft(config, draft.Lint);
        MapTerminalToDraft(config, draft.Terminal);
        draft.Snippets = snippets?.Clone() ?? new SnippetSettings();
        return new ApplicationSettingsSnapshot(draft);
    }

    public static ApplicationConfig ToLegacy(ApplicationSettingsDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);
        var config = new ApplicationConfig();
        ApplyToLegacy(draft, config);
        return config;
    }

    public static void ApplyToLegacy(ApplicationSettingsDraft draft, IApplicationConfig destination)
    {
        ArgumentNullException.ThrowIfNull(draft);
        ArgumentNullException.ThrowIfNull(destination);
        if (destination is not ApplicationConfig config)
        {
            ApplyToLegacyViaReflection(draft, destination);
            return;
        }

        ApplyAppearanceToLegacy(draft.Appearance, config);
        ApplyEditorToLegacy(draft.Editor, config);
        ApplySqlResultsToLegacy(draft.SqlResults, config);
        ApplyImportExportToLegacy(draft.ImportExport, config);
        ApplyFilesStartupToLegacy(draft.FilesStartup, config);
        ApplyLintToLegacy(draft.Lint, config);
        ApplyTerminalToLegacy(draft.Terminal, config);
    }

    public static SnippetSettings CloneSnippets(SnippetSettings snippets) => snippets.Clone();

    private static void MapAppearanceToDraft(IApplicationConfig source, AppearanceSettings target)
    {
        target.AlternatingRows = source.AlternatingRows;
        target.DoLegend = source.DoLegend;
        target.UseSpecialColoring = source.UseSpecialColoring;
        target.FontName = source.FontName;
        target.FontSize = source.FontSize;
        target.AutoSizeColumnsMode = (int)source.AutoSizeColumnsMode;
        target.GrifOffsetHeight = source.GrifOffsetHeight;
        target.BackgroundFastColored = RgbaColor.FromLegacy(source.BackgroundFastColored);
        target.SelectionColorFastColored = RgbaColor.FromLegacy(source.SelectionColorFastColored);
        target.DisabledColorFastColored = RgbaColor.FromLegacy(source.DisabledColorFastColored);
        target.IndentBackColorFastColored = RgbaColor.FromLegacy(source.IndentBackColorFastColored);
        target.LineNumberColorFastColored = RgbaColor.FromLegacy(source.LineNumberColorFastColored);
        target.FoldingIndicatorColorFastColored = RgbaColor.FromLegacy(source.FoldingIndicatorColorFastColored);
        target.ForeColorFastColored = RgbaColor.FromLegacy(source.ForeColorFastColored);
        target.FontkeyWordsStyle1 = RgbaColor.FromLegacy(source.FontkeyWordsStyle1);
        target.FontkeyWordsStyle2 = RgbaColor.FromLegacy(source.FontkeyWordsStyle2);
        target.FontparamStyle = RgbaColor.FromLegacy(source.FontparamStyle);
        target.FontmyCommandsStyle = RgbaColor.FromLegacy(source.FontmyCommandsStyle);
        target.FontnumberStyle = RgbaColor.FromLegacy(source.FontnumberStyle);
        target.FontcommentsStyle = RgbaColor.FromLegacy(source.FontcommentsStyle);
        target.FontstringsStyle = RgbaColor.FromLegacy(source.FontstringsStyle);
        target.FontsameWordsStyle = RgbaColor.FromLegacy(source.FontsameWordsStyle);
        target.DgvDefaultCellStyleBackColor = RgbaColor.FromLegacy(source.DgvDefaultCellStyleBackColor);
        target.DgvAlternatingRowsDefaultCellStyleBackColor = RgbaColor.FromLegacy(source.DgvAlternatingRowsDefaultCellStyleBackColor);
        target.DgvDefaultCellStyleForeColor = RgbaColor.FromLegacy(source.DgvDefaultCellStyleForeColor);
        target.DgvRowHeadersDefaultCellStyleBack = RgbaColor.FromLegacy(source.DgvRowHeadersDefaultCellStyleBack);
        target.DgvColumnHeadersDefaultCellStyleFore = RgbaColor.FromLegacy(source.DgvColumnHeadersDefaultCellStyleFore);
        target.DgvColumnHeadersDefaultCellStyleBack = RgbaColor.FromLegacy(source.DgvColumnHeadersDefaultCellStyleBack);
        target.DocMapBackColor = RgbaColor.FromLegacy(source.DocMapBackColor);
        target.DocMapForeColor = RgbaColor.FromLegacy(source.DocMapForeColor);
        target.TabColor = RgbaColor.FromLegacy(source.TabColor);
        target.SelectedtabColor = RgbaColor.FromLegacy(source.SelectedtabColor);
        target.TabTitleColor = RgbaColor.FromLegacy(source.TabTitleColor);
        target.StripBack = RgbaColor.FromLegacy(source.StripBack);
        target.StripFore = RgbaColor.FromLegacy(source.StripFore);
        target.TreeViewBackColor = RgbaColor.FromLegacy(source.TreeViewBackColor);
        target.TreeViewForeColor = RgbaColor.FromLegacy(source.TreeViewForeColor);
        target.TreeViewLineColor = RgbaColor.FromLegacy(source.TreeViewLineColor);
        target.TextBoxFileSearchBackColor = RgbaColor.FromLegacy(source.TextBoxFileSearchBackColor);
        target.TextBoxFileSearchForeColor = RgbaColor.FromLegacy(source.TextBoxFileSearchForeColor);
        target.MenuItemSelected = RgbaColor.FromLegacy(source.MenuItemSelected);
        target.MenuItemSelectedGradientBegin = RgbaColor.FromLegacy(source.MenuItemSelectedGradientBegin);
        target.MenuItemSelectedGradientEnd = RgbaColor.FromLegacy(source.MenuItemSelectedGradientEnd);
        target.MenuItemBorder = RgbaColor.FromLegacy(source.MenuItemBorder);
        target.MenuItemPressedGradientBegin = RgbaColor.FromLegacy(source.MenuItemPressedGradientBegin);
        target.MenuItemPressedGradientMiddle = RgbaColor.FromLegacy(source.MenuItemPressedGradientMiddle);
        target.MenuItemPressedGradientEnd = RgbaColor.FromLegacy(source.MenuItemPressedGradientEnd);
        target.ButtonSelectedHighlightBorder = RgbaColor.FromLegacy(source.ButtonSelectedHighlightBorder);
        target.GroupingRowColorBack = RgbaColor.FromLegacy(source.GroupingRowColorBack);
    }

    private static void ApplyAppearanceToLegacy(AppearanceSettings source, ApplicationConfig target)
    {
        target.AlternatingRows = source.AlternatingRows;
        target.DoLegend = source.DoLegend;
        target.UseSpecialColoring = source.UseSpecialColoring;
        target.FontName = source.FontName;
        target.FontSize = source.FontSize;
        target.AutoSizeColumnsMode = (System.Windows.Forms.DataGridViewAutoSizeColumnsMode)source.AutoSizeColumnsMode;
        target.GrifOffsetHeight = source.GrifOffsetHeight;
        target.BackgroundFastColored = source.BackgroundFastColored.ToLegacy().ToList();
        target.SelectionColorFastColored = source.SelectionColorFastColored.ToLegacy().ToList();
        target.DisabledColorFastColored = source.DisabledColorFastColored.ToLegacy().ToList();
        target.IndentBackColorFastColored = source.IndentBackColorFastColored.ToLegacy().ToList();
        target.LineNumberColorFastColored = source.LineNumberColorFastColored.ToLegacy().ToList();
        target.FoldingIndicatorColorFastColored = source.FoldingIndicatorColorFastColored.ToLegacy().ToList();
        target.ForeColorFastColored = source.ForeColorFastColored.ToLegacy().ToList();
        target.FontkeyWordsStyle1 = source.FontkeyWordsStyle1.ToLegacy().ToList();
        target.FontkeyWordsStyle2 = source.FontkeyWordsStyle2.ToLegacy().ToList();
        target.FontparamStyle = source.FontparamStyle.ToLegacy().ToList();
        target.FontmyCommandsStyle = source.FontmyCommandsStyle.ToLegacy().ToList();
        target.FontnumberStyle = source.FontnumberStyle.ToLegacy().ToList();
        target.FontcommentsStyle = source.FontcommentsStyle.ToLegacy().ToList();
        target.FontstringsStyle = source.FontstringsStyle.ToLegacy().ToList();
        target.FontsameWordsStyle = source.FontsameWordsStyle.ToLegacy().ToList();
        target.DgvDefaultCellStyleBackColor = source.DgvDefaultCellStyleBackColor.ToLegacy().ToList();
        target.DgvAlternatingRowsDefaultCellStyleBackColor = source.DgvAlternatingRowsDefaultCellStyleBackColor.ToLegacy().ToList();
        target.DgvDefaultCellStyleForeColor = source.DgvDefaultCellStyleForeColor.ToLegacy().ToList();
        target.DgvRowHeadersDefaultCellStyleBack = source.DgvRowHeadersDefaultCellStyleBack.ToLegacy().ToList();
        target.DgvColumnHeadersDefaultCellStyleFore = source.DgvColumnHeadersDefaultCellStyleFore.ToLegacy().ToList();
        target.DgvColumnHeadersDefaultCellStyleBack = source.DgvColumnHeadersDefaultCellStyleBack.ToLegacy().ToList();
        target.DocMapBackColor = source.DocMapBackColor.ToLegacy().ToList();
        target.DocMapForeColor = source.DocMapForeColor.ToLegacy().ToList();
        target.TabColor = source.TabColor.ToLegacy().ToList();
        target.SelectedtabColor = source.SelectedtabColor.ToLegacy().ToList();
        target.TabTitleColor = source.TabTitleColor.ToLegacy().ToList();
        target.StripBack = source.StripBack.ToLegacy().ToList();
        target.StripFore = source.StripFore.ToLegacy().ToList();
        target.TreeViewBackColor = source.TreeViewBackColor.ToLegacy().ToList();
        target.TreeViewForeColor = source.TreeViewForeColor.ToLegacy().ToList();
        target.TreeViewLineColor = source.TreeViewLineColor.ToLegacy().ToList();
        target.TextBoxFileSearchBackColor = source.TextBoxFileSearchBackColor.ToLegacy().ToList();
        target.TextBoxFileSearchForeColor = source.TextBoxFileSearchForeColor.ToLegacy().ToList();
        target.MenuItemSelected = source.MenuItemSelected.ToLegacy().ToList();
        target.MenuItemSelectedGradientBegin = source.MenuItemSelectedGradientBegin.ToLegacy().ToList();
        target.MenuItemSelectedGradientEnd = source.MenuItemSelectedGradientEnd.ToLegacy().ToList();
        target.MenuItemBorder = source.MenuItemBorder.ToLegacy().ToList();
        target.MenuItemPressedGradientBegin = source.MenuItemPressedGradientBegin.ToLegacy().ToList();
        target.MenuItemPressedGradientMiddle = source.MenuItemPressedGradientMiddle.ToLegacy().ToList();
        target.MenuItemPressedGradientEnd = source.MenuItemPressedGradientEnd.ToLegacy().ToList();
        target.ButtonSelectedHighlightBorder = source.ButtonSelectedHighlightBorder.ToLegacy().ToList();
        target.GroupingRowColorBack = source.GroupingRowColorBack.ToLegacy().ToList();
    }

    private static void MapEditorToDraft(IApplicationConfig source, EditorSettings target)
    {
        target.GenerateToolTipTime = source.GenerateToolTipTime;
        target.DelayedTextChangedInterval = source.DelayedTextChangedInterval;
        target.ToolTipDelay = source.ToolTipDelay;
        target.PopupMenuDefaultAppearInterval = source.PopupMenuDefaultAppearInterval;
        target.FileSearchTimeout = source.FileSearchTimeout;
        target.TypoCorrect = source.TypoCorrect;
        target.TypoPatternList = source.TypoPatternList;
        target.TypoLimit = source.TypoLimit;
        target.KeyWordsListForColoring1 = source.KeyWordsListForColoring1;
        target.KeyWordsListForColoring2 = source.KeyWordsListForColoring2;
        target.QuickSnippets = source.QuickSnippets;
        target.AutoCompleteBrackets = source.AutoCompleteBrackets;
        target.BracketFolding = source.BracketFolding;
        target.DontUseIndent = source.DontUseIndent;
        target.CurrentWordLengthLimit = source.CurrentWordLengthLimit;
        target.ContextScripts = source.ContextScripts;
        target.LineInterval = source.LineInterval;
        target.DoNotCollapseRegionsOnOpening = source.DoNotCollapseRegionsOnOpening;
        target.EditorHotkeys = source.EditorHotkeys;
        target.RestoreFoldingState = source.RestoreFoldingState;
        target.WordWrap = source.WordWrap;
        target.WordWrapAutoIndent = source.WordWrapAutoIndent;
    }

    private static void ApplyEditorToLegacy(EditorSettings source, ApplicationConfig target)
    {
        target.GenerateToolTipTime = source.GenerateToolTipTime;
        target.DelayedTextChangedInterval = source.DelayedTextChangedInterval;
        target.ToolTipDelay = source.ToolTipDelay;
        target.PopupMenuDefaultAppearInterval = source.PopupMenuDefaultAppearInterval;
        target.FileSearchTimeout = source.FileSearchTimeout;
        target.TypoCorrect = source.TypoCorrect;
        target.TypoPatternList = source.TypoPatternList;
        target.TypoLimit = source.TypoLimit;
        target.KeyWordsListForColoring1 = source.KeyWordsListForColoring1;
        target.KeyWordsListForColoring2 = source.KeyWordsListForColoring2;
        target.QuickSnippets = source.QuickSnippets;
        target.AutoCompleteBrackets = source.AutoCompleteBrackets;
        target.BracketFolding = source.BracketFolding;
        target.DontUseIndent = source.DontUseIndent;
        target.CurrentWordLengthLimit = source.CurrentWordLengthLimit;
        target.ContextScripts = source.ContextScripts;
        target.LineInterval = source.LineInterval;
        target.DoNotCollapseRegionsOnOpening = source.DoNotCollapseRegionsOnOpening;
        target.EditorHotkeys = source.EditorHotkeys;
        target.RestoreFoldingState = source.RestoreFoldingState;
        target.WordWrap = source.WordWrap;
        target.WordWrapAutoIndent = source.WordWrapAutoIndent;
    }

    private static void MapSqlResultsToDraft(IApplicationConfig source, SqlResultsSettings target)
    {
        target.DoNotWarnFullUpdateDelete = source.DoNotWarnFullUpdateDelete;
        target.FastLogin = source.FastLogin;
        target.UseSpecialTabNames = source.UseSpecialTabNames;
        target.SelectedFormatter = source.SelectedFormatter;
        target.ResultRowsLimit = source.ResultRowsLimit;
        target.ResultRowsLimitWarning = source.ResultRowsLimitWarning;
        target.PinDataByDefault = source.PinDataByDefault;
        target.ConnectionTimeout = source.ConnectionTimeout;
        target.CommandTimeout = source.CommandTimeout;
        target.CommandDistTimeout = source.CommandDistTimeout;
        target.LongQueryWarning = source.LongQueryWarning;
        target.ElapsedWarning = source.ElapsedWarning;
        target.EstimatedWarning = source.EstimatedWarning;
        target.EstimatedWarningInterval = source.EstimatedWarningInterval;
        target.CloseConnectionByDefault = source.CloseConnectionByDefault;
        target.DateTimeFormat = source.DateTimeFormat;
        target.DecimalFormat = source.DecimalFormat;
        target.IntegerFormat = source.IntegerFormat;
        target.ForceDecimalFormat = source.ForceDecimalFormat;
        target.DontShowOwner = source.DontShowOwner;
        target.SortMethod = source.SortMethod;
        target.ResetSchema = source.ResetSchema;
        target.LoadSourcesOnStartup = source.LoadSourcesOnStartup;
        target.OnlineOnlyDdls = source.OnlineOnlyDdls;
        target.MaxSchemaParallelism = source.MaxSchemaParallelism;
        target.CloseWaringLevel = source.CloseWaringLevel;
        target.LastReadMessage = source.LastReadMessage;
        target.RegularActionTimerMinutes = source.RegularActionTimerMinutes;
        target.CachedDatabaseDictionary = ConvertCacheToSnapshot(source.CachedDatabaseDictionary);
    }

    private static void ApplySqlResultsToLegacy(SqlResultsSettings source, ApplicationConfig target)
    {
        target.DoNotWarnFullUpdateDelete = source.DoNotWarnFullUpdateDelete;
        target.FastLogin = source.FastLogin;
        target.UseSpecialTabNames = source.UseSpecialTabNames;
        target.SelectedFormatter = source.SelectedFormatter;
        target.ResultRowsLimit = source.ResultRowsLimit;
        target.ResultRowsLimitWarning = source.ResultRowsLimitWarning;
        target.PinDataByDefault = source.PinDataByDefault;
        target.ConnectionTimeout = source.ConnectionTimeout;
        target.CommandTimeout = source.CommandTimeout;
        target.CommandDistTimeout = source.CommandDistTimeout;
        target.LongQueryWarning = source.LongQueryWarning;
        target.ElapsedWarning = source.ElapsedWarning;
        target.EstimatedWarning = source.EstimatedWarning;
        target.EstimatedWarningInterval = source.EstimatedWarningInterval;
        target.CloseConnectionByDefault = source.CloseConnectionByDefault;
        target.DateTimeFormat = source.DateTimeFormat;
        target.DecimalFormat = source.DecimalFormat;
        target.IntegerFormat = source.IntegerFormat;
        target.ForceDecimalFormat = source.ForceDecimalFormat;
        target.DontShowOwner = source.DontShowOwner;
        target.SortMethod = source.SortMethod;
        target.ResetSchema = source.ResetSchema;
        target.LoadSourcesOnStartup = source.LoadSourcesOnStartup;
        target.OnlineOnlyDdls = source.OnlineOnlyDdls;
        target.MaxSchemaParallelism = source.MaxSchemaParallelism;
        target.CloseWaringLevel = source.CloseWaringLevel;
        target.LastReadMessage = source.LastReadMessage;
        target.RegularActionTimerMinutes = source.RegularActionTimerMinutes;
        target.CachedDatabaseDictionary = ConvertCacheToLegacy(source.CachedDatabaseDictionary);
    }

    private static void MapImportExportToDraft(IApplicationConfig source, ImportExportSettings target)
    {
        target.MyFastXlsxExportList = source.MyFastXlsxExportList;
        target.DefaultNvarcharLength = source.DefaultNvarcharLength;
        target.SepInExportedCsv = source.SepInExportedCsv;
        target.SepRowsInExportedCsv = source.SepRowsInExportedCsv;
        target.EncondingName = source.EncondingName;
        target.DecimalDelimInCsv = source.DecimalDelimInCsv;
        target.PasteAsExternalSep = source.PasteAsExternalSep;
        target.SepInExternal = source.SepInExternal;
        target.ExternalMAXERRORS = source.ExternalMAXERRORS;
        target.ImportExisting = source.ImportExisting;
        target.CtrlVmode = source.CtrlVmode;
        target.UseXlsb = source.UseXlsb;
        target.UseSpecialSeparatorMode = source.UseSpecialSeparatorMode;
        target.SpecialSeparator = source.SpecialSeparator;
    }

    private static void ApplyImportExportToLegacy(ImportExportSettings source, ApplicationConfig target)
    {
        target.MyFastXlsxExportList = source.MyFastXlsxExportList;
        target.DefaultNvarcharLength = source.DefaultNvarcharLength;
        target.SepInExportedCsv = source.SepInExportedCsv;
        target.SepRowsInExportedCsv = source.SepRowsInExportedCsv;
        target.EncondingName = source.EncondingName;
        target.DecimalDelimInCsv = source.DecimalDelimInCsv;
        target.PasteAsExternalSep = source.PasteAsExternalSep;
        target.SepInExternal = source.SepInExternal;
        target.ExternalMAXERRORS = source.ExternalMAXERRORS;
        target.ImportExisting = source.ImportExisting;
        target.CtrlVmode = source.CtrlVmode;
        target.UseXlsb = source.UseXlsb;
        target.UseSpecialSeparatorMode = source.UseSpecialSeparatorMode;
        target.SpecialSeparator = source.SpecialSeparator;
    }

    private static void MapFilesStartupToDraft(IApplicationConfig source, FilesStartupSettings target)
    {
        target.NotFirstLaunch = source.NotFirstLaunch;
        target.StartsFolderPaths = source.StartsFolderPaths;
        target.SortByLastWrite = source.SortByLastWrite;
        target.SortByName = source.SortByName;
        target.StartFilesExtra = source.StartFilesExtra;
        target.SimpleStartupRestore = source.SimpleStartupRestore;
        target.MaxRecentFilesCount = source.MaxRecentFilesCount;
        target.SortByMyName = source.SortByMyName;
    }

    private static void ApplyFilesStartupToLegacy(FilesStartupSettings source, ApplicationConfig target)
    {
        target.NotFirstLaunch = source.NotFirstLaunch;
        target.StartsFolderPaths = source.StartsFolderPaths;
        target.SortByLastWrite = source.SortByLastWrite;
        target.SortByName = source.SortByName;
        target.StartFilesExtra = source.StartFilesExtra;
        target.SimpleStartupRestore = source.SimpleStartupRestore;
        target.MaxRecentFilesCount = source.MaxRecentFilesCount;
        target.SortByMyName = source.SortByMyName;
    }

    private static void MapLintToDraft(IApplicationConfig source, LintSettings target)
    {
        target.DisabledLintRules = source.DisabledLintRules;
    }

    private static void ApplyLintToLegacy(LintSettings source, ApplicationConfig target)
    {
        target.DisabledLintRules = source.DisabledLintRules;
    }

    private static void MapTerminalToDraft(IApplicationConfig source, TerminalSettings target)
    {
        target.TerminalPanelVisible = source.TerminalPanelVisible;
        target.TerminalPanelHeight = source.TerminalPanelHeight;
        target.TerminalShell = source.TerminalShell;
    }

    private static void ApplyTerminalToLegacy(TerminalSettings source, ApplicationConfig target)
    {
        target.TerminalPanelVisible = source.TerminalPanelVisible;
        target.TerminalPanelHeight = source.TerminalPanelHeight;
        target.TerminalShell = source.TerminalShell;
    }

    private static Dictionary<string, Dictionary<int, DatabaseInfoSnapshot>> ConvertCacheToSnapshot(
        Dictionary<string, Dictionary<int, DatabaseInfo>>? value)
    {
        var result = new Dictionary<string, Dictionary<int, DatabaseInfoSnapshot>>(StringComparer.Ordinal);
        foreach (var connection in value ?? [])
        {
            result[connection.Key] = connection.Value.ToDictionary(
                item => item.Key,
                item => new DatabaseInfoSnapshot(item.Value.SchemaId, item.Value.DatabaseName, item.Value.DatabaseOwner, item.Value.SchemaName));
        }

        return result;
    }

    private static Dictionary<string, Dictionary<int, DatabaseInfo>> ConvertCacheToLegacy(
        Dictionary<string, Dictionary<int, DatabaseInfoSnapshot>>? value)
    {
        var result = new Dictionary<string, Dictionary<int, DatabaseInfo>>(StringComparer.Ordinal);
        foreach (var connection in value ?? [])
        {
            result[connection.Key] = connection.Value.ToDictionary(
                item => item.Key,
                item => new DatabaseInfo(item.Value.SchemaId, item.Value.DatabaseName, item.Value.DatabaseOwner, item.Value.SchemaName));
        }

        return result;
    }

    private static void ApplyToLegacyViaReflection(ApplicationSettingsDraft draft, IApplicationConfig destination)
    {
        ApplyAppearanceToLegacyViaReflection(draft.Appearance, destination);
        ApplyEditorToLegacyViaReflection(draft.Editor, destination);
        ApplySqlResultsToLegacyViaReflection(draft.SqlResults, destination);
        ApplyImportExportToLegacyViaReflection(draft.ImportExport, destination);
        ApplyFilesStartupToLegacyViaReflection(draft.FilesStartup, destination);
        ApplyLintToLegacyViaReflection(draft.Lint, destination);
        ApplyTerminalToLegacyViaReflection(draft.Terminal, destination);
    }

    private static void ApplyAppearanceToLegacyViaReflection(AppearanceSettings source, IApplicationConfig target)
    {
        target.AlternatingRows = source.AlternatingRows;
        target.DoLegend = source.DoLegend;
        target.UseSpecialColoring = source.UseSpecialColoring;
        target.FontName = source.FontName;
        target.FontSize = source.FontSize;
        target.AutoSizeColumnsMode = (System.Windows.Forms.DataGridViewAutoSizeColumnsMode)source.AutoSizeColumnsMode;
        target.GrifOffsetHeight = source.GrifOffsetHeight;
        target.BackgroundFastColored = source.BackgroundFastColored.ToLegacy().ToList();
        target.SelectionColorFastColored = source.SelectionColorFastColored.ToLegacy().ToList();
        target.DisabledColorFastColored = source.DisabledColorFastColored.ToLegacy().ToList();
        target.IndentBackColorFastColored = source.IndentBackColorFastColored.ToLegacy().ToList();
        target.LineNumberColorFastColored = source.LineNumberColorFastColored.ToLegacy().ToList();
        target.FoldingIndicatorColorFastColored = source.FoldingIndicatorColorFastColored.ToLegacy().ToList();
        target.ForeColorFastColored = source.ForeColorFastColored.ToLegacy().ToList();
        target.FontkeyWordsStyle1 = source.FontkeyWordsStyle1.ToLegacy().ToList();
        target.FontkeyWordsStyle2 = source.FontkeyWordsStyle2.ToLegacy().ToList();
        target.FontparamStyle = source.FontparamStyle.ToLegacy().ToList();
        target.FontmyCommandsStyle = source.FontmyCommandsStyle.ToLegacy().ToList();
        target.FontnumberStyle = source.FontnumberStyle.ToLegacy().ToList();
        target.FontcommentsStyle = source.FontcommentsStyle.ToLegacy().ToList();
        target.FontstringsStyle = source.FontstringsStyle.ToLegacy().ToList();
        target.FontsameWordsStyle = source.FontsameWordsStyle.ToLegacy().ToList();
        target.DgvDefaultCellStyleBackColor = source.DgvDefaultCellStyleBackColor.ToLegacy().ToList();
        target.DgvAlternatingRowsDefaultCellStyleBackColor = source.DgvAlternatingRowsDefaultCellStyleBackColor.ToLegacy().ToList();
        target.DgvDefaultCellStyleForeColor = source.DgvDefaultCellStyleForeColor.ToLegacy().ToList();
        target.DgvRowHeadersDefaultCellStyleBack = source.DgvRowHeadersDefaultCellStyleBack.ToLegacy().ToList();
        target.DgvColumnHeadersDefaultCellStyleFore = source.DgvColumnHeadersDefaultCellStyleFore.ToLegacy().ToList();
        target.DgvColumnHeadersDefaultCellStyleBack = source.DgvColumnHeadersDefaultCellStyleBack.ToLegacy().ToList();
        target.DocMapBackColor = source.DocMapBackColor.ToLegacy().ToList();
        target.DocMapForeColor = source.DocMapForeColor.ToLegacy().ToList();
        target.TabColor = source.TabColor.ToLegacy().ToList();
        target.SelectedtabColor = source.SelectedtabColor.ToLegacy().ToList();
        target.TabTitleColor = source.TabTitleColor.ToLegacy().ToList();
        target.StripBack = source.StripBack.ToLegacy().ToList();
        target.StripFore = source.StripFore.ToLegacy().ToList();
        target.TreeViewBackColor = source.TreeViewBackColor.ToLegacy().ToList();
        target.TreeViewForeColor = source.TreeViewForeColor.ToLegacy().ToList();
        target.TreeViewLineColor = source.TreeViewLineColor.ToLegacy().ToList();
        target.TextBoxFileSearchBackColor = source.TextBoxFileSearchBackColor.ToLegacy().ToList();
        target.TextBoxFileSearchForeColor = source.TextBoxFileSearchForeColor.ToLegacy().ToList();
        target.MenuItemSelected = source.MenuItemSelected.ToLegacy().ToList();
        target.MenuItemSelectedGradientBegin = source.MenuItemSelectedGradientBegin.ToLegacy().ToList();
        target.MenuItemSelectedGradientEnd = source.MenuItemSelectedGradientEnd.ToLegacy().ToList();
        target.MenuItemBorder = source.MenuItemBorder.ToLegacy().ToList();
        target.MenuItemPressedGradientBegin = source.MenuItemPressedGradientBegin.ToLegacy().ToList();
        target.MenuItemPressedGradientMiddle = source.MenuItemPressedGradientMiddle.ToLegacy().ToList();
        target.MenuItemPressedGradientEnd = source.MenuItemPressedGradientEnd.ToLegacy().ToList();
        target.ButtonSelectedHighlightBorder = source.ButtonSelectedHighlightBorder.ToLegacy().ToList();
        target.GroupingRowColorBack = source.GroupingRowColorBack.ToLegacy().ToList();
    }

    private static void ApplyEditorToLegacyViaReflection(EditorSettings source, IApplicationConfig target)
    {
        target.GenerateToolTipTime = source.GenerateToolTipTime;
        target.DelayedTextChangedInterval = source.DelayedTextChangedInterval;
        target.ToolTipDelay = source.ToolTipDelay;
        target.PopupMenuDefaultAppearInterval = source.PopupMenuDefaultAppearInterval;
        target.FileSearchTimeout = source.FileSearchTimeout;
        target.TypoCorrect = source.TypoCorrect;
        target.TypoPatternList = source.TypoPatternList;
        target.TypoLimit = source.TypoLimit;
        target.KeyWordsListForColoring1 = source.KeyWordsListForColoring1;
        target.KeyWordsListForColoring2 = source.KeyWordsListForColoring2;
        target.QuickSnippets = source.QuickSnippets;
        target.AutoCompleteBrackets = source.AutoCompleteBrackets;
        target.BracketFolding = source.BracketFolding;
        target.DontUseIndent = source.DontUseIndent;
        target.CurrentWordLengthLimit = source.CurrentWordLengthLimit;
        target.ContextScripts = source.ContextScripts;
        target.LineInterval = source.LineInterval;
        target.DoNotCollapseRegionsOnOpening = source.DoNotCollapseRegionsOnOpening;
        target.EditorHotkeys = source.EditorHotkeys;
        target.RestoreFoldingState = source.RestoreFoldingState;
        target.WordWrap = source.WordWrap;
        target.WordWrapAutoIndent = source.WordWrapAutoIndent;
    }

    private static void ApplySqlResultsToLegacyViaReflection(SqlResultsSettings source, IApplicationConfig target)
    {
        target.DoNotWarnFullUpdateDelete = source.DoNotWarnFullUpdateDelete;
        target.FastLogin = source.FastLogin;
        target.UseSpecialTabNames = source.UseSpecialTabNames;
        target.SelectedFormatter = source.SelectedFormatter;
        target.ResultRowsLimit = source.ResultRowsLimit;
        target.ResultRowsLimitWarning = source.ResultRowsLimitWarning;
        target.PinDataByDefault = source.PinDataByDefault;
        target.ConnectionTimeout = source.ConnectionTimeout;
        target.CommandTimeout = source.CommandTimeout;
        target.CommandDistTimeout = source.CommandDistTimeout;
        target.LongQueryWarning = source.LongQueryWarning;
        target.ElapsedWarning = source.ElapsedWarning;
        target.EstimatedWarning = source.EstimatedWarning;
        target.EstimatedWarningInterval = source.EstimatedWarningInterval;
        target.CloseConnectionByDefault = source.CloseConnectionByDefault;
        target.DateTimeFormat = source.DateTimeFormat;
        target.DecimalFormat = source.DecimalFormat;
        target.IntegerFormat = source.IntegerFormat;
        target.ForceDecimalFormat = source.ForceDecimalFormat;
        target.DontShowOwner = source.DontShowOwner;
        target.SortMethod = source.SortMethod;
        target.ResetSchema = source.ResetSchema;
        target.LoadSourcesOnStartup = source.LoadSourcesOnStartup;
        target.OnlineOnlyDdls = source.OnlineOnlyDdls;
        target.MaxSchemaParallelism = source.MaxSchemaParallelism;
        target.CloseWaringLevel = source.CloseWaringLevel;
        target.LastReadMessage = source.LastReadMessage;
        target.RegularActionTimerMinutes = source.RegularActionTimerMinutes;
        target.CachedDatabaseDictionary = ConvertCacheToLegacy(source.CachedDatabaseDictionary);
    }

    private static void ApplyImportExportToLegacyViaReflection(ImportExportSettings source, IApplicationConfig target)
    {
        target.MyFastXlsxExportList = source.MyFastXlsxExportList;
        target.DefaultNvarcharLength = source.DefaultNvarcharLength;
        target.SepInExportedCsv = source.SepInExportedCsv;
        target.SepRowsInExportedCsv = source.SepRowsInExportedCsv;
        target.EncondingName = source.EncondingName;
        target.DecimalDelimInCsv = source.DecimalDelimInCsv;
        target.PasteAsExternalSep = source.PasteAsExternalSep;
        target.SepInExternal = source.SepInExternal;
        target.ExternalMAXERRORS = source.ExternalMAXERRORS;
        target.ImportExisting = source.ImportExisting;
        target.CtrlVmode = source.CtrlVmode;
        target.UseXlsb = source.UseXlsb;
        target.UseSpecialSeparatorMode = source.UseSpecialSeparatorMode;
        target.SpecialSeparator = source.SpecialSeparator;
    }

    private static void ApplyFilesStartupToLegacyViaReflection(FilesStartupSettings source, IApplicationConfig target)
    {
        target.NotFirstLaunch = source.NotFirstLaunch;
        target.StartsFolderPaths = source.StartsFolderPaths;
        target.SortByLastWrite = source.SortByLastWrite;
        target.SortByName = source.SortByName;
        target.StartFilesExtra = source.StartFilesExtra;
        target.SimpleStartupRestore = source.SimpleStartupRestore;
        target.MaxRecentFilesCount = source.MaxRecentFilesCount;
        target.SortByMyName = source.SortByMyName;
    }

    private static void ApplyLintToLegacyViaReflection(LintSettings source, IApplicationConfig target)
    {
        target.DisabledLintRules = source.DisabledLintRules;
    }

    private static void ApplyTerminalToLegacyViaReflection(TerminalSettings source, IApplicationConfig target)
    {
        target.TerminalPanelVisible = source.TerminalPanelVisible;
        target.TerminalPanelHeight = source.TerminalPanelHeight;
        target.TerminalShell = source.TerminalShell;
    }
}
