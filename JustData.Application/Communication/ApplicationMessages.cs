namespace JustData.Application.Communication;

/// <summary>Published after the transactional settings document has been saved.</summary>
public sealed record SettingsSavedMessage;

/// <summary>Published when the active connection changes.</summary>
public sealed record ActiveConnectionChangedMessage(string ConnectionName);

/// <summary>Published after schema refresh completes for a connection.</summary>
public sealed record SchemaRefreshedMessage(string ConnectionName);
