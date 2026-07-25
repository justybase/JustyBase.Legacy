using System.Diagnostics;

namespace JustyBaseLegacy.UI.DbForms
{
    public partial class CreateSequenceNz : Form
    {
        public CreateSequenceNz(Action<Form> DoColorize)
        {
            InitializeComponent();
            DoColorize(this);
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var p = new Process();
            p.StartInfo = new ProcessStartInfo("https://www.ibm.com/docs/en/netezza?topic=reference-create-sequence")
            {
                UseShellExecute = true
            };
            p.Start();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        public string SeqName { get; set; }
        public string SqlCode { get; set; }

        private void button2_Click(object sender, EventArgs e)
        {
            SeqName = tbName.Text;
            string minVal = cbNoMin.Checked ? "NO MINVALUE" : $"MINVALUE {numMin.Value}";
            string maxVal = cbNoMax.Checked ? "NO MAXVALUE" : $"MAXVALUE {numMin.Value}";
            SqlCode = $@"
CREATE SEQUENCE {tbName.Text} AS {cbDataType.Text} 
   START WITH {numStart.Value} 
   INCREMENT BY {numIncrement.Value} 
   {minVal}
   {maxVal}
   {(cbCycle.Checked ? "CYCLE" : "NO CYCLE")};";


            DialogResult = DialogResult.OK;
        }

        private void cbNoMin_CheckedChanged(object sender, EventArgs e)
        {
            if (cbNoMin.Checked)
            {
                numMin.Enabled = false;
            }
            else
            {
                numMin.Enabled = true;
            }
        }

        private void cbNoMax_CheckedChanged(object sender, EventArgs e)
        {
            if (cbNoMax.Checked)
            {
                numMax.Enabled = false;
            }
            else
            {
                numMax.Enabled = true;
            }
        }
    }
}
