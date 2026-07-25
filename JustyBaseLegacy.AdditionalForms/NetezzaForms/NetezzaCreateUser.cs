using System.Diagnostics;

namespace JustyBaseLegacy.UI.DbForms
{
    public partial class NetezzaCreateUser : Form
    {
        public NetezzaCreateUser(string name, Action<Form> DoColorize, List<string> list)
        {
            ConnectionName = name;
            _list = list;
            InitializeComponent();
            DoColorize(this);
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var p = new Process();
            p.StartInfo = new ProcessStartInfo("https://www.ibm.com/docs/en/netezza?topic=reference-create-user")
            {
                UseShellExecute = true
            };
            p.Start();
        }

        public string Sql { get; set; }
        private void btOk_Click(object sender, EventArgs e)
        {
            string expire = "--EXPIRE PASSWORD";
            string PASSWORDEXPIRY = "--PASSWORDEXPIRY 100";
            if (numPassExpiry.Value > 0)
            {
                PASSWORDEXPIRY = $"PASSWORDEXPIRY {numPassExpiry.Value}";
            }

            if (cbPasswordExpire.Checked)
            {
                expire = "EXPIRE PASSWORD";
            }
            string IN_GROUP = "IN GROUP GROUP_NAME";
            if (!string.IsNullOrWhiteSpace(comboGroups.Text))
            {
                IN_GROUP = $"IN GROUP {comboGroups.Text}";
            }

            string validUntil = "--VALID UNTIL '<valid_date>'";
            if (dateTimePickerValidUntil.Value != new DateTime(1900, 1, 1))
            {
                validUntil = $"VALID UNTIL '{dateTimePickerValidUntil.Value.ToString("yyyy-MM-dd")}'";
            }

            Sql = @$"CREATE USER {tbName.Text}
WITH PASSWORD '{tbPassword.Text}'
{expire}
{PASSWORDEXPIRY}
--AUTH {{LOCAL | DEFAULT}}
--SYSID <userid> |
{IN_GROUP}
--IN RESOURCEGROUP <rsg> 
{validUntil}
--DEFPRIORITY {{CRITICAL | HIGH | NORMAL | LOW | NONE}} |
--MAXPRIORITY {{CRITICAL | HIGH | NORMAL | LOW | NONE}} |
--ROWSETLIMIT <rslimit> |
--SESSIONTIMEOUT <sessiontimeout> |
--QUERYTIMEOUT <querytimeout> |
--CONCURRENT SESSIONS <concsessions> |
--SECURITY LABEL {{'<seclabel>|PUBLIC::'}} |
--AUDIT CATEGORY {{NONE | '<category>[,<category>…]'}}
--COLLECT HISTORY {{ON | OFF | DEFAULT}} |
--ALLOW CROSS JOIN {{TRUE | FALSE | NULL}} |
--ACCESS TIME {{ALL | DEFAULT | (< access_time >[,< access_time >…])}}
;";
            DialogResult = DialogResult.OK;
        }

        private void btCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }
        public string ConnectionName { get; set; }
        private List<string> _list;

        private void comboGroups_DropDown(object sender, EventArgs e)
        {
            List<string> list = new List<string>();
            comboGroups.Items.Clear();

            list = _list;

            comboGroups.Items.AddRange(list.ToArray());
        }
    }
}
