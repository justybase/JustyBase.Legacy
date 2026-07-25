using CommunityToolkit.Mvvm.ComponentModel;
using JustData.Application.Settings;

namespace JustData.ViewModels.Preferences;

public abstract class SettingsSectionViewModel<T>(T values) : ObservableObject where T : class
{
    public T Values { get; private set; } = values ?? throw new ArgumentNullException(nameof(values));

    internal void ReplaceValues(T values)
    {
        Values = values ?? throw new ArgumentNullException(nameof(values));
        OnPropertyChanged(nameof(Values));
        OnPropertiesReplaced();
    }

    protected virtual void OnPropertiesReplaced() { }
}

public sealed class AppearanceSettingsViewModel(AppearanceSettings values) : SettingsSectionViewModel<AppearanceSettings>(values)
{
    public string FontName { get => Values.FontName; set { if (Values.FontName != value) { Values.FontName = value; OnPropertyChanged(); } } }
    public float FontSize { get => Values.FontSize; set { if (Values.FontSize != value) { Values.FontSize = value; OnPropertyChanged(); } } }
    public bool UseSpecialColoring { get => Values.UseSpecialColoring; set { if (Values.UseSpecialColoring != value) { Values.UseSpecialColoring = value; OnPropertyChanged(); } } }
    public bool AlternatingRows { get => Values.AlternatingRows; set { if (Values.AlternatingRows != value) { Values.AlternatingRows = value; OnPropertyChanged(); } } }
    public bool DoLegend { get => Values.DoLegend; set { if (Values.DoLegend != value) { Values.DoLegend = value; OnPropertyChanged(); } } }
    public RgbaColor BackgroundFastColored { get => Values.BackgroundFastColored; set { if (Values.BackgroundFastColored != value) { Values.BackgroundFastColored = value; OnPropertyChanged(); } } }

    protected override void OnPropertiesReplaced() => OnPropertyChanged(string.Empty);
}

public sealed class EditorSettingsViewModel(EditorSettings values) : SettingsSectionViewModel<EditorSettings>(values)
{
    public int FileSearchTimeout { get => Values.FileSearchTimeout; set { if (Values.FileSearchTimeout != value) { Values.FileSearchTimeout = value; OnPropertyChanged(); } } }
    public bool TypoCorrect { get => Values.TypoCorrect; set { if (Values.TypoCorrect != value) { Values.TypoCorrect = value; OnPropertyChanged(); } } }
    public int TypoLimit { get => Values.TypoLimit; set { if (Values.TypoLimit != value) { Values.TypoLimit = value; OnPropertyChanged(); } } }
    public bool AutoCompleteBrackets { get => Values.AutoCompleteBrackets; set { if (Values.AutoCompleteBrackets != value) { Values.AutoCompleteBrackets = value; OnPropertyChanged(); } } }
    public bool BracketFolding { get => Values.BracketFolding; set { if (Values.BracketFolding != value) { Values.BracketFolding = value; OnPropertyChanged(); } } }
    public bool DontUseIndent { get => Values.DontUseIndent; set { if (Values.DontUseIndent != value) { Values.DontUseIndent = value; OnPropertyChanged(); } } }
    public int WordWrap { get => Values.WordWrap; set { if (Values.WordWrap != value) { Values.WordWrap = value; OnPropertyChanged(); } } }
    public int WordWrapAutoIndent { get => Values.WordWrapAutoIndent; set { if (Values.WordWrapAutoIndent != value) { Values.WordWrapAutoIndent = value; OnPropertyChanged(); } } }
    public Dictionary<string, string> QuickSnippets => Values.QuickSnippets;

    protected override void OnPropertiesReplaced() => OnPropertyChanged(string.Empty);
}

