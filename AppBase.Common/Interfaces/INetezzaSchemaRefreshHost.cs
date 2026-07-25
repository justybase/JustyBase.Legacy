namespace AppBase.Common;

/// <summary>View-owned schema refresh operation required by the Netezza service.</summary>
public interface INetezzaSchemaRefreshHost
{
    Task RefreshTableListInternalAsync(string connectionName, bool disableInUi = true);
}
