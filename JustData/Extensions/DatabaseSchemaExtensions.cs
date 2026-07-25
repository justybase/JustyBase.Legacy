using AppBase.Data.Core.Core;
using System;
using System.Windows.Forms;

namespace JustyBaseLegacy.UI.Extensions
{
    public static class DatabaseSchemaExtensions
    {
        /// <summary>
        /// Initializes the database schema tree view for general database types
        /// </summary>
        public static void InitSchema(this IGeneralDb database, TreeView treeView, ContextMenuStrip cmStripTabeli, ContextMenuStrip cmStripViewGeneral, ContextMenuStrip cmStripAliasesGeneral, ContextMenuStrip cmStripProcs, ContextMenuStrip cmStripSynonymsGeneral, string connName, ContextMenuStrip allTablesGeneral, ContextMenuStrip cmColumns, ContextMenuStrip cmConstraints, ContextMenuStrip cmIndexes, ContextMenuStrip cmPartitions, ContextMenuStrip cmTriggers, ContextMenuStrip cmsDB2Server, ContextMenuStrip cmsSynonyms, int index = -1)
        {
            switch (database)
            {
#if INCLUDE_MSSQL
                case MsSqlDb msSqlDb:
                    msSqlDb.InitMsSqlSchema(treeView, cmStripTabeli, cmStripViewGeneral, cmStripAliasesGeneral, cmStripProcs, cmStripSynonymsGeneral, connName, allTablesGeneral, cmColumns, cmConstraints, cmIndexes, cmPartitions, cmTriggers, cmsDB2Server, cmsSynonyms, index);
                    break;
#endif
#if INCLUDE_DB2
                case DB2 db2:
                    db2.InitDB2Schema(treeView, cmStripTabeli, cmStripViewGeneral, cmStripAliasesGeneral, cmStripProcs, cmStripSynonymsGeneral, connName, allTablesGeneral, cmColumns, cmConstraints, cmIndexes, cmPartitions, cmTriggers, cmsDB2Server, cmsSynonyms, index);
                    break;
#endif

#if INCLUDE_POSTGRES
                case Postgres postgres:
                    postgres.InitPostgresSchema(treeView, cmStripTabeli, cmStripViewGeneral, cmStripAliasesGeneral, cmStripProcs, cmStripSynonymsGeneral, connName, allTablesGeneral, cmColumns, cmConstraints, cmIndexes, cmPartitions, cmTriggers, cmsDB2Server, cmsSynonyms, index);
                    break;
#endif
#if INCLUDE_ORACLE
                case AppBase.Data.Oracle oracle:
                    oracle.InitOracleSchema(treeView, cmStripTabeli, cmStripViewGeneral, cmStripAliasesGeneral, cmStripProcs, cmStripSynonymsGeneral, connName, allTablesGeneral, cmColumns, cmConstraints, cmIndexes, cmPartitions, cmTriggers, cmsDB2Server, cmsSynonyms, index);
                    break;
#endif
                default:
                    throw new NotSupportedException($"Database type {database.GetType().Name} is not supported for schema initialization.");
            }
        }
    }
}
