using AppBase.Common.Enums;
using AppBase.Common.Interfaces;
using AppBase.Data.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace JustyBaseLegacy.UI
{
    public partial class TableListForm : Form
    {
        public int ColumnNum { get; set; }
        public TableListForm(INetezzaCompletionContext completionContext, INetezzaSchemaTableCatalog schemaTables, int columnNum)
        {
            ArgumentNullException.ThrowIfNull(schemaTables);
            InitializeComponent();
            List<string> ls = new List<string>();
            string connName = completionContext.SelectedConnectionName;
            if (!completionContext.DatabaseDictionary.TryGetValue(connName, out var dbDict)
                || !schemaTables.TablesByConnection.TryGetValue(connName, out var baseTables))
            {
                return;
            }
            foreach (var item in baseTables.Values)
            {
                if (item.TABLE_KIND != TypeInDatabase.table || item.COLUMN_COUNT != columnNum)
                {
                    continue;
                }
                string dbName = dbDict.TryGetValue(item.DATABASE_ID, out var dbInfo)
                    ? dbInfo.DatabaseName
                    : string.Empty;
                ls.Add($"{dbName}..{item.TABLE_NAME}");
            }
            ls.Sort();

            comboBox1.Items.AddRange(ls.ToArray());
        }
        public string GetSelected()
        {
            if (comboBox1.SelectedItem != null)
            {
                return comboBox1.SelectedItem.ToString();
            }
            else
            {
                return null;
            }
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            this.Hide();
        }
    }
}
