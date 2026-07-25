// BaseWindow — legacy utility methods (kept after removing old context menu handlers).
// Old context menu event handlers moved to MvvmSchemaContextMenu in BaseWindow.MvvmSchemaActions.cs.
using AppBase.Common;
using AppBase.Common.Interfaces;
using AppBase.Data;
using AppBase.Data.Core.Core;
using AppBase.Data.Core.Enums;
using AppBase.Data.Core.Interfaces;
using JustyBase.NetezzaDriver;
using JustyBase.NetezzaCatalogSql;
using System.Data.Common;
using System.Text;
using System.Windows.Forms;


namespace JustyBaseLegacy.UI
{
    public partial class BaseWindow
    {
        public void SelectDatabaseTab()
        {
            if (_leftTabs.SelectedIndex != 0)
            {
                _leftTabs.SelectedIndex = 0;
            }
        }

        public string GetDropTableSequenceCodeById(int objectID, string objectTypeName = "TABLE")
        {
            string connectionName = _mvvmDatabaseExplorerControl?.DatabaseTreeView.SelectedNode?.Parent?.Parent?.Parent?.Text ?? string.Empty;
            string ownerTabeli = NetezzaHelpers.baseTableDictionary[connectionName][objectID].TABLE_OWNER;
            string databaseName = _completionContext.DatabaseDictionary[connectionName][NetezzaHelpers.baseTableDictionary[connectionName][objectID].DATABASE_ID].DatabaseName;
            var tableData = NetezzaHelpers.baseTableDictionary[connectionName][objectID];

            return $"DROP {objectTypeName} {databaseName}.{ownerTabeli}.{tableData.TABLE_NAME};";
        }


        public bool KeepConnectionOpen => CurrentUpper.KeepConnectionOpen;

        public string GetSeqCodeById(int objectID, string connectionName)
        {
            string ownerTabeli = NetezzaHelpers.baseTableDictionary[connectionName][objectID].TABLE_OWNER;
            string databaseName = _completionContext.DatabaseDictionary[connectionName][NetezzaHelpers.baseTableDictionary[connectionName][objectID].DATABASE_ID].DatabaseName;
            var tableData = NetezzaHelpers.baseTableDictionary[connectionName][objectID];

            using DbConnection conn = IGeneralDbService.GeneralDic[connectionName].GetConnection(databaseName);
            conn.Open();

            using DbCommand cmd = conn.CreateCommand();
            cmd.CommandText = NetezzaSystemSql.GetSequenceMetadata(tableData.TABLE_NAME);

            using var rdr = cmd.ExecuteReader();
            rdr.Read();

            string typeNme = rdr.GetString(0);
            string LAST_VALUE = rdr.GetString(1);
            string INCREMENT_BY = rdr.GetString(2);
            string MINVALUE = rdr.GetString(3);
            string MAXVALUE = rdr.GetString(4);
            int IS_CYCLED = rdr.GetInt32(5);

            string clipText = @$"
CREATE SEQUENCE {databaseName}.{ownerTabeli}.{tableData.TABLE_NAME}
AS {typeNme}
START WITH {LAST_VALUE}
INCREMENT BY {INCREMENT_BY}
{(MINVALUE == null ? "NO MINVALUE" : "MINVALUE " + MINVALUE)}
{(MAXVALUE == null || typeNme == "INTEGER" && MAXVALUE == "2147483647" || typeNme == "BIGINT" && MAXVALUE == "9223372036854775807" ? "NO MAXVALUE" : "MAXVALUE " + MAXVALUE)}
{(IS_CYCLED == 1 ? "CYCLE" : "NO CYCLE")};";
            return clipText;

        }

        private void DgvVariables_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex >= 1 && e.RowIndex >= 0)
            {
                CurrentTB.InsertText(DgvVariables[e.ColumnIndex, e.RowIndex].Value.ToString(), true);
                CurrentTB.Focus();
            }
        }
        //https://stackoverflow.com/questions/20915260/c-sharp-winforms-dragdrop-within-the-same-treeviewcontrol



        private int _lastXLocation = 0;
        private int _lastYLocation = 0;
        private void BaseWindow_Move(object sender, EventArgs e)
        {
            int xLoc = this.Location.X;
            int yLoc = this.Location.Y;


            if (this.Width + xLoc > Screen.FromControl(this).WorkingArea.Width && xLoc < _lastXLocation)
            {
                _tabControlMain.Invalidate();
            }
            if (this.Height + yLoc > Screen.FromControl(this).WorkingArea.Height && yLoc < _lastYLocation)
            {
                _leftTabs.Invalidate();
            }
            _lastXLocation = xLoc;
            _lastYLocation = yLoc;
        }

        private void VerticalHorizontalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (CurrentSplitContainer is not null)
            {
                if (CurrentSplitContainer.Orientation == Orientation.Horizontal)
                {
                    CurrentSplitContainer.Orientation = Orientation.Vertical;
                }
                else
                {
                    CurrentSplitContainer.Orientation = Orientation.Horizontal;
                }
            }
        }




        private void AddNewConnectionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowLoginForm();
        }
    }
}
