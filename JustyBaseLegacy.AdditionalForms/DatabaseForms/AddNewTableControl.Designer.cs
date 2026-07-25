namespace JustyBaseLegacy.UI.DbForms
{
    partial class AddNewTableControl
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
            this.dgvAddNewTable = new System.Windows.Forms.DataGridView();
            this.btCreate = new System.Windows.Forms.Button();
            this.btCancel = new System.Windows.Forms.Button();
            this.lbDocs = new System.Windows.Forms.LinkLabel();
            this.tbName = new System.Windows.Forms.TextBox();
            this.tbTableDesc = new System.Windows.Forms.TextBox();
            this.labelDesc = new System.Windows.Forms.Label();
            this.ClName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ClDataType = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.ClAllowNulls = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.ClPk = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ClDist = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ClDesc = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.dgvAddNewTable)).BeginInit();
            this.SuspendLayout();
            // 
            // dgvAddNewTable
            // 
            this.dgvAddNewTable.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvAddNewTable.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvAddNewTable.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.ClName,
            this.ClDataType,
            this.ClAllowNulls,
            this.ClPk,
            this.ClDist,
            this.ClDesc});
            this.dgvAddNewTable.GridColor = System.Drawing.SystemColors.Control;
            this.dgvAddNewTable.Location = new System.Drawing.Point(0, 31);
            this.dgvAddNewTable.Name = "dgvAddNewTable";
            this.dgvAddNewTable.RowTemplate.Height = 25;
            this.dgvAddNewTable.Size = new System.Drawing.Size(1234, 279);
            this.dgvAddNewTable.TabIndex = 0;
            this.dgvAddNewTable.DefaultValuesNeeded += new System.Windows.Forms.DataGridViewRowEventHandler(this.dgvAddNewTable_DefaultValuesNeeded);
            // 
            // btCreate
            // 
            this.btCreate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btCreate.Location = new System.Drawing.Point(1065, 333);
            this.btCreate.Name = "btCreate";
            this.btCreate.Size = new System.Drawing.Size(75, 23);
            this.btCreate.TabIndex = 1;
            this.btCreate.Text = "Create";
            this.btCreate.UseVisualStyleBackColor = true;
            this.btCreate.Click += new System.EventHandler(this.btCreate_Click);
            // 
            // btCancel
            // 
            this.btCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btCancel.Location = new System.Drawing.Point(1146, 333);
            this.btCancel.Name = "btCancel";
            this.btCancel.Size = new System.Drawing.Size(75, 23);
            this.btCancel.TabIndex = 1;
            this.btCancel.Text = "Cancel";
            this.btCancel.UseVisualStyleBackColor = true;
            this.btCancel.Click += new System.EventHandler(this.btCancel_Click);
            // 
            // lbDocs
            // 
            this.lbDocs.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lbDocs.AutoSize = true;
            this.lbDocs.Location = new System.Drawing.Point(7, 350);
            this.lbDocs.Name = "lbDocs";
            this.lbDocs.Size = new System.Drawing.Size(33, 15);
            this.lbDocs.TabIndex = 2;
            this.lbDocs.TabStop = true;
            this.lbDocs.Text = "Docs";
            this.lbDocs.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lbDocs_LinkClicked);
            // 
            // tbName
            // 
            this.tbName.Location = new System.Drawing.Point(7, 3);
            this.tbName.Name = "tbName";
            this.tbName.Size = new System.Drawing.Size(222, 23);
            this.tbName.TabIndex = 3;
            this.tbName.Text = "NEW_TABLE_NAME";
            // 
            // tbTableDesc
            // 
            this.tbTableDesc.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbTableDesc.Location = new System.Drawing.Point(221, 316);
            this.tbTableDesc.Multiline = true;
            this.tbTableDesc.Name = "tbTableDesc";
            this.tbTableDesc.Size = new System.Drawing.Size(838, 40);
            this.tbTableDesc.TabIndex = 4;
            // 
            // labelDesc
            // 
            this.labelDesc.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelDesc.AutoSize = true;
            this.labelDesc.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.labelDesc.Location = new System.Drawing.Point(114, 341);
            this.labelDesc.Name = "labelDesc";
            this.labelDesc.Size = new System.Drawing.Size(101, 15);
            this.labelDesc.TabIndex = 5;
            this.labelDesc.Text = "Table description";
            // 
            // ClName
            // 
            this.ClName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.ClName.HeaderText = "Column Name";
            this.ClName.Name = "ClName";
            // 
            // ClDataType
            // 
            this.ClDataType.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.ClDataType.HeaderText = "Data Type";
            this.ClDataType.Items.AddRange(new object[] {
            "INTEGER",
            "BIGINT",
            "SMALLINT",
            "BYTEINT",
            "CHAR",
            "VARCHAR(50)",
            "NVARCHAR(50)",
            "DATE",
            "TIME",
            "TIMESTAMP",
            "INTERVAL",
            "FLOAT",
            "DOUBLE",
            "REAL",
            "NUMERIC(16,4)",
            "BOOLEAN"});
            this.ClDataType.Name = "ClDataType";
            // 
            // ClAllowNulls
            // 
            this.ClAllowNulls.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.ClAllowNulls.FillWeight = 40F;
            this.ClAllowNulls.HeaderText = "Allow Nulls";
            this.ClAllowNulls.Name = "ClAllowNulls";
            // 
            // ClPk
            // 
            this.ClPk.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.ClPk.FillWeight = 40F;
            this.ClPk.HeaderText = "# Primary key";
            this.ClPk.Name = "ClPk";
            this.ClPk.ToolTipText = "number of column in primary key";
            // 
            // ClDist
            // 
            this.ClDist.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.ClDist.FillWeight = 40F;
            this.ClDist.HeaderText = "#Distribution";
            this.ClDist.Name = "ClDist";
            this.ClDist.ToolTipText = "number of column in distribution";
            // 
            // ClDesc
            // 
            this.ClDesc.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.ClDesc.HeaderText = "Description";
            this.ClDesc.Name = "ClDesc";
            // 
            // AddNewTableControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.labelDesc);
            this.Controls.Add(this.tbTableDesc);
            this.Controls.Add(this.tbName);
            this.Controls.Add(this.lbDocs);
            this.Controls.Add(this.btCancel);
            this.Controls.Add(this.btCreate);
            this.Controls.Add(this.dgvAddNewTable);
            this.DoubleBuffered = true;
            this.Name = "AddNewTableControl";
            this.Size = new System.Drawing.Size(1234, 371);
            ((System.ComponentModel.ISupportInitialize)(this.dgvAddNewTable)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataGridView dgvAddNewTable;
        private System.Windows.Forms.Button btCreate;
        private System.Windows.Forms.Button btCancel;
        private System.Windows.Forms.LinkLabel lbDocs;
        private System.Windows.Forms.TextBox tbName;
        private System.Windows.Forms.TextBox tbTableDesc;
        private System.Windows.Forms.Label labelDesc;
        private System.Windows.Forms.DataGridViewTextBoxColumn ClName;
        private System.Windows.Forms.DataGridViewComboBoxColumn ClDataType;
        private System.Windows.Forms.DataGridViewCheckBoxColumn ClAllowNulls;
        private System.Windows.Forms.DataGridViewTextBoxColumn ClPk;
        private System.Windows.Forms.DataGridViewTextBoxColumn ClDist;
        private System.Windows.Forms.DataGridViewTextBoxColumn ClDesc;
    }
}
