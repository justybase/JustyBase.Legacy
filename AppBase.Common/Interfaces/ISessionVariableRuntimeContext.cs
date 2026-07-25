namespace AppBase.Common.Interfaces;

/// <summary>
/// Mutable session/global variable state used by the SQL shell lifecycle.
/// </summary>
public interface ISessionVariableRuntimeContext
{
    Dictionary<string, Dictionary<string, string>> SessionVariables { get; }
    Dictionary<string, string> GlobalVariables { get; }
    string ActualTabTitleText { get; set; }
    string ReplaceGlobalVariables(string query);
}
