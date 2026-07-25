using JustData.Application.Files;

namespace JustyBaseLegacy.UI.Files;

/// <summary>WinForms file-system watcher adapter for the files panel.</summary>
public sealed class WinFormsFileWatchService : IFileWatchService
{
    private readonly List<FileSystemWatcher> _watchers = [];
    private bool _disposed;

    public IDisposable Watch(IReadOnlyList<string> roots, Action<FileChange> onChanged)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var watchers = new List<FileSystemWatcher>();
        foreach (var root in roots.Where(Directory.Exists))
        {
            var watcher = new FileSystemWatcher(root)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName,
                EnableRaisingEvents = true
            };
            watcher.Created += (_, args) => onChanged(new(FileChangeKind.Created, args.FullPath));
            watcher.Deleted += (_, args) => onChanged(new(FileChangeKind.Deleted, args.FullPath));
            watcher.Renamed += (_, args) => onChanged(new(FileChangeKind.Renamed, args.FullPath, args.OldFullPath));
            _watchers.Add(watcher);
            watchers.Add(watcher);
        }
        return new WatchRegistration(watchers, _watchers);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        foreach (var watcher in _watchers.ToArray()) watcher.Dispose();
        _watchers.Clear();
    }

    private sealed class WatchRegistration(List<FileSystemWatcher> owned, List<FileSystemWatcher> all) : IDisposable
    {
        private bool _disposed;
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            foreach (var watcher in owned)
            {
                watcher.Dispose();
                all.Remove(watcher);
            }
        }
    }
}
