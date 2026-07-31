namespace JustData.Application.Settings;

public readonly record struct RgbaColor(byte R, byte G, byte B, byte A)
{
    public static RgbaColor FromLegacy(IReadOnlyList<byte>? value) => value is { Count: >= 4 }
        ? new(value[0], value[1], value[2], value[3])
        : new(0, 0, 0, 255);

    public byte[] ToLegacy() => [R, G, B, A];
}

public sealed record DatabaseInfoSnapshot(int SchemaId, string DatabaseName, string DatabaseOwner, string SchemaName);

public sealed class SnippetSettings
{
    public List<string> Keywords { get; set; } = [];
    public List<string> Snippets { get; set; } = [];
    public List<string> MonkeySnippets { get; set; } = [];

    public SnippetSettings Clone() => new()
    {
        Keywords = Keywords?.ToList() ?? [],
        Snippets = Snippets?.ToList() ?? [],
        MonkeySnippets = MonkeySnippets?.ToList() ?? []
    };
}

public sealed class AppearanceSettings
{
    public bool AlternatingRows { get; set; }
    public bool DoLegend { get; set; }
    public bool UseSpecialColoring { get; set; }
    public string FontName { get; set; } = string.Empty;
    public float FontSize { get; set; }
    public int AutoSizeColumnsMode { get; set; }
    public int GrifOffsetHeight { get; set; }
    public RgbaColor BackgroundFastColored { get; set; }
    public RgbaColor SelectionColorFastColored { get; set; }
    public RgbaColor DisabledColorFastColored { get; set; }
    public RgbaColor IndentBackColorFastColored { get; set; }
    public RgbaColor LineNumberColorFastColored { get; set; }
    public RgbaColor FoldingIndicatorColorFastColored { get; set; }
    public RgbaColor ForeColorFastColored { get; set; }
    public RgbaColor FontkeyWordsStyle1 { get; set; }
    public RgbaColor FontkeyWordsStyle2 { get; set; }
    public RgbaColor FontparamStyle { get; set; }
    public RgbaColor FontmyCommandsStyle { get; set; }
    public RgbaColor FontnumberStyle { get; set; }
    public RgbaColor FontcommentsStyle { get; set; }
    public RgbaColor FontstringsStyle { get; set; }
    public RgbaColor FontsameWordsStyle { get; set; }
    public RgbaColor DgvDefaultCellStyleBackColor { get; set; }
    public RgbaColor DgvAlternatingRowsDefaultCellStyleBackColor { get; set; }
    public RgbaColor DgvDefaultCellStyleForeColor { get; set; }
    public RgbaColor DgvRowHeadersDefaultCellStyleBack { get; set; }
    public RgbaColor DgvColumnHeadersDefaultCellStyleFore { get; set; }
    public RgbaColor DgvColumnHeadersDefaultCellStyleBack { get; set; }
    public RgbaColor DocMapBackColor { get; set; }
    public RgbaColor DocMapForeColor { get; set; }
    public RgbaColor TabColor { get; set; }
    public RgbaColor SelectedtabColor { get; set; }
    public RgbaColor TabTitleColor { get; set; }
    public RgbaColor StripBack { get; set; }
    public RgbaColor StripFore { get; set; }
    public RgbaColor TreeViewBackColor { get; set; }
    public RgbaColor TreeViewForeColor { get; set; }
    public RgbaColor TreeViewLineColor { get; set; }
    public RgbaColor TextBoxFileSearchBackColor { get; set; }
    public RgbaColor TextBoxFileSearchForeColor { get; set; }
    public RgbaColor MenuItemSelected { get; set; }
    public RgbaColor MenuItemSelectedGradientBegin { get; set; }
    public RgbaColor MenuItemSelectedGradientEnd { get; set; }
    public RgbaColor MenuItemBorder { get; set; }
    public RgbaColor MenuItemPressedGradientBegin { get; set; }
    public RgbaColor MenuItemPressedGradientMiddle { get; set; }
    public RgbaColor MenuItemPressedGradientEnd { get; set; }
    public RgbaColor ButtonSelectedHighlightBorder { get; set; }
    public RgbaColor GroupingRowColorBack { get; set; }

