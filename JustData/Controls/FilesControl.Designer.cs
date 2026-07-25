namespace JustyBaseLegacy.UI.Controls
{
    partial class FilesControl
    {
        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            filesTreeView = new System.Windows.Forms.TreeView();
            textBoxFileSearch = new System.Windows.Forms.TextBox();
            panelSearchContainer = new System.Windows.Forms.Panel();
            labelSearchIcon = new System.Windows.Forms.Label();
            panelSearchContainer.SuspendLayout();
            SuspendLayout();
            // 
            // filesTreeView
            // 
            filesTreeView.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            filesTreeView.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            filesTreeView.HotTracking = false;
            filesTreeView.Indent = 16;
            filesTreeView.ItemHeight = 22;
            filesTreeView.Location = new System.Drawing.Point(8, 8);
            filesTreeView.Name = "filesTreeView";
            filesTreeView.ShowLines = false;
            filesTreeView.Size = new System.Drawing.Size(184, 134);
            filesTreeView.TabIndex = 0;
            // 
            // textBoxFileSearch
            // 
            textBoxFileSearch.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            textBoxFileSearch.BorderStyle = System.Windows.Forms.BorderStyle.None;
            textBoxFileSearch.Font = new System.Drawing.Font("Segoe UI", 9F);
            textBoxFileSearch.ForeColor = System.Drawing.Color.Gray;
            textBoxFileSearch.Location = new System.Drawing.Point(22, 5);
            textBoxFileSearch.Name = "textBoxFileSearch";
            textBoxFileSearch.Size = new System.Drawing.Size(158, 16);
            textBoxFileSearch.TabIndex = 1;
            textBoxFileSearch.Text = "Search files...";
            // 
            // panelSearchContainer
            // 
            panelSearchContainer.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            panelSearchContainer.BackColor = System.Drawing.Color.White;
            panelSearchContainer.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            panelSearchContainer.Controls.Add(textBoxFileSearch);
            panelSearchContainer.Controls.Add(labelSearchIcon);
            panelSearchContainer.Location = new System.Drawing.Point(8, 148);
            panelSearchContainer.Name = "panelSearchContainer";
            panelSearchContainer.Padding = new System.Windows.Forms.Padding(2);
            panelSearchContainer.Size = new System.Drawing.Size(184, 26);
            panelSearchContainer.TabIndex = 2;
            // 
            // labelSearchIcon
            // 
            labelSearchIcon.Dock = System.Windows.Forms.DockStyle.Left;
            labelSearchIcon.Font = new System.Drawing.Font("Segoe UI Symbol", 9F);
            labelSearchIcon.ForeColor = System.Drawing.Color.Gray;
            labelSearchIcon.Location = new System.Drawing.Point(2, 2);
            labelSearchIcon.Name = "labelSearchIcon";
            labelSearchIcon.Size = new System.Drawing.Size(20, 20);
            labelSearchIcon.TabIndex = 0;
            labelSearchIcon.Text = "🔍";
            labelSearchIcon.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // FilesControl
            // 
            AllowDrop = true;
            AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            BackColor = System.Drawing.SystemColors.Control;
            Controls.Add(filesTreeView);
            Controls.Add(panelSearchContainer);
            Name = "FilesControl";
            Padding = new System.Windows.Forms.Padding(5);
            Size = new System.Drawing.Size(200, 179);
            DoubleBuffered = true;
            panelSearchContainer.ResumeLayout(false);
            panelSearchContainer.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        public System.Windows.Forms.TreeView filesTreeView;
        public System.Windows.Forms.TextBox textBoxFileSearch;
        private System.Windows.Forms.Panel panelSearchContainer;
        private System.Windows.Forms.Label labelSearchIcon;
    }
}
