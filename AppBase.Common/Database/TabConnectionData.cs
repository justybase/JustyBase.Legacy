using System.Data.Common;

namespace AppBase.Common;

public sealed class TabConnectionData
{
    public DbConnection? Connection { get; set; }
    public bool CloseConnectionByDefault { get; set; }
    public List<DbCommand> Commands { get; set; } = [];
    public string ConnectionName { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
}
