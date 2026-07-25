namespace JustData.Application.History;

public sealed record HistoryEntry(DateTime Date, string Sql, string Database, string ConnectionName);
