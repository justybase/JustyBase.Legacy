using DatabaseDataGridView.WinForms.Interfaces;
using FastColoredTextBoxNS.Helpers;

namespace AppBase.Common.Configuration;

public interface IApplicationConfig : IColorConfig, IEditorConfig
{

    bool NotFirstLaunch { get; set; }
    List<string> StartsFolderPaths { get; set; }
    bool SortByLastWrite { get; set; }
    bool SortByName { get; set; }
    Dictionary<string, bool> StartFilesExtra { get; set; }
    bool SimpleStartupRestore { get; set; }
    int MaxRecentFilesCount { get; set; }
    bool SortByMyName { get; set; }
    int SortMethod { get; set; }
    bool DontShowOwner { get; set; }
    int SelectedFormatter { get; set; }
    bool DoNotWarnFullUpdateDelete { get; set; }
    bool FastLogin { get; set; }
    bool UseSpecialTabNames { get; set; }

    int DelayedTextChangedInterval { get; set; }
    int ResultRowsLimit { get; set; }
    int ResultRowsLimitWarning { get; set; }
    bool PinDataByDefault { get; set; }
    int ConnectionTimeout { get; set; }
    int CommandTimeout { get; set; }
    int CommandDistTimeout { get; set; }
    int FileSearchTimeout { get; set; }
    int ToolTipDelay { get; set; }

    int LongQueryWarning { get; set; }
    int ElapsedWarning { get; set; }
    int EstimatedWarning { get; set; }
    int EstimatedWarningInterval { get; set; }
    bool CloseConnectionByDefault { get; set; }
    bool DoLegend { get; set; }
    string FontName { get; set; }
    float FontSize { get; set; }
    DataGridViewAutoSizeColumnsMode AutoSizeColumnsMode { get; set; }

    List<string> KeyWordsListForColoring1 { get; set; }
    List<string> KeyWordsListForColoring2 { get; set; }

    List<string> MyFastXlsxExportList { get; set; }
    int DefaultNvarcharLength { get; set; }
    string SepInExportedCsv { get; set; }
    string SepRowsInExportedCsv { get; set; }
    string EncondingName { get; set; }
    string DecimalDelimInCsv { get; set; }
    string PasteAsExternalSep { get; set; }
    string SepInExternal { get; set; }
    int ExternalMAXERRORS { get; set; }
    bool ImportExisting { get; set; }
    int CtrlVmode { get; set; }
    bool UseXlsb { get; set; }
    bool UseSpecialSeparatorMode { get; set; }
    string SpecialSeparator { get; set; }
    string DateTimeFormat { get; set; }
    string DecimalFormat { get; set; }
    string IntegerFormat { get; set; }
    bool ForceDecimalFormat { get; set; }

    Dictionary<string, List<string>> ContextScripts { get; set; }
    int LineInterval { get; set; }
    int CloseWaringLevel { get; set; }
    bool ResetSchema { get; set; }
    int RefreshMode { get; }
    bool LoadSourcesOnStartup { get; set; }
    bool OnlineOnlyDdls { get; set; }

    int MaxSchemaParallelism { get; set; }

    bool DoNotCollapseRegionsOnOpening { get; set; }

    int LastReadMessage { get; set; }
    bool RestoreFoldingState { get; set; }
    int RegularActionTimerMinutes { get; set; }
    int WordWrap { get; set; }
    int WordWrapAutoIndent { get; set; }
    bool TerminalPanelVisible { get; set; }
    int TerminalPanelHeight { get; set; }
    int TerminalShell { get; set; }
    List<string> DisabledLintRules { get; set; }
    bool LintEditorHighlightShown { get; set; }
    List<string> DisabledHighlightRules { get; set; }

    // Embedded FIM (local GGUF)
    bool EnableEmbeddedFimAi { get; set; }
    string EmbeddedFimModelId { get; set; }
    int EmbeddedFimDebounceMs { get; set; }
    int EmbeddedFimDebounceSeconds { get; set; }
    int EmbeddedFimMaxTokens { get; set; }
    string EmbeddedFimPreset { get; set; }
    int EmbeddedFimMaxPromptTokens { get; set; }
    double EmbeddedFimPrefixPercentage { get; set; }
    double EmbeddedFimSuffixPercentage { get; set; }
    string EmbeddedFimContextWindow { get; set; }
    bool EmbeddedFimPreferVulkan { get; set; }
    int EmbeddedFimGpuLayers { get; set; }
    List<string> EmbeddedFimAcceptedLicenseModelIds { get; set; }
    bool EmbeddedFimAutoPresetApplied { get; set; }

    public Dictionary<string, Dictionary<int, DatabaseInfo>> CachedDatabaseDictionary { get; set; }
    public void MakeChangesInWrongConfigValues();



}

public record DatabaseInfo(int SchemaId,string DatabaseName, string DatabaseOwner, string SchemaName);
