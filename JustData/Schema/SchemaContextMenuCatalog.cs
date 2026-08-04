using AppBase.Common.Enums;
using JustData.Application.Schema;
using JustyBase.Core.Schema;
using CoreCatalog = JustyBase.Core.Schema.SchemaContextMenuCatalog;

namespace JustyBaseLegacy.UI.Schema;

internal enum SchemaContextAction
{
    UserScripts, DdlNew, DdlClipboard, SelectClipboard, SelectNew, SelectDuplicates,
    SelectDeletedRows, GrantClipboard, Groom, CommentClipboard, Drop, GenerateStatistics,
    EmptyTable, ShowDistribution, ChangeDistribution, Recreate, AddKey, AddUnique,
    ImportData, ExportData, EditColumnComment, DropColumn, AddColumn, SelectSequence,
    Refresh, CollapseAll, DdlAll,
    CreateTable, CreateSequence, CreateProcedure, AddSynonym,
    ShowTableSizes, ShowQueryHistory, ShowUserSessions,
    CreateUser
}

internal sealed record SchemaContextMenuEntry(
    string Text,
    SchemaContextAction? Action = null,
    IReadOnlyList<SchemaContextMenuEntry>? Children = null,
    bool Enabled = true)
{
    public static SchemaContextMenuEntry Separator { get; } = new("-");
}

/// <summary>
/// Legacy UI tree over shared <see cref="JustyBase.Core.Schema.SchemaContextMenuCatalog"/> SQL ids.
/// Host-only actions (Create…, Refresh, charts, column ops) stay local.
/// </summary>
internal static class SchemaContextMenuCatalog
{
    public static IReadOnlyList<SchemaContextMenuEntry> GetEntries(SchemaNode node)
    {
        if (node.Kind == SchemaNodeKind.Column && node.LegacyObjectId.HasValue)
            return ColumnEntries;
        bool netezza = node.LegacyObjectId.HasValue
            && Enum.TryParse(node.ProviderKind, true, out TypeInDatabase _);
        if (!netezza)
            return GetGeneralEntries(node);

        Enum.TryParse(node.ProviderKind, true, out TypeInDatabase providerKind);
        return providerKind switch
        {
            TypeInDatabase.table => TableEntries,
            TypeInDatabase.view => ViewEntries,
            TypeInDatabase.thisExternal => ExternalEntries,
            TypeInDatabase.procedure or TypeInDatabase.function or TypeInDatabase.thisAggregate => ProcedureEntries,
            TypeInDatabase.sequence => SequenceEntries,
            TypeInDatabase.synonym => SynonymEntries,
            _ when node.Kind == SchemaNodeKind.Column => ColumnEntries,
            _ => GetGeneralEntries(node)
        };
    }

    public static string? GetSharedActionId(SchemaContextAction action) => action switch
    {
        SchemaContextAction.SelectNew or SchemaContextAction.SelectClipboard => CoreCatalog.Ids.SelectTop100,
        SchemaContextAction.SelectDuplicates => CoreCatalog.Ids.Duplicates,
        SchemaContextAction.SelectDeletedRows => CoreCatalog.Ids.Deleted,
        SchemaContextAction.GrantClipboard => CoreCatalog.Ids.Grant,
        SchemaContextAction.CommentClipboard => CoreCatalog.Ids.Comment,
        SchemaContextAction.GenerateStatistics => CoreCatalog.Ids.Statistics,
        SchemaContextAction.Groom => CoreCatalog.Ids.Groom,
        SchemaContextAction.ShowDistribution => CoreCatalog.Ids.Distribution,
        SchemaContextAction.EmptyTable => CoreCatalog.Ids.Empty,
        SchemaContextAction.Recreate => CoreCatalog.Ids.Recreate,
        SchemaContextAction.ImportData => CoreCatalog.Ids.Import,
        SchemaContextAction.ExportData => CoreCatalog.Ids.Export,
        SchemaContextAction.Drop => CoreCatalog.Ids.Drop,
        SchemaContextAction.DdlNew or SchemaContextAction.DdlClipboard => CoreCatalog.Ids.Ddl,
        _ => null
    };

