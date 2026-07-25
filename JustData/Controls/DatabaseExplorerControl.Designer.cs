using System.Drawing;
using System.Windows.Forms;
using DatabaseDataGridView.WinForms;
using JustyBaseLegacy.AdditionalForms;
using AppBase.Common;

namespace JustyBaseLegacy.UI.Controls
{
    partial class DatabaseExplorerControl
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
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
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            splitContainerBase = new SplitContainer();
            panelTreeContainer = new Panel();
            databaseTreeView = new TreeView();
            panelControlsContainer = new Panel();
            cbWhatDb = new ComboBox();
            cbSearchDb = new ComboBox();
            panelSchemaSearchContainer = new Panel();
            tbFastSchemaSearch = new TextBox();
            labelSchemaSearchIcon = new Label();
            panelSearchContainer = new Panel();
            textBoxDbSearch = new TextBox();
            labelSearchIcon = new Label();
            dgvFastDbBrowser = new ThemedDataGridView();
            Type = new DataGridViewTextBoxColumn();
            colName = new DataGridViewTextBoxColumn();
            databaseName = new DataGridViewTextBoxColumn();
            Description = new DataGridViewTextBoxColumn();
            colOwner = new DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)splitContainerBase).BeginInit();
            splitContainerBase.Panel1.SuspendLayout();
            splitContainerBase.Panel2.SuspendLayout();
            splitContainerBase.SuspendLayout();
            panelTreeContainer.SuspendLayout();
            panelControlsContainer.SuspendLayout();
            panelSchemaSearchContainer.SuspendLayout();
            panelSearchContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvFastDbBrowser).BeginInit();
            SuspendLayout();
            // 
            // splitContainerBase
            // 
            splitContainerBase.Dock = DockStyle.Fill;
            splitContainerBase.ImeMode = ImeMode.Hiragana;
            splitContainerBase.Location = new Point(0, 0);
            splitContainerBase.Name = "splitContainerBase";
            splitContainerBase.Orientation = Orientation.Horizontal;
            // 
            // splitContainerBase.Panel1
            // 
            splitContainerBase.Panel1.Controls.Add(panelTreeContainer);
            splitContainerBase.Panel1.Padding = new Padding(5);
            splitContainerBase.Panel1MinSize = 0;
            // 
            // splitContainerBase.Panel2
            // 
            splitContainerBase.Panel2.Controls.Add(dgvFastDbBrowser);
            splitContainerBase.Panel2Collapsed = true;
            splitContainerBase.Panel2MinSize = 0;
            splitContainerBase.Size = new Size(269, 647);
            splitContainerBase.SplitterDistance = 430;
            splitContainerBase.TabIndex = 1;
            // 
            // panelTreeContainer
            // 
            panelTreeContainer.Controls.Add(databaseTreeView);
            panelTreeContainer.Controls.Add(panelControlsContainer);
            panelTreeContainer.Dock = DockStyle.Fill;
            panelTreeContainer.Location = new Point(5, 5);
            panelTreeContainer.Name = "panelTreeContainer";
            panelTreeContainer.Size = new Size(259, 637);
            panelTreeContainer.TabIndex = 0;
            // 
            // databaseTreeView
            // 
            databaseTreeView.Dock = DockStyle.Fill;
            databaseTreeView.HotTracking = false;
            databaseTreeView.Indent = 16;
            databaseTreeView.ItemHeight = 22;
            databaseTreeView.Location = new Point(0, 0);
            databaseTreeView.Margin = new Padding(0);
            databaseTreeView.Name = "databaseTreeView";
            databaseTreeView.ShowLines = false;
            databaseTreeView.Size = new Size(259, 557);
            databaseTreeView.TabIndex = 1;
            // 
            // panelControlsContainer
            // 
            panelControlsContainer.Controls.Add(cbWhatDb);
            panelControlsContainer.Controls.Add(cbSearchDb);
            panelControlsContainer.Controls.Add(panelSchemaSearchContainer);
            panelControlsContainer.Controls.Add(panelSearchContainer);
            panelControlsContainer.Dock = DockStyle.Bottom;
            panelControlsContainer.Location = new Point(0, 557);
            panelControlsContainer.Name = "panelControlsContainer";
            panelControlsContainer.Size = new Size(259, 80);
            panelControlsContainer.TabIndex = 2;
            // 
            // cbWhatDb
            // 
            cbWhatDb.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            cbWhatDb.DropDownStyle = ComboBoxStyle.DropDownList;
            cbWhatDb.FormattingEnabled = true;
            cbWhatDb.Items.AddRange(new object[] { "all dbs" });
            cbWhatDb.Location = new Point(0, 3);
            cbWhatDb.Margin = new Padding(0);
            cbWhatDb.Name = "cbWhatDb";
            cbWhatDb.Size = new Size(259, 23);
            cbWhatDb.TabIndex = 0;
            // 
            // cbSearchDb
            // 
            cbSearchDb.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            cbSearchDb.DropDownStyle = ComboBoxStyle.DropDownList;
            cbSearchDb.FormattingEnabled = true;
            cbSearchDb.Items.AddRange(new object[] { "table/view/etc.", "table/view/etc. or column", "sources" });
            cbSearchDb.Location = new Point(0, 29);
            cbSearchDb.Margin = new Padding(0);
            cbSearchDb.Name = "cbSearchDb";
            cbSearchDb.Size = new Size(259, 23);
            cbSearchDb.TabIndex = 1;
            // 
            // panelSchemaSearchContainer
            // 
            panelSchemaSearchContainer.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            panelSchemaSearchContainer.BackColor = Color.White;
            panelSchemaSearchContainer.BorderStyle = BorderStyle.FixedSingle;
            panelSchemaSearchContainer.Controls.Add(tbFastSchemaSearch);
            panelSchemaSearchContainer.Controls.Add(labelSchemaSearchIcon);
            panelSchemaSearchContainer.Location = new Point(0, 55);
            panelSchemaSearchContainer.Name = "panelSchemaSearchContainer";
            panelSchemaSearchContainer.Padding = new Padding(2);
            panelSchemaSearchContainer.Size = new Size(259, 25);
            panelSchemaSearchContainer.TabIndex = 2;
            // 
            // tbFastSchemaSearch
            // 
            tbFastSchemaSearch.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            tbFastSchemaSearch.BorderStyle = BorderStyle.None;
            tbFastSchemaSearch.Font = new Font("Segoe UI", 9.75F);
            tbFastSchemaSearch.Location = new Point(22, 4);
            tbFastSchemaSearch.Name = "tbFastSchemaSearch";
            tbFastSchemaSearch.Size = new Size(233, 18);
            tbFastSchemaSearch.TabIndex = 1;
            // 
            // labelSchemaSearchIcon
            // 
            labelSchemaSearchIcon.Dock = DockStyle.Left;
            labelSchemaSearchIcon.Font = new Font("Segoe UI Symbol", 9F);
            labelSchemaSearchIcon.ForeColor = Color.Gray;
            labelSchemaSearchIcon.Location = new Point(2, 2);
            labelSchemaSearchIcon.Name = "labelSchemaSearchIcon";
            labelSchemaSearchIcon.Size = new Size(20, 19);
            labelSchemaSearchIcon.TabIndex = 0;
            labelSchemaSearchIcon.Text = "🔍";
            labelSchemaSearchIcon.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // panelSearchContainer
            // 
            panelSearchContainer.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            panelSearchContainer.BackColor = Color.White;
            panelSearchContainer.BorderStyle = BorderStyle.FixedSingle;
            panelSearchContainer.Controls.Add(textBoxDbSearch);
            panelSearchContainer.Controls.Add(labelSearchIcon);
            panelSearchContainer.Location = new Point(0, 54);
            panelSearchContainer.Name = "panelSearchContainer";
            panelSearchContainer.Padding = new Padding(2);
            panelSearchContainer.Size = new Size(259, 26);
            panelSearchContainer.TabIndex = 3;
            // 
            // textBoxDbSearch
            // 
            textBoxDbSearch.Location = new Point(0, 0);
            textBoxDbSearch.Name = "textBoxDbSearch";
            textBoxDbSearch.Size = new Size(100, 23);
            textBoxDbSearch.TabIndex = 0;
            // 
            // labelSearchIcon
            // 
            labelSearchIcon.Dock = DockStyle.Left;
            labelSearchIcon.Font = new Font("Segoe UI Symbol", 9F);
            labelSearchIcon.ForeColor = Color.Gray;
            labelSearchIcon.Location = new Point(2, 2);
            labelSearchIcon.Name = "labelSearchIcon";
            labelSearchIcon.Size = new Size(20, 20);
            labelSearchIcon.TabIndex = 0;
            labelSearchIcon.Text = "🔍";
            labelSearchIcon.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // dgvFastDbBrowser
            // 
            dgvFastDbBrowser.AllowUserToAddRows = false;
            dgvFastDbBrowser.AllowUserToDeleteRows = false;
            dgvFastDbBrowser.AllowUserToResizeRows = false;
            dgvFastDbBrowser.BackgroundColor = SystemColors.Window;
            dgvFastDbBrowser.BorderStyle = BorderStyle.None;
            dgvFastDbBrowser.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgvFastDbBrowser.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvFastDbBrowser.Columns.AddRange(new DataGridViewColumn[] { Type, colName, databaseName, Description, colOwner });
            dgvFastDbBrowser.Dock = DockStyle.Fill;
            dgvFastDbBrowser.Location = new Point(0, 0);
            dgvFastDbBrowser.MultiSelect = false;
            dgvFastDbBrowser.Name = "dgvFastDbBrowser";
            dgvFastDbBrowser.ReadOnly = true;
            dgvFastDbBrowser.RowHeadersVisible = false;
            dgvFastDbBrowser.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvFastDbBrowser.Size = new Size(150, 46);
            dgvFastDbBrowser.TabIndex = 0;
            dgvFastDbBrowser.VirtualMode = true;
            // 
            // Type
            // 
            Type.HeaderText = "Type";
            Type.Name = "Type";
            Type.ReadOnly = true;
            Type.Width = 60;
            // 
            // colName
            // 
            colName.HeaderText = "Name";
            colName.Name = "colName";
            colName.ReadOnly = true;
            colName.Width = 120;
            // 
            // databaseName
            // 
            databaseName.HeaderText = "Database";
            databaseName.Name = "databaseName";
            databaseName.ReadOnly = true;
            databaseName.Width = 80;
            // 
            // Description
            // 
            Description.HeaderText = "Description";
            Description.Name = "Description";
            Description.ReadOnly = true;
            Description.Width = 150;
            // 
            // colOwner
            // 
            colOwner.HeaderText = "Owner";
            colOwner.Name = "colOwner";
            colOwner.ReadOnly = true;
            colOwner.Width = 80;
            // 
            // DatabaseExplorerControl
            // 
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            Controls.Add(splitContainerBase);
            Name = "DatabaseExplorerControl";
            Size = new Size(269, 647);
            splitContainerBase.Panel1.ResumeLayout(false);
            splitContainerBase.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainerBase).EndInit();
            splitContainerBase.ResumeLayout(false);
            panelTreeContainer.ResumeLayout(false);
            panelControlsContainer.ResumeLayout(false);
            panelSchemaSearchContainer.ResumeLayout(false);
            panelSchemaSearchContainer.PerformLayout();
            panelSearchContainer.ResumeLayout(false);
            panelSearchContainer.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvFastDbBrowser).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private SplitContainer splitContainerBase;
        private Panel panelTreeContainer;
        private TreeView databaseTreeView;
        private Panel panelControlsContainer;
        private ComboBox cbWhatDb;
        private ComboBox cbSearchDb;
        private Panel panelSchemaSearchContainer;
        private Label labelSchemaSearchIcon;
        private TextBox tbFastSchemaSearch;
        private Panel panelSearchContainer;
        private Label labelSearchIcon;
        private TextBox textBoxDbSearch;
        private ThemedDataGridView dgvFastDbBrowser;
        private DataGridViewTextBoxColumn Type;
        private DataGridViewTextBoxColumn colName;
        private DataGridViewTextBoxColumn databaseName;
        private DataGridViewTextBoxColumn Description;
        private DataGridViewTextBoxColumn colOwner;
    }
}
