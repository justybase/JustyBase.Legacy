using System.Diagnostics;

namespace JustDataAdditionalForms
{
    public partial class AddColumnForm : Form
    {
        public AddColumnForm(Action<Form> DoColorize)
        {
            InitializeComponent();
            DoColorize(this);
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var p = new Process();
            p.StartInfo = new ProcessStartInfo("https://www.ibm.com/docs/en/netezza?topic=reference-alter-table")
            {
                UseShellExecute = true
            };
            p.Start();
        }

        private void cbDataType_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selTxt = cbDataType.SelectedItem.ToString();

            if (selTxt == "VARCHAR" || selTxt == "NVARCHAR")
            {
                labelLen.Enabled = true;
                numLen.Enabled = true;
                labelPrec.Enabled = false;
                numPrec.Enabled = false;
                labelScale.Enabled = false;
                numScale.Enabled = false;
            }
            else if (selTxt == "NUMERIC")
            {
                labelLen.Enabled = false;
                numLen.Enabled = false;
                labelPrec.Enabled = true;
                numPrec.Enabled = true;
                labelScale.Enabled = true;
                numScale.Enabled = true;
            }
            else
            {
                labelLen.Enabled = false;
                numLen.Enabled = false;
                labelPrec.Enabled = false;
                numPrec.Enabled = false;
                labelScale.Enabled = false;
                numScale.Enabled = false;
            }
        }
        public string ChosedColumn { get; set; }
        public string ChosedColumnName { get; set; }
        private void btSave_Click(object sender, EventArgs e)
        {
            string selTxt = cbDataType.SelectedItem.ToString();
            ChosedColumnName = selTxt;
            ChosedColumn = tbName.Text + " " + selTxt;
            if (selTxt == "VARCHAR" || selTxt == "NVARCHAR")
            {
                ChosedColumn += $"({numLen.Value})";
            }
            else if (selTxt == "NUMERIC")
            {
                ChosedColumn += $"({numPrec.Value},{numScale.Value})";
            }

            if (!cbAllowNulls.Checked)
            {
                if (string.IsNullOrWhiteSpace(tbDefault.Text))
                {
                    lbWarningDefault.Visible = true;
                    return;
                }
                ChosedColumn += $" NOT NULL DEFAULT {tbDefault.Text}";
            }

            DialogResult = DialogResult.OK;
        }

        private void btCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        private void cbAllowNulls_CheckedChanged(object sender, EventArgs e)
        {
            if (!cbAllowNulls.Checked)
            {
                tbDefault.Enabled = true;
                lbDefault.Enabled = true;
            }
            else
            {
                tbDefault.Enabled = false;
                lbDefault.Enabled = false;
            }
            lbWarningDefault.Visible = false;
        }

        private void tbDefault_TextChanged(object sender, EventArgs e)
        {
            lbWarningDefault.Visible = false;
        }
    }
}