    public static string? TryFormatSharedSql(SchemaContextAction action, string qualifiedObject)
    {
        string? id = GetSharedActionId(action);
        if (id is null || !CoreCatalog.TryGet(id, out SchemaContextMenuAction shared) || shared.IsHostOnly)
            return null;
        return CoreCatalog.Format(shared, qualifiedObject);
    }

    private static IReadOnlyList<SchemaContextMenuEntry> GetGeneralEntries(SchemaNode node) => node.Kind switch
    {
        SchemaNodeKind.Table =>
        [
            new("DDL to new query window", SchemaContextAction.DdlNew),
            new("DDL to clipboard", SchemaContextAction.DdlClipboard),
            SchemaContextMenuEntry.Separator,
            new("Select Top 100 to clipboard", SchemaContextAction.SelectClipboard),
            new("Select Top 100 to new query window", SchemaContextAction.SelectNew),
            new("Select duplicates to clipboard", SchemaContextAction.SelectDuplicates)
        ],
        SchemaNodeKind.View or SchemaNodeKind.Procedure or SchemaNodeKind.Function
            or SchemaNodeKind.Alias or SchemaNodeKind.Nickname or SchemaNodeKind.Synonym =>
        [
            new("DDL to new query window", SchemaContextAction.DdlNew),
            new("DDL to clipboard", SchemaContextAction.DdlClipboard)
        ],
        SchemaNodeKind.ObjectGroup when IsTableGroup(node.Name) =>
        [
            new("Refresh", SchemaContextAction.Refresh),
            new("Collapse all", SchemaContextAction.CollapseAll),
            new("DDL Tables", SchemaContextAction.DdlAll)
        ],
        SchemaNodeKind.ObjectGroup =>
        [
            new("Refresh", SchemaContextAction.Refresh),
            new("Collapse all", SchemaContextAction.CollapseAll)
        ],
        SchemaNodeKind.Schema when node.Name.Equals("Tables", StringComparison.OrdinalIgnoreCase) =>
        [
            new("Refresh", SchemaContextAction.Refresh),
            new("Collapse all", SchemaContextAction.CollapseAll),
            new("DDL Tables", SchemaContextAction.DdlAll)
        ],
        SchemaNodeKind.Connection or SchemaNodeKind.Database =>
        [
            new("Refresh", SchemaContextAction.Refresh),
            new("Collapse all", SchemaContextAction.CollapseAll),
            SchemaContextMenuEntry.Separator,
            new("Create", Children:
            [
                new("New table…", SchemaContextAction.CreateTable),
                new("New sequence…", SchemaContextAction.CreateSequence),
                new("New procedure…", SchemaContextAction.CreateProcedure),
                new("New synonym…", SchemaContextAction.AddSynonym)
            ]),
            new("Reports", Children:
            [
                new("Table sizes…", SchemaContextAction.ShowTableSizes),
                new("Query history…", SchemaContextAction.ShowQueryHistory),
                new("User sessions…", SchemaContextAction.ShowUserSessions)
            ]),
            SchemaContextMenuEntry.Separator,
            new("Create user…", SchemaContextAction.CreateUser)
        ],
        SchemaNodeKind.Schema =>
        [
            new("Refresh", SchemaContextAction.Refresh),
            new("Collapse all", SchemaContextAction.CollapseAll),
            SchemaContextMenuEntry.Separator,
            new("Create", Children:
            [
                new("New table…", SchemaContextAction.CreateTable),
                new("New sequence…", SchemaContextAction.CreateSequence),
                new("New procedure…", SchemaContextAction.CreateProcedure),
                new("New synonym…", SchemaContextAction.AddSynonym)
            ])
        ],
        _ => []
    };

