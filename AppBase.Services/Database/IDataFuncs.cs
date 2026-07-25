using System.Data;

namespace AppBase.Services;

public interface IDataFuncs
{
    DataTable GetDataTable(IDataReader rdr, int l = 1, Action<string>? onErrorMessage = null);
}