    public AppearanceSettings Clone() => new()
    {
        AlternatingRows = AlternatingRows,
        DoLegend = DoLegend,
        UseSpecialColoring = UseSpecialColoring,
        FontName = FontName,
        FontSize = FontSize,
        AutoSizeColumnsMode = AutoSizeColumnsMode,
        GrifOffsetHeight = GrifOffsetHeight,
        BackgroundFastColored = BackgroundFastColored,
        SelectionColorFastColored = SelectionColorFastColored,
        DisabledColorFastColored = DisabledColorFastColored,
        IndentBackColorFastColored = IndentBackColorFastColored,
        LineNumberColorFastColored = LineNumberColorFastColored,
        FoldingIndicatorColorFastColored = FoldingIndicatorColorFastColored,
        ForeColorFastColored = ForeColorFastColored,
        FontkeyWordsStyle1 = FontkeyWordsStyle1,
        FontkeyWordsStyle2 = FontkeyWordsStyle2,
        FontparamStyle = FontparamStyle,
        FontmyCommandsStyle = FontmyCommandsStyle,
        FontnumberStyle = FontnumberStyle,
        FontcommentsStyle = FontcommentsStyle,
        FontstringsStyle = FontstringsStyle,
        FontsameWordsStyle = FontsameWordsStyle,
        DgvDefaultCellStyleBackColor = DgvDefaultCellStyleBackColor,
        DgvAlternatingRowsDefaultCellStyleBackColor = DgvAlternatingRowsDefaultCellStyleBackColor,
        DgvDefaultCellStyleForeColor = DgvDefaultCellStyleForeColor,
        DgvRowHeadersDefaultCellStyleBack = DgvRowHeadersDefaultCellStyleBack,
        DgvColumnHeadersDefaultCellStyleFore = DgvColumnHeadersDefaultCellStyleFore,
        DgvColumnHeadersDefaultCellStyleBack = DgvColumnHeadersDefaultCellStyleBack,
        DocMapBackColor = DocMapBackColor,
        DocMapForeColor = DocMapForeColor,
        TabColor = TabColor,
        SelectedtabColor = SelectedtabColor,
        TabTitleColor = TabTitleColor,
        StripBack = StripBack,
        StripFore = StripFore,
        TreeViewBackColor = TreeViewBackColor,
        TreeViewForeColor = TreeViewForeColor,
        TreeViewLineColor = TreeViewLineColor,
        TextBoxFileSearchBackColor = TextBoxFileSearchBackColor,
        TextBoxFileSearchForeColor = TextBoxFileSearchForeColor,
        MenuItemSelected = MenuItemSelected,
        MenuItemSelectedGradientBegin = MenuItemSelectedGradientBegin,
        MenuItemSelectedGradientEnd = MenuItemSelectedGradientEnd,
        MenuItemBorder = MenuItemBorder,
        MenuItemPressedGradientBegin = MenuItemPressedGradientBegin,
        MenuItemPressedGradientMiddle = MenuItemPressedGradientMiddle,
        MenuItemPressedGradientEnd = MenuItemPressedGradientEnd,
        ButtonSelectedHighlightBorder = ButtonSelectedHighlightBorder,
        GroupingRowColorBack = GroupingRowColorBack
    };
}

public sealed class EditorSettings
{
    public int GenerateToolTipTime { get; set; }
    public int DelayedTextChangedInterval { get; set; }
    public int ToolTipDelay { get; set; }
    public int PopupMenuDefaultAppearInterval { get; set; }
    public int FileSearchTimeout { get; set; }
    public bool TypoCorrect { get; set; }
    public List<string> TypoPatternList { get; set; } = [];
    public int TypoLimit { get; set; }
    public List<string> KeyWordsListForColoring1 { get; set; } = [];
    public List<string> KeyWordsListForColoring2 { get; set; } = [];
    public Dictionary<string, string> QuickSnippets { get; set; } = [];
    public bool AutoCompleteBrackets { get; set; }
    public bool BracketFolding { get; set; }
    public bool DontUseIndent { get; set; }
    public int CurrentWordLengthLimit { get; set; }
    public Dictionary<string, List<string>> ContextScripts { get; set; } = [];
    public int LineInterval { get; set; }
    public bool DoNotCollapseRegionsOnOpening { get; set; }
    public string EditorHotkeys { get; set; } = string.Empty;
    public bool RestoreFoldingState { get; set; }
    public int WordWrap { get; set; }
    public int WordWrapAutoIndent { get; set; }

