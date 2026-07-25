// BaseWindow — legacy utility methods (kept after removing old context menu handlers).
// Old context menu event handlers moved to MvvmSchemaContextMenu in BaseWindow.MvvmSchemaActions.cs.
using AppBase.Common;
using AppBase.Common.Interfaces;
using AppBase.Data;
using AppBase.Data.Core.Core;
using AppBase.Data.Core.Enums;
using AppBase.Data.Core.Interfaces;
using JustyBase.NetezzaDriver;
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
            string ownerTabeli = _schemaTables.TablesByConnection[connectionName][objectID].TABLE_OWNER;
            string databaseName = _completionContext.DatabaseDictionary[connectionName][_schemaTables.TablesByConnection[connectionName][objectID].DATABASE_ID].DatabaseName;
            var tableData = _schemaTables.TablesByConnection[connectionName][objectID];

            return $"DROP {objectTypeName} {databaseName}.{ownerTabeli}.{tableData.TABLE_NAME};";
        }


        public bool KeepConnectionOpen => CurrentUpper.KeepConnectionOpen;

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
