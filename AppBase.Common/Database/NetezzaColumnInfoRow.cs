namespace AppBase.Common;

public record class NetezzaColumnInfoRow
{
    public UInt16 COLUMN_NUMBER { get; init; }
    public int TABLE_ID { get; init; }
    public int DATABASE_ID { get; init; }
    public string COLUMN_NAME { get; init; } = string.Empty;
    public string? COLUMN_DESCRIPTION { get; init; }
    public string DATA_TYPE { get; init; } = string.Empty;
    public bool IS_NULLABLE { get; init; }
    public sbyte? DISTSEQNO { get; init; }
    public sbyte? ORGSEQNO { get; init; }
    public string? COLDEFAULT { get; init; }
}

