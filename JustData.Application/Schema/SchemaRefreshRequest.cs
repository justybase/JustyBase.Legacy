namespace JustData.Application.Schema;

/// <summary>Provider-neutral schema refresh options. Netezza maps these onto its download modes.</summary>
public enum SchemaRefreshMode
{
    Full = 0,
    Partial = 1,
    PartialOnlyTables = 2
}

/// <summary>Optional parameters for <see cref="ISchemaRepository.RefreshAsync"/>.</summary>
public sealed record SchemaRefreshRequest(
    SchemaRefreshMode Mode = SchemaRefreshMode.Partial,
    IReadOnlyList<string>? DatabasesToRefresh = null,
    bool LoadSources = false);
