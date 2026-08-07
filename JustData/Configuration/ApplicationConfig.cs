using AppBase.Common.Configuration;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Windows.Forms;

namespace AppBase.Common;


public sealed class ApplicationConfig : IApplicationConfig
{
    public bool NotFirstLaunch { get; set; }
    public List<string> StartsFolderPaths { get; set; }
    public bool SortByLastWrite { get; set; }
    public bool SortByName { get; set; }
    public Dictionary<string, bool> StartFilesExtra { get; set; }
    public bool SimpleStartupRestore { get; set; } = true;
    public int MaxRecentFilesCount { get; set; } = 20;
    public bool SortByMyName { get; set; }
    public int SortMethod { get; set; }
    public bool DontShowOwner { get; set; }
    public int SelectedFormatter { get; set; }
    public bool DoNotWarnFullUpdateDelete { get; set; }
    public bool FastLogin { get; set; }
    public bool UseSpecialTabNames { get; set; }
    public int GenerateToolTipTime { get; set; } = 200;
    public int DelayedTextChangedInterval { get; set; } = 100;
    public int ResultRowsLimit { get; set; } = 200_000;
    public int ResultRowsLimitWarning { get; set; } = 100_000;
    public bool PinDataByDefault { get; set; }
    public int ConnectionTimeout { get; set; } = 5;
    public int CommandTimeout { get; set; } = 3600;
    public int CommandDistTimeout { get; set; } = 60;
    public int FileSearchTimeout { get; set; } = 10_000;
    public int ToolTipDelay { get; set; } = 500;
    public int PopupMenuDefaultAppearInterval { get; set; } = 150;
    public int LongQueryWarning { get; set; } = 36_000;
    public int ElapsedWarning { get; set; } = 300;
    public int EstimatedWarning { get; set; } = 500_000;
    public int EstimatedWarningInterval { get; set; } = 120_000;
    public bool CloseConnectionByDefault { get; set; }
    public bool AlternatingRows { get; set; }
    public bool DoLegend { get; set; }
    public bool UseSpecialColoring { get; set; }
    public string FontName { get; set; }
    public float FontSize { get; set; }
    public DataGridViewAutoSizeColumnsMode AutoSizeColumnsMode { get; set; }
    public List<byte> BackgroundFastColored { get; set; }
    public List<byte> SelectionColorFastColored { get; set; }
    public List<byte> DisabledColorFastColored { get; set; }
    public List<byte> IndentBackColorFastColored { get; set; }
    public List<byte> LineNumberColorFastColored { get; set; }
    public List<byte> FoldingIndicatorColorFastColored { get; set; }
    public List<byte> ForeColorFastColored { get; set; }
    public List<byte> FontkeyWordsStyle1 { get; set; }
    public List<byte> FontkeyWordsStyle2 { get; set; }
    public List<byte> FontparamStyle { get; set; }
    public List<byte> FontmyCommandsStyle { get; set; }
    public List<byte> FontnumberStyle { get; set; }
    public List<byte> FontcommentsStyle { get; set; }
    public List<byte> FontstringsStyle { get; set; }
    public List<byte> FontsameWordsStyle { get; set; }
    public List<byte> DgvDefaultCellStyleBackColor { get; set; }
    public List<byte> DgvAlternatingRowsDefaultCellStyleBackColor { get; set; }
    public List<byte> DgvDefaultCellStyleForeColor { get; set; }
    public List<byte> DgvRowHeadersDefaultCellStyleBack { get; set; }
    public List<byte> DgvColumnHeadersDefaultCellStyleFore { get; set; }
    public List<byte> DgvColumnHeadersDefaultCellStyleBack { get; set; }
    public List<byte> DocMapBackColor { get; set; }
    public List<byte> DocMapForeColor { get; set; }
    public List<byte> TabColor { get; set; }
    public List<byte> SelectedtabColor { get; set; }
    public List<byte> TabTitleColor { get; set; }
    public List<byte> StripBack { get; set; }
    public List<byte> StripFore { get; set; }
    public List<byte> TreeViewBackColor { get; set; }
    public List<byte> TreeViewForeColor { get; set; }
    public List<byte> TreeViewLineColor { get; set; }
    public List<byte> TextBoxFileSearchBackColor { get; set; }
    public List<byte> TextBoxFileSearchForeColor { get; set; }
    public List<byte> MenuItemSelected { get; set; }
    public List<byte> MenuItemSelectedGradientBegin { get; set; }
    public List<byte> MenuItemSelectedGradientEnd { get; set; }
    public List<byte> MenuItemBorder { get; set; }
    public List<byte> MenuItemPressedGradientBegin { get; set; }
    public List<byte> MenuItemPressedGradientMiddle { get; set; }
    public List<byte> MenuItemPressedGradientEnd { get; set; }
    public List<byte> ButtonSelectedHighlightBorder { get; set; }
    public List<byte> GroupingRowColorBack { get; set; }
    public bool TypoCorrect { get; set; }
    public List<string> TypoPatternList { get; set; }
    public int TypoLimit { get; set; }
    public List<string> KeyWordsListForColoring1 { get; set; }
    public List<string> KeyWordsListForColoring2 { get; set; }
    public Dictionary<string, string> QuickSnippets { get; set; }
    public List<string> MyFastXlsxExportList { get; set; }
    public int DefaultNvarcharLength { get; set; }
    public string SepInExportedCsv { get; set; }
    public string SepRowsInExportedCsv { get; set; }
    public string EncondingName { get; set; }
    public string DecimalDelimInCsv { get; set; }
    public string PasteAsExternalSep { get; set; }
    public string SepInExternal { get; set; }
    public int ExternalMAXERRORS { get; set; }
    public bool ImportExisting { get; set; }
    public int CtrlVmode { get; set; }
    public bool UseXlsb { get; set; }
    public bool UseSpecialSeparatorMode { get; set; }
    public string SpecialSeparator { get; set; }
    public string DateTimeFormat { get; set; }
    public string DecimalFormat { get; set; }
    public string IntegerFormat { get; set; }
    public bool ForceDecimalFormat { get; set; }
    public bool AutoCompleteBrackets { get; set; }
    public bool BracketFolding { get; set; }
    public int LargeScriptCharThreshold { get; set; } = 150_000;
    public int LargeScriptLineThreshold { get; set; } = 500;
    public bool DontUseIndent { get; set; }
    public int CurrentWordLengthLimit { get; set; }
    public Dictionary<string, List<string>> ContextScripts { get; set; }
    public int GrifOffsetHeight { get; set; }
    public int LineInterval { get; set; }
    public int CloseWaringLevel { get; set; }
    public bool ResetSchema { get; set; }
    public int RefreshMode => 1;
    public bool LoadSourcesOnStartup { get; set; }
    public bool OnlineOnlyDdls { get; set; } = true;
    public int MaxSchemaParallelism { get; set; } = 16;
    public bool DoNotCollapseRegionsOnOpening { get; set; }
    public string EditorHotkeys { get; set; }
    public int LastReadMessage { get; set; }
    public bool RestoreFoldingState { get; set; } = true;
    public int RegularActionTimerMinutes { get; set; } = 10;
    public int WordWrap { get; set; }
    public int WordWrapAutoIndent { get; set; }
    public bool TerminalPanelVisible { get; set; }
    public int TerminalPanelHeight { get; set; } = 200;
    public int TerminalShell { get; set; }
    public List<string> DisabledLintRules { get; set; } = [];
    public bool LintEditorHighlightShown { get; set; } = true;
    public List<string> DisabledHighlightRules { get; set; } = [];

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
    public int EmbeddedFimCtxSize { get; set; } = 4096;
    public List<string> EmbeddedFimAcceptedLicenseModelIds { get; set; } = [];
    public bool EmbeddedFimAutoPresetApplied { get; set; }

