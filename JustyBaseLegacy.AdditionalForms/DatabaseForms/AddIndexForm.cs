using System.Diagnostics;

namespace JustyBaseLegacy.UI.DbForms
{
    public partial class AddIndexForm : Form
    {
        public AddIndexForm(string tabName, string schemaName, string[] cols, Action<Form> DoColorize, Action<string> runSqlNoResluts,
            Func<string, string> quoteNameIfNeeded,
            Action<DataGridView> rowUpInDgv
            )
        {

            InitializeComponent();
            _runSqlNoResluts = runSqlNoResluts;
            _quoteNameIfNeeded = quoteNameIfNeeded;
            _rowUpInDgv = rowUpInDgv;
            DoColorize(this);
            this.tbTabName.Text = tabName;
            this.tbSchema.Text = schemaName;
            this.tbIndexSchema.Text = schemaName;
            _cols = cols;
            this.colName.Items.AddRange(_cols);
            foreach (Control c in this.Controls)
            {
                if (c == tbSql)
                {
                    continue;
                }

                if (c is TextBox textBox)
                {
                    textBox.TextChanged += new EventHandler(c_ControlChanged);
                }

                if (c is CheckBox checkBox)
                {
                    checkBox.CheckStateChanged += new EventHandler(c_ControlChanged);
                }

                if (c is DataGridView gridView)
                {
                    //gridView.RowStateChanged += c_ControlChanged;
                    //gridView.RowsAdded += c_ControlChanged;
                    //gridView.RowsRemoved += c_ControlChanged;
                    //gridView.CellLeave += c_ControlChanged;
                    gridView.CellValueChanged += c_ControlChanged;
                }
                radioButton1.CheckedChanged += c_ControlChanged;
                radioButton2.CheckedChanged += c_ControlChanged;
                radioButton3.CheckedChanged += c_ControlChanged;
                cbStats.SelectedIndexChanged += c_ControlChanged;
            }
        }
        private readonly Action<string> _runSqlNoResluts;
        private readonly Func<string, string> _quoteNameIfNeeded;
        private readonly Action<DataGridView> _rowUpInDgv;

        void c_ControlChanged(object sender, EventArgs e)
        {
            string unique = " ";
            string partitioned = "";
            if (cbPartitioned.CheckState == CheckState.Checked)
            {
                partitioned = "\r\nPARTITIONED --in tablespace name";
            }
            else if (cbPartitioned.CheckState == CheckState.Unchecked)
            {
                partitioned = "\r\nNOT PARTITIONED";
            }

            string specification = "";
            if (cbSpecification.CheckState == CheckState.Checked)
            {
                specification = "\r\nSPECIFICATION ONLY";
            }

            string cluster = "";
            if (cbCluster.CheckState == CheckState.Checked)
            {
                cluster = "\r\nCLUSTER";
            }

            string reverseScan = "";
            if (radioButton2.Checked)
            {
                reverseScan = "\r\nALLOW REVERSE SCANS";
            }
            else if (radioButton3.Checked)
            {
                reverseScan = "\r\nDISALLOW REVERSE SCANS";
            }
            string collect = "";
            if (cbStats.Text != "Default")
            {
                collect = "\r\n" + cbStats.Text;
            }

            string compress = "";
            if (cbCompress.CheckState == CheckState.Checked)
            {
                compress = "\r\nCOMPRESS YES";
            }
            else if (cbCompress.CheckState == CheckState.Unchecked)
            {
                compress = "\r\nCOMPRESS NO";
            }

            string nullKeys = "";
            if (cbIncludeNulls.CheckState == CheckState.Checked)
            {
                nullKeys = "\r\nINCLUDE NULL KEYS";
            }
            else if (cbIncludeNulls.CheckState == CheckState.Unchecked)
            {
                nullKeys = "\r\nEXCLUDE NULL KEYS";
            }

            string[] cols = new string[dataGridView1.RowCount];
            int i = 0;
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                string val1 = "";
                if (row.Cells[0].Value is not null)
                {
                    val1 = _quoteNameIfNeeded(row.Cells[0].Value.ToString());
                    if (row.Cells[1].Value is not null)
                    {
                        string sortOrder = row.Cells[1].Value.ToString();
                        if (sortOrder != "Default")
                        {
                            val1 += $" {sortOrder}";
                        }
                    }

                }
                cols[i++] = val1;
            }

            if (cbUnique.CheckState == CheckState.Checked)
            {
                unique = " UNIQUE ";
            }

            tbSql.Text = @$"CREATE{unique}INDEX {_quoteNameIfNeeded(tbIndName.Text)}
ON {_quoteNameIfNeeded(tbSchema.Text)}.{_quoteNameIfNeeded(tbTabName.Text)}
(
    {String.Join(",\r\n    ", cols)}
){partitioned}{specification}{cluster}{reverseScan}{collect}{compress}{nullKeys}
;";
        }

        string[] _cols;
        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x02000000;    // Turn on WS_EX_COMPOSITED
                return cp;
            }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var p = new Process();
            p.StartInfo = new ProcessStartInfo("https://www.ibm.com/docs/en/db2/11.5?topic=statements-create-index")
            {
                UseShellExecute = true
            };
            p.Start();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        private void btAdd_Click(object sender, EventArgs e)
        {
            dataGridView1.Rows.Add();
        }

        private void btMinus_Click(object sender, EventArgs e)
        {
            Int32 selectedCellCount = dataGridView1.GetCellCount(DataGridViewElementStates.Selected);
            if (selectedCellCount > 0)
            {
                var cell = dataGridView1.SelectedCells[0];
                dataGridView1.Rows.RemoveAt(cell.RowIndex);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedCells.Count > 0)
            {
                _rowUpInDgv(dataGridView1);
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedCells.Count > 0)
            {
                _rowUpInDgv(dataGridView1);
            }
        }

        public string Sql { get; set; }
        private async void button1_Click(object sender, EventArgs e)
        {
            Sql = tbSql.Text;
            button1.Enabled = false;
            button2.Enabled = false;
            try
            {
                await Task.Run(() => _runSqlNoResluts(tbSql.Text));
            }
            catch (Exception ex)
            {
                tbSql.Text = ex.Message;
                return;
            }
            finally
            {
                button1.Enabled = true;
                button2.Enabled = true;
            }

            DialogResult = DialogResult.OK;
        }
    }
}
