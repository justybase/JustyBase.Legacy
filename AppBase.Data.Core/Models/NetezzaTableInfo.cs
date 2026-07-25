using AppBase.Common.Enums;


namespace AppBase.Data.Core.Models;
public record class NetezzaTableInfo
{
    public int DATABASE_ID { get; init; }
    public required string TABLE_NAME { get; init; }
    public required string TABLE_DESC { get; init; }
    public required string TABLE_OWNER { get; init; }
    public required string TABLE_SCHEMA { get; init; }
    public required string TABLE_OBJECT_OWNER { get; init; }
    public TypeInDatabase TABLE_KIND { get; init; }
    public int FIRST_COLUMN_ID { get; set; }
    public int COLUMN_COUNT { get; set; }

}
