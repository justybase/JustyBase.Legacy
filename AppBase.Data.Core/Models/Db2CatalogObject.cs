namespace AppBase.Data.Core.Models;

/// <summary>DB2 catalog object groups exposed by the schema explorer.</summary>
public enum Db2CatalogObjectType
{
    Table,
    View,
    Nickname,
    Alias,
    Procedure,
    Function,
    Server,
    ServerOption,
    Wrapper,
    WrapperOption,
    UserMapping,
    PassthruAuth
}

/// <summary>
/// Provider-neutral DB2 catalog row used by the MVVM schema explorer.
/// Schema is null for federated/global objects.
/// </summary>
public sealed record Db2CatalogObject(
    Db2CatalogObjectType Type,
    string Name,
    string? Schema = null,
    string? Description = null,
    string? Owner = null,
    bool SupportsColumns = false);
