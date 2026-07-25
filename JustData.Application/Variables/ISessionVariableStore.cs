namespace JustData.Application.Variables;

public interface ISessionVariableStore
{
    event EventHandler? Changed;

    IReadOnlyDictionary<string, string> GetSessionVariables(string documentKey);

    IReadOnlyDictionary<string, string> GlobalVariables { get; }

    void ClearGlobalVariables();
}
