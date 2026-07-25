using System.Text.Json.Serialization;

namespace DatabaseDataGridView.WinForms;

/// <summary>Data sent through the named pipe for first-render timing measurements.</summary>
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(FirstRenderProbeData))]
internal sealed partial class FirstRenderProbeJsonContext : JsonSerializerContext
{
}

internal sealed record FirstRenderProbeData(
    [property: JsonPropertyName("runId")] string RunId,
    [property: JsonPropertyName("columnCount")] int ColumnCount,
    [property: JsonPropertyName("elapsedMilliseconds")] long ElapsedMilliseconds);
