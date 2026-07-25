using FastColoredTextBoxNS;

namespace JustyBaseLegacy.UI
{
    public partial class ContexScripts : Form
    {

        private readonly List<Scrip> _scripts = new List<Scrip>();
        private readonly AutocompleteMenu _popupMenu;
        private readonly Dictionary<string, List<string>> _contextScripts;

        public ContexScripts(Action<Form> DoColorize, int toolTipDelay, Dictionary<string, List<string>> contextScripts)
        {
            InitializeComponent();
            DoColorize(this);
            _contextScripts = contextScripts;
            fctbMain.ToolTipDelay = toolTipDelay;
            _popupMenu = new AutocompleteMenu(fctbMain);
            _popupMenu.Items.SetAutocompleteItems(["$db", "$schema", "$name", "$columns", "$signature"]);
            _popupMenu.SearchPattern = @"[\$\w]";
            _popupMenu.AllowTabKey = true;

            fctbMain.KeyDown += FctbMain_KeyDown;

            int n = _contextScripts.Count;
            var scriptsTemp = _contextScripts;

            foreach (var scr in scriptsTemp)
            {
                _scripts.Add(new Scrip()
                {
                    Name = scr.Key,
                    Pre = scr.Value[0],
                    Main = scr.Value[1],
                    Post = scr.Value[2],
                    Flags = scr.Value[3]
                });
            }

            listBox1.Items.Clear();
            tbName.Text = "";
            cbTables.Checked = false;
            cbViews.Checked = false;
            cbProc.Checked = false;
            cbExt.Checked = false;
            cbSyn.Checked = false;

            for (int i = 0; i < n; i++)
            {
                listBox1.Items.Add(_scripts[i].Name);
            }
            if (n > 0)
            {
                listBox1.SelectedIndex = 0;
                tbName.Text = _scripts[0].Name;
                fctbPre.Text = _scripts[0].Pre;
                fctbMain.Text = _scripts[0].Main;
                fctbPost.Text = _scripts[0].Post;
                cbTables.Checked = _scripts[0].Flags[0] == 'Y' ? true : false;
                cbViews.Checked = _scripts[0].Flags[1] == 'Y' ? true : false;
                cbProc.Checked = _scripts[0].Flags[2] == 'Y' ? true : false;
                cbExt.Checked = _scripts[0].Flags[3] == 'Y' ? true : false;
                cbSyn.Checked = _scripts[0].Flags[4] == 'Y' ? true : false;
            }
        }

        private void FctbMain_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.D4 && ModifierKeys == Keys.Shift)
            {
                //kk.Add(e.KeyCode);
                _popupMenu.MinFragmentLength = 1;
                //popupMenu.Show(true);
            }
            else
            {
                _popupMenu.MinFragmentLength = 3;
            }
        }

        private void ListBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            int indx = listBox1.SelectedIndex;
            if (indx == -1)
                return;

            tbName.Text = listBox1.Text;
            fctbPre.Text = _scripts[indx].Pre;
            fctbMain.Text = _scripts[indx].Main;
            fctbPost.Text = _scripts[indx].Post;
            cbTables.Checked = _scripts[indx].Flags[0] == 'Y' ? true : false;
            cbViews.Checked = _scripts[indx].Flags[1] == 'Y' ? true : false;
            cbProc.Checked = _scripts[indx].Flags[2] == 'Y' ? true : false;
            cbExt.Checked = _scripts[indx].Flags[3] == 'Y' ? true : false;
            cbSyn.Checked = _scripts[indx].Flags[4] == 'Y' ? true : false;
        }

        private void BtSave_Click(object sender, EventArgs e)
        {
            //listBox1.Text = tbName.Text;
            //listBox1.Refresh();
            int indx = listBox1.SelectedIndex;

            if (indx != -1)
            {
                _scripts[indx].Name = tbName.Text;
                _scripts[indx].Pre = fctbPre.TextFast;
                _scripts[indx].Main = fctbMain.TextFast;
                _scripts[indx].Post = fctbPost.TextFast;

                _scripts[indx].Flags = (cbTables.Checked ? "Y" : "N") + (cbViews.Checked ? "Y" : "N") +
                       (cbProc.Checked ? "Y" : "N") + (cbExt.Checked ? "Y" : "N") + (cbSyn.Checked ? "Y" : "N");


                listBox1.Items.RemoveAt(indx);
                listBox1.Items.Insert(indx, tbName.Text);
                listBox1.SelectedIndex = indx;
            }
            _contextScripts.Clear();

            foreach (var item in _scripts)
            {
                _contextScripts[item.Name] = new List<string> { item.Pre, item.Main, item.Post, item.Flags };
            }
        }

        class Scrip
        {
            public string Name { get; set; }
            public string Pre { get; set; }
            public string Main { get; set; }
            public string Post { get; set; }
            public string Flags { get; set; }
        }

        private void BtAdd_Click(object sender, EventArgs e)
        {
            _scripts.Add(new Scrip()
            {
                Name = $"script{listBox1.Items.Count}",
                Pre = $"pre{listBox1.Items.Count}",
                Main = $"main{listBox1.Items.Count}",
                Post = $"post{listBox1.Items.Count}",
                Flags = "YYYYY"
            });
            int ind = listBox1.Items.Add($"script{listBox1.Items.Count}");
            listBox1.SelectedIndex = ind;
        }

        private void BtDelete_Click(object sender, EventArgs e)
        {
            _scripts.RemoveAt(listBox1.SelectedIndex);
            listBox1.Items.RemoveAt(listBox1.SelectedIndex);
            if (listBox1.Items.Count > 0)
            {
                listBox1.SelectedIndex = 0;
            }
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            fctbMain.InsertText((sender as Button).Text);
        }
    }
}
