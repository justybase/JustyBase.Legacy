namespace JustyBaseLegacy.UI.DbForms
{
    partial class DB2CreateServer
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DB2CreateServer));
            cbDataSource = new ComboBox();
            groupBox1 = new GroupBox();
            lbPass = new Label();
            lbUser = new Label();
            tbPassword = new TextBox();
            tbUser = new TextBox();
            tbAuthInfo = new TextBox();
            cbAuthorization = new CheckBox();
            tbVersion = new TextBox();
            label3 = new Label();
            label2 = new Label();
            label1 = new Label();
            tbWrapper = new TextBox();
            tbType = new TextBox();
            groupBox2 = new GroupBox();
            tbServerName = new TextBox();
            groupBox3 = new GroupBox();
            btGetSql = new Button();
            btCopySql1 = new Button();
            tbSql1 = new TextBox();
            groupBox4 = new GroupBox();
            btLoadSampleOptions = new Button();
            dgvOptions = new ThemedDataGridView();
            ColOptionName = new DataGridViewTextBoxColumn();
            ColOptionValue = new DataGridViewTextBoxColumn();
            linkLabel1 = new LinkLabel();
            linkLabel2 = new LinkLabel();
            groupBox1.SuspendLayout();
            groupBox2.SuspendLayout();
            groupBox3.SuspendLayout();
            groupBox4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvOptions).BeginInit();
            SuspendLayout();
            // 
            // cbDataSource
            // 
            cbDataSource.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            cbDataSource.AutoCompleteSource = AutoCompleteSource.ListItems;
            cbDataSource.FormattingEnabled = true;
            cbDataSource.Location = new Point(10, 22);
            cbDataSource.Name = "cbDataSource";
            cbDataSource.Size = new Size(300, 23);
            cbDataSource.TabIndex = 0;
            cbDataSource.SelectedIndexChanged += cbDataSource_SelectedIndexChanged;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(lbPass);
            groupBox1.Controls.Add(lbUser);
            groupBox1.Controls.Add(tbPassword);
            groupBox1.Controls.Add(tbUser);
            groupBox1.Controls.Add(tbAuthInfo);
            groupBox1.Controls.Add(cbAuthorization);
            groupBox1.Controls.Add(tbVersion);
            groupBox1.Controls.Add(label3);
            groupBox1.Controls.Add(label2);
            groupBox1.Controls.Add(label1);
            groupBox1.Controls.Add(tbWrapper);
            groupBox1.Controls.Add(tbType);
            groupBox1.Controls.Add(cbDataSource);
            groupBox1.Location = new Point(12, 86);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(316, 321);
            groupBox1.TabIndex = 1;
            groupBox1.TabStop = false;
            groupBox1.Text = "Data Source";
            // 
            // lbPass
            // 
            lbPass.AutoSize = true;
            lbPass.Enabled = false;
            lbPass.Location = new Point(15, 252);
            lbPass.Name = "lbPass";
            lbPass.Size = new Size(57, 15);
            lbPass.TabIndex = 7;
            lbPass.Text = "password";
            // 
            // lbUser
            // 
            lbUser.AutoSize = true;
            lbUser.Enabled = false;
            lbUser.Location = new Point(15, 221);
            lbUser.Name = "lbUser";
            lbUser.Size = new Size(29, 15);
            lbUser.TabIndex = 7;
            lbUser.Text = "user";
            // 
            // tbPassword
            // 
            tbPassword.Enabled = false;
            tbPassword.Location = new Point(78, 244);
            tbPassword.Name = "tbPassword";
            tbPassword.Size = new Size(232, 23);
            tbPassword.TabIndex = 6;
            tbPassword.UseSystemPasswordChar = true;
            // 
            // tbUser
            // 
            tbUser.Enabled = false;
            tbUser.Location = new Point(78, 213);
            tbUser.Name = "tbUser";
            tbUser.Size = new Size(232, 23);
            tbUser.TabIndex = 6;
            // 
            // tbAuthInfo
            // 
            tbAuthInfo.BorderStyle = BorderStyle.None;
            tbAuthInfo.Enabled = false;
            tbAuthInfo.Location = new Point(14, 174);
            tbAuthInfo.Multiline = true;
            tbAuthInfo.Name = "tbAuthInfo";
            tbAuthInfo.ReadOnly = true;
            tbAuthInfo.Size = new Size(252, 37);
            tbAuthInfo.TabIndex = 5;
            tbAuthInfo.Text = "Required only for Db2 family data sources. must include DBADM authority";
            // 
            // cbAuthorization
            // 
            cbAuthorization.AutoSize = true;
            cbAuthorization.Location = new Point(15, 152);
            cbAuthorization.Name = "cbAuthorization";
            cbAuthorization.Size = new Size(124, 19);
            cbAuthorization.TabIndex = 4;
            cbAuthorization.Text = "User Authorization";
            cbAuthorization.UseVisualStyleBackColor = true;
            cbAuthorization.CheckedChanged += cbAuthorization_CheckedChanged;
            // 
            // tbVersion
            // 
            tbVersion.Location = new Point(70, 116);
            tbVersion.Name = "tbVersion";
            tbVersion.Size = new Size(240, 23);
            tbVersion.TabIndex = 3;
            tbVersion.Text = "samples: '8i' or 4 or 4.1 or 4.1.2";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(10, 124);
            label3.Name = "label3";
            label3.Size = new Size(45, 15);
            label3.TabIndex = 2;
            label3.Text = "version";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(10, 88);
            label2.Name = "label2";
            label2.Size = new Size(50, 15);
            label2.TabIndex = 2;
            label2.Text = "wrapper";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(10, 59);
            label1.Name = "label1";
            label1.Size = new Size(30, 15);
            label1.TabIndex = 2;
            label1.Text = "type";
            // 
            // tbWrapper
            // 
            tbWrapper.Location = new Point(69, 80);
            tbWrapper.Name = "tbWrapper";
            tbWrapper.ReadOnly = true;
            tbWrapper.Size = new Size(241, 23);
            tbWrapper.TabIndex = 1;
            // 
            // tbType
            // 
            tbType.Location = new Point(69, 51);
            tbType.Name = "tbType";
            tbType.ReadOnly = true;
            tbType.Size = new Size(241, 23);
            tbType.TabIndex = 1;
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(tbServerName);
            groupBox2.Location = new Point(12, 4);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(316, 76);
            groupBox2.TabIndex = 2;
            groupBox2.TabStop = false;
            groupBox2.Text = "Server Name";
            // 
            // tbServerName
            // 
            tbServerName.Location = new Point(10, 31);
            tbServerName.Name = "tbServerName";
            tbServerName.Size = new Size(300, 23);
            tbServerName.TabIndex = 0;
            // 
            // groupBox3
            // 
            groupBox3.Controls.Add(btGetSql);
            groupBox3.Controls.Add(btCopySql1);
            groupBox3.Controls.Add(tbSql1);
            groupBox3.Location = new Point(334, 250);
            groupBox3.Name = "groupBox3";
            groupBox3.Size = new Size(454, 188);
            groupBox3.TabIndex = 4;
            groupBox3.TabStop = false;
            groupBox3.Text = "Sql";
            // 
            // btGetSql
            // 
            btGetSql.Location = new Point(210, 159);
            btGetSql.Name = "btGetSql";
            btGetSql.Size = new Size(101, 23);
            btGetSql.TabIndex = 1;
            btGetSql.Text = "Generate SQL";
            btGetSql.UseVisualStyleBackColor = true;
            btGetSql.Click += btGenerateSql_Click;
            // 
            // btCopySql1
            // 
            btCopySql1.Location = new Point(317, 159);
            btCopySql1.Name = "btCopySql1";
            btCopySql1.Size = new Size(130, 23);
            btCopySql1.TabIndex = 1;
            btCopySql1.Text = "Copy SQL && Close";
            btCopySql1.UseVisualStyleBackColor = true;
            btCopySql1.Click += btCopySql1_Click_1;
            // 
            // tbSql1
            // 
            tbSql1.Location = new Point(6, 22);
            tbSql1.Multiline = true;
            tbSql1.Name = "tbSql1";
            tbSql1.ScrollBars = ScrollBars.Vertical;
            tbSql1.Size = new Size(441, 135);
            tbSql1.TabIndex = 0;
            // 
            // groupBox4
            // 
            groupBox4.Controls.Add(btLoadSampleOptions);
            groupBox4.Controls.Add(dgvOptions);
            groupBox4.Controls.Add(linkLabel1);
            groupBox4.Location = new Point(334, 4);
            groupBox4.Name = "groupBox4";
            groupBox4.Size = new Size(454, 240);
            groupBox4.TabIndex = 5;
            groupBox4.TabStop = false;
            groupBox4.Text = "Data Source Options";
            // 
            // btLoadSampleOptions
            // 
            btLoadSampleOptions.Location = new Point(340, 201);
            btLoadSampleOptions.Name = "btLoadSampleOptions";
            btLoadSampleOptions.Size = new Size(108, 23);
            btLoadSampleOptions.TabIndex = 2;
            btLoadSampleOptions.Text = "Load example";
            btLoadSampleOptions.UseVisualStyleBackColor = true;
            btLoadSampleOptions.Click += BtLoadSampleOptions_Click;
            // 
            // dgvOptions
            // 
            dgvOptions.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvOptions.Columns.AddRange(new DataGridViewColumn[] { ColOptionName, ColOptionValue });
            dgvOptions.Location = new Point(6, 22);
            dgvOptions.Name = "dgvOptions";
            dgvOptions.Size = new Size(442, 173);
            dgvOptions.TabIndex = 1;
            // 
            // ColOptionName
            // 
            ColOptionName.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            ColOptionName.HeaderText = "Option name";
            ColOptionName.Name = "ColOptionName";
            // 
            // ColOptionValue
            // 
            ColOptionValue.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            ColOptionValue.HeaderText = "Value";
            ColOptionValue.Name = "ColOptionValue";
            // 
            // linkLabel1
            // 
            linkLabel1.AutoSize = true;
            linkLabel1.Location = new Point(6, 198);
            linkLabel1.Name = "linkLabel1";
            linkLabel1.Size = new Size(78, 15);
            linkLabel1.TabIndex = 0;
            linkLabel1.TabStop = true;
            linkLabel1.Text = "Options Docs";
            linkLabel1.LinkClicked += linkLabel1_LinkClicked;
            // 
            // linkLabel2
            // 
            linkLabel2.AutoSize = true;
            linkLabel2.Location = new Point(12, 423);
            linkLabel2.Name = "linkLabel2";
            linkLabel2.Size = new Size(33, 15);
            linkLabel2.TabIndex = 6;
            linkLabel2.TabStop = true;
            linkLabel2.Text = "Docs";
            linkLabel2.LinkClicked += linkLabel2_LinkClicked;
            // 
            // DB2CreateServer
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(linkLabel2);
            Controls.Add(groupBox4);
            Controls.Add(groupBox3);
            Controls.Add(groupBox2);
            Controls.Add(groupBox1);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MaximumSize = new Size(816, 489);
            MinimizeBox = false;
            MinimumSize = new Size(816, 489);
            Name = "DB2CreateServer";
            ShowInTaskbar = false;
            Text = "Create Server";
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            groupBox3.ResumeLayout(false);
            groupBox3.PerformLayout();
            groupBox4.ResumeLayout(false);
            groupBox4.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvOptions).EndInit();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox cbDataSource;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TextBox tbWrapper;
        private System.Windows.Forms.TextBox tbType;
        private System.Windows.Forms.TextBox tbVersion;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.TextBox tbServerName;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Button btCopySql1;
        private System.Windows.Forms.TextBox tbSql1;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.LinkLabel linkLabel1;
        private System.Windows.Forms.LinkLabel linkLabel2;
        private System.Windows.Forms.Label lbPass;
        private System.Windows.Forms.Label lbUser;
        private System.Windows.Forms.TextBox tbPassword;
        private System.Windows.Forms.TextBox tbUser;
        private System.Windows.Forms.TextBox tbAuthInfo;
        private System.Windows.Forms.CheckBox cbAuthorization;
        private ThemedDataGridView dgvOptions;
        private System.Windows.Forms.DataGridViewTextBoxColumn ColOptionName;
        private System.Windows.Forms.DataGridViewTextBoxColumn ColOptionValue;
        private System.Windows.Forms.Button btLoadSampleOptions;
        private System.Windows.Forms.Button btGetSql;
    }
}
