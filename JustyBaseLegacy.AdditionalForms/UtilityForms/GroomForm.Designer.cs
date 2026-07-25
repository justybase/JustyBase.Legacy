namespace JustyBaseLegacy.UI
{
    partial class GroomForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GroomForm));
            groupBox1 = new GroupBox();
            cbOptions = new ComboBox();
            cbMode = new ComboBox();
            label2 = new Label();
            label1 = new Label();
            btGromOk = new Button();
            btGroomCancel = new Button();
            linkLabel1 = new LinkLabel();
            groupBox1.SuspendLayout();
            SuspendLayout();
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(cbOptions);
            groupBox1.Controls.Add(cbMode);
            groupBox1.Controls.Add(label2);
            groupBox1.Controls.Add(label1);
            groupBox1.Location = new Point(4, 2);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(368, 101);
            groupBox1.TabIndex = 0;
            groupBox1.TabStop = false;
            groupBox1.Text = "Specify Grooming Parametrs";
            // 
            // cbOptions
            // 
            cbOptions.FormattingEnabled = true;
            cbOptions.Items.AddRange(new object[] { "DEFAULT", "NONE", "specify <backupset id> here" });
            cbOptions.Location = new Point(124, 52);
            cbOptions.Name = "cbOptions";
            cbOptions.Size = new Size(239, 23);
            cbOptions.TabIndex = 1;
            // 
            // cbMode
            // 
            cbMode.DropDownStyle = ComboBoxStyle.DropDownList;
            cbMode.FormattingEnabled = true;
            cbMode.Items.AddRange(new object[] { "RECORDS ALL", "RECORDS READY", "PAGES ALL", "PAGES START", "VERSIONS" });
            cbMode.Location = new Point(124, 19);
            cbMode.Name = "cbMode";
            cbMode.Size = new Size(239, 23);
            cbMode.TabIndex = 1;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(8, 60);
            label2.Name = "label2";
            label2.Size = new Size(108, 15);
            label2.TabIndex = 0;
            label2.Text = "Backup Set Option:";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(8, 27);
            label1.Name = "label1";
            label1.Size = new Size(41, 15);
            label1.TabIndex = 0;
            label1.Text = "Mode:";
            // 
            // btGromOk
            // 
            btGromOk.Location = new Point(210, 109);
            btGromOk.Name = "btGromOk";
            btGromOk.Size = new Size(75, 23);
            btGromOk.TabIndex = 1;
            btGromOk.Text = "OK";
            btGromOk.UseVisualStyleBackColor = true;
            btGromOk.Click += btGromOk_Click;
            // 
            // btGroomCancel
            // 
            btGroomCancel.Location = new Point(293, 109);
            btGroomCancel.Name = "btGroomCancel";
            btGroomCancel.Size = new Size(75, 23);
            btGroomCancel.TabIndex = 2;
            btGroomCancel.Text = "Cancel";
            btGroomCancel.UseVisualStyleBackColor = true;
            btGroomCancel.Click += btGroomCancel_Click;
            // 
            // linkLabel1
            // 
            linkLabel1.AutoSize = true;
            linkLabel1.Location = new Point(12, 111);
            linkLabel1.Name = "linkLabel1";
            linkLabel1.Size = new Size(33, 15);
            linkLabel1.TabIndex = 3;
            linkLabel1.TabStop = true;
            linkLabel1.Text = "Docs";
            linkLabel1.LinkClicked += linkLabel1_LinkClicked;
            // 
            // GroomForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(377, 136);
            Controls.Add(linkLabel1);
            Controls.Add(btGroomCancel);
            Controls.Add(btGromOk);
            Controls.Add(groupBox1);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "GroomForm";
            ShowInTaskbar = false;
            Text = "Grooming Options";
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.ComboBox cbOptions;
        private System.Windows.Forms.ComboBox cbMode;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btGromOk;
        private System.Windows.Forms.Button btGroomCancel;
        private System.Windows.Forms.LinkLabel linkLabel1;
    }
}
