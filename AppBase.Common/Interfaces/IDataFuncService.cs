using System.Data;

namespace AppBase.Common;

public interface IDataFuncService
{
    DataTable GetDataTable(IDataReader rdr, int l = 1);
}