public sealed class SqlResultsSettingsViewModel(SqlResultsSettings values) : SettingsSectionViewModel<SqlResultsSettings>(values)
{
    public int CommandTimeout { get => Values.CommandTimeout; set { if (Values.CommandTimeout != value) { Values.CommandTimeout = value; OnPropertyChanged(); } } }
    public int ResultRowsLimit { get => Values.ResultRowsLimit; set { if (Values.ResultRowsLimit != value) { Values.ResultRowsLimit = value; OnPropertyChanged(); } } }
    public int ResultRowsLimitWarning { get => Values.ResultRowsLimitWarning; set { if (Values.ResultRowsLimitWarning != value) { Values.ResultRowsLimitWarning = value; OnPropertyChanged(); } } }
    public int MaxSchemaParallelism { get => Values.MaxSchemaParallelism; set { if (Values.MaxSchemaParallelism != value) { Values.MaxSchemaParallelism = value; OnPropertyChanged(); } } }
    public bool FastLogin { get => Values.FastLogin; set { if (Values.FastLogin != value) { Values.FastLogin = value; OnPropertyChanged(); } } }
    public int RefreshMode => Values.RefreshMode;

    protected override void OnPropertiesReplaced() => OnPropertyChanged(string.Empty);
}

public sealed class ImportExportSettingsViewModel(ImportExportSettings values) : SettingsSectionViewModel<ImportExportSettings>(values)
{
    public string SepInExportedCsv { get => Values.SepInExportedCsv; set { if (Values.SepInExportedCsv != value) { Values.SepInExportedCsv = value; OnPropertyChanged(); } } }
    public string SepRowsInExportedCsv { get => Values.SepRowsInExportedCsv; set { if (Values.SepRowsInExportedCsv != value) { Values.SepRowsInExportedCsv = value; OnPropertyChanged(); } } }
    public string EncondingName { get => Values.EncondingName; set { if (Values.EncondingName != value) { Values.EncondingName = value; OnPropertyChanged(); } } }
    public bool ImportExisting { get => Values.ImportExisting; set { if (Values.ImportExisting != value) { Values.ImportExisting = value; OnPropertyChanged(); } } }

    protected override void OnPropertiesReplaced() => OnPropertyChanged(string.Empty);
}

public sealed class FilesStartupSettingsViewModel(FilesStartupSettings values) : SettingsSectionViewModel<FilesStartupSettings>(values)
{
    public List<string> StartsFolderPaths => Values.StartsFolderPaths;
    public Dictionary<string, bool> StartFilesExtra => Values.StartFilesExtra;
    public bool SimpleStartupRestore { get => Values.SimpleStartupRestore; set { if (Values.SimpleStartupRestore != value) { Values.SimpleStartupRestore = value; OnPropertyChanged(); } } }
    public int MaxRecentFilesCount { get => Values.MaxRecentFilesCount; set { if (Values.MaxRecentFilesCount != value) { Values.MaxRecentFilesCount = value; OnPropertyChanged(); } } }

    protected override void OnPropertiesReplaced() => OnPropertyChanged(string.Empty);
}

public sealed class LintSettingsViewModel(LintSettings values) : SettingsSectionViewModel<LintSettings>(values)
{
    public List<string> DisabledLintRules => Values.DisabledLintRules;
    public bool EditorHighlightShown { get => Values.EditorHighlightShown; set { if (Values.EditorHighlightShown != value) { Values.EditorHighlightShown = value; OnPropertyChanged(); } } }
    public List<string> DisabledHighlightRules => Values.DisabledHighlightRules;
    protected override void OnPropertiesReplaced() => OnPropertyChanged(string.Empty);
}

public sealed class TerminalSettingsViewModel(TerminalSettings values) : SettingsSectionViewModel<TerminalSettings>(values)
{
    public bool TerminalPanelVisible { get => Values.TerminalPanelVisible; set { if (Values.TerminalPanelVisible != value) { Values.TerminalPanelVisible = value; OnPropertyChanged(); } } }
    public int TerminalPanelHeight { get => Values.TerminalPanelHeight; set { if (Values.TerminalPanelHeight != value) { Values.TerminalPanelHeight = value; OnPropertyChanged(); } } }
    public int TerminalShell { get => Values.TerminalShell; set { if (Values.TerminalShell != value) { Values.TerminalShell = value; OnPropertyChanged(); } } }

    protected override void OnPropertiesReplaced() => OnPropertyChanged(string.Empty);
}
