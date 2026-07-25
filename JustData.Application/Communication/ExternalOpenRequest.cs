namespace JustData.Application.Communication;

/// <summary>A validated file-open request originating from another process.</summary>
public sealed record ExternalOpenRequest(string Path)
{
    public static bool TryCreate(string? rawPath, out ExternalOpenRequest? request)
    {
        request = null;
        if (string.IsNullOrWhiteSpace(rawPath) || rawPath.IndexOf('\0') >= 0)
        {
            return false;
        }

        string path;
        try
        {
            path = System.IO.Path.GetFullPath(rawPath.Trim());
        }
        catch (Exception) when (rawPath is not null)
        {
            return false;
        }

        if (!path.EndsWith(".sql", StringComparison.OrdinalIgnoreCase)
            && !path.EndsWith(".manysql", StringComparison.OrdinalIgnoreCase)
            && !path.EndsWith(".manysql.enc", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        request = new ExternalOpenRequest(path);
        return true;
    }
}

public interface IExternalOpenRequestRouter
{
    Task RouteAsync(ExternalOpenRequest request, CancellationToken cancellationToken = default);
    void SetDispatcher(IUiDispatcher dispatcher);
    void SetWorkspaceHandler(Action<ExternalOpenRequest> handler);
}
