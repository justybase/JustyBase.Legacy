using AppBase.Common;
using AppBase.Data.Completion;

namespace AppBase.Services;

public sealed class FormatterService : IFormatterService
{
    public string Format(string sql) => LegacySqlAuthoringServices.FormatSql(sql);
}
