
namespace DatabaseDataGridView.WinForms
{
    partial class CustomDataGridView
    {
        /// <summary> 
        /// Wymagana zmienna projektanta.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Dispose all used resources.
        /// </summary>
        /// <param name="disposing">True to dispose managed resources; otherwise false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support; do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CustomDataGridView));
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            this.dataGridView1 = new DatabaseDataGridView.WinForms.ThemedDataGridView();
            this.groupPanel = new System.Windows.Forms.Panel();
            this.cbAprox = new System.Windows.Forms.CheckBox();
            this.btDownload = new System.Windows.Forms.Button();
            this.btRowView = new System.Windows.Forms.Button();
            this.btOpenInExcel = new System.Windows.Forms.Button();
            this.btCopyAsText = new System.Windows.Forms.Button();
            this.btCopyAsExcel = new System.Windows.Forms.Button();
            this.cbJumpToColumn = new System.Windows.Forms.ComboBox();
            this.lbCnt = new System.Windows.Forms.Label();
            this.tbSearch = new CueTextBox();
            this.dgvLabel = new System.Windows.Forms.Label();
            this.dgvDrop = new System.Windows.Forms.DataGridView();
            this.cmsGroup = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.itemRemoveGrouping = new System.Windows.Forms.ToolStripMenuItem();
            this.itemRemoveSorting = new System.Windows.Forms.ToolStripMenuItem();
            this.dgvSummaries = new System.Windows.Forms.DataGridView();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.groupPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvDrop)).BeginInit();
            this.cmsGroup.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvSummaries)).BeginInit();
            this.SuspendLayout();
            // 
            // dataGridView1
            // 
            this.dataGridView1.AllowDrop = true;
            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.AllowUserToDeleteRows = false;
            this.dataGridView1.AllowUserToResizeRows = false;
            this.dataGridView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridView1.BackgroundColor = System.Drawing.Color.White;
            this.dataGridView1.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.dataGridView1.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle1.NullValue = "NULL";
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dataGridView1.DefaultCellStyle = dataGridViewCellStyle1;
            this.dataGridView1.EnableHeadersVisualStyles = false;
            this.dataGridView1.Location = new System.Drawing.Point(0, 25);
            this.dataGridView1.Margin = new System.Windows.Forms.Padding(0);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.ReadOnly = true;
            this.dataGridView1.RowHeadersWidth = 50;
            this.dataGridView1.RowTemplate.Height = 25;
            this.dataGridView1.Size = new System.Drawing.Size(812, 355);
            this.dataGridView1.TabIndex = 4;
            this.dataGridView1.RowHeaderMouseClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.DataGridView1_RowHeaderMouseClick);
            // 
            // groupPanel
            // 
            this.groupPanel.AllowDrop = true;
            this.groupPanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupPanel.BackColor = System.Drawing.Color.White;
            this.groupPanel.Controls.Add(this.cbAprox);
            this.groupPanel.Controls.Add(this.btDownload);
            this.groupPanel.Controls.Add(this.btRowView);
            this.groupPanel.Controls.Add(this.btOpenInExcel);
            this.groupPanel.Controls.Add(this.btCopyAsText);
            this.groupPanel.Controls.Add(this.btCopyAsExcel);
            this.groupPanel.Controls.Add(this.cbJumpToColumn);
            this.groupPanel.Controls.Add(this.lbCnt);
            this.groupPanel.Controls.Add(this.tbSearch);
            this.groupPanel.Controls.Add(this.dgvLabel);
            this.groupPanel.Controls.Add(this.dgvDrop);
            this.groupPanel.Location = new System.Drawing.Point(0, 0);
            this.groupPanel.Margin = new System.Windows.Forms.Padding(0);
            this.groupPanel.Name = "groupPanel";
            this.groupPanel.Size = new System.Drawing.Size(812, 26);
            this.groupPanel.TabIndex = 1;
            this.groupPanel.DragDrop += new System.Windows.Forms.DragEventHandler(this.GroupPanel_DragDrop);
            this.groupPanel.DragOver += new System.Windows.Forms.DragEventHandler(this.GroupPanel_DragOver);
            // 
            // cbAprox
            // 
            this.cbAprox.AutoSize = true;
            this.cbAprox.Location = new System.Drawing.Point(187, 5);
            this.cbAprox.Name = "cbAprox";
            this.cbAprox.Size = new System.Drawing.Size(15, 14);
            this.cbAprox.TabIndex = 11;
            this.toolTip1.SetToolTip(this.cbAprox, "search for text contains in field even in non text data (be aware current culture" +
        " settings are used)");
            this.cbAprox.UseVisualStyleBackColor = true;
            // 
            // btDownload
            // 
            this.btDownload.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btDownload.BackgroundImage")));
            this.btDownload.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btDownload.ImageAlign = System.Drawing.ContentAlignment.TopLeft;
            this.btDownload.Location = new System.Drawing.Point(283, 0);
            this.btDownload.Margin = new System.Windows.Forms.Padding(0);
            this.btDownload.Name = "btDownload";
            this.btDownload.Size = new System.Drawing.Size(26, 26);
            this.btDownload.TabIndex = 10;
            this.btDownload.UseVisualStyleBackColor = true;
            this.btDownload.Click += new System.EventHandler(this.BtDownload_Click);
            // 
            // btRowView
            // 
            this.btRowView.Location = new System.Drawing.Point(309, 0);
            this.btRowView.Margin = new System.Windows.Forms.Padding(0);
            this.btRowView.Name = "btRowView";
            this.btRowView.Size = new System.Drawing.Size(26, 26);
            this.btRowView.TabIndex = 12;
            this.btRowView.UseVisualStyleBackColor = true;
            this.btRowView.Click += new System.EventHandler(this.BtRowView_Click);
            // 
            // btOpenInExcel
            // 
            this.btOpenInExcel.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btOpenInExcel.BackgroundImage")));
            this.btOpenInExcel.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btOpenInExcel.ImageAlign = System.Drawing.ContentAlignment.TopLeft;
            this.btOpenInExcel.Location = new System.Drawing.Point(257, 0);
            this.btOpenInExcel.Margin = new System.Windows.Forms.Padding(0);
            this.btOpenInExcel.Name = "btOpenInExcel";
            this.btOpenInExcel.Size = new System.Drawing.Size(26, 26);
            this.btOpenInExcel.TabIndex = 10;
            this.btOpenInExcel.UseVisualStyleBackColor = true;
            this.btOpenInExcel.Click += new System.EventHandler(this.BtOpenInExcel_Click);
            // 
            // btCopyAsText
            // 
            this.btCopyAsText.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btCopyAsText.BackgroundImage")));
            this.btCopyAsText.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btCopyAsText.Location = new System.Drawing.Point(231, 0);
            this.btCopyAsText.Margin = new System.Windows.Forms.Padding(0);
            this.btCopyAsText.Name = "btCopyAsText";
            this.btCopyAsText.Size = new System.Drawing.Size(26, 26);
            this.btCopyAsText.TabIndex = 10;
            this.btCopyAsText.UseVisualStyleBackColor = true;
            this.btCopyAsText.Click += new System.EventHandler(this.BtCopyAsText_Click);
            // 
            // btCopyAsExcel
            // 
            this.btCopyAsExcel.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btCopyAsExcel.BackgroundImage")));
            this.btCopyAsExcel.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btCopyAsExcel.Font = new System.Drawing.Font("Segoe UI Emoji", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.btCopyAsExcel.Location = new System.Drawing.Point(205, 0);
            this.btCopyAsExcel.Margin = new System.Windows.Forms.Padding(0);
            this.btCopyAsExcel.Name = "btCopyAsExcel";
            this.btCopyAsExcel.Size = new System.Drawing.Size(26, 26);
            this.btCopyAsExcel.TabIndex = 10;
            this.btCopyAsExcel.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            this.btCopyAsExcel.UseVisualStyleBackColor = true;
            this.btCopyAsExcel.Click += new System.EventHandler(this.BtCopyAsExcel_Click);
            // 
            // cbJumpToColumn
            // 
            this.cbJumpToColumn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cbJumpToColumn.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.cbJumpToColumn.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.cbJumpToColumn.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.cbJumpToColumn.FormattingEnabled = true;
            this.cbJumpToColumn.Location = new System.Drawing.Point(658, 0);
            this.cbJumpToColumn.Margin = new System.Windows.Forms.Padding(0);
            this.cbJumpToColumn.Name = "cbJumpToColumn";
            this.cbJumpToColumn.Size = new System.Drawing.Size(154, 25);
            this.cbJumpToColumn.TabIndex = 9;
            this.cbJumpToColumn.DropDown += new System.EventHandler(this.CbJumpToColumn_DropDown);
            this.cbJumpToColumn.SelectedIndexChanged += new System.EventHandler(this.CbJumpToColumn_SelectedIndexChanged);
            // 
            // lbCnt
            // 
            this.lbCnt.AutoSize = true;
            this.lbCnt.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.lbCnt.Location = new System.Drawing.Point(312, 2);
            this.lbCnt.Name = "lbCnt";
            this.lbCnt.Size = new System.Drawing.Size(45, 19);
            this.lbCnt.TabIndex = 8;
            this.lbCnt.Text = "label1";
            // 
            // tbSearch
            // 
            this.tbSearch.Cue = "search";
            this.tbSearch.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.tbSearch.Location = new System.Drawing.Point(0, 0);
            this.tbSearch.MaxLength = 256;
            this.tbSearch.Name = "tbSearch";
            this.tbSearch.Size = new System.Drawing.Size(181, 25);
            this.tbSearch.TabIndex = 7;
            this.tbSearch.TextChanged += new System.EventHandler(this.TbSearch_TextChanged);
            // 
            // dgvLabel
            // 
            this.dgvLabel.AutoSize = true;
            this.dgvLabel.BackColor = System.Drawing.Color.Transparent;
            this.dgvLabel.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.dgvLabel.ForeColor = System.Drawing.SystemColors.ButtonShadow;
            this.dgvLabel.Location = new System.Drawing.Point(377, 0);
            this.dgvLabel.Margin = new System.Windows.Forms.Padding(0);
            this.dgvLabel.Name = "dgvLabel";
            this.dgvLabel.Size = new System.Drawing.Size(192, 19);
            this.dgvLabel.TabIndex = 2;
            this.dgvLabel.Text = "Drag column header to group";
            // 
            // dgvDrop
            // 
            this.dgvDrop.AllowDrop = true;
            this.dgvDrop.AllowUserToAddRows = false;
            this.dgvDrop.AllowUserToDeleteRows = false;
            this.dgvDrop.AllowUserToResizeColumns = false;
            this.dgvDrop.AllowUserToResizeRows = false;
            this.dgvDrop.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvDrop.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.ColumnHeader;
            this.dgvDrop.BackgroundColor = System.Drawing.Color.Gainsboro;
            this.dgvDrop.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.dgvDrop.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.Disable;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgvDrop.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle2;
            this.dgvDrop.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvDrop.ContextMenuStrip = this.cmsGroup;
            this.dgvDrop.Cursor = System.Windows.Forms.Cursors.Hand;
            this.dgvDrop.EnableHeadersVisualStyles = false;
            this.dgvDrop.GridColor = System.Drawing.Color.Gray;
            this.dgvDrop.Location = new System.Drawing.Point(360, 0);
            this.dgvDrop.Margin = new System.Windows.Forms.Padding(0);
            this.dgvDrop.Name = "dgvDrop";
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle3.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            dataGridViewCellStyle3.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvDrop.RowHeadersDefaultCellStyle = dataGridViewCellStyle3;
            this.dgvDrop.RowTemplate.Height = 25;
            this.dgvDrop.Size = new System.Drawing.Size(298, 26);
            this.dgvDrop.TabIndex = 0;
            this.dgvDrop.DragDrop += new System.Windows.Forms.DragEventHandler(this.DgvDrop_DragDrop);
            this.dgvDrop.DragOver += new System.Windows.Forms.DragEventHandler(this.DgvDrop_DragOver);
            this.dgvDrop.MouseDown += new System.Windows.Forms.MouseEventHandler(this.DgvDrop_MouseDown);
            this.dgvDrop.MouseMove += new System.Windows.Forms.MouseEventHandler(this.DgvDrop_MouseMove);
            // 
            // cmsGroup
            // 
            this.cmsGroup.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.itemRemoveGrouping,
            this.itemRemoveSorting});
            this.cmsGroup.Name = "cmsGroup";
            this.cmsGroup.Size = new System.Drawing.Size(171, 48);
            // 
            // itemRemoveGrouping
            // 
            this.itemRemoveGrouping.Name = "itemRemoveGrouping";
            this.itemRemoveGrouping.Size = new System.Drawing.Size(170, 22);
            this.itemRemoveGrouping.Text = "Remove Grouping";
            this.itemRemoveGrouping.Click += new System.EventHandler(this.ItemRemoveGrouping_Click);
            // 
            // itemRemoveSorting
            // 
            this.itemRemoveSorting.Name = "itemRemoveSorting";
            this.itemRemoveSorting.Size = new System.Drawing.Size(170, 22);
            this.itemRemoveSorting.Text = "Remove Sorting";
            this.itemRemoveSorting.Click += new System.EventHandler(this.ItemRemoveSorting_Click);
            // 
            // dgvSummaries
            // 
            this.dgvSummaries.AllowUserToAddRows = false;
            this.dgvSummaries.AllowUserToDeleteRows = false;
            this.dgvSummaries.AllowUserToResizeColumns = false;
            this.dgvSummaries.AllowUserToResizeRows = false;
            this.dgvSummaries.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvSummaries.BackgroundColor = System.Drawing.Color.White;
            this.dgvSummaries.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.dgvSummaries.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle4.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle4.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle4.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            dataGridViewCellStyle4.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle4.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle4.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle4.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvSummaries.DefaultCellStyle = dataGridViewCellStyle4;
            this.dgvSummaries.EnableHeadersVisualStyles = false;
            this.dgvSummaries.Location = new System.Drawing.Point(0, 381);
            this.dgvSummaries.Margin = new System.Windows.Forms.Padding(0);
            this.dgvSummaries.Name = "dgvSummaries";
            this.dgvSummaries.ReadOnly = true;
            this.dgvSummaries.RowHeadersWidth = 50;
            this.dgvSummaries.RowTemplate.Height = 25;
            this.dgvSummaries.Size = new System.Drawing.Size(812, 19);
            this.dgvSummaries.TabIndex = 5;
            // 
            // toolTip1
            // 
            this.toolTip1.ToolTipIcon = System.Windows.Forms.ToolTipIcon.Info;
            this.toolTip1.ToolTipTitle = "Info";
            // 
            // MyDataGridView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.Controls.Add(this.dgvSummaries);
            this.Controls.Add(this.groupPanel);
            this.Controls.Add(this.dataGridView1);
            this.DoubleBuffered = true;
            this.Name = "MyDataGridView";
            this.Size = new System.Drawing.Size(812, 400);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.groupPanel.ResumeLayout(false);
            this.groupPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvDrop)).EndInit();
            this.cmsGroup.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvSummaries)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Panel groupPanel;
        private System.Windows.Forms.DataGridView dgvSummaries;
        private ThemedDataGridView dataGridView1;
        private System.Windows.Forms.DataGridView dgvDrop;
        private System.Windows.Forms.ContextMenuStrip cmsGroup;
        private System.Windows.Forms.ToolStripMenuItem itemRemoveGrouping;
        private System.Windows.Forms.ToolStripMenuItem itemRemoveSorting;
        private System.Windows.Forms.Label dgvLabel;
        private CueTextBox tbSearch;
        private System.Windows.Forms.Label lbCnt;
        private System.Windows.Forms.ComboBox cbJumpToColumn;
        private System.Windows.Forms.Button btOpenInExcel;
        private System.Windows.Forms.Button btCopyAsExcel;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.Button btCopyAsText;
        private System.Windows.Forms.CheckBox cbAprox;
        private System.Windows.Forms.Button btDownload;
        private System.Windows.Forms.Button btRowView;
    }
}