    // AI Chat (shared JustyBase.Ai pipeline)
    public bool EnableAiChat { get; set; }
    public List<JustyBase.Ai.Models.ChatSession> ChatSessions { get; set; } = [];
    public string AiChatBackendId { get; set; } = "codex";
    public string AiChatOpenAiCompatibleEndpoint { get; set; } = "http://localhost:1234/v1";
    public string? AiChatOpenAiCompatibleApiKey { get; set; }
    public string AiChatDefaultModel { get; set; } = "gpt-5.6-luna";
    public string AiChatDefaultReasoningEffort { get; set; } = "low";
    public string AiChatDefaultMode { get; set; } = "expert";
    public bool AiChatAutoConnect { get; set; }
    public int AiChatHistoryLimit { get; set; } = 10;
    public string AiChatSystemPromptOverride { get; set; } = "";
    public double AiChatTemperature { get; set; } = 0.7;
    public int AiChatMaxTokens { get; set; } = 2048;
    public int AiChatRequestTimeoutMs { get; set; } = 60000;
    public int AiChatMaxRetries { get; set; } = 1;
    public string AiChatPreset { get; set; } = "balanced";
    public bool AiChatPresetIsCustom { get; set; }
    public bool EnableEmbeddedChatAi { get; set; }
    public string EmbeddedChatModelId { get; set; } = "qwen3.5-4b";
    public int EmbeddedChatGpuLayers { get; set; } = 99;
    public int EmbeddedChatCtxSize { get; set; } = 4096;
    public List<string> EmbeddedChatAcceptedLicenseModelIds { get; set; } = [];
    public bool LlamaServerPreferVulkan { get; set; } = true;

