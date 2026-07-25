namespace JustyBaseLegacy.UI.DbForms
{
    partial class ImportTableDataNetezza
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ImportTableDataNetezza));
            tabControl1 = new TabControl();
            tabPage1 = new TabPage();
            groupBox1 = new GroupBox();
            cbHeader = new CheckBox();
            cbCtrlChars = new ComboBox();
            cbCrInString = new ComboBox();
            cbCompress = new ComboBox();
            cbBoolStyle = new ComboBox();
            cbDateStyle = new ComboBox();
            cbDecimalDelim = new ComboBox();
            cbEnconding = new ComboBox();
            label8 = new Label();
            label9 = new Label();
            label7 = new Label();
            label6 = new Label();
            label5 = new Label();
            label3 = new Label();
            tbDelim = new TextBox();
            label10 = new Label();
            label2 = new Label();
            btPath = new Button();
            label1 = new Label();
            tbPath = new TextBox();
            linkLabel1 = new LinkLabel();
            btCancel = new Button();
            btOk = new Button();
            btClipboard = new Button();
            openFileDialog1 = new OpenFileDialog();
            tabControl1.SuspendLayout();
            tabPage1.SuspendLayout();
            groupBox1.SuspendLayout();
            SuspendLayout();
            // 
            // tabControl1
            // 
            tabControl1.Controls.Add(tabPage1);
            tabControl1.Location = new Point(2, 12);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(430, 375);
            tabControl1.TabIndex = 1;
            // 
            // tabPage1
            // 
            tabPage1.Controls.Add(groupBox1);
            tabPage1.Controls.Add(btPath);
            tabPage1.Controls.Add(label1);
            tabPage1.Controls.Add(tbPath);
            tabPage1.Location = new Point(4, 24);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new Padding(3);
            tabPage1.Size = new Size(422, 347);
            tabPage1.TabIndex = 0;
            tabPage1.Text = "Options";
            tabPage1.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(cbHeader);
            groupBox1.Controls.Add(cbCtrlChars);
            groupBox1.Controls.Add(cbCrInString);
            groupBox1.Controls.Add(cbCompress);
            groupBox1.Controls.Add(cbBoolStyle);
            groupBox1.Controls.Add(cbDateStyle);
            groupBox1.Controls.Add(cbDecimalDelim);
            groupBox1.Controls.Add(cbEnconding);
            groupBox1.Controls.Add(label8);
            groupBox1.Controls.Add(label9);
            groupBox1.Controls.Add(label7);
            groupBox1.Controls.Add(label6);
            groupBox1.Controls.Add(label5);
            groupBox1.Controls.Add(label3);
            groupBox1.Controls.Add(tbDelim);
            groupBox1.Controls.Add(label10);
            groupBox1.Controls.Add(label2);
            groupBox1.Location = new Point(10, 49);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(409, 292);
            groupBox1.TabIndex = 3;
            groupBox1.TabStop = false;
            groupBox1.Text = "File format";
            // 
            // cbHeader
            // 
            cbHeader.AutoSize = true;
            cbHeader.Location = new Point(254, 27);
            cbHeader.Name = "cbHeader";
            cbHeader.Size = new Size(103, 19);
            cbHeader.TabIndex = 4;
            cbHeader.Text = "IncludeHeader";
            cbHeader.UseVisualStyleBackColor = true;
            // 
            // cbCtrlChars
            // 
            cbCtrlChars.AutoCompleteCustomSource.AddRange(new string[] { "True", "False", "On", "Off" });
            cbCtrlChars.FormattingEnabled = true;
            cbCtrlChars.Items.AddRange(new object[] { "True", "False", "On", "Off" });
            cbCtrlChars.Location = new Point(109, 198);
            cbCtrlChars.Name = "cbCtrlChars";
            cbCtrlChars.Size = new Size(100, 23);
            cbCtrlChars.TabIndex = 3;
            // 
            // cbCrInString
            // 
            cbCrInString.AutoCompleteCustomSource.AddRange(new string[] { "True", "False", "On", "Off" });
            cbCrInString.FormattingEnabled = true;
            cbCrInString.Items.AddRange(new object[] { "True", "False", "On", "Off" });
            cbCrInString.Location = new Point(109, 170);
            cbCrInString.Name = "cbCrInString";
            cbCrInString.Size = new Size(100, 23);
            cbCrInString.TabIndex = 3;
            // 
            // cbCompress
            // 
            cbCompress.AutoCompleteCustomSource.AddRange(new string[] { "True", "False", "On", "Off" });
            cbCompress.FormattingEnabled = true;
            cbCompress.Items.AddRange(new object[] { "True", "False", "On", "Off" });
            cbCompress.Location = new Point(109, 140);
            cbCompress.Name = "cbCompress";
            cbCompress.Size = new Size(100, 23);
            cbCompress.TabIndex = 3;
            // 
            // cbBoolStyle
            // 
            cbBoolStyle.AutoCompleteCustomSource.AddRange(new string[] { "YMD", "DMY", "MDY", "MONDY", "DMONY", "Y2MD", "DMY2", "MDY2", "MONDY2", "DMONY2" });
            cbBoolStyle.FormattingEnabled = true;
            cbBoolStyle.Items.AddRange(new object[] { "1_0", "T_F", "Y_N", "YES_NO", "TRUE_FALSE" });
            cbBoolStyle.Location = new Point(109, 111);
            cbBoolStyle.Name = "cbBoolStyle";
            cbBoolStyle.Size = new Size(100, 23);
            cbBoolStyle.TabIndex = 3;
            // 
            // cbDateStyle
            // 
            cbDateStyle.AutoCompleteCustomSource.AddRange(new string[] { "YMD", "DMY", "MDY", "MONDY", "DMONY", "Y2MD", "DMY2", "MDY2", "MONDY2", "DMONY2" });
            cbDateStyle.FormattingEnabled = true;
            cbDateStyle.Items.AddRange(new object[] { "YMD", "DMY", "MDY", "MONDY", "DMONY", "Y2MD", "DMY2", "MDY2", "MONDY2", "DMONY2" });
            cbDateStyle.Location = new Point(109, 81);
            cbDateStyle.Name = "cbDateStyle";
            cbDateStyle.Size = new Size(100, 23);
            cbDateStyle.TabIndex = 3;
            // 
            // cbDecimalDelim
            // 
            cbDecimalDelim.FormattingEnabled = true;
            cbDecimalDelim.Items.AddRange(new object[] { ".", "," });
            cbDecimalDelim.Location = new Point(109, 51);
            cbDecimalDelim.Name = "cbDecimalDelim";
            cbDecimalDelim.Size = new Size(100, 23);
            cbDecimalDelim.TabIndex = 3;
            // 
            // cbEnconding
            // 
            cbEnconding.FormattingEnabled = true;
            cbEnconding.Items.AddRange(new object[] { "Internal", "Latin9", "UTF8" });
            cbEnconding.Location = new Point(109, 227);
            cbEnconding.Name = "cbEnconding";
            cbEnconding.Size = new Size(100, 23);
            cbEnconding.TabIndex = 2;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(10, 206);
            label8.Name = "label8";
            label8.Size = new Size(56, 15);
            label8.TabIndex = 0;
            label8.Text = "CtrlChars";
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new Point(8, 178);
            label9.Name = "label9";
            label9.Size = new Size(63, 15);
            label9.TabIndex = 0;
            label9.Text = "CRinString";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(8, 150);
            label7.Name = "label7";
            label7.Size = new Size(60, 15);
            label7.TabIndex = 0;
            label7.Text = "Compress";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(10, 119);
            label6.Name = "label6";
            label6.Size = new Size(58, 15);
            label6.TabIndex = 0;
            label6.Text = "Bool style";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(10, 89);
            label5.Name = "label5";
            label5.Size = new Size(58, 15);
            label5.TabIndex = 0;
            label5.Text = "Date style";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(10, 59);
            label3.Name = "label3";
            label3.Size = new Size(100, 15);
            label3.TabIndex = 0;
            label3.Text = "Decimal delimiter";
            // 
            // tbDelim
            // 
            tbDelim.Location = new Point(109, 22);
            tbDelim.Name = "tbDelim";
            tbDelim.Size = new Size(100, 23);
            tbDelim.TabIndex = 1;
            tbDelim.Text = "|";
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Location = new Point(10, 235);
            label10.Name = "label10";
            label10.Size = new Size(64, 15);
            label10.TabIndex = 0;
            label10.Text = "Enconding";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(10, 30);
            label2.Name = "label2";
            label2.Size = new Size(93, 15);
            label2.TabIndex = 0;
            label2.Text = "Colum delimiter";
            // 
            // btPath
            // 
            btPath.Location = new Point(383, 12);
            btPath.Name = "btPath";
            btPath.Size = new Size(33, 23);
            btPath.TabIndex = 2;
            btPath.Text = "...";
            btPath.UseVisualStyleBackColor = true;
            btPath.Click += btPath_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(13, 23);
            label1.Name = "label1";
            label1.Size = new Size(31, 15);
            label1.TabIndex = 1;
            label1.Text = "Path";
            // 
            // tbPath
            // 
            tbPath.Location = new Point(47, 13);
            tbPath.Name = "tbPath";
            tbPath.Size = new Size(330, 23);
            tbPath.TabIndex = 0;
            // 
            // linkLabel1
            // 
            linkLabel1.AutoSize = true;
            linkLabel1.Location = new Point(6, 407);
            linkLabel1.Name = "linkLabel1";
            linkLabel1.Size = new Size(33, 15);
            linkLabel1.TabIndex = 6;
            linkLabel1.TabStop = true;
            linkLabel1.Text = "Docs";
            linkLabel1.LinkClicked += linkLabel1_LinkClicked;
            // 
            // btCancel
            // 
            btCancel.Location = new Point(353, 397);
            btCancel.Name = "btCancel";
            btCancel.Size = new Size(75, 23);
            btCancel.TabIndex = 3;
            btCancel.Text = "Cancel";
            btCancel.UseVisualStyleBackColor = true;
            btCancel.Click += btCancel_Click;
            // 
            // btOk
            // 
            btOk.Location = new Point(272, 398);
            btOk.Name = "btOk";
            btOk.Size = new Size(75, 23);
            btOk.TabIndex = 4;
            btOk.Text = "OK";
            btOk.UseVisualStyleBackColor = true;
            btOk.Click += btOk_Click;
            // 
            // btClipboard
            // 
            btClipboard.Location = new Point(127, 398);
            btClipboard.Name = "btClipboard";
            btClipboard.Size = new Size(111, 23);
            btClipboard.TabIndex = 5;
            btClipboard.Text = "Copy to clipboard";
            btClipboard.UseVisualStyleBackColor = true;
            btClipboard.Click += btClipboard_Click;
            // 
            // openFileDialog1
            // 
            openFileDialog1.FileName = "openFileDialog1";
            openFileDialog1.Filter = "dat files (*.dat)|*.dat|All files (*.*)|*.* ";
            // 
            // ImportTableDataNetezza
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(434, 431);
            Controls.Add(linkLabel1);
            Controls.Add(btCancel);
            Controls.Add(btOk);
            Controls.Add(btClipboard);
            Controls.Add(tabControl1);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MaximumSize = new Size(450, 470);
            MinimizeBox = false;
            MinimumSize = new Size(450, 470);
            Name = "ImportTableDataNetezza";
            Text = "Import Data";
            tabControl1.ResumeLayout(false);
            tabPage1.ResumeLayout(false);
            tabPage1.PerformLayout();
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.CheckBox cbHeader;
        private System.Windows.Forms.ComboBox cbCtrlChars;
        private System.Windows.Forms.ComboBox cbCrInString;
        private System.Windows.Forms.ComboBox cbCompress;
        private System.Windows.Forms.ComboBox cbBoolStyle;
        private System.Windows.Forms.ComboBox cbDateStyle;
        private System.Windows.Forms.ComboBox cbDecimalDelim;
        private System.Windows.Forms.ComboBox cbEnconding;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox tbDelim;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button btPath;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox tbPath;
        private System.Windows.Forms.LinkLabel linkLabel1;
        private System.Windows.Forms.Button btCancel;
        private System.Windows.Forms.Button btOk;
        private System.Windows.Forms.Button btClipboard;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
    }
}
