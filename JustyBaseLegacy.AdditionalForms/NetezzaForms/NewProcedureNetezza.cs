using System.Diagnostics;

namespace JustyBaseLegacy.UI.DbForms
{
    public partial class NewProcedureNetezza : Form
    {
        public NewProcedureNetezza(Form baseWindow, Action<Form> DoColorize, Action<DataGridView> doubleBuff)
        {
            InitializeComponent();
            DoColorize(this);
            doubleBuff(dataGridView1);
        }

        private void dataGridView1_DefaultValuesNeeded(object sender, DataGridViewRowEventArgs e)
        {
            e.Row.Cells[0].Value = e.Row.Index + 1;
            e.Row.Cells[2].Value = $"ARGUMENT_{e.Row.Index + 1}";
            e.Row.Cells[2].Value = "INTEGER";
        }

        public string ProcName { get; set; }
        public string ProcCode { get; set; }

        private void btOK_Click(object sender, EventArgs e)
        {
            ProcName = tbProcName.Text;

            string callerOwner = "OWNER";
            if (cbCaller.Checked)
            {
                callerOwner = "CALLER";
            }
            List<string> argvNames = new List<string>();
            List<string> argvTypes = new List<string>();

            foreach (DataGridViewRow item in dataGridView1.Rows)
            {
                if (item.Cells[1].Value is null)
                {
                    break;
                }
                argvNames.Add((item.Cells[1].Value as string) + $" ALIAS FOR ${(item.Index + 1)};");
                argvTypes.Add((item.Cells[2].Value as string));
            }


            ProcCode = $@"
CREATE OR REPLACE PROCEDURE {ProcName}({String.Join(',', argvTypes)})
RETURNS {cbDataType.Text}
EXECUTE AS {callerOwner}
LANGUAGE NZPLSQL AS
BEGIN_PROC
DECLARE
    {String.Join("\r\n    ", argvNames)}
BEGIN
    -- YOUR CODE GOES HERE
    
    EXCEPTION
    WHEN OTHERS THEN
        ROLLBACK;
        RAISE EXCEPTION  'Procedure failed: %', sqlerrm;
        --RAISE NOTICE 'Caught error, continuing %', sqlerrm;

END;
END_PROC;
";

            DialogResult = DialogResult.OK;
        }

        private void btCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        private void Docs_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var p = new Process();
            p.StartInfo = new ProcessStartInfo("https://www.ibm.com/docs/en/netezza?topic=procedure-create-stored")
            {
                UseShellExecute = true
            };
            p.Start();
        }
    }
}
