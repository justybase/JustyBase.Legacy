using CommunityToolkit.Mvvm.Messaging;
using JustData.Application.Communication;
using JustData.Application.Schema;

namespace JustyBaseLegacy.UI.Schema;

public interface ISchemaRefreshCoordinator
{
    Task<IReadOnlyList<SchemaNode>> RefreshAsync(string connectionName, CancellationToken cancellationToken = default);
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

    public async Task<IReadOnlyList<SchemaNode>> RefreshAsync(string connectionName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connectionName))
            throw new ArgumentException("A connection name is required.", nameof(connectionName));

        await _repository.RefreshAsync(connectionName, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<SchemaNode> roots = await _repository.GetRootsAsync(connectionName, cancellationToken).ConfigureAwait(false);
        _messenger.Send(new SchemaRefreshedMessage(connectionName));
        return roots;
    }
}
