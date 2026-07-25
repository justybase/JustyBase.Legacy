namespace JustyBaseLegacy.UI.Controls
{
    partial class VariablesControl
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Panel _headerPanel;
        private System.Windows.Forms.Label _titleLabel;
        private System.Windows.Forms.Button _btClearVariables;
        private System.Windows.Forms.DataGridView _dgvVariables;
        private System.Windows.Forms.DataGridViewTextBoxColumn _nameColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn _valueColumn;

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
            components = new System.ComponentModel.Container();
            _headerPanel = new System.Windows.Forms.Panel();
            _titleLabel = new System.Windows.Forms.Label();
            _btClearVariables = new System.Windows.Forms.Button();
            _dgvVariables = new System.Windows.Forms.DataGridView();
            _nameColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            _valueColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();

            ((System.ComponentModel.ISupportInitialize)(_dgvVariables)).BeginInit();
            _headerPanel.SuspendLayout();
            SuspendLayout();
            //
            // _headerPanel
            //
            _headerPanel.Dock = System.Windows.Forms.DockStyle.Top;
            _headerPanel.Name = "_headerPanel";
            _headerPanel.Padding = new System.Windows.Forms.Padding(8, 4, 8, 4);
            _headerPanel.Size = new System.Drawing.Size(275, 34);
            _headerPanel.TabIndex = 0;
            //
            // _titleLabel
            //
            _titleLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            _titleLabel.Name = "_titleLabel";
            _titleLabel.Padding = new System.Windows.Forms.Padding(2, 0, 0, 0);
            _titleLabel.Size = new System.Drawing.Size(152, 26);
            _titleLabel.TabIndex = 0;
            _titleLabel.Text = "Locals";
            _titleLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // _btClearVariables
            //
            _btClearVariables.Dock = System.Windows.Forms.DockStyle.Right;
            _btClearVariables.FlatAppearance.BorderSize = 0;
            _btClearVariables.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            _btClearVariables.Name = "_btClearVariables";
            _btClearVariables.Size = new System.Drawing.Size(62, 26);
            _btClearVariables.TabIndex = 2;
            _btClearVariables.Text = "Clear";
            _btClearVariables.UseVisualStyleBackColor = false;
            //
            // _dgvVariables
            //
            _dgvVariables.AllowUserToAddRows = false;
            _dgvVariables.AllowUserToDeleteRows = false;
            _dgvVariables.AllowUserToResizeRows = false;
            _dgvVariables.BackgroundColor = System.Drawing.SystemColors.Window;
            _dgvVariables.BorderStyle = System.Windows.Forms.BorderStyle.None;
            _dgvVariables.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
            _dgvVariables.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            _dgvVariables.ColumnHeadersVisible = true;
            _dgvVariables.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            _nameColumn,
            _valueColumn});
            _dgvVariables.Dock = System.Windows.Forms.DockStyle.Fill;
            _dgvVariables.Location = new System.Drawing.Point(0, 34);
            _dgvVariables.MultiSelect = false;
            _dgvVariables.Name = "dgvVariables";
            _dgvVariables.ReadOnly = true;
            _dgvVariables.RowHeadersVisible = false;
            _dgvVariables.RowTemplate.Height = 24;
            _dgvVariables.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            _dgvVariables.Size = new System.Drawing.Size(275, 619);
            _dgvVariables.TabIndex = 1;
            _dgvVariables.VirtualMode = true;
            //
            // _nameColumn
            //
            _nameColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            _nameColumn.FillWeight = 42F;
            _nameColumn.HeaderText = "Name";
            _nameColumn.MinimumWidth = 90;
            _nameColumn.Name = "Name";
            _nameColumn.ReadOnly = true;
            _nameColumn.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            _nameColumn.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            //
            // _valueColumn
            //
            _valueColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            _valueColumn.FillWeight = 58F;
            _valueColumn.HeaderText = "Value";
            _valueColumn.MinimumWidth = 110;
            _valueColumn.Name = "Value";
            _valueColumn.ReadOnly = true;
            _valueColumn.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            _valueColumn.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            //
            // VariablesControl
            // 
            // VariablesControl
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            BackColor = System.Drawing.SystemColors.Control;
            DoubleBuffered = true;
            Controls.Add(_dgvVariables);
            Controls.Add(_headerPanel);
            Name = "VariablesControl";
            Size = new System.Drawing.Size(275, 653);
            _headerPanel.Controls.Add(_titleLabel);
            _headerPanel.Controls.Add(_btClearVariables);
            _headerPanel.ResumeLayout(false);
            _headerPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(_dgvVariables)).EndInit();
            ResumeLayout(false);
        }

        #endregion
    }
}
