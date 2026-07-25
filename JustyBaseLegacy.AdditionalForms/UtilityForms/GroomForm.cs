using System.Diagnostics;
using JustyBase.NetezzaDdl;

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
            ResultSql = NetezzaMaintenanceSql.BuildGroom(
                _tableName,
                cbMode.SelectedItem?.ToString() ?? NetezzaMaintenanceSql.GroomModes[0],
                cbOptions.SelectedItem?.ToString());
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
