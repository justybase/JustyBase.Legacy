namespace AppBase.Common.Interfaces;

public interface ITabNameProvider
{
    string GetNextName(HashSet<string> existingTabNames);
}
