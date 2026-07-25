namespace JustData.Application.Files;

public interface IFileWatchService : IDisposable
{
    IDisposable Watch(
        IReadOnlyList<string> roots,
        Action<FileChange> onChanged);
}
