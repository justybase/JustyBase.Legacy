namespace JustyBaseLegacy.UI
{
    public partial class SaveSnippet : Form
    {
        public SaveSnippet()
        {
            InitializeComponent();
        }

        public string GetName()
        {
            return tbName.Text;
        }

        public bool IsStandard()
        {
            return rbStandard.Checked;
        }

        public bool IsQuick()
        {
            return rbQuick.Checked;
        }


        private void button1_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }


        private void tbName_MouseDown(object sender, MouseEventArgs e)
        {
            if (tbName.Text == "snippet name here..")
            {
                tbName.Text = "";
            }
        }

        private void rbStandard_CheckedChanged(object sender, EventArgs e)
        {
            if (rbStandard.Checked)
            {
                rbQuick.Checked = false;
            }
            else
            {
                rbQuick.Checked = true;
            }
        }
    }
}
