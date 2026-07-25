namespace JustyBaseLegacy.UI.DbForms
{
    partial class NewProcedureNetezza
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NewProcedureNetezza));
            btOK = new Button();
            btCancel = new Button();
            cbDataType = new ComboBox();
            labelDataType = new Label();
            groupArgs = new GroupBox();
            dataGridView1 = new ThemedDataGridView();
            Col1 = new DataGridViewTextBoxColumn();
            Col2 = new DataGridViewTextBoxColumn();
            Col3 = new DataGridViewComboBoxColumn();
            tbProcName = new TextBox();
            label1 = new Label();
            cbCaller = new CheckBox();
            Docs = new LinkLabel();
            groupArgs.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            SuspendLayout();
            // 
            // btOK
            // 
            btOK.Location = new Point(422, 332);
            btOK.Name = "btOK";
            btOK.Size = new Size(75, 23);
            btOK.TabIndex = 1;
            btOK.Text = "OK";
            btOK.UseVisualStyleBackColor = true;
            btOK.Click += btOK_Click;
            // 
            // btCancel
            // 
            btCancel.Location = new Point(503, 332);
            btCancel.Name = "btCancel";
            btCancel.Size = new Size(75, 23);
            btCancel.TabIndex = 1;
            btCancel.Text = "Cancel";
            btCancel.UseVisualStyleBackColor = true;
            btCancel.Click += btCancel_Click;
            // 
            // cbDataType
            // 
            cbDataType.FormattingEnabled = true;
            cbDataType.Items.AddRange(new object[] { "INTEGER", "REFTABLE(ENTER_TABLE_NAME)", "BIGINT", "SMALLINT", "BYTEINT", "CHAR", "VARCHAR(50)", "NVARCHAR(50)", "DATE", "TIME", "TIMESTAMP", "INTERVAL", "FLOAT", "DOUBLE", "REAL", "NUMERIC(16,4)", "BOOLEAN" });
            cbDataType.Location = new Point(388, 297);
            cbDataType.Name = "cbDataType";
            cbDataType.Size = new Size(190, 23);
            cbDataType.TabIndex = 3;
            cbDataType.Text = "INTEGER";
            // 
            // labelDataType
            // 
            labelDataType.AutoSize = true;
            labelDataType.Location = new Point(321, 305);
            labelDataType.Name = "labelDataType";
            labelDataType.Size = new Size(47, 15);
            labelDataType.TabIndex = 1;
            labelDataType.Text = "Returns";
            // 
            // groupArgs
            // 
            groupArgs.Controls.Add(dataGridView1);
            groupArgs.Location = new Point(12, 51);
            groupArgs.Name = "groupArgs";
            groupArgs.Size = new Size(569, 235);
            groupArgs.TabIndex = 2;
            groupArgs.TabStop = false;
            groupArgs.Text = "Arguments";
            // 
            // dataGridView1
            // 
            dataGridView1.BackgroundColor = SystemColors.Control;
            dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView1.Columns.AddRange(new DataGridViewColumn[] { Col1, Col2, Col3 });
            dataGridView1.Dock = DockStyle.Fill;
            dataGridView1.Location = new Point(3, 19);
            dataGridView1.Name = "dataGridView1";
            dataGridView1.Size = new Size(563, 213);
            dataGridView1.TabIndex = 0;
            dataGridView1.DefaultValuesNeeded += dataGridView1_DefaultValuesNeeded;
            // 
            // Col1
            // 
            Col1.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            Col1.FillWeight = 30F;
            Col1.HeaderText = "#";
            Col1.Name = "Col1";
            Col1.ReadOnly = true;
            // 
            // Col2
            // 
            Col2.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            Col2.HeaderText = "Name";
            Col2.Name = "Col2";
            // 
            // Col3
            // 
            Col3.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            Col3.HeaderText = "DataType";
            Col3.Items.AddRange(new object[] { "INTEGER", "BIGINT", "SMALLINT", "BYTEINT", "CHAR", "VARCHAR(50)", "NVARCHAR(50)", "DATE", "TIME", "TIMESTAMP", "INTERVAL", "FLOAT", "DOUBLE", "REAL", "NUMERIC(16,4)", "BOOLEAN" });
            Col3.Name = "Col3";
            // 
            // tbProcName
            // 
            tbProcName.Location = new Point(66, 9);
            tbProcName.Name = "tbProcName";
            tbProcName.Size = new Size(273, 23);
            tbProcName.TabIndex = 3;
            tbProcName.Text = "NEW_PROCEDURE";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 9F);
            label1.Location = new Point(12, 17);
            label1.Name = "label1";
            label1.Size = new Size(42, 15);
            label1.TabIndex = 4;
            label1.Text = "Name:";
            // 
            // cbCaller
            // 
            cbCaller.AutoSize = true;
            cbCaller.Checked = true;
            cbCaller.CheckState = CheckState.Checked;
            cbCaller.Location = new Point(368, 13);
            cbCaller.Name = "cbCaller";
            cbCaller.Size = new Size(113, 19);
            cbCaller.TabIndex = 5;
            cbCaller.Text = "Execute as Caller";
            cbCaller.UseVisualStyleBackColor = true;
            // 
            // Docs
            // 
            Docs.AutoSize = true;
            Docs.Location = new Point(12, 332);
            Docs.Name = "Docs";
            Docs.Size = new Size(33, 15);
            Docs.TabIndex = 6;
            Docs.TabStop = true;
            Docs.Text = "Docs";
            Docs.LinkClicked += Docs_LinkClicked;
            // 
            // NewProcedureNetezza
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(598, 358);
            Controls.Add(Docs);
            Controls.Add(cbDataType);
            Controls.Add(cbCaller);
            Controls.Add(labelDataType);
            Controls.Add(label1);
            Controls.Add(tbProcName);
            Controls.Add(groupArgs);
            Controls.Add(btCancel);
            Controls.Add(btOK);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MaximumSize = new Size(614, 397);
            MinimizeBox = false;
            MinimumSize = new Size(614, 397);
            Name = "NewProcedureNetezza";
            Text = "Create Procedure";
            groupArgs.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btOK;
        private System.Windows.Forms.Button btCancel;
        private System.Windows.Forms.GroupBox groupArgs;
        private ThemedDataGridView dataGridView1;
        private System.Windows.Forms.TextBox tbProcName;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckBox cbCaller;
        private System.Windows.Forms.ComboBox cbDataType;
        private System.Windows.Forms.Label labelDataType;
        private System.Windows.Forms.DataGridViewTextBoxColumn Col1;
        private System.Windows.Forms.DataGridViewTextBoxColumn Col2;
        private System.Windows.Forms.DataGridViewComboBoxColumn Col3;
        private System.Windows.Forms.LinkLabel Docs;
    }
}
