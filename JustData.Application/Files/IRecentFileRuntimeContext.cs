namespace JustData.Application.Files;

/// <summary>
/// Mutable recent-file state used by the WinForms shell while it updates menus.
/// </summary>
public interface IRecentFileRuntimeContext
{
    List<string> RecentFiles { get; }
    List<string> RecentManySqlFiles { get; }
    void SaveRecentFiles();
}
