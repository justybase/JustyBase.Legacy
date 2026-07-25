using System.Text.Json.Serialization;

namespace JustyBaseLegacy.UI.Editor;

/// <summary>Wire format for .manysql / .manysql.enc JSON bundles.</summary>
[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Default,
    WriteIndented = false,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(ManySqlWireModel))]
internal sealed partial class AppJsonSerializerContext : JsonSerializerContext
{
}

internal sealed class ManySqlWireModel
{
    public List<string>? SqlPaths { get; set; }
    public List<List<string>>? SqlContentList { get; set; }
    public List<string>? TabsOrder { get; set; }
    public int SelectedTabNum { get; set; }
}
