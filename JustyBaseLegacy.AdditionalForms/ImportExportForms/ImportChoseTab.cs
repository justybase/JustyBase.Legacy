namespace JustyBaseLegacy.UI.DbForms
{
    public partial class ImportChoseTab : Form
    {
        public ImportChoseTab(string[] names, Action<Form> DoColorize, Action<DataGridView> DoubleBuff)
        {
            InitializeComponent();
            DoColorize(this);
            DoubleBuff(dataGridView1);
            int i = 0;
            foreach (string tabName in names)
            {
                if (i == 0)
                {
                    dataGridView1.Rows.Add(new[] { (object)true, tabName });
                }
                else
                {
                    dataGridView1.Rows.Add(new[] { (object)false, tabName });
                }
                i++;
            }
            SelectedTabs = new List<string>();

        }

        private void btCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        public List<string> SelectedTabs { get; set; }
        private void btOk_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow item in dataGridView1.Rows)
            {
                string nme = (string)item.Cells[1].Value;
                if (string.IsNullOrWhiteSpace(nme))
                {
                    break;
                }
                if ((bool)item.Cells[0].Value)
                {
                    SelectedTabs.Add(nme);
                }
            }
            DialogResult = DialogResult.OK;
        }
    }
}
