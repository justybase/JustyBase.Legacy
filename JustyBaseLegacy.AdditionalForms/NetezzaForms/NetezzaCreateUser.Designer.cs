namespace JustyBaseLegacy.UI.DbForms
{
    partial class NetezzaCreateUser
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NetezzaCreateUser));
            linkLabel1 = new LinkLabel();
            label1 = new Label();
            tbName = new TextBox();
            label2 = new Label();
            tbPassword = new TextBox();
            cbPasswordExpire = new CheckBox();
            label3 = new Label();
            label4 = new Label();
            numPassExpiry = new NumericUpDown();
            label5 = new Label();
            comboGroups = new ComboBox();
            linkLabel2 = new LinkLabel();
            btOk = new Button();
            btCancel = new Button();
            dateTimePickerValidUntil = new DateTimePicker();
            ((System.ComponentModel.ISupportInitialize)numPassExpiry).BeginInit();
            SuspendLayout();
            // 
            // linkLabel1
            // 
            linkLabel1.AutoSize = true;
            linkLabel1.Location = new Point(12, 426);
            linkLabel1.Name = "linkLabel1";
            linkLabel1.Size = new Size(33, 15);
            linkLabel1.TabIndex = 0;
            linkLabel1.TabStop = true;
            linkLabel1.Text = "Docs";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(23, 22);
            label1.Name = "label1";
            label1.Size = new Size(39, 15);
            label1.TabIndex = 1;
            label1.Text = "Name";
            // 
            // tbName
            // 
            tbName.Location = new Point(105, 14);
            tbName.Name = "tbName";
            tbName.Size = new Size(147, 23);
            tbName.TabIndex = 2;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(5, 50);
            label2.Name = "label2";
            label2.Size = new Size(57, 15);
            label2.TabIndex = 1;
            label2.Text = "Password";
            // 
            // tbPassword
            // 
            tbPassword.Location = new Point(106, 42);
            tbPassword.Name = "tbPassword";
            tbPassword.Size = new Size(147, 23);
            tbPassword.TabIndex = 2;
            tbPassword.UseSystemPasswordChar = true;
            // 
            // cbPasswordExpire
            // 
            cbPasswordExpire.AutoSize = true;
            cbPasswordExpire.Checked = true;
            cbPasswordExpire.CheckState = CheckState.Checked;
            cbPasswordExpire.Location = new Point(276, 46);
            cbPasswordExpire.Name = "cbPasswordExpire";
            cbPasswordExpire.Size = new Size(110, 19);
            cbPasswordExpire.TabIndex = 3;
            cbPasswordExpire.Text = "password expire";
            cbPasswordExpire.UseVisualStyleBackColor = true;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(5, 108);
            label3.Name = "label3";
            label3.Size = new Size(59, 15);
            label3.TabIndex = 1;
            label3.Text = "Valid unitl";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(4, 80);
            label4.Name = "label4";
            label4.Size = new Size(91, 15);
            label4.TabIndex = 1;
            label4.Text = "Password expiry";
            // 
            // numPassExpiry
            // 
            numPassExpiry.Location = new Point(106, 71);
            numPassExpiry.Maximum = new decimal(new int[] { 366, 0, 0, 0 });
            numPassExpiry.Minimum = new decimal(new int[] { 1, 0, 0, int.MinValue });
            numPassExpiry.Name = "numPassExpiry";
            numPassExpiry.Size = new Size(145, 23);
            numPassExpiry.TabIndex = 4;
            numPassExpiry.Value = new decimal(new int[] { 1, 0, 0, int.MinValue });
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(5, 142);
            label5.Name = "label5";
            label5.Size = new Size(52, 15);
            label5.TabIndex = 1;
            label5.Text = "In group";
            // 
            // comboGroups
            // 
            comboGroups.FormattingEnabled = true;
            comboGroups.Location = new Point(106, 134);
            comboGroups.Name = "comboGroups";
            comboGroups.Size = new Size(147, 23);
            comboGroups.TabIndex = 5;
            comboGroups.DropDown += comboGroups_DropDown;
            // 
            // linkLabel2
            // 
            linkLabel2.AutoSize = true;
            linkLabel2.Location = new Point(12, 207);
            linkLabel2.Name = "linkLabel2";
            linkLabel2.Size = new Size(33, 15);
            linkLabel2.TabIndex = 6;
            linkLabel2.TabStop = true;
            linkLabel2.Text = "Docs";
            linkLabel2.LinkClicked += linkLabel2_LinkClicked;
            // 
            // btOk
            // 
            btOk.Location = new Point(261, 196);
            btOk.Name = "btOk";
            btOk.Size = new Size(75, 23);
            btOk.TabIndex = 7;
            btOk.Text = "OK";
            btOk.UseVisualStyleBackColor = true;
            btOk.Click += btOk_Click;
            // 
            // btCancel
            // 
            btCancel.Location = new Point(342, 196);
            btCancel.Name = "btCancel";
            btCancel.Size = new Size(75, 23);
            btCancel.TabIndex = 7;
            btCancel.Text = "Cancel";
            btCancel.UseVisualStyleBackColor = true;
            btCancel.Click += btCancel_Click;
            // 
            // dateTimePickerValidUntil
            // 
            dateTimePickerValidUntil.Location = new Point(105, 102);
            dateTimePickerValidUntil.MinDate = new DateTime(1900, 1, 1, 0, 0, 0, 0);
            dateTimePickerValidUntil.Name = "dateTimePickerValidUntil";
            dateTimePickerValidUntil.Size = new Size(200, 23);
            dateTimePickerValidUntil.TabIndex = 8;
            dateTimePickerValidUntil.Value = new DateTime(1900, 1, 1, 0, 0, 0, 0);
            // 
            // NetezzaCreateUser
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(429, 231);
            Controls.Add(dateTimePickerValidUntil);
            Controls.Add(btCancel);
            Controls.Add(btOk);
            Controls.Add(linkLabel2);
            Controls.Add(comboGroups);
            Controls.Add(numPassExpiry);
            Controls.Add(cbPasswordExpire);
            Controls.Add(tbPassword);
            Controls.Add(label4);
            Controls.Add(tbName);
            Controls.Add(label5);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(linkLabel1);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MaximumSize = new Size(614, 397);
            MinimizeBox = false;
            Name = "NetezzaCreateUser";
            Text = "Create user";
            ((System.ComponentModel.ISupportInitialize)numPassExpiry).EndInit();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox tbName;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox tbPassword;
        private System.Windows.Forms.CheckBox cbPasswordExpire;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.NumericUpDown numPassExpiry;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ComboBox comboGroups;
        private System.Windows.Forms.LinkLabel linkLabel2;
        private System.Windows.Forms.LinkLabel linkLabel1;
        private System.Windows.Forms.Button btOk;
        private System.Windows.Forms.Button btCancel;
        private System.Windows.Forms.DateTimePicker dateTimePickerValidUntil;
    }
}