    public EditorSettings Clone() => new()
    {
        GenerateToolTipTime = GenerateToolTipTime,
        DelayedTextChangedInterval = DelayedTextChangedInterval,
        ToolTipDelay = ToolTipDelay,
        PopupMenuDefaultAppearInterval = PopupMenuDefaultAppearInterval,
        FileSearchTimeout = FileSearchTimeout,
        TypoCorrect = TypoCorrect,
        TypoPatternList = TypoPatternList?.ToList() ?? [],
        TypoLimit = TypoLimit,
        KeyWordsListForColoring1 = KeyWordsListForColoring1?.ToList() ?? [],
        KeyWordsListForColoring2 = KeyWordsListForColoring2?.ToList() ?? [],
        QuickSnippets = QuickSnippets is not null ? new(QuickSnippets) : [],
        AutoCompleteBrackets = AutoCompleteBrackets,
        BracketFolding = BracketFolding,
        DontUseIndent = DontUseIndent,
        CurrentWordLengthLimit = CurrentWordLengthLimit,
        ContextScripts = ContextScripts?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToList()) ?? [],
        LineInterval = LineInterval,
        DoNotCollapseRegionsOnOpening = DoNotCollapseRegionsOnOpening,
        EditorHotkeys = EditorHotkeys,
        RestoreFoldingState = RestoreFoldingState,
        WordWrap = WordWrap,
        WordWrapAutoIndent = WordWrapAutoIndent
    };
}

public sealed class SqlResultsSettings
{
    public bool DoNotWarnFullUpdateDelete { get; set; }
    public bool FastLogin { get; set; }
    public bool UseSpecialTabNames { get; set; }
    public int SelectedFormatter { get; set; }
    public int ResultRowsLimit { get; set; }
    public int ResultRowsLimitWarning { get; set; }
    public bool PinDataByDefault { get; set; }
    public int ConnectionTimeout { get; set; }
    public int CommandTimeout { get; set; }
    public int CommandDistTimeout { get; set; }
    public int LongQueryWarning { get; set; }
    public int ElapsedWarning { get; set; }
    public int EstimatedWarning { get; set; }
    public int EstimatedWarningInterval { get; set; }
    public bool CloseConnectionByDefault { get; set; }
    public string DateTimeFormat { get; set; } = string.Empty;
    public string DecimalFormat { get; set; } = string.Empty;
    public string IntegerFormat { get; set; } = string.Empty;
    public bool ForceDecimalFormat { get; set; }
    public bool DontShowOwner { get; set; }
    public int SortMethod { get; set; }
    public bool ResetSchema { get; set; }
    public bool LoadSourcesOnStartup { get; set; }
    public bool OnlineOnlyDdls { get; set; }
    public int MaxSchemaParallelism { get; set; }
    public int CloseWaringLevel { get; set; }
    public int LastReadMessage { get; set; }
    public int RegularActionTimerMinutes { get; set; }
    public Dictionary<string, Dictionary<int, DatabaseInfoSnapshot>> CachedDatabaseDictionary { get; set; } = [];
    public int RefreshMode => 1;

