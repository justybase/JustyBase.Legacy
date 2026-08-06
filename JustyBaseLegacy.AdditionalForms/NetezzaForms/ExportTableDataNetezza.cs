using System.Diagnostics;
using JustyBase.NetezzaDdl;

namespace JustyBaseLegacy.UI.DbForms
{
    public partial class ExportTableDataNetezza : Form
    {
        public string tableName { get; set; }
        public string DbName { get; set; }
        public ExportTableDataNetezza(string dbName, string txt, Action<Form> DoColorize)
        {
            DbName = dbName;
            tableName = txt;
            InitializeComponent();
            DoColorize(this);
            cbDecimalDelim.Text = ".";

            cbDateStyle.Text = "YMD";
            cbBoolStyle.Text = "1_0";
            cbCompress.Text = "False";
            cbCrInString.Text = "False";
            cbCtrlChars.Text = "False";
            cbEnconding.Text = "Internal";
        }

        private void btPath_Click(object sender, EventArgs e)
        {
            var r = saveFileDialog1.ShowDialog();
            if (r == DialogResult.OK)
            {
                tbPath.Text = saveFileDialog1.FileName;
            }

        }
        public string GetCode { get; set; }

        private void fillGetCode()
        {
            string REMOTESOURCE = "DOTNET";

            string usingClause = NetezzaImportSql.BuildUsingClause(new NetezzaImportUsingOptions
            {
                IncludeHeader = cbHeader.Checked,
                Delimiter = tbDelim.Text,
                DecimalDelim = cbDecimalDelim.Text,
                DateStyle = cbDateStyle.Text,
                BoolStyle = cbBoolStyle.Text,
                Compress = cbCompress.Text == "True",
                CrInString = cbCrInString.Text == "True",
                AllowControlCharacters = cbCtrlChars.Text == "True",
                EncodingName = cbEnconding.Text,
                EscapeChar = "\\",
                RemoteSource = REMOTESOURCE
            });

            GetCode = $@"
CREATE EXTERNAL TABLE '{tbPath.Text}'
{usingClause}
AS 
SELECT * FROM {DbName}.{tableName};";
        }

        private void btOk_Click(object sender, EventArgs e)
        {
            fillGetCode();

            DialogResult = DialogResult.OK;
        }

        private void btCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        private void btClipboard_Click(object sender, EventArgs e)
        {
            fillGetCode();
            Clipboard.SetText(GetCode);
            DialogResult = DialogResult.None;
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var p = new Process();
            p.StartInfo = new ProcessStartInfo("https://www.ibm.com/docs/en/netezza?topic=options-option-summary")
            {
                UseShellExecute = true
            };
            p.Start();
        }
    }
}
