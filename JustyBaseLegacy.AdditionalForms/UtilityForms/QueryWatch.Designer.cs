namespace JustyBaseLegacy.UI
{
    partial class QueryWatch
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _autoRefreshTimer?.Dispose();
                components?.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            DataGridViewCellStyle headerStyle = new DataGridViewCellStyle();
            DataGridViewCellStyle cellStyle = new DataGridViewCellStyle();
            DataGridViewCellStyle detailsHeaderStyle = new DataGridViewCellStyle();
            DataGridViewCellStyle detailsCellStyle = new DataGridViewCellStyle();
            panelHeader = new Panel();
            labelTitle = new Label();
            labelConnection = new Label();
            buttonRefresh = new Button();
            checkBoxAutoRefresh = new CheckBox();
            labelStatus = new Label();
            splitMain = new SplitContainer();
            queryWatchDataGridView = new ThemedDataGridView();
            panelDetails = new Panel();
            labelDetails = new Label();
            detailsDataGridView = new ThemedDataGridView();
            panelHeader.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitMain).BeginInit();
            splitMain.Panel1.SuspendLayout();
            splitMain.Panel2.SuspendLayout();
            splitMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)queryWatchDataGridView).BeginInit();
            panelDetails.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)detailsDataGridView).BeginInit();
            SuspendLayout();
            //
            // panelHeader
            //
            panelHeader.BackColor = Color.White;
            panelHeader.Controls.Add(labelTitle);
            panelHeader.Controls.Add(labelConnection);
            panelHeader.Controls.Add(buttonRefresh);
            panelHeader.Controls.Add(checkBoxAutoRefresh);
            panelHeader.Controls.Add(labelStatus);
            panelHeader.Dock = DockStyle.Top;
            panelHeader.Location = new Point(0, 0);
            panelHeader.Name = "panelHeader";
            panelHeader.Padding = new Padding(20);
            panelHeader.Size = new Size(1200, 112);
            panelHeader.TabIndex = 0;
            //
            // labelTitle
            //
            labelTitle.AutoSize = true;
            labelTitle.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            labelTitle.ForeColor = Color.FromArgb(33, 37, 41);
            labelTitle.Location = new Point(20, 16);
            labelTitle.Name = "labelTitle";
            labelTitle.Size = new Size(126, 25);
            labelTitle.TabIndex = 0;
            labelTitle.Text = "Query Watch";
            //
            // labelConnection
            //
            labelConnection.AutoSize = true;
            labelConnection.Font = new Font("Segoe UI", 10F);
            labelConnection.ForeColor = Color.FromArgb(108, 117, 125);
            labelConnection.Location = new Point(160, 22);
            labelConnection.Name = "labelConnection";
            labelConnection.Size = new Size(74, 19);
            labelConnection.TabIndex = 1;
            labelConnection.Text = "Connection";
            //
            // buttonRefresh
            //
            buttonRefresh.BackColor = Color.FromArgb(0, 123, 255);
            buttonRefresh.FlatAppearance.BorderSize = 0;
            buttonRefresh.FlatStyle = FlatStyle.Flat;
            buttonRefresh.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            buttonRefresh.ForeColor = Color.White;
            buttonRefresh.Location = new Point(20, 56);
            buttonRefresh.Name = "queryWatchRefreshButton";
            buttonRefresh.Size = new Size(120, 32);
            buttonRefresh.TabIndex = 2;
            buttonRefresh.Text = "Refresh now";
            buttonRefresh.UseVisualStyleBackColor = false;
            //
            // checkBoxAutoRefresh
            //
            checkBoxAutoRefresh.AutoSize = true;
            checkBoxAutoRefresh.Font = new Font("Segoe UI", 10F);
            checkBoxAutoRefresh.ForeColor = Color.FromArgb(73, 80, 87);
            checkBoxAutoRefresh.Location = new Point(156, 60);
            checkBoxAutoRefresh.Name = "queryWatchAutoRefreshCheckBox";
            checkBoxAutoRefresh.Size = new Size(236, 23);
            checkBoxAutoRefresh.TabIndex = 3;
            checkBoxAutoRefresh.Text = "Auto-refresh every 30 seconds";
            checkBoxAutoRefresh.UseVisualStyleBackColor = true;
            //
            // labelStatus
            //
            labelStatus.AutoSize = true;
            labelStatus.Font = new Font("Segoe UI", 9F);
            labelStatus.ForeColor = Color.FromArgb(108, 117, 125);
            labelStatus.Location = new Point(420, 64);
            labelStatus.Name = "labelStatus";
            labelStatus.Size = new Size(87, 15);
            labelStatus.TabIndex = 4;
            labelStatus.Text = "Not refreshed";
            //
            // splitMain
            //
            splitMain.Dock = DockStyle.Fill;
            splitMain.Location = new Point(0, 112);
            splitMain.Name = "splitMain";
            splitMain.Orientation = Orientation.Horizontal;
            splitMain.Panel1.Controls.Add(queryWatchDataGridView);
            splitMain.Panel1.Padding = new Padding(20, 0, 20, 8);
            splitMain.Panel2.Controls.Add(panelDetails);
            splitMain.Panel2.Padding = new Padding(20, 0, 20, 20);
            splitMain.Size = new Size(1200, 588);
            splitMain.SplitterDistance = 320;
            splitMain.SplitterWidth = 8;
            splitMain.TabIndex = 1;
            //
            // queryWatchDataGridView
            //
            queryWatchDataGridView.AllowUserToAddRows = false;
            queryWatchDataGridView.AllowUserToDeleteRows = false;
            queryWatchDataGridView.BackgroundColor = Color.White;
            queryWatchDataGridView.BorderStyle = BorderStyle.None;
            queryWatchDataGridView.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            queryWatchDataGridView.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            headerStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            headerStyle.BackColor = Color.FromArgb(248, 249, 250);
            headerStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            headerStyle.ForeColor = Color.FromArgb(73, 80, 87);
            headerStyle.SelectionBackColor = Color.FromArgb(248, 249, 250);
            headerStyle.SelectionForeColor = Color.FromArgb(73, 80, 87);
            headerStyle.WrapMode = DataGridViewTriState.False;
            queryWatchDataGridView.ColumnHeadersDefaultCellStyle = headerStyle;
            queryWatchDataGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            queryWatchDataGridView.ColumnHeadersHeight = 34;
            cellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            cellStyle.BackColor = Color.White;
            cellStyle.Font = new Font("Segoe UI", 9F);
            cellStyle.ForeColor = Color.FromArgb(73, 80, 87);
            cellStyle.SelectionBackColor = Color.FromArgb(0, 123, 255);
            cellStyle.SelectionForeColor = Color.White;
            cellStyle.WrapMode = DataGridViewTriState.False;
            queryWatchDataGridView.DefaultCellStyle = cellStyle;
            queryWatchDataGridView.Dock = DockStyle.Fill;
            queryWatchDataGridView.EnableHeadersVisualStyles = false;
            queryWatchDataGridView.GridColor = Color.FromArgb(233, 236, 239);
            queryWatchDataGridView.Name = "queryWatchDataGridView";
            queryWatchDataGridView.ReadOnly = true;
            queryWatchDataGridView.RowHeadersVisible = false;
            queryWatchDataGridView.RowTemplate.Height = 32;
            queryWatchDataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            queryWatchDataGridView.MultiSelect = false;
            queryWatchDataGridView.TabIndex = 0;
            //
            // panelDetails
            //
            panelDetails.Controls.Add(detailsDataGridView);
            panelDetails.Controls.Add(labelDetails);
            panelDetails.Dock = DockStyle.Fill;
            panelDetails.Name = "panelDetails";
            panelDetails.TabIndex = 0;
            //
            // labelDetails
            //
            labelDetails.Dock = DockStyle.Top;
            labelDetails.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            labelDetails.ForeColor = Color.FromArgb(73, 80, 87);
            labelDetails.Location = new Point(0, 0);
            labelDetails.Name = "labelDetails";
            labelDetails.Padding = new Padding(0, 4, 0, 8);
            labelDetails.Size = new Size(1160, 32);
            labelDetails.TabIndex = 0;
            labelDetails.Text = "Session details";
            //
            // detailsDataGridView
            //
            detailsDataGridView.AllowUserToAddRows = false;
            detailsDataGridView.AllowUserToDeleteRows = false;
            detailsDataGridView.AllowUserToResizeRows = false;
            detailsDataGridView.BackgroundColor = Color.White;
            detailsDataGridView.BorderStyle = BorderStyle.None;
            detailsDataGridView.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            detailsDataGridView.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            detailsHeaderStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            detailsHeaderStyle.BackColor = Color.FromArgb(248, 249, 250);
            detailsHeaderStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            detailsHeaderStyle.ForeColor = Color.FromArgb(73, 80, 87);
            detailsHeaderStyle.SelectionBackColor = Color.FromArgb(248, 249, 250);
            detailsHeaderStyle.SelectionForeColor = Color.FromArgb(73, 80, 87);
            detailsDataGridView.ColumnHeadersDefaultCellStyle = detailsHeaderStyle;
            detailsDataGridView.ColumnHeadersHeight = 30;
            detailsDataGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            detailsCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            detailsCellStyle.BackColor = Color.White;
            detailsCellStyle.Font = new Font("Segoe UI", 9F);
            detailsCellStyle.ForeColor = Color.FromArgb(73, 80, 87);
            detailsCellStyle.SelectionBackColor = Color.FromArgb(0, 123, 255);
            detailsCellStyle.SelectionForeColor = Color.White;
            detailsCellStyle.WrapMode = DataGridViewTriState.True;
            detailsDataGridView.DefaultCellStyle = detailsCellStyle;
            detailsDataGridView.Dock = DockStyle.Fill;
            detailsDataGridView.EnableHeadersVisualStyles = false;
            detailsDataGridView.GridColor = Color.FromArgb(233, 236, 239);
            detailsDataGridView.Name = "queryWatchDetailsDataGridView";
            detailsDataGridView.ReadOnly = true;
            detailsDataGridView.RowHeadersVisible = false;
            detailsDataGridView.RowTemplate.Height = 28;
            detailsDataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            detailsDataGridView.TabIndex = 1;
            detailsDataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Property",
                HeaderText = "Property",
                ReadOnly = true,
                Width = 220,
                MinimumWidth = 140,
            });
            detailsDataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Value",
                HeaderText = "Value",
                ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                MinimumWidth = 200,
            });
            //
            // QueryWatch
            //
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Color.FromArgb(248, 249, 250);
            ClientSize = new Size(1200, 700);
            Controls.Add(splitMain);
            Controls.Add(panelHeader);
            Font = new Font("Segoe UI", 9F);
            MinimumSize = new Size(800, 480);
            Name = "QueryWatch";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Query Watch";
            DoubleBuffered = true;
            panelHeader.ResumeLayout(false);
            panelHeader.PerformLayout();
            splitMain.Panel1.ResumeLayout(false);
            splitMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitMain).EndInit();
            splitMain.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)queryWatchDataGridView).EndInit();
            panelDetails.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)detailsDataGridView).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Panel panelHeader;
        private Label labelTitle;
        private Label labelConnection;
        private Button buttonRefresh;
        private CheckBox checkBoxAutoRefresh;
        private Label labelStatus;
        private SplitContainer splitMain;
        private ThemedDataGridView queryWatchDataGridView;
        private Panel panelDetails;
        private Label labelDetails;
        private ThemedDataGridView detailsDataGridView;
    }
}