    public SqlResultsSettings Clone() => new()
    {
        DoNotWarnFullUpdateDelete = DoNotWarnFullUpdateDelete,
        FastLogin = FastLogin,
        UseSpecialTabNames = UseSpecialTabNames,
        SelectedFormatter = SelectedFormatter,
        ResultRowsLimit = ResultRowsLimit,
        ResultRowsLimitWarning = ResultRowsLimitWarning,
        PinDataByDefault = PinDataByDefault,
        ConnectionTimeout = ConnectionTimeout,
        CommandTimeout = CommandTimeout,
        CommandDistTimeout = CommandDistTimeout,
        LongQueryWarning = LongQueryWarning,
        ElapsedWarning = ElapsedWarning,
        EstimatedWarning = EstimatedWarning,
        EstimatedWarningInterval = EstimatedWarningInterval,
        CloseConnectionByDefault = CloseConnectionByDefault,
        DateTimeFormat = DateTimeFormat,
        DecimalFormat = DecimalFormat,
        IntegerFormat = IntegerFormat,
        ForceDecimalFormat = ForceDecimalFormat,
        DontShowOwner = DontShowOwner,
        SortMethod = SortMethod,
        ResetSchema = ResetSchema,
        LoadSourcesOnStartup = LoadSourcesOnStartup,
        OnlineOnlyDdls = OnlineOnlyDdls,
        MaxSchemaParallelism = MaxSchemaParallelism,
        CloseWaringLevel = CloseWaringLevel,
        LastReadMessage = LastReadMessage,
        RegularActionTimerMinutes = RegularActionTimerMinutes,
        CachedDatabaseDictionary = CachedDatabaseDictionary?.ToDictionary(
            c => c.Key,
            c => c.Value.ToDictionary(d => d.Key, d => d.Value)) ?? []
    };
}

public sealed class ImportExportSettings
{
    public List<string> MyFastXlsxExportList { get; set; } = [];
    public int DefaultNvarcharLength { get; set; }
    public string SepInExportedCsv { get; set; } = string.Empty;
    public string SepRowsInExportedCsv { get; set; } = string.Empty;
    public string EncondingName { get; set; } = string.Empty;
    public string DecimalDelimInCsv { get; set; } = string.Empty;
    public string PasteAsExternalSep { get; set; } = string.Empty;
    public string SepInExternal { get; set; } = string.Empty;
    public int ExternalMAXERRORS { get; set; }
    public bool ImportExisting { get; set; }
    public int CtrlVmode { get; set; }
    public bool UseXlsb { get; set; }
    public bool UseSpecialSeparatorMode { get; set; }
    public string SpecialSeparator { get; set; } = string.Empty;

    public ImportExportSettings Clone() => new()
    {
        MyFastXlsxExportList = MyFastXlsxExportList?.ToList() ?? [],
        DefaultNvarcharLength = DefaultNvarcharLength,
        SepInExportedCsv = SepInExportedCsv,
        SepRowsInExportedCsv = SepRowsInExportedCsv,
        EncondingName = EncondingName,
        DecimalDelimInCsv = DecimalDelimInCsv,
        PasteAsExternalSep = PasteAsExternalSep,
        SepInExternal = SepInExternal,
        ExternalMAXERRORS = ExternalMAXERRORS,
        ImportExisting = ImportExisting,
        CtrlVmode = CtrlVmode,
        UseXlsb = UseXlsb,
        UseSpecialSeparatorMode = UseSpecialSeparatorMode,
        SpecialSeparator = SpecialSeparator
    };
}

public sealed class FilesStartupSettings
{
    public bool NotFirstLaunch { get; set; }
    public List<string> StartsFolderPaths { get; set; } = [];
    public bool SortByLastWrite { get; set; }
    public bool SortByName { get; set; }
    public Dictionary<string, bool> StartFilesExtra { get; set; } = [];
    public bool SimpleStartupRestore { get; set; }
    public int MaxRecentFilesCount { get; set; }
    public bool SortByMyName { get; set; }

    public FilesStartupSettings Clone() => new()
    {
        NotFirstLaunch = NotFirstLaunch,
        StartsFolderPaths = StartsFolderPaths?.ToList() ?? [],
        SortByLastWrite = SortByLastWrite,
        SortByName = SortByName,
        StartFilesExtra = StartFilesExtra is not null ? new(StartFilesExtra) : [],
        SimpleStartupRestore = SimpleStartupRestore,
        MaxRecentFilesCount = MaxRecentFilesCount,
        SortByMyName = SortByMyName
    };
}

public sealed class LintSettings
{
    public List<string> DisabledLintRules { get; set; } = [];
    public bool EditorHighlightShown { get; set; } = true;
    public List<string> DisabledHighlightRules { get; set; } = [];

    public LintSettings Clone() => new()
    {
        DisabledLintRules = DisabledLintRules?.ToList() ?? [],
        EditorHighlightShown = EditorHighlightShown,
        DisabledHighlightRules = DisabledHighlightRules?.ToList() ?? []
    };
}

