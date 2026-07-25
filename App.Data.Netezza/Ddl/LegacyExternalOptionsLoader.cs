using System.Data.Common;
using AppBase.Data.Core.Core;
using JustyBase.NetezzaDdl;
using JustyBase.NetezzaDdl.Models;
using JustyBase.NetezzaCatalogSql;

namespace AppBase.Data.Ddl;

public static class LegacyExternalOptionsLoader
{
    public static Task<NetezzaExternalTableOptions> LoadAsync(
        IGeneralDb generalDb,
        string databaseName,
        int objectId,
        bool forceDataObject)
        => Task.Run(() => Load(generalDb, databaseName, objectId, forceDataObject));

    private static NetezzaExternalTableOptions Load(
        IGeneralDb generalDb,
        string databaseName,
        int objectId,
        bool forceDataObject)
    {
        using DbConnection connection = generalDb.GetConnection(databaseName);
        connection.Open();

        NetezzaExternalTableCachedInfo cached;
        using (DbCommand cmd = connection.CreateCommand())
        {
            cmd.CommandText = NetezzaSystemSql.GetExternalOptions(objectId);
            using DbDataReader rd = cmd.ExecuteReader();
            if (!rd.Read())
            {
                return new NetezzaExternalTableOptions
                {
                    DataObject = forceDataObject ? "no path available" : null
                };
            }

            cached = NetezzaExternalOptionsMapper.FromReader(rd);
        }

        using DbCommand extObjCmd = connection.CreateCommand();
        extObjCmd.CommandText = NetezzaSystemSql.GetExternalObjectName(objectId);
        object? fileTemp = extObjCmd.ExecuteScalar();
        string? dataObject;
        if (fileTemp is DBNull)
            dataObject = forceDataObject ? "no path available" : null;
        else
            dataObject = fileTemp as string;

        return NetezzaExternalOptionsMapper.ToOptions(cached with
        {
            DataObject = dataObject,
            // Catalog returns real CR/LF; DDL must emit escaped \n/\r (until NetezzaDdl package picks up NormalizeRecordDelim).
            RecordDelim = NormalizeRecordDelim(cached.RecordDelim)
        });
    }

    private static string? NormalizeRecordDelim(string? value)
        => value?.Replace("\r", "\\r").Replace("\n", "\\n");
}
