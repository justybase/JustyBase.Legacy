namespace JustyBaseLegacy.UI.DbForms
{
    partial class CreateSequenceNz
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CreateSequenceNz));
            linkLabel1 = new LinkLabel();
            tbName = new TextBox();
            cbDataType = new ComboBox();
            numStart = new NumericUpDown();
            numIncrement = new NumericUpDown();
            numMin = new NumericUpDown();
            numMax = new NumericUpDown();
            cbCycle = new CheckBox();
            label1 = new Label();
            label2 = new Label();
            label3 = new Label();
            label4 = new Label();
            label5 = new Label();
            label6 = new Label();
            button1 = new Button();
            button2 = new Button();
            cbNoMin = new CheckBox();
            cbNoMax = new CheckBox();
            ((System.ComponentModel.ISupportInitialize)numStart).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numIncrement).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numMin).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numMax).BeginInit();
            SuspendLayout();
            // 
            // linkLabel1
            // 
            linkLabel1.AutoSize = true;
            linkLabel1.Location = new Point(12, 247);
            linkLabel1.Name = "linkLabel1";
            linkLabel1.Size = new Size(33, 15);
            linkLabel1.TabIndex = 0;
            linkLabel1.TabStop = true;
            linkLabel1.Text = "Docs";
            linkLabel1.LinkClicked += linkLabel1_LinkClicked;
            // 
            // tbName
            // 
            tbName.Location = new Point(123, 11);
            tbName.Name = "tbName";
            tbName.Size = new Size(123, 23);
            tbName.TabIndex = 1;
            // 
            // cbDataType
            // 
            cbDataType.FormattingEnabled = true;
            cbDataType.Items.AddRange(new object[] { "BYTEINT", "SMALLINT", "INTEGER", "BIGINT" });
            cbDataType.Location = new Point(123, 43);
            cbDataType.Name = "cbDataType";
            cbDataType.Size = new Size(123, 23);
            cbDataType.TabIndex = 2;
            cbDataType.Text = "BIGINT";
            // 
            // numStart
            // 
            numStart.Location = new Point(123, 72);
            numStart.Name = "numStart";
            numStart.Size = new Size(123, 23);
            numStart.TabIndex = 3;
            numStart.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // numIncrement
            // 
            numIncrement.Location = new Point(123, 101);
            numIncrement.Name = "numIncrement";
            numIncrement.Size = new Size(123, 23);
            numIncrement.TabIndex = 3;
            numIncrement.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // numMin
            // 
            numMin.Location = new Point(124, 130);
            numMin.Name = "numMin";
            numMin.Size = new Size(63, 23);
            numMin.TabIndex = 3;
            // 
            // numMax
            // 
            numMax.Enabled = false;
            numMax.Location = new Point(123, 159);
            numMax.Name = "numMax";
            numMax.Size = new Size(64, 23);
            numMax.TabIndex = 3;
            // 
            // cbCycle
            // 
            cbCycle.AutoSize = true;
            cbCycle.Location = new Point(124, 198);
            cbCycle.Name = "cbCycle";
            cbCycle.Size = new Size(53, 19);
            cbCycle.TabIndex = 4;
            cbCycle.Text = "cycle";
            cbCycle.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 19);
            label1.Name = "label1";
            label1.Size = new Size(91, 15);
            label1.TabIndex = 5;
            label1.Text = "Sequence name";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(12, 80);
            label2.Name = "label2";
            label2.Size = new Size(57, 15);
            label2.TabIndex = 5;
            label2.Text = "Start with";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(12, 109);
            label3.Name = "label3";
            label3.Size = new Size(77, 15);
            label3.TabIndex = 5;
            label3.Text = "Increment by";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(13, 167);
            label4.Name = "label4";
            label4.Size = new Size(57, 15);
            label4.TabIndex = 5;
            label4.Text = "Maxvalue";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(12, 138);
            label5.Name = "label5";
            label5.Size = new Size(56, 15);
            label5.TabIndex = 5;
            label5.Text = "Minvalue";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(13, 51);
            label6.Name = "label6";
            label6.Size = new Size(57, 15);
            label6.TabIndex = 5;
            label6.Text = "Data type";
            // 
            // button1
            // 
            button1.Location = new Point(184, 239);
            button1.Name = "button1";
            button1.Size = new Size(75, 23);
            button1.TabIndex = 6;
            button1.Text = "Cancel";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // button2
            // 
            button2.Location = new Point(99, 239);
            button2.Name = "button2";
            button2.Size = new Size(75, 23);
            button2.TabIndex = 6;
            button2.Text = "Save";
            button2.UseVisualStyleBackColor = true;
            button2.Click += button2_Click;
            // 
            // cbNoMin
            // 
            cbNoMin.AutoSize = true;
            cbNoMin.Location = new Point(193, 134);
            cbNoMin.Name = "cbNoMin";
            cbNoMin.Size = new Size(64, 19);
            cbNoMin.TabIndex = 7;
            cbNoMin.Text = "no min";
            cbNoMin.UseVisualStyleBackColor = true;
            cbNoMin.CheckedChanged += cbNoMin_CheckedChanged;
            // 
            // cbNoMax
            // 
            cbNoMax.AutoSize = true;
            cbNoMax.Checked = true;
            cbNoMax.CheckState = CheckState.Checked;
            cbNoMax.Location = new Point(193, 166);
            cbNoMax.Name = "cbNoMax";
            cbNoMax.Size = new Size(65, 19);
            cbNoMax.TabIndex = 7;
            cbNoMax.Text = "no max";
            cbNoMax.UseVisualStyleBackColor = true;
            cbNoMax.CheckedChanged += cbNoMax_CheckedChanged;
            // 
            // CreateSequenceNz
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(271, 269);
            Controls.Add(cbNoMax);
            Controls.Add(cbNoMin);
            Controls.Add(button2);
            Controls.Add(button1);
            Controls.Add(label5);
            Controls.Add(label6);
            Controls.Add(label4);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(cbCycle);
            Controls.Add(numMax);
            Controls.Add(numMin);
            Controls.Add(numIncrement);
            Controls.Add(numStart);
            Controls.Add(cbDataType);
            Controls.Add(tbName);
            Controls.Add(linkLabel1);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MaximumSize = new Size(287, 308);
            MinimizeBox = false;
            MinimumSize = new Size(287, 308);
            Name = "CreateSequenceNz";
            Text = "Create sequence";
            ((System.ComponentModel.ISupportInitialize)numStart).EndInit();
            ((System.ComponentModel.ISupportInitialize)numIncrement).EndInit();
            ((System.ComponentModel.ISupportInitialize)numMin).EndInit();
            ((System.ComponentModel.ISupportInitialize)numMax).EndInit();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.LinkLabel linkLabel1;
        private System.Windows.Forms.TextBox tbName;
        private System.Windows.Forms.ComboBox cbDataType;
        private System.Windows.Forms.NumericUpDown numStart;
        private System.Windows.Forms.NumericUpDown numIncrement;
        private System.Windows.Forms.NumericUpDown numMin;
        private System.Windows.Forms.NumericUpDown numMax;
        private System.Windows.Forms.CheckBox cbCycle;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.CheckBox cbNoMin;
        private System.Windows.Forms.CheckBox cbNoMax;
    }
}
