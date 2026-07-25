using AppBase.Common;

namespace JustyBaseLegacy.UI.Schema;

/// <summary>Small UI callback adapter for Netezza services that need a table-list refresh.</summary>
public sealed class NetezzaSchemaRefreshController : INetezzaSchemaRefreshHost
{
    private readonly Func<string, bool, Task> _refresh;

    public NetezzaSchemaRefreshController(Func<string, bool, Task> refresh)
    {
        _refresh = refresh ?? throw new ArgumentNullException(nameof(refresh));
    }

    public Task RefreshTableListInternalAsync(string connectionName, bool disableInUi = true) =>
        _refresh(connectionName, disableInUi);
}
