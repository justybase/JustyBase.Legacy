namespace JustData.Application.Schema;

public enum SchemaNodeKind
{
    Connection,
    Database,
    Schema,
    Table,
    View,
    Procedure,
    Function,
    Alias,
    Synonym,
    Sequence,
    Column,
    Index,
    Constraint,
    Partition,
    Trigger,
    Unknown
}

public sealed record SchemaPath(
    string Connection,
    string? Database = null,
    string? Schema = null,
    string? Object = null)
{
    public override string ToString() => string.Join(".",
        new[] { Connection, Database, Schema, Object }
            .Where(value => !string.IsNullOrWhiteSpace(value)));
}

public sealed record SchemaNode(
    string Id,
    string Name,
    SchemaNodeKind Kind,
    SchemaPath Path,
    bool HasChildren,
    int? LegacyObjectId = null,
    string? ProviderKind = null,
    string? DisplayName = null,
    string? Description = null,
    string? Owner = null);

public sealed record SchemaSearchRequest(
    string Query,
    string? Connection = null,
    bool IncludeColumns = false,
    int MaxResults = 1_000);

public sealed record SchemaSearchResult(
    IReadOnlyList<SchemaNode> Nodes,
    bool IsTruncated = false);

public sealed record SchemaReference(
    string Name,
    SchemaNodeKind Kind,
    int Position,
    string? Database = null,
    string? Schema = null);

public enum OutlineNodeKind
{
    Statement,
    Select,
    Cte,
    TempTable,
    Table,
    View,
    Join,
    Subquery,
    Procedure,
    Block,
    Warning
}

/// <summary>Source-backed hierarchical symbol used by the SQL Outline.</summary>
public sealed record OutlineNode(
    string Name,
    OutlineNodeKind Kind,
    int Position,
    int Length = 0,
    string? Alias = null,
    string? Database = null,
    string? Schema = null,
    bool IsIncomplete = false,
    IReadOnlyList<OutlineNode>? Children = null)
{
    public IReadOnlyList<OutlineNode> Children { get; init; } = Children ?? [];
    public string Id => $"{Kind}:{Position}:{Name}";
}

public sealed record SqlOutline(IReadOnlyList<OutlineNode> Nodes, bool IsIncomplete = false)
{
    public static SqlOutline Empty { get; } = new([]);
}

public enum SchemaDdlKind
{
    Create,
    SelectTop,
    AddCode
}

public sealed record SchemaDdlRequest(SchemaNode Node, SchemaDdlKind Kind);
