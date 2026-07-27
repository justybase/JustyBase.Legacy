using CommunityToolkit.Mvvm.Messaging;
using JustData.Application.Communication;
using JustData.Application.Schema;

namespace JustyBaseLegacy.UI.Schema;

public interface ISchemaRefreshCoordinator
{
    Task<IReadOnlyList<SchemaNode>> RefreshAsync(
        string connectionName,
        SchemaRefreshRequest? request = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SchemaNode>> AttachDatabaseAsync(
        string connectionName,
        string databaseName,
        CancellationToken cancellationToken = default);

    /// <summary>Publishes <see cref="SchemaRefreshedMessage"/> after an external catalog mutation.</summary>
    Task<IReadOnlyList<SchemaNode>> NotifyRefreshedAsync(
        string connectionName,
        CancellationToken cancellationToken = default);
}

/// <summary>Provider-neutral schema refresh boundary for WinForms presenters.</summary>
public sealed class SchemaRefreshCoordinator : ISchemaRefreshCoordinator
{
    private readonly ISchemaRepository _repository;
    private readonly IMessenger _messenger;

    public SchemaRefreshCoordinator(ISchemaRepository repository, IMessenger messenger)
    {
        _repository = repository;
        _messenger = messenger;
    }

    public async Task<IReadOnlyList<SchemaNode>> RefreshAsync(
        string connectionName,
        SchemaRefreshRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connectionName))
            throw new ArgumentException("A connection name is required.", nameof(connectionName));

        await _repository.RefreshAsync(connectionName, cancellationToken, request).ConfigureAwait(false);
        return await PublishAsync(connectionName, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<SchemaNode>> AttachDatabaseAsync(
        string connectionName,
        string databaseName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connectionName))
            throw new ArgumentException("A connection name is required.", nameof(connectionName));
        if (string.IsNullOrWhiteSpace(databaseName))
            throw new ArgumentException("A database name is required.", nameof(databaseName));

        bool attached = await _repository.AttachDatabaseAsync(connectionName, databaseName, cancellationToken).ConfigureAwait(false);
        if (!attached)
            throw new InvalidOperationException($"Failed to attach database '{databaseName}' on connection '{connectionName}'.");

        return await PublishAsync(connectionName, cancellationToken).ConfigureAwait(false);
    }

    public Task<IReadOnlyList<SchemaNode>> NotifyRefreshedAsync(
        string connectionName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connectionName))
            throw new ArgumentException("A connection name is required.", nameof(connectionName));

        return PublishAsync(connectionName, cancellationToken);
    }

    private async Task<IReadOnlyList<SchemaNode>> PublishAsync(string connectionName, CancellationToken cancellationToken)
    {
        IReadOnlyList<SchemaNode> roots = await _repository.GetRootsAsync(connectionName, cancellationToken).ConfigureAwait(false);
        _messenger.Send(new SchemaRefreshedMessage(connectionName));
        return roots;
    }
}
