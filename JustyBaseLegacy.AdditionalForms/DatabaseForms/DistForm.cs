using System.Diagnostics;

namespace JustyBaseLegacy.UI.DbForms
{
    public partial class DistForm : Form
    {

        List<string> avaiable;
        List<string> dist;

        public DistForm(List<string> allCols, List<string> distCols, Action<Form> DoColorize)
        {
            InitializeComponent();
            DoColorize(this);
            avaiable = allCols.Except(distCols).ToList();
            dist = distCols;
            this.lbAvaiable.Items.AddRange(avaiable.ToArray());
            this.lbDist.Items.AddRange(dist.ToArray());
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var p = new Process();
            p.StartInfo = new ProcessStartInfo("https://www.ibm.com/docs/en/netezza?topic=databases-distribution-keys")
            {
                UseShellExecute = true
            };
            p.Start();
        }

        private void toRight()
        {
            foreach (string selItem in lbAvaiable.SelectedItems)
            {

                avaiable.Remove(selItem.ToString());
                dist.Add(selItem.ToString());
            }
            this.lbAvaiable.Items.Clear();
            this.lbDist.Items.Clear();
            this.lbAvaiable.Items.AddRange(avaiable.ToArray());
            this.lbDist.Items.AddRange(dist.ToArray());
        }
        private void btToDist_Click(object sender, EventArgs e)
        {
            toRight();
        }

        private void toLeft()
        {
            foreach (string selItem in lbDist.SelectedItems)
            {
                dist.Remove(selItem.ToString());
                avaiable.Add(selItem.ToString());
            }
            this.lbAvaiable.Items.Clear();
            this.lbDist.Items.Clear();

            this.lbAvaiable.Items.AddRange(avaiable.ToArray());
            this.lbDist.Items.AddRange(dist.ToArray());
        }
        private void btRemoveFromDist_Click(object sender, EventArgs e)
        {
            toLeft();
        }

        public List<string> DistCols { get; set; }

        private void btSave_Click(object sender, EventArgs e)
        {
            // TO DO !!

            List<string> distCols = new List<string>();

            foreach (var item in lbDist.Items)
            {
                distCols.Add(item.ToString());
            }
            DistCols = distCols;
            DialogResult = DialogResult.OK;
        }

        private void btCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }
    }
}
