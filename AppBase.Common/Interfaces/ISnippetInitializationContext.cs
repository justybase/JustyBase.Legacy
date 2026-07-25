namespace AppBase.Common.Interfaces;

/// <summary>
/// Initializes the persisted snippet and special-name files used by the editor.
/// </summary>
public interface ISnippetInitializationContext
{
    void Initialize(string snippetsJson, string specialNamesJson);
}
