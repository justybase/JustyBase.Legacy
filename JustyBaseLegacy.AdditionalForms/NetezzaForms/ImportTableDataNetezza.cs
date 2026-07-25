using System.Diagnostics;

namespace JustyBaseLegacy.UI.DbForms
{
    public partial class ImportTableDataNetezza : Form
    {

        public string tableName { get; set; }
        public string DbName { get; set; }
        public string GetCode { get; set; }

        private string _configDirectory;

        public ImportTableDataNetezza(string dbName, string txt, Action<Form> DoColorize, string configDirectory)
        {
            _configDirectory = configDirectory;
            tableName = txt;
            DbName = dbName;
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
            var r = openFileDialog1.ShowDialog();
            if (r == DialogResult.OK)
            {
                tbPath.Text = openFileDialog1.FileName;
            }
        }
        private void fillGetCode()
        {
            string REMOTESOURCE = "DOTNET";

            GetCode = @$"
INSERT INTO {DbName}.{tableName}
SELECT * FROM 
EXTERNAL '{tbPath.Text}'
USING
({(cbHeader.Checked ? @"
    IncludeHeader" : "")}
    DELIMITER '{tbDelim.Text}'
    DecimalDelim '{cbDecimalDelim.Text}'
    DATESTYLE '{cbDateStyle.Text}'
    BOOLSTYLE '{cbBoolStyle.Text}'
    COMPRESS '{cbCompress.Text}'
    CRINSTRING '{cbCrInString.Text}'
    CTRLCHARS '{cbCtrlChars.Text}'
    ENCODING '{cbEnconding.Text}'
    ESCAPECHAR '\'
    REMOTESOURCE '{REMOTESOURCE}'
    LOGDIR '{_configDirectory}\data'
);";
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
    }
}
