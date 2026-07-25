namespace AppBase.Common.Interfaces
{
    public interface IDatabaseDownloader
    {
        Task<bool> DownloadOneDb(string connectionName, string dbName);
    }
}
