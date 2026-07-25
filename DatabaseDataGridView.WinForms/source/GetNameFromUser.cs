namespace DatabaseDataGridView.WinForms;

public partial class GetNameFromUser : Form
{
    public GetNameFromUser()
    {
        InitializeComponent();
    }

    private void TbName_MouseDown(object sender, MouseEventArgs e)
    {
        if (tbName.Text == "JustyBaseLegacy")
        {
            tbName.Text = "";
        }
    }

    private void Button1_Click(object sender, EventArgs e)
    {
        this.DialogResult = DialogResult.OK;
    }

    private void Button2_Click(object sender, EventArgs e)
    {
        this.DialogResult = DialogResult.Cancel;
    }

    public string GetName()
    {
        return tbName.Text;
    }

    public bool IsAllTabls()
    {
        return cbAllTabs.Checked;
    }


    private void GetNameFromUser_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
        {
            this.DialogResult = DialogResult.Cancel;
        }
        else if (e.KeyCode == Keys.Return)
        {
            this.DialogResult = DialogResult.OK;
        }
    }
}
