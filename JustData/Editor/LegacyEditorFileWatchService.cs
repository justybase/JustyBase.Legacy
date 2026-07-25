using JustData.Application.Editor;

namespace JustyBaseLegacy.UI.Editor;

public sealed class WinFormsEditorFileWatchService : IEditorFileWatchService
{
    private readonly object _gate = new();
    private readonly List<FileSystemWatcher> _watchers = [];
    private bool _disposed;

    public IDisposable Watch(string path, Action<EditorFileChange> onChanged)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(onChanged);
        ObjectDisposedException.ThrowIf(_disposed, this);

        string fullPath = Path.GetFullPath(path);
        string? directory = Path.GetDirectoryName(fullPath);
        string fileName = Path.GetFileName(fullPath);
        if (string.IsNullOrWhiteSpace(directory))
            throw new ArgumentException("The document path must have a directory.", nameof(path));

        var watcher = new FileSystemWatcher(directory, fileName)
        {
            IncludeSubdirectories = false,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
            EnableRaisingEvents = true
        };

        DateTime lastChanged = DateTime.MinValue;
        void Raise(EditorFileChange change)
        {
            if (change.Kind == EditorFileChangeKind.Changed)
            {
                DateTime now = DateTime.UtcNow;
                if (now - lastChanged < TimeSpan.FromMilliseconds(150))
                    return;
                lastChanged = now;
            }
            onChanged(change);
        }

        watcher.Changed += (_, args) => Raise(new(EditorFileChangeKind.Changed, args.FullPath));
        watcher.Deleted += (_, args) => Raise(new(EditorFileChangeKind.Deleted, args.FullPath));
        watcher.Renamed += (_, args) => Raise(new(EditorFileChangeKind.Renamed, args.FullPath, args.OldFullPath));

        lock (_gate)
        {
            if (_disposed)
            {
                watcher.Dispose();
                throw new ObjectDisposedException(nameof(WinFormsEditorFileWatchService));
            }

            _watchers.Add(watcher);
        }

        return new Registration(this, watcher);
    }

    public void Dispose()
    {
        FileSystemWatcher[] watchers;
        lock (_gate)
        {
            if (_disposed)
                return;
            _disposed = true;
            watchers = _watchers.ToArray();
            _watchers.Clear();
        }

        foreach (var watcher in watchers)
            watcher.Dispose();
    }

    private void Remove(FileSystemWatcher watcher)
    {
        lock (_gate)
            _watchers.Remove(watcher);
        watcher.Dispose();
    }

    private sealed class Registration(WinFormsEditorFileWatchService owner, FileSystemWatcher watcher) : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
                owner.Remove(watcher);
        }
    }
}
