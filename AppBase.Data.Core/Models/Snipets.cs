using System.Text.Json.Serialization;

namespace AppBase.Common;

public sealed class Snipets
{
    public required string[] Keywords { get; set; }
    public required string[] Snippets { get; set; }
    public required string[] MonkeySnippets { get; set; }
}

[JsonSerializable(typeof(Snipets))]
public partial class MyJsonContextSnipets : JsonSerializerContext
{
}
