using System.Diagnostics;

namespace JustyBaseLegacy.UI.DbForms
{
    public partial class AddNewTableControl : UserControl
    {
        public AddNewTableControl(string dbName, Action<Control> DoColorize, Action<DataGridView> DoubleBuff, Action<string, string, string> someAction)
        {
            _someAction = someAction;

            InitializeComponent();
            DoColorize(this);

            if (!string.IsNullOrWhiteSpace(dbName))
            {
                tbName.Text = $"{dbName}.ADMIN.NEW_TABLE_NAME";
            }

            DoubleBuff(dgvAddNewTable);
        }
        Action<string, string, string> _someAction;


        private void lbDocs_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string txt = @"https://www.ibm.com/docs/en/netezza?topic=reference-create-table";
            var p = new Process();
            p.StartInfo = new ProcessStartInfo(txt)
            {
                UseShellExecute = true
            };
            p.Start();
        }

        private void btCancel_Click(object sender, EventArgs e)
        {
            MessageBox.Show("This feature is not implemented yet.", "Not implemented", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }



        private void btCreate_Click(object sender, EventArgs e)
        {
            string tableName = tbName.Text;
            List<string> cols = new List<string>();
            SortedDictionary<string, string> distDic = new SortedDictionary<string, string>();
            SortedDictionary<string, string> keysDic = new SortedDictionary<string, string>();

            string colDesc = "";

            foreach (DataGridViewRow item in dgvAddNewTable.Rows)
            {
                if (item.Cells[0].Value is null)
                    break;

                string name = item.Cells[0].Value.ToString();
                object dataType = item.Cells[1].Value;

                if (dataType is null)
                {
                    item.DefaultCellStyle.BackColor = Color.Yellow;
                    return;
                }


                string pkNum = item.Cells[3].Value?.ToString();
                string distNume = item.Cells[4].Value?.ToString();
                string desc = item.Cells[5].Value?.ToString();


                if (!string.IsNullOrWhiteSpace(distNume))
                    distDic[distNume] = name;

                if (!string.IsNullOrWhiteSpace(pkNum))
                    keysDic[pkNum] = name;

                string notNullTxt = " NOT NULL";
                if (item.Cells[2].Value is not null)
                {
                    if ((bool)item.Cells[2].Value)
                    {
                        notNullTxt = "";
                    }
                }

                if (!string.IsNullOrWhiteSpace(desc))
                    colDesc += $"COMMENT ON COLUMN {tableName}.{name} IS '{desc}';\r\n";



                cols.Add(name + " " + dataType.ToString() + notNullTxt);
            }


            string distTxt = "RANDOM";
            if (distDic.Count > 0)
            {
                distTxt = "(" + String.Join(",", distDic.Values) + ")";
            }

            string pkstring = "";
            if (keysDic.Count > 0)
            {
                pkstring = $"ALTER TABLE {tableName} ADD CONSTRAINT PK_{tableName} PRIMARY KEY ({String.Join(",", keysDic.Values)});";
            }

            string tableDescSql = "";
            if (!string.IsNullOrWhiteSpace(tbTableDesc.Text))
            {
                tableDescSql = $"COMMENT ON TABLE {tableName} IS '{tbTableDesc.Text}';\r\n";
            }


            string cteateTxt = $@"
CREATE TABLE {tableName}
(
    {String.Join(",\r\n    ", cols)}
)
DISTRIBUTE ON {distTxt}
--ORGANIZE ON (COL1,COL2,...)
;
{tableDescSql}
{pkstring}
{colDesc}
";

            _someAction(null, $"create off ... {tbName.Text}", cteateTxt);
        }

        private void dgvAddNewTable_DefaultValuesNeeded(object sender, DataGridViewRowEventArgs e)
        {
            e.Row.Cells[1].Value = "INTEGER";
            e.Row.Cells[2].Value = false;
        }
    }
}
