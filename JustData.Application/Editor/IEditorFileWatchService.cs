namespace JustData.Application.Editor;

public interface IEditorFileWatchService : IDisposable
{
    IDisposable Watch(string path, Action<EditorFileChange> onChanged);
}
