using System.Diagnostics;

namespace JustyBaseLegacy.UI.DbForms
{
    public partial class Create_group : Form
    {
        public Create_group(string name, Action<Form> DoColorize)
        {
            ConnectionName = name;
            InitializeComponent();
            DoColorize(this);
        }
        public string ConnectionName { get; set; }
        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var p = new Process();
            p.StartInfo = new ProcessStartInfo("https://www.ibm.com/docs/en/netezza?topic=reference-create-group")
            {
                UseShellExecute = true
            };
            p.Start();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        public string Sql { get; set; }
        private void button1_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(tbName.Text))
            {
                MessageBox.Show("Enter group name");
                return;
            }

            Sql = $@"--https://www.ibm.com/docs/en/netezza?topic=reference-create-group
CREATE GROUP {tbName.Text}
DEFPRIORITY {cbDEFPRIORITY.SelectedItem}
MAXPRIORITY {cbMAXPRIORITY.SelectedItem}
ROWSETLIMIT {numROWSETLIMIT.Value}
SESSIONTIMEOUT {numSESSIONTIMEOUT.Value}
QUERYTIMEOUT {numQUERYTIMEOUT.Value}
CONCURRENT SESSIONS {numConcSession.Value}
RESOURCE MINIMUM {numResourceMin.Value}
RESOURCE MAXIMUM {numResourceMax.Value}
JOB MAXIMUM {numJobMaximum.Value}
COLLECT HISTORY {cbCollectHistory.SelectedItem}
ALLOW CROSS JOIN {cbAllowCross.SelectedItem}
PASSWORDEXPIRY {numPasswordExpiry.Value}
ACCESS TIME {cbAccessTime.SelectedItem}
";
            if (!string.IsNullOrWhiteSpace(tbUser.Text))
            {
                Sql += $"USER  {tbUser.Text}\r\n";
            }
            Sql += ";";
            DialogResult = DialogResult.OK;
        }

    }
}