    private static bool IsTableGroup(string name) =>
        name.Equals("TABLE", StringComparison.OrdinalIgnoreCase)
        || name.Equals("Tables", StringComparison.OrdinalIgnoreCase);

    private static readonly IReadOnlyList<SchemaContextMenuEntry> TableEntries =
    [
        new("User Scripts", SchemaContextAction.UserScripts),
        new("Others", Children:
        [
            new("Groom table", SchemaContextAction.Groom),
            new("Add comment to clipboard", SchemaContextAction.CommentClipboard),
            new("Drop Table", SchemaContextAction.Drop),
            new("Generate statistics", SchemaContextAction.GenerateStatistics),
            new("Empty table", SchemaContextAction.EmptyTable)
        ]),
        SchemaContextMenuEntry.Separator,
        new("DDL to new query window", SchemaContextAction.DdlNew),
        new("DDL to clipboard", SchemaContextAction.DdlClipboard),
        SchemaContextMenuEntry.Separator,
        new("Select Top 100 to clipboard", SchemaContextAction.SelectClipboard),
        new("Select Top 100 to new query window", SchemaContextAction.SelectNew),
        new("Select duplicates to clipboard", SchemaContextAction.SelectDuplicates),
        new("Select deleted rows", SchemaContextAction.SelectDeletedRows),
        SchemaContextMenuEntry.Separator,
        new("Grant to clipboard", SchemaContextAction.GrantClipboard),
        new("Show distribution", SchemaContextAction.ShowDistribution),
        new("Change distribution", SchemaContextAction.ChangeDistribution),
        new("Recreate to new tab", SchemaContextAction.Recreate),
        SchemaContextMenuEntry.Separator,
        new("Add key to clipboard", SchemaContextAction.AddKey),
        new("Add unique constraint to clipboard", SchemaContextAction.AddUnique),
        SchemaContextMenuEntry.Separator,
        new("Import Data", SchemaContextAction.ImportData),
        new("Export Data", SchemaContextAction.ExportData)
    ];

    private static readonly IReadOnlyList<SchemaContextMenuEntry> ViewEntries =
    [
        new("User Scripts", SchemaContextAction.UserScripts),
        new("DDL to new query window", SchemaContextAction.DdlNew),
        new("DDL to clipboard", SchemaContextAction.DdlClipboard),
        new("Select to Clipboard", SchemaContextAction.SelectClipboard),
        new("Select Duplicates to Clipboard", SchemaContextAction.SelectDuplicates),
        new("Drop view", SchemaContextAction.Drop)
    ];

    private static readonly IReadOnlyList<SchemaContextMenuEntry> ExternalEntries =
    [
        new("User Scripts", SchemaContextAction.UserScripts),
        new("DDL to new query window", SchemaContextAction.DdlNew),
        new("DDL to clipboard", SchemaContextAction.DdlClipboard),
        new("Add new", Enabled: false)
    ];

    private static readonly IReadOnlyList<SchemaContextMenuEntry> ProcedureEntries =
    [new("User Scripts", SchemaContextAction.UserScripts), new("DDL to clipboard", SchemaContextAction.DdlClipboard), new("DDL to new query window", SchemaContextAction.DdlNew)];

    private static readonly IReadOnlyList<SchemaContextMenuEntry> ColumnEntries =
    [new("Edit Comment", SchemaContextAction.EditColumnComment), new("Drop Column", SchemaContextAction.DropColumn), new("Add Column", SchemaContextAction.AddColumn)];

    private static readonly IReadOnlyList<SchemaContextMenuEntry> SequenceEntries =
    [new("Select from sequence", SchemaContextAction.SelectSequence), new("DDL sequence to clipboard", SchemaContextAction.DdlClipboard), new("Drop sequence", SchemaContextAction.Drop)];

    private static readonly IReadOnlyList<SchemaContextMenuEntry> SynonymEntries =
    [new("User Scripts", SchemaContextAction.UserScripts), new("DDL synonym to clipboard", SchemaContextAction.DdlClipboard)];
}
