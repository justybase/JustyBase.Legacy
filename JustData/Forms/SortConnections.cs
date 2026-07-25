using DatabaseDataGridView.WinForms;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace JustyBaseLegacy.UI.DbForms
{
    public partial class SortConnections : Form
    {
        private readonly IUiHelperService _uiHelperService;
        public SortConnections(IUiHelperService uiHelperService, List<string> names, string defName)
        {
            InitializeComponent();
            _uiHelperService = uiHelperService ?? throw new ArgumentNullException(nameof(uiHelperService));
            _uiHelperService.DoubleBufDateGridView(dataGridView1);

            int i = 0;
            foreach (var name in names)
            {
                if (name == defName)
                {
                    dataGridView1.Rows.Add(new object[] { i++, name, true });
                }
                else
                {
                    dataGridView1.Rows.Add(new object[] { i++, name, false });
                }
            }
        }

        private void dataGridView1_DefaultValuesNeeded(object sender, DataGridViewRowEventArgs e)
        {
            if (e.Row.Index >= 0)
            {
                e.Row.Cells[0].Value = e.Row.Index.ToString();
            }
        }

        private void btUp_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedCells.Count == 0)
            {
                MessageBox.Show("Please select one cell.", "Sort connections", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            RowUpInDgv(dataGridView1);
        }

        public static void RowUpInDgv(DataGridView dgv)
        {
            int selectedRow = dgv.SelectedCells[0].RowIndex;
            if (selectedRow == 0)
            {
                return;
            }

            object[] tmpCels = new object[dgv.ColumnCount];

            for (int i = 0; i < dgv.ColumnCount; i++)
            {
                tmpCels[i] = dgv.Rows[selectedRow - 1].Cells[i].Value;
            }

            dgv.Rows.RemoveAt(selectedRow - 1);
            dgv.Rows.Insert(selectedRow, tmpCels);
        }

        private void btDown_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedCells.Count == 0)
            {
                MessageBox.Show("Please select one cell.", "Sort connections", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            RowDownInDgv(dataGridView1);
        }

        public static void RowDownInDgv(DataGridView dgv)
        {
            int selectedRow = dgv.SelectedCells[0].RowIndex;
            if (selectedRow == dgv.Rows.Count - 1)
            {
                return;
            }

            object[] tmpCels = new object[dgv.ColumnCount];

            for (int i = 0; i < dgv.ColumnCount; i++)
            {
                tmpCels[i] = dgv.Rows[selectedRow + 1].Cells[i].Value;
            }

            dgv.Rows.RemoveAt(selectedRow + 1);
            dgv.Rows.Insert(selectedRow, tmpCels);
        }

        private void dataGridView1_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == 2)
            {
                dataGridView1.CellBeginEdit -= dataGridView1_CellBeginEdit;
                for (int i = 0; i < dataGridView1.Rows.Count; i++) // only one conenction can be selected
                {
                    if (i != e.RowIndex)
                    {
                        dataGridView1[2, i].Value = false;
                    }
                }
                dataGridView1.CellBeginEdit += dataGridView1_CellBeginEdit;
            }
        }

        public List<int> NewOrder { get; set; }
        public int DefNum { get; set; }
        private void btOK_Click(object sender, EventArgs e)
        {
            NewOrder = new List<int>();
            for (int i = 0; i < dataGridView1.RowCount; i++)
            {
                NewOrder.Add((int)dataGridView1.Rows[i].Cells[0].Value);
                if ((bool)dataGridView1.Rows[i].Cells[2].Value)
                {
                    DefNum = i;
                }
            }

            DialogResult = DialogResult.OK;
        }

        private void btCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }
    }
}
