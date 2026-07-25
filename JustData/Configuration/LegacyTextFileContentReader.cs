using AppBase.Common.Interfaces;

namespace JustyBaseLegacy.UI.Configuration;

/// <summary>Small file adapter kept separate from the shell runtime state.</summary>
public sealed class LegacyTextFileContentReader : ITextFileContentReader
{
    public string GetContentOfTextFile(string realFilePath) => File.ReadAllText(realFilePath);
}
