namespace AppBase.Common;

public interface ISqlTextScanner
{
    bool IsInsideQuotedLiteral(string sql, int position);
    bool IsInsideComment(string sql, int position);
}
