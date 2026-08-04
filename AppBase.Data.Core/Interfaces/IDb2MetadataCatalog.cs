using AppBase.Data.Core.Models;

namespace AppBase.Data.Core.Interfaces;

/// <summary>Exposes the loaded DB2 catalog to provider-neutral consumers.</summary>
public interface IDb2MetadataCatalog
{
    IReadOnlyList<Db2CatalogObject> Db2CatalogObjects { get; }
}
