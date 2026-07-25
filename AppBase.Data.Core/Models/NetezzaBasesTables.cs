namespace AppBase.Data.Core.Models;

public record class NetezzaBasesTables
{
    public int TABLE_ID { get; init; }
    public int DATABASE_ID { get; init; }
    public string TABLE_NAME { get; init; } = string.Empty;
    /// <summary>Schema or owner — used for tree display depending on connection mode.</summary>
    public string OWNER_NAME { get; init; } = string.Empty;
    public string SCHEMA_NAME { get; init; } = string.Empty;
    public string OBJECT_OWNER_NAME { get; init; } = string.Empty;
    public string OBJECT_TYPE { get; init; } = string.Empty;
}
