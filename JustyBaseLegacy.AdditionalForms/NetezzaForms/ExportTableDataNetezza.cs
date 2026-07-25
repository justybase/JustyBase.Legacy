using System.Diagnostics;

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

            GetCode = @$"
CREATE EXTERNAL TABLE '{tbPath.Text}'
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
)
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
