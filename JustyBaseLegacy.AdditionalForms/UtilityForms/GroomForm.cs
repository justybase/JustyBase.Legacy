using System.Diagnostics;

namespace JustyBaseLegacy.UI
{
    public partial class GroomForm : Form
    {
        public GroomForm(string tableName, Action<Form> DoColorize)
        {
            InitializeComponent();
            DoColorize(this);
            cbMode.SelectedIndex = 0;
            cbOptions.SelectedIndex = 0;
            _tableName = tableName;
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var p = new Process();
            p.StartInfo = new ProcessStartInfo("https://www.ibm.com/docs/en/netezza?topic=databases-groom-tables")
            {
                UseShellExecute = true
            };
            p.Start();
        }

        string _tableName = "";
        public string ResultSql { get; set; }
        private void btGromOk_Click(object sender, EventArgs e)
        {
            string backupSet = cbOptions.SelectedItem.ToString();
            if (backupSet != "DEFAULT" && backupSet != "NONE")
            {
                backupSet = $"'{backupSet}'";
            }

            ResultSql = $"GROOM TABLE {_tableName} {cbMode.SelectedItem} RECLAIM BACKUPSET {backupSet};";
            DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btGroomCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
