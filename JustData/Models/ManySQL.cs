using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace JustyBaseLegacy.UI.Models;

public sealed class ManySQL
{
    public List<string> SqlPaths { get; set; }
    public List<List<string>> SqlContentList { get; set; }
    public List<string> TabsOrder { get; set; }
    public int SelectedTabNum { get; set; }

    public ManySQL()
    {
        SqlPaths = new List<string>();
        SqlContentList = new List<List<string>>();
        TabsOrder = new List<string>();
    }
}

[JsonSerializable(typeof(ManySQL))]
public partial class MyJsonContextManySQL : JsonSerializerContext
{
}
