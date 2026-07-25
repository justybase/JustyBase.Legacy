namespace JustyBaseLegacy.UI.Helpers;

public interface IResultsTabNaming
{
    string NextResultTitle(IReadOnlyList<string> existingTitles);
    string NextLogTitle(IReadOnlyList<string> existingTitles);
}
