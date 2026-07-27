namespace AppBase.Common.Interfaces;

/// <summary>
/// Session/global variable state used by the SQL shell lifecycle.
/// Read operations return snapshots; mutation is performed through explicit
/// commands so a window cannot leak mutable state to another consumer.
/// </summary>
public interface ISessionVariableRuntimeContext
{
    IReadOnlyDictionary<string, string> GlobalVariables { get; }
    string ActualTabTitleText { get; set; }
    IReadOnlyDictionary<string, string> GetSessionVariables(string documentKey);
    bool HasSessionVariables(string documentKey);
    int GetSessionVariableCount(string documentKey);
    void EnsureSessionVariables(string documentKey);
    void CopySessionVariables(string sourceDocumentKey, string destinationDocumentKey);
    void SetSessionVariable(string documentKey, string name, string? value);
    void SetGlobalVariable(string name, string? value);
    void SetSessionVariables(string documentKey, IReadOnlyDictionary<string, string> values);
    string ReplaceGlobalVariables(string query);
}
