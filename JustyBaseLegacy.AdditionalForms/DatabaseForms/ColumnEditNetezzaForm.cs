using AppBase.Common;
using System.Diagnostics;

namespace JustyBaseLegacy.UI.DbForms
{
    public partial class ColumnEditNetezzaForm : Form
    {
        public ColumnEditNetezzaForm(string actualDesc, Action<Form> DoColorize)
        {
            InitializeComponent();
            DoColorize(this);

            tbColumnDesc.Text = actualDesc;
        }
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
            p.StartInfo = new ProcessStartInfo("https://www.ibm.com/docs/en/netezza?topic=reference-comment")
            {
                UseShellExecute = true
            };
            p.Start();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        public string finalDesc { get; set; }
        private void button1_Click(object sender, EventArgs e)
        {
            finalDesc = tbColumnDesc.Text;
            DialogResult = DialogResult.OK;
        }
    }
}
