namespace JustDataAdditionalForms
{
	partial class AddColumnForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AddColumnForm));
            linkLabel1 = new LinkLabel();
            tbName = new TextBox();
            label1 = new Label();
            label2 = new Label();
            cbDataType = new ComboBox();
            numPrec = new NumericUpDown();
            labelPrec = new Label();
            numScale = new NumericUpDown();
            labelScale = new Label();
            numLen = new NumericUpDown();
            labelLen = new Label();
            btSave = new Button();
            btCancel = new Button();
            cbAllowNulls = new CheckBox();
            tbDefault = new TextBox();
            lbDefault = new Label();
            lbWarningDefault = new Label();
            ((System.ComponentModel.ISupportInitialize)numPrec).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numScale).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numLen).BeginInit();
            SuspendLayout();
            // 
            // linkLabel1
            // 
            linkLabel1.AutoSize = true;
            linkLabel1.Location = new Point(12, 177);
            linkLabel1.Name = "linkLabel1";
            linkLabel1.Size = new Size(33, 15);
            linkLabel1.TabIndex = 0;
            linkLabel1.TabStop = true;
            linkLabel1.Text = "Docs";
            linkLabel1.LinkClicked += linkLabel1_LinkClicked;
            // 
            // tbName
            // 
            tbName.Location = new Point(115, 12);
            tbName.Name = "tbName";
            tbName.Size = new Size(241, 23);
            tbName.TabIndex = 1;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(11, 15);
            label1.Name = "label1";
            label1.Size = new Size(39, 15);
            label1.TabIndex = 2;
            label1.Text = "Name";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(11, 49);
            label2.Name = "label2";
            label2.Size = new Size(59, 15);
            label2.TabIndex = 3;
            label2.Text = "Data Type";
            // 
            // cbDataType
            // 
            cbDataType.AutoCompleteCustomSource.AddRange(new string[] { "INTEGER", "BIGINT", "CHAR", "VARCHAR", "NVARCHAR", "DATE", "TIMESTAMP", "FLOAT", "DOUBLE", "NUMERIC" });
            cbDataType.FormattingEnabled = true;
            cbDataType.Items.AddRange(new object[] { "INTEGER", "BIGINT", "SMALLINT", "BYTEINT", "CHAR", "VARCHAR", "NVARCHAR", "DATE", "TIME", "TIMESTAMP", "INTERVAL", "FLOAT", "DOUBLE", "REAL", "NUMERIC", "BOOLEAN" });
            cbDataType.Location = new Point(115, 41);
            cbDataType.MaximumSize = new Size(241, 0);
            cbDataType.MinimumSize = new Size(241, 0);
            cbDataType.Name = "cbDataType";
            cbDataType.Size = new Size(241, 23);
            cbDataType.TabIndex = 4;
            cbDataType.SelectedIndexChanged += cbDataType_SelectedIndexChanged;
            // 
            // numPrec
            // 
            numPrec.Enabled = false;
            numPrec.Location = new Point(115, 68);
            numPrec.Maximum = new decimal(new int[] { 38, 0, 0, 0 });
            numPrec.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numPrec.Name = "numPrec";
            numPrec.Size = new Size(67, 23);
            numPrec.TabIndex = 5;
            numPrec.Value = new decimal(new int[] { 20, 0, 0, 0 });
            // 
            // labelPrec
            // 
            labelPrec.AutoSize = true;
            labelPrec.Enabled = false;
            labelPrec.Location = new Point(11, 76);
            labelPrec.Name = "labelPrec";
            labelPrec.Size = new Size(55, 15);
            labelPrec.TabIndex = 6;
            labelPrec.Text = "Precision";
            // 
            // numScale
            // 
            numScale.Enabled = false;
            numScale.Location = new Point(289, 68);
            numScale.Maximum = new decimal(new int[] { 9, 0, 0, 0 });
            numScale.Name = "numScale";
            numScale.Size = new Size(67, 23);
            numScale.TabIndex = 5;
            numScale.Value = new decimal(new int[] { 4, 0, 0, 0 });
            // 
            // labelScale
            // 
            labelScale.AutoSize = true;
            labelScale.Enabled = false;
            labelScale.Location = new Point(208, 76);
            labelScale.Name = "labelScale";
            labelScale.Size = new Size(34, 15);
            labelScale.TabIndex = 6;
            labelScale.Text = "Scale";
            // 
            // numLen
            // 
            numLen.Enabled = false;
            numLen.Location = new Point(115, 97);
            numLen.Maximum = new decimal(new int[] { 64000, 0, 0, 0 });
            numLen.Name = "numLen";
            numLen.Size = new Size(67, 23);
            numLen.TabIndex = 5;
            numLen.ThousandsSeparator = true;
            numLen.Value = new decimal(new int[] { 50, 0, 0, 0 });
            // 
            // labelLen
            // 
            labelLen.AutoSize = true;
            labelLen.Enabled = false;
            labelLen.Location = new Point(12, 105);
            labelLen.Name = "labelLen";
            labelLen.Size = new Size(44, 15);
            labelLen.TabIndex = 6;
            labelLen.Text = "Length";
            // 
            // btSave
            // 
            btSave.Location = new Point(208, 172);
            btSave.Name = "btSave";
            btSave.Size = new Size(75, 23);
            btSave.TabIndex = 7;
            btSave.Text = "Save";
            btSave.UseVisualStyleBackColor = true;
            btSave.Click += btSave_Click;
            // 
            // btCancel
            // 
            btCancel.Location = new Point(289, 173);
            btCancel.Name = "btCancel";
            btCancel.Size = new Size(75, 23);
            btCancel.TabIndex = 8;
            btCancel.Text = "Cancel";
            btCancel.UseVisualStyleBackColor = true;
            btCancel.Click += btCancel_Click;
            // 
            // cbAllowNulls
            // 
            cbAllowNulls.AutoSize = true;
            cbAllowNulls.Checked = true;
            cbAllowNulls.CheckState = CheckState.Checked;
            cbAllowNulls.Location = new Point(12, 133);
            cbAllowNulls.Name = "cbAllowNulls";
            cbAllowNulls.Size = new Size(79, 19);
            cbAllowNulls.TabIndex = 9;
            cbAllowNulls.Text = "allow nuls";
            cbAllowNulls.UseVisualStyleBackColor = true;
            cbAllowNulls.CheckedChanged += cbAllowNulls_CheckedChanged;
            // 
            // tbDefault
            // 
            tbDefault.Enabled = false;
            tbDefault.Location = new Point(208, 129);
            tbDefault.Name = "tbDefault";
            tbDefault.Size = new Size(148, 23);
            tbDefault.TabIndex = 10;
            tbDefault.TextChanged += tbDefault_TextChanged;
            // 
            // lbDefault
            // 
            lbDefault.AutoSize = true;
            lbDefault.Enabled = false;
            lbDefault.Location = new Point(144, 134);
            lbDefault.Name = "lbDefault";
            lbDefault.Size = new Size(44, 15);
            lbDefault.TabIndex = 11;
            lbDefault.Text = "default";
            // 
            // lbWarningDefault
            // 
            lbWarningDefault.AutoSize = true;
            lbWarningDefault.ForeColor = Color.Red;
            lbWarningDefault.Location = new Point(12, 155);
            lbWarningDefault.Name = "lbWarningDefault";
            lbWarningDefault.Size = new Size(115, 15);
            lbWarningDefault.TabIndex = 12;
            lbWarningDefault.Text = "specify default value";
            lbWarningDefault.Visible = false;
            // 
            // AddColumnForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(368, 201);
            Controls.Add(lbWarningDefault);
            Controls.Add(lbDefault);
            Controls.Add(tbDefault);
            Controls.Add(cbAllowNulls);
            Controls.Add(btCancel);
            Controls.Add(btSave);
            Controls.Add(labelPrec);
            Controls.Add(numPrec);
            Controls.Add(labelLen);
            Controls.Add(numLen);
            Controls.Add(labelScale);
            Controls.Add(numScale);
            Controls.Add(cbDataType);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(tbName);
            Controls.Add(linkLabel1);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "AddColumnForm";
            ShowInTaskbar = false;
            Text = "Add column";
            ((System.ComponentModel.ISupportInitialize)numPrec).EndInit();
            ((System.ComponentModel.ISupportInitialize)numScale).EndInit();
            ((System.ComponentModel.ISupportInitialize)numLen).EndInit();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.LinkLabel linkLabel1;
		private System.Windows.Forms.TextBox tbName;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.ComboBox cbDataType;
		private System.Windows.Forms.NumericUpDown numPrec;
		private System.Windows.Forms.Label labelPrec;
		private System.Windows.Forms.NumericUpDown numScale;
		private System.Windows.Forms.Label labelScale;
		private System.Windows.Forms.NumericUpDown numLen;
		private System.Windows.Forms.Label labelLen;
		private System.Windows.Forms.Button btSave;
		private System.Windows.Forms.Button btCancel;
		private System.Windows.Forms.CheckBox cbAllowNulls;
		private System.Windows.Forms.TextBox tbDefault;
		private System.Windows.Forms.Label lbDefault;
		private System.Windows.Forms.Label lbWarningDefault;
	}
}
