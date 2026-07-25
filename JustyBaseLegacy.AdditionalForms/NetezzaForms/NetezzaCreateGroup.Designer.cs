namespace JustyBaseLegacy.UI.DbForms
{
    partial class Create_group
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Create_group));
            linkLabel1 = new LinkLabel();
            button1 = new Button();
            button2 = new Button();
            tbName = new TextBox();
            label1 = new Label();
            label2 = new Label();
            cbDEFPRIORITY = new ComboBox();
            label3 = new Label();
            cbMAXPRIORITY = new ComboBox();
            labelX = new Label();
            numROWSETLIMIT = new NumericUpDown();
            label4 = new Label();
            numSESSIONTIMEOUT = new NumericUpDown();
            numQUERYTIMEOUT = new NumericUpDown();
            label5 = new Label();
            label6 = new Label();
            numConcSession = new NumericUpDown();
            label7 = new Label();
            numResourceMin = new NumericUpDown();
            label8 = new Label();
            numResourceMax = new NumericUpDown();
            label9 = new Label();
            numJobMaximum = new NumericUpDown();
            cbCollectHistory = new ComboBox();
            label10 = new Label();
            label11 = new Label();
            cbAllowCross = new ComboBox();
            label12 = new Label();
            tbUser = new TextBox();
            label13 = new Label();
            numPasswordExpiry = new NumericUpDown();
            label14 = new Label();
            cbAccessTime = new ComboBox();
            ((System.ComponentModel.ISupportInitialize)numROWSETLIMIT).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numSESSIONTIMEOUT).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numQUERYTIMEOUT).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numConcSession).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numResourceMin).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numResourceMax).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numJobMaximum).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numPasswordExpiry).BeginInit();
            SuspendLayout();
            // 
            // linkLabel1
            // 
            linkLabel1.AutoSize = true;
            linkLabel1.Location = new Point(13, 476);
            linkLabel1.Name = "linkLabel1";
            linkLabel1.Size = new Size(33, 15);
            linkLabel1.TabIndex = 0;
            linkLabel1.TabStop = true;
            linkLabel1.Text = "Docs";
            linkLabel1.LinkClicked += linkLabel1_LinkClicked;
            // 
            // button1
            // 
            button1.Location = new Point(170, 472);
            button1.Name = "button1";
            button1.Size = new Size(75, 23);
            button1.TabIndex = 1;
            button1.Text = "OK";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // button2
            // 
            button2.Location = new Point(251, 472);
            button2.Name = "button2";
            button2.Size = new Size(75, 23);
            button2.TabIndex = 1;
            button2.Text = "Cancel";
            button2.UseVisualStyleBackColor = true;
            button2.Click += button2_Click;
            // 
            // tbName
            // 
            tbName.Location = new Point(126, 17);
            tbName.Name = "tbName";
            tbName.Size = new Size(191, 23);
            tbName.TabIndex = 2;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 25);
            label1.Name = "label1";
            label1.Size = new Size(39, 15);
            label1.TabIndex = 3;
            label1.Text = "Name";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(12, 54);
            label2.Name = "label2";
            label2.Size = new Size(66, 15);
            label2.TabIndex = 4;
            label2.Text = "Def priority";
            // 
            // cbDEFPRIORITY
            // 
            cbDEFPRIORITY.FormattingEnabled = true;
            cbDEFPRIORITY.Items.AddRange(new object[] { "CRITICAL", "HIGH", "NORMAL", "LOW", "NONE" });
            cbDEFPRIORITY.Location = new Point(126, 46);
            cbDEFPRIORITY.Name = "cbDEFPRIORITY";
            cbDEFPRIORITY.Size = new Size(191, 23);
            cbDEFPRIORITY.TabIndex = 5;
            cbDEFPRIORITY.Text = "NORMAL";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(12, 83);
            label3.Name = "label3";
            label3.Size = new Size(70, 15);
            label3.TabIndex = 4;
            label3.Text = "Max priority";
            // 
            // cbMAXPRIORITY
            // 
            cbMAXPRIORITY.FormattingEnabled = true;
            cbMAXPRIORITY.Items.AddRange(new object[] { "CRITICAL", "HIGH", "NORMAL", "LOW", "NONE" });
            cbMAXPRIORITY.Location = new Point(126, 75);
            cbMAXPRIORITY.Name = "cbMAXPRIORITY";
            cbMAXPRIORITY.Size = new Size(191, 23);
            cbMAXPRIORITY.TabIndex = 5;
            cbMAXPRIORITY.Text = "NORMAL";
            // 
            // labelX
            // 
            labelX.AutoSize = true;
            labelX.Location = new Point(12, 112);
            labelX.Name = "labelX";
            labelX.Size = new Size(72, 15);
            labelX.TabIndex = 4;
            labelX.Text = "Rowset limit";
            // 
            // numROWSETLIMIT
            // 
            numROWSETLIMIT.Location = new Point(126, 104);
            numROWSETLIMIT.Maximum = new decimal(new int[] { 247483647, 0, 0, 0 });
            numROWSETLIMIT.Name = "numROWSETLIMIT";
            numROWSETLIMIT.Size = new Size(191, 23);
            numROWSETLIMIT.TabIndex = 6;
            numROWSETLIMIT.ThousandsSeparator = true;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(12, 141);
            label4.Name = "label4";
            label4.Size = new Size(91, 15);
            label4.TabIndex = 4;
            label4.Text = "Session timeout";
            // 
            // numSESSIONTIMEOUT
            // 
            numSESSIONTIMEOUT.Location = new Point(126, 133);
            numSESSIONTIMEOUT.Maximum = new decimal(new int[] { 35791394, 0, 0, 0 });
            numSESSIONTIMEOUT.Name = "numSESSIONTIMEOUT";
            numSESSIONTIMEOUT.Size = new Size(191, 23);
            numSESSIONTIMEOUT.TabIndex = 6;
            numSESSIONTIMEOUT.ThousandsSeparator = true;
            // 
            // numQUERYTIMEOUT
            // 
            numQUERYTIMEOUT.Location = new Point(126, 162);
            numQUERYTIMEOUT.Maximum = new decimal(new int[] { 35791394, 0, 0, 0 });
            numQUERYTIMEOUT.Name = "numQUERYTIMEOUT";
            numQUERYTIMEOUT.Size = new Size(191, 23);
            numQUERYTIMEOUT.TabIndex = 6;
            numQUERYTIMEOUT.ThousandsSeparator = true;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(12, 170);
            label5.Name = "label5";
            label5.Size = new Size(84, 15);
            label5.TabIndex = 4;
            label5.Text = "Query timeout";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(11, 202);
            label6.Name = "label6";
            label6.Size = new Size(109, 15);
            label6.TabIndex = 4;
            label6.Text = "Concurent sessions";
            // 
            // numConcSession
            // 
            numConcSession.Location = new Point(126, 194);
            numConcSession.Maximum = new decimal(new int[] { 35791394, 0, 0, 0 });
            numConcSession.Name = "numConcSession";
            numConcSession.Size = new Size(191, 23);
            numConcSession.TabIndex = 6;
            numConcSession.ThousandsSeparator = true;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(12, 234);
            label7.Name = "label7";
            label7.Size = new Size(111, 15);
            label7.TabIndex = 4;
            label7.Text = "Resource minimum";
            // 
            // numResourceMin
            // 
            numResourceMin.Location = new Point(126, 226);
            numResourceMin.Name = "numResourceMin";
            numResourceMin.Size = new Size(191, 23);
            numResourceMin.TabIndex = 6;
            numResourceMin.ThousandsSeparator = true;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(12, 263);
            label8.Name = "label8";
            label8.Size = new Size(112, 15);
            label8.TabIndex = 4;
            label8.Text = "Resource maximum";
            // 
            // numResourceMax
            // 
            numResourceMax.Location = new Point(126, 255);
            numResourceMax.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numResourceMax.Name = "numResourceMax";
            numResourceMax.Size = new Size(191, 23);
            numResourceMax.TabIndex = 6;
            numResourceMax.ThousandsSeparator = true;
            numResourceMax.Value = new decimal(new int[] { 100, 0, 0, 0 });
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new Point(13, 292);
            label9.Name = "label9";
            label9.Size = new Size(82, 15);
            label9.TabIndex = 4;
            label9.Text = "Job maximum";
            // 
            // numJobMaximum
            // 
            numJobMaximum.Location = new Point(126, 284);
            numJobMaximum.Name = "numJobMaximum";
            numJobMaximum.Size = new Size(191, 23);
            numJobMaximum.TabIndex = 6;
            numJobMaximum.ThousandsSeparator = true;
            // 
            // cbCollectHistory
            // 
            cbCollectHistory.FormattingEnabled = true;
            cbCollectHistory.Items.AddRange(new object[] { "ON", "OFF", "DEFAULT" });
            cbCollectHistory.Location = new Point(126, 313);
            cbCollectHistory.Name = "cbCollectHistory";
            cbCollectHistory.Size = new Size(191, 23);
            cbCollectHistory.TabIndex = 7;
            cbCollectHistory.Text = "DEFAULT";
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Location = new Point(13, 321);
            label10.Name = "label10";
            label10.Size = new Size(83, 15);
            label10.TabIndex = 4;
            label10.Text = "Collect history";
            // 
            // label11
            // 
            label11.AutoSize = true;
            label11.Location = new Point(13, 351);
            label11.Name = "label11";
            label11.Size = new Size(90, 15);
            label11.TabIndex = 4;
            label11.Text = "Allow cross join";
            // 
            // cbAllowCross
            // 
            cbAllowCross.FormattingEnabled = true;
            cbAllowCross.Items.AddRange(new object[] { "TRUE", "FALSE", "NULL" });
            cbAllowCross.Location = new Point(126, 343);
            cbAllowCross.Name = "cbAllowCross";
            cbAllowCross.Size = new Size(191, 23);
            cbAllowCross.TabIndex = 7;
            cbAllowCross.Text = "TRUE";
            // 
            // label12
            // 
            label12.AutoSize = true;
            label12.Location = new Point(16, 438);
            label12.Name = "label12";
            label12.Size = new Size(30, 15);
            label12.TabIndex = 4;
            label12.Text = "User";
            // 
            // tbUser
            // 
            tbUser.Location = new Point(126, 430);
            tbUser.Name = "tbUser";
            tbUser.Size = new Size(191, 23);
            tbUser.TabIndex = 8;
            // 
            // label13
            // 
            label13.AutoSize = true;
            label13.Location = new Point(13, 382);
            label13.Name = "label13";
            label13.Size = new Size(91, 15);
            label13.TabIndex = 4;
            label13.Text = "Password Expiry";
            // 
            // numPasswordExpiry
            // 
            numPasswordExpiry.Location = new Point(126, 372);
            numPasswordExpiry.Name = "numPasswordExpiry";
            numPasswordExpiry.Size = new Size(191, 23);
            numPasswordExpiry.TabIndex = 6;
            numPasswordExpiry.ThousandsSeparator = true;
            // 
            // label14
            // 
            label14.AutoSize = true;
            label14.Location = new Point(16, 409);
            label14.Name = "label14";
            label14.Size = new Size(70, 15);
            label14.TabIndex = 4;
            label14.Text = "Access time";
            // 
            // cbAccessTime
            // 
            cbAccessTime.FormattingEnabled = true;
            cbAccessTime.Items.AddRange(new object[] { "ALL", "DEFAULT", "ACCESS TIME (DAY 1,2,3,4,5,6,7 START '09:00' END '17:00')", "ACCESS TIME (DAY 1 START '09:00' END '17:00', DAY 2 START '10:00' END '18:00')" });
            cbAccessTime.Location = new Point(126, 401);
            cbAccessTime.Name = "cbAccessTime";
            cbAccessTime.Size = new Size(191, 23);
            cbAccessTime.TabIndex = 7;
            cbAccessTime.Text = "ALL";
            // 
            // Create_group
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(348, 500);
            Controls.Add(tbUser);
            Controls.Add(cbAllowCross);
            Controls.Add(cbAccessTime);
            Controls.Add(cbCollectHistory);
            Controls.Add(numPasswordExpiry);
            Controls.Add(numJobMaximum);
            Controls.Add(numResourceMax);
            Controls.Add(numResourceMin);
            Controls.Add(numConcSession);
            Controls.Add(numQUERYTIMEOUT);
            Controls.Add(numSESSIONTIMEOUT);
            Controls.Add(numROWSETLIMIT);
            Controls.Add(label12);
            Controls.Add(label11);
            Controls.Add(label10);
            Controls.Add(label14);
            Controls.Add(label13);
            Controls.Add(label9);
            Controls.Add(cbMAXPRIORITY);
            Controls.Add(label8);
            Controls.Add(label7);
            Controls.Add(cbDEFPRIORITY);
            Controls.Add(label6);
            Controls.Add(label5);
            Controls.Add(label4);
            Controls.Add(labelX);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(tbName);
            Controls.Add(button2);
            Controls.Add(button1);
            Controls.Add(linkLabel1);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MaximumSize = new Size(364, 539);
            MinimizeBox = false;
            MinimumSize = new Size(364, 539);
            Name = "Create_group";
            ShowInTaskbar = false;
            Text = "Create_group";
            ((System.ComponentModel.ISupportInitialize)numROWSETLIMIT).EndInit();
            ((System.ComponentModel.ISupportInitialize)numSESSIONTIMEOUT).EndInit();
            ((System.ComponentModel.ISupportInitialize)numQUERYTIMEOUT).EndInit();
            ((System.ComponentModel.ISupportInitialize)numConcSession).EndInit();
            ((System.ComponentModel.ISupportInitialize)numResourceMin).EndInit();
            ((System.ComponentModel.ISupportInitialize)numResourceMax).EndInit();
            ((System.ComponentModel.ISupportInitialize)numJobMaximum).EndInit();
            ((System.ComponentModel.ISupportInitialize)numPasswordExpiry).EndInit();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.LinkLabel linkLabel1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.TextBox tbName;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox cbMAXPRIORITY;
        private System.Windows.Forms.Label labelX;
        private System.Windows.Forms.NumericUpDown numROWSETLIMIT;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.NumericUpDown numSESSIONTIMEOUT;
        private System.Windows.Forms.NumericUpDown numQUERYTIMEOUT;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.NumericUpDown numConcSession;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.NumericUpDown numResourceMin;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.NumericUpDown numResourceMax;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.NumericUpDown numJobMaximum;
        private System.Windows.Forms.ComboBox cbCollectHistory;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.ComboBox cbAllowCross;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.TextBox tbUser;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.NumericUpDown numPasswordExpiry;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.ComboBox cbAccessTime;
        private System.Windows.Forms.ComboBox cbDEFPRIORITY;
    }
}
