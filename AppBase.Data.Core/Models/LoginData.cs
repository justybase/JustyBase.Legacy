using System.Text.Json.Serialization;

namespace AppBase.Common;

public sealed class LoginData
{
    public string Name { get; set; } = string.Empty;
    public string Driver { get; set; } = string.Empty;
    public string Server { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Database { get; set; } = string.Empty;
    public int DefaultIndex { get; set; }
}

[JsonSerializable(typeof(List<LoginData>))]
public partial class MyJsonContextLoginData : JsonSerializerContext
{
}
