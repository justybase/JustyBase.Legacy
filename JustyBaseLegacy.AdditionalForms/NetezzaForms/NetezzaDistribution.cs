using AppBase.Common.WindowManagement;
using DatabaseDataGridView.WinForms.Coloring;

namespace JustyBaseLegacy.UI
{
    public partial class NetezzaDistribution : Form
    {
        public NetezzaDistribution(string name, IColorTheme colorTheme)
        {

            InitializeComponent();
            colorTheme.ColorForm(this);
            this.Text = name;
        }

        private Action<nint> _send1 = (x) => WindowNativeMethods.SendMessage(x, WindowConstants.WM_SETREDRAW, 0, 0);

        private Action<nint> _send2 = (x) => WindowNativeMethods.SendMessage(x, WindowConstants.WM_SETREDRAW, 1, 0);

        private int _onePanelWidth;
        public void Init2()
        {
            if (ForPlotDic.Count < Slices)
            {
                Min = 0;
                MinWDeleted = 0;
            }

            labelSkew.Text = Skew.ToString("N1");
            labelMax.Text = Max.ToString("N0");
            labelMin.Text = Min.ToString("N0");
            labelRows.Text = Rows.ToString("N0");
            labelCreate.Text = crtTime.ToString();
            labelAlocated.Text = (AlocatedBytes / 1024.0 / 1024.0 / 1024.0).ToString("N2") + " GB";
            labelUsed.Text = (UsedBytes / 1024.0 / 1024.0 / 1024.0).ToString("N2") + " GB";
            labelObjID.Text = ObjId.ToString();

            if (Max != 0)
            {
                labelDelta.Text = ((double)(Max - Min) / Max).ToString("P");
            }
            else
            {
                labelDelta.Text = "-";
            }


            labelMax2.Text = MaxWDeleted.ToString("N0");
            labelMin2.Text = MinWDeleted.ToString("N0");
            labelRows2.Text = RowsWDeleted.ToString("N0");
            if (MaxWDeleted != 0)
            {
                labelDelta2.Text = ((double)(MaxWDeleted - MinWDeleted) / MaxWDeleted).ToString("P");
            }
            else
            {
                labelDelta2.Text = "-";
            }

            chartBt.Enabled = false;
            chartBt2.Enabled = false;
            _send1(chartPanel.Handle);
            chartPanel.Controls.Clear();
            DoPlot(false);
            _send2(chartPanel.Handle);
            chartPanel.Refresh();
            chartBt.Enabled = true;
            chartBt2.Enabled = true;
        }

        public long Slices { get; set; }

        public double Skew { get; set; }

        public long Rows { get; set; }
        public long Min { get; set; }
        public long Max { get; set; }

        public long RowsWDeleted { get; set; }
        public long MinWDeleted { get; set; }
        public long MaxWDeleted { get; set; }
        public DateTime crtTime { get; set; }
        public long AlocatedBytes { get; set; }
        public long UsedBytes { get; set; }

        public long ObjId { get; set; }

        public Dictionary<int, (long count, long countWdeleted, string sliceName)> ForPlotDic { get; set; }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void Chart_Click(object sender, EventArgs e)
        {
            //testData();
            chartBt.Enabled = false;
            chartBt2.Enabled = false;
            _send1(chartPanel.Handle);
            chartPanel.Controls.Clear();
            DoPlot(false);
            _send2(chartPanel.Handle);
            chartPanel.Refresh();
            chartBt.Enabled = true;
            chartBt2.Enabled = true;
        }
        private void ChartBt2_Click(object sender, EventArgs e)
        {
            chartBt.Enabled = false;
            chartBt2.Enabled = false;
            _send1(chartPanel.Handle);
            chartPanel.Controls.Clear();
            DoPlot(true);
            _send2(chartPanel.Handle);
            chartPanel.Refresh();
            chartBt.Enabled = true;
            chartBt2.Enabled = true;
        }

        private void DoPlot(bool showDeleted)
        {
            _onePanelWidth = (int)(chartPanel.Width / Slices);

            if (_onePanelWidth * Slices < chartPanel.Width)
            {
                int tmp = chartPanel.Width - (int)(_onePanelWidth * Slices);
                chartPanel.Width -= tmp;
                this.Width -= tmp;
            }

            for (int i = 0; i < Slices; i++)
            {
                if (!ForPlotDic.ContainsKey(i))
                {
                    ForPlotDic[i] = (0, 0, i.ToString());
                }

                int normalizedRows;
                if (showDeleted)
                {
                    normalizedRows = (int)(chartPanel.Height * ((double)ForPlotDic[i].countWdeleted / MaxWDeleted));
                }
                else
                {
                    normalizedRows = (int)(chartPanel.Height * ((double)ForPlotDic[i].count / Max));
                }

                int positionPfTop = chartPanel.Height - normalizedRows;

                var panelX = new Panel
                {
                    BackColor = Color.DarkGreen,
                    Size = new Size(_onePanelWidth, normalizedRows),
                    Location = new Point(i * _onePanelWidth, positionPfTop)
                    // BorderStyle = BorderStyle.FixedSingle,
                    // ForeColor = Color.DarkGreen
                };
                chartPanel.Controls.Add(panelX);
            }
        }
    }
}
