using AppBase.Common.Enums;

namespace AppBase.Common.Models;

public sealed class DatabaseTag
{
    public TypeInDatabase KIND_ID { get; set; }
    public int OBJECT_ID { get; set; }
}