    public Dictionary<string, Dictionary<int, DatabaseInfo>> CachedDatabaseDictionary { get; set; }

    public void MakeChangesInWrongConfigValues()
    {
        if (CurrentWordLengthLimit == 0)
        {
            CurrentWordLengthLimit = 1000;
        }

        if (GrifOffsetHeight == 0)
        {
            GrifOffsetHeight = 11;
        }

        if (CloseWaringLevel == 0)
        {
            CloseWaringLevel = 2;
        }

        if (DefaultNvarcharLength == 0)
        {
            DefaultNvarcharLength = 255;
        }

        if (TypoLimit == 0)
        {
            TypoLimit = 1;
        }

        // Font settings
        if (string.IsNullOrWhiteSpace(FontName))
        {
            FontName = "Consolas";
        }

        if (FontSize == 0)
        {
            FontSize = 10;
        }

        // AutoSizeColumnsMode settings (default 10)
        if (AutoSizeColumnsMode == 0)
        {
            AutoSizeColumnsMode = (DataGridViewAutoSizeColumnsMode)10;
        }

        // String settings
        this.SepInExportedCsv ??= ";";
        this.SepRowsInExportedCsv ??= "\r\n";
        this.EncondingName ??= "utf-8";
        this.DecimalDelimInCsv ??= ".";
        this.PasteAsExternalSep ??= "\t";
        this.SepInExternal ??= "|";
        this.SpecialSeparator ??= " +";
        this.DateTimeFormat ??= "yyyy-MM-dd HH:mm:ss";
        this.DecimalFormat ??= "N4";
        this.IntegerFormat ??= "N0";


        // EditorHotkeys - very long string with default keyboard shortcuts
        if (string.IsNullOrWhiteSpace(EditorHotkeys))
        {
            EditorHotkeys = "Tab=IndentIncrease, Escape=ClearHints, PgUp=GoPageUp, PgDn=GoPageDown, End=GoEnd, Home=GoHome, Left=GoLeft, Up=GoUp, Right=GoRight, Down=GoDown, Ins=ReplaceMode, Del=DeleteCharRight, F3=FindNext, Shift\u002BTab=IndentDecrease, Shift\u002BPgUp=GoPageUpWithSelection, Shift\u002BPgDn=GoPageDownWithSelection, Shift\u002BEnd=GoEndWithSelection, Shift\u002BHome=GoHomeWithSelection, Shift\u002BLeft=GoLeftWithSelection, Shift\u002BUp=GoUpWithSelection, Shift\u002BRight=GoRightWithSelection, Shift\u002BDown=GoDownWithSelection, Shift\u002BIns=Paste, Shift\u002BDel=Cut, Ctrl\u002BBack=ClearWordLeft, Ctrl\u002BSpace=AutocompleteMenu, Ctrl\u002BEnd=GoLastLine, Ctrl\u002BHome=GoFirstLine, Ctrl\u002BLeft=GoWordLeft, Ctrl\u002BUp=ScrollUp, Ctrl\u002BRight=GoWordRight, Ctrl\u002BDown=ScrollDown, Ctrl\u002BIns=Copy, Ctrl\u002BDel=ClearWordRight, Ctrl\u002B0=ZoomNormal, Ctrl\u002BA=SelectAll, Ctrl\u002BC=Copy, Ctrl\u002BD=CloneLine, Ctrl\u002BE=MacroExecute, Ctrl\u002BF=FindDialog, Ctrl\u002BG=GoToDialog, Ctrl\u002BH=ReplaceDialog, Ctrl\u002BI=AutoIndentChars, Ctrl\u002BJ=UpperCaseNoTxt, Ctrl\u002BM=MacroRecord, Ctrl\u002BN=GoNextBookmark, Ctrl\u002BU=UpperCase, Ctrl\u002BV=Paste, Ctrl\u002BX=Cut, Ctrl\u002BY=Redo, Ctrl\u002BZ=Undo, Ctrl\u002BAdd=ZoomIn, Ctrl\u002BSeparator=BookmarkLine, Ctrl\u002BSubtract=ZoomOut, Ctrl\u002BOemMinus=NavigateBackward, Ctrl\u002BOemQuestion=CommentSelected, Ctrl\u002BShift\u002BEnd=GoLastLineWithSelection, Ctrl\u002BShift\u002BHome=GoFirstLineWithSelection, Ctrl\u002BShift\u002BLeft=GoWordLeftWithSelection, Ctrl\u002BShift\u002BRight=GoWordRightWithSelection, Ctrl\u002BShift\u002BB=UnbookmarkLine, Ctrl\u002BShift\u002BJ=LowerCaseNoTxt, Ctrl\u002BShift\u002BN=GoPrevBookmark, Ctrl\u002BShift\u002BU=LowerCase, Ctrl\u002BShift\u002BOemMinus=NavigateForward, Alt\u002BBack=Undo, Alt\u002BUp=MoveSelectedLinesUp, Alt\u002BDown=MoveSelectedLinesDown, Alt\u002BF=FindChar, Alt\u002BShift\u002BLeft=GoLeft_ColumnSelectionMode, Alt\u002BShift\u002BUp=GoUp_ColumnSelectionMode, Alt\u002BShift\u002BRight=GoRight_ColumnSelectionMode, Alt\u002BShift\u002BDown=GoDown_ColumnSelectionMode";
        }

        // Initialize collections if they are null
        StartsFolderPaths ??= new List<string>();
        StartFilesExtra ??= new Dictionary<string, bool>();
        MyFastXlsxExportList ??= new List<string>();

        // Initialize TypoPatternList with default values
        if (TypoPatternList == null || TypoPatternList.Count == 0)
        {
            TypoPatternList = new List<string>
        {
            "SELECT", "DISTINCT", "FROM", "WHERE", "GROUP", "HAVING", "ORDER",
            "PARTITION", "BETWEEN", "LIMIT", "FIRST_VALUE", "LAST_VALUE",
            "DENSE_RANK", "DROP", "CROSS", "JOIN", "LEFT", "SUBSTRING", "INTO",
            "DATE_PART", "DECODE", "NULLIF", "COALESCE", "RENAME"
        };
        }

        // Initialize KeyWordsListForColoring1
        if (KeyWordsListForColoring1 == null || KeyWordsListForColoring1.Count == 0)
        {
            KeyWordsListForColoring1 = new List<string>
        {
            "set", "commit", "catalog", "rowid", "in", "versions", "groom", "truncate",
            "explain", "verbose", "final", "synonym", "sequence", "dataobject", "maxerrors",
            "delimiter", "escapechar", "timestyle", "logdir", "y2base", "encoding",
            "remotesource", "DECIMALDELIM", "skiprows", "primary", "unique", "alter",
            "add", "constraint", "action", "no", "references", "foreign", "key", "default",
            "national", "character", "varying", "comment", "column", "by", "grant", "to",
            "view", "using", "external", "nvarchar", "varchar", "char", "nchar", "numeric",
            "boolean", "float", "double", "date", "datetime", "time", "interval", "byteint",
            "smallint", "real", "bigint", "integer", "int", "timestamp", "drop", "session",
            "if", "exists", "or", "procedure", "call", "insert", "into", "asc", "desc",
            "over", "limit", "null", "is", "not", "delete", "when", "then", "update",
            "table", "random", "distribute", "organize", "on", "row", "rows", "between",
            "and", "current", "preceding", "following", "unbounded", "with", "create",
            "temp", "select", "from", "where", "group", "having", "order", "distinct",
            "cross", "join", "left", "case", "else", "partition", "end", "as", "union",
            "all", "minus", "intersect", "like", "RETURNS", "EXECUTE", "LANGUAGE",
            "NZPLSQL", "BEGIN_PROC", "DECLARE", "ALIAS", "FOR", "BEGIN", "END_PROC",
            "CALLER", "OWNER", "RAISE", "NOTICE", "RENAME", "PRIVILEGES", "GENERATE",
            "EXPRESS", "STATISTICS", "inner"
        };
        }

        // Initialize KeyWordsListForColoring2
        if (KeyWordsListForColoring2 == null || KeyWordsListForColoring2.Count == 0)
        {
            KeyWordsListForColoring2 = new List<string>
        {
            "replace", "strleft", "strright", "upper", "lower", "substring", "substr",
            "to_number", "to_timestamp", "cast", "to_char", "to_date", "date_part",
            "date_trunc", "now", "row_number", "lag", "lead", "FIRST_VALUE", "LAST_VALUE",
            "DENSE_RANK", "nvl", "nvl2", "coalesce", "nullif", "count", "sum", "avg",
            "min", "max", "median", "group_concat", "le_dst", "dle_dst", "nysiis",
            "dbl_mp", "pri_mp", "sec_mp", "score_mp", "random", "abs", "ceil", "floor",
            "mod", "round", "trunc", "ascii", "btrim", "trim", "chr", "initcap", "instr",
            "length", "lpad", "ltrim", "repeat", "rpad", "rtrim", "add_months", "age",
            "last_day", "months_between", "overlaps", "translate", "decode", "extract",
            "current_date", "current_time", "current_timestamp", "current_catalog",
            "current_user", "current_db", "current_userid", "current_useroid"
        };
        }

        // Initialize QuickSnippets
        if (QuickSnippets == null || QuickSnippets.Count == 0)
        {
            QuickSnippets = new Dictionary<string, string>
        {
            { "SX", "SELECT" },
            { "SX*", "SELECT * FROM" },
            { "WX", "WHERE" },
            { "LX", "LIMIT" },
            { "HX", "HAVING" },
            { "GX", "GROUP BY" },
            { "FX", "FROM" },
            { "OX", "ORDER BY" },
            { "LIKE", "LIKE '%^%'" }
        };
        }

        // Initialize ContextScripts
        if (ContextScripts == null || ContextScripts.Count == 0)
        {
            ContextScripts = new Dictionary<string, List<string>>
        {
            { "Script1", new List<string> { "select 'pre';", "select 'main';", "select 'post';", "YYYYY" } }
        };
        }

        // Initialize colors - check if each color is null or empty
        BackgroundFastColored ??= new List<byte> { 30, 30, 30, 255 };
        SelectionColorFastColored ??= new List<byte> { 255, 255, 255, 255 };
        DisabledColorFastColored ??= new List<byte> { 180, 180, 180, 255 };
        IndentBackColorFastColored ??= new List<byte> { 30, 30, 30, 255 };
        LineNumberColorFastColored ??= new List<byte> { 0, 122, 204, 255 };
        FoldingIndicatorColorFastColored ??= new List<byte> { 30, 30, 30, 255 };
        ForeColorFastColored ??= new List<byte> { 250, 250, 250, 255 };
        FontkeyWordsStyle1 ??= new List<byte> { 114, 176, 99, 255 };
        FontkeyWordsStyle2 ??= new List<byte> { 250, 0, 250, 255 };
        FontparamStyle ??= new List<byte> { 50, 205, 50, 255 };
        FontmyCommandsStyle ??= new List<byte> { 120, 0, 0, 255 };
        FontnumberStyle ??= new List<byte> { 224, 159, 99, 255 };
        FontcommentsStyle ??= new List<byte> { 0, 128, 128, 255 };
        FontstringsStyle ??= new List<byte> { 207, 85, 45, 255 };
        FontsameWordsStyle ??= new List<byte> { 150, 150, 150, 100 };
        DgvDefaultCellStyleBackColor ??= new List<byte> { 30, 30, 30, 255 };
        DgvAlternatingRowsDefaultCellStyleBackColor ??= new List<byte> { 38, 38, 38, 255 };
        DgvDefaultCellStyleForeColor ??= new List<byte> { 241, 241, 241, 255 };
        DgvRowHeadersDefaultCellStyleBack ??= new List<byte> { 15, 15, 15, 255 };
        DgvColumnHeadersDefaultCellStyleFore ??= new List<byte> { 241, 241, 241, 255 };
        DgvColumnHeadersDefaultCellStyleBack ??= new List<byte> { 15, 15, 15, 255 };
        DocMapBackColor ??= new List<byte> { 30, 30, 30, 255 };
        DocMapForeColor ??= new List<byte> { 241, 241, 241, 255 };
        TabColor ??= new List<byte> { 45, 45, 48, 255 };
        SelectedtabColor ??= new List<byte> { 0, 122, 204, 255 };
        TabTitleColor ??= new List<byte> { 241, 241, 241, 255 };
        StripBack ??= new List<byte> { 45, 45, 48, 255 };
        StripFore ??= new List<byte> { 241, 241, 241, 255 };
        TreeViewBackColor ??= new List<byte> { 30, 30, 30, 255 };
        TreeViewForeColor ??= new List<byte> { 241, 241, 241, 255 };
        TreeViewLineColor ??= new List<byte> { 200, 200, 200, 255 };
        TextBoxFileSearchBackColor ??= new List<byte> { 30, 30, 30, 255 };
        TextBoxFileSearchForeColor ??= new List<byte> { 241, 241, 241, 255 };
        MenuItemSelected ??= new List<byte> { 62, 62, 66, 255 };
        MenuItemSelectedGradientBegin ??= new List<byte> { 62, 62, 66, 255 };
        MenuItemSelectedGradientEnd ??= new List<byte> { 45, 45, 48, 255 };
        MenuItemBorder ??= new List<byte> { 80, 80, 80, 255 };
        MenuItemPressedGradientBegin ??= new List<byte> { 80, 80, 80, 255 };
        MenuItemPressedGradientMiddle ??= new List<byte> { 62, 62, 66, 255 };
        MenuItemPressedGradientEnd ??= new List<byte> { 45, 45, 48, 255 };
        ButtonSelectedHighlightBorder ??= new List<byte> { 0, 0, 0, 255 };
        GroupingRowColorBack ??= new List<byte> { 120, 120, 120, 255 };

        // Boolean settings - assign default values where it makes sense
        // From the JSON most have false value, so no need to set them
        // But several have different default values:
        SortByLastWrite = true; // from JSON
        SortByMyName = true; // from JSON
        CloseConnectionByDefault = true; // from JSON
        AlternatingRows = true; // from JSON
        DoLegend = true; // from JSON
        TypoCorrect = true; // from JSON
        UseXlsb = true; // from JSON
        AutoCompleteBrackets = true; // from JSON
    }
}


[JsonSerializable(typeof(ApplicationConfig))]
public partial class MyJsonContextApplicationConfig : JsonSerializerContext
{
}



