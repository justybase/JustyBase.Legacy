using System.Text.Json.Serialization;

namespace AppBase.Common.JsonContext;

[JsonSerializable(typeof(string[]))]
public partial class MyJsonContextStringArray : JsonSerializerContext
{
}

[JsonSerializable(typeof(List<string>))]
public partial class MyJsonContextStringList : JsonSerializerContext
{
}