public sealed class TerminalSettings
{
    public bool TerminalPanelVisible { get; set; }
    public int TerminalPanelHeight { get; set; }
    public int TerminalShell { get; set; }

    public TerminalSettings Clone() => new()
    {
        TerminalPanelVisible = TerminalPanelVisible,
        TerminalPanelHeight = TerminalPanelHeight,
        TerminalShell = TerminalShell
    };
}

public sealed class EmbeddedFimSettings
{
    public bool EnableEmbeddedFimAi { get; set; }
    public string EmbeddedFimModelId { get; set; } = "qwen2.5-coder-3b";
    public int EmbeddedFimDebounceMs { get; set; } = 600;
    public int EmbeddedFimDebounceSeconds { get; set; }
    public int EmbeddedFimMaxTokens { get; set; } = 50;
    public string EmbeddedFimPreset { get; set; } = "Medium";
    public int EmbeddedFimMaxPromptTokens { get; set; } = 1536;
    public double EmbeddedFimPrefixPercentage { get; set; } = 0.65;
    public double EmbeddedFimSuffixPercentage { get; set; } = 0.35;
    public string EmbeddedFimContextWindow { get; set; } = "Medium";
    public bool EmbeddedFimPreferVulkan { get; set; } = true;
    public int EmbeddedFimGpuLayers { get; set; } = 99;
    public List<string> EmbeddedFimAcceptedLicenseModelIds { get; set; } = [];
    public bool EmbeddedFimAutoPresetApplied { get; set; }

    public EmbeddedFimSettings Clone() => new()
    {
        EnableEmbeddedFimAi = EnableEmbeddedFimAi,
        EmbeddedFimModelId = EmbeddedFimModelId,
        EmbeddedFimDebounceMs = EmbeddedFimDebounceMs,
        EmbeddedFimDebounceSeconds = EmbeddedFimDebounceSeconds,
        EmbeddedFimMaxTokens = EmbeddedFimMaxTokens,
        EmbeddedFimPreset = EmbeddedFimPreset,
        EmbeddedFimMaxPromptTokens = EmbeddedFimMaxPromptTokens,
        EmbeddedFimPrefixPercentage = EmbeddedFimPrefixPercentage,
        EmbeddedFimSuffixPercentage = EmbeddedFimSuffixPercentage,
        EmbeddedFimContextWindow = EmbeddedFimContextWindow,
        EmbeddedFimPreferVulkan = EmbeddedFimPreferVulkan,
        EmbeddedFimGpuLayers = EmbeddedFimGpuLayers,
        EmbeddedFimAcceptedLicenseModelIds = EmbeddedFimAcceptedLicenseModelIds?.ToList() ?? [],
        EmbeddedFimAutoPresetApplied = EmbeddedFimAutoPresetApplied
    };
}

public sealed class ApplicationSettingsDraft
{
    public AppearanceSettings Appearance { get; set; } = new();
    public EditorSettings Editor { get; set; } = new();
    public SqlResultsSettings SqlResults { get; set; } = new();
    public ImportExportSettings ImportExport { get; set; } = new();
    public FilesStartupSettings FilesStartup { get; set; } = new();
    public LintSettings Lint { get; set; } = new();
    public TerminalSettings Terminal { get; set; } = new();
    public EmbeddedFimSettings EmbeddedFim { get; set; } = new();
    public SnippetSettings Snippets { get; set; } = new();

    public ApplicationSettingsDraft Clone() => new()
    {
        Appearance = Appearance.Clone(),
        Editor = Editor.Clone(),
        SqlResults = SqlResults.Clone(),
        ImportExport = ImportExport.Clone(),
        FilesStartup = FilesStartup.Clone(),
        Lint = Lint.Clone(),
        Terminal = Terminal.Clone(),
        EmbeddedFim = EmbeddedFim.Clone(),
        Snippets = Snippets.Clone()
    };
}

public sealed class ApplicationSettingsSnapshot
{
    public ApplicationSettingsDraft Values { get; }

    public ApplicationSettingsSnapshot(ApplicationSettingsDraft values)
    {
        Values = values?.Clone() ?? throw new ArgumentNullException(nameof(values));
    }

    public ApplicationSettingsDraft ToDraft() => Values.Clone();
}
