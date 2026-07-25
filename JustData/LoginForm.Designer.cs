using System.Diagnostics.Tracing;
using System.Drawing;

namespace JustyBaseLegacy.UI
{
	partial class LoginForm
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
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LoginForm));
            userNameTextBox = new System.Windows.Forms.TextBox();
            passwordTextBox = new System.Windows.Forms.TextBox();
            serverTextBox = new System.Windows.Forms.TextBox();
            connectionSelectorComboBox = new System.Windows.Forms.ComboBox();
            selectDatabaseButton = new System.Windows.Forms.Button();
            saveBt = new System.Windows.Forms.Button();
            addNewButton = new System.Windows.Forms.Button();
            deleteButton = new System.Windows.Forms.Button();
            nameTextBox = new System.Windows.Forms.TextBox();
            checkBoxFastLogin = new System.Windows.Forms.CheckBox();
            toolTip1 = new System.Windows.Forms.ToolTip(components);
            xButton = new System.Windows.Forms.Button();
            btReorder = new System.Windows.Forms.Button();
            checkBox1 = new System.Windows.Forms.CheckBox();
            pictureBox1 = new System.Windows.Forms.PictureBox();
            label1 = new System.Windows.Forms.Label();
            label2 = new System.Windows.Forms.Label();
            label3 = new System.Windows.Forms.Label();
            label4 = new System.Windows.Forms.Label();
            label5 = new System.Windows.Forms.Label();
            groupBox1 = new System.Windows.Forms.GroupBox();
            cmsHelpConnections = new System.Windows.Forms.ContextMenuStrip(components);
            tsmiNz = new System.Windows.Forms.ToolStripMenuItem();
            tsmiDB2 = new System.Windows.Forms.ToolStripMenuItem();
            tsmiOracle = new System.Windows.Forms.ToolStripMenuItem();
            tsmiAccess = new System.Windows.Forms.ToolStripMenuItem();
            tsmiMsSqlStandard = new System.Windows.Forms.ToolStripMenuItem();
            tsmiMsSqlTrusted = new System.Windows.Forms.ToolStripMenuItem();
            tsmiPostgres = new System.Windows.Forms.ToolStripMenuItem();
            tsmiSQLite = new System.Windows.Forms.ToolStripMenuItem();
            tsmiMySql = new System.Windows.Forms.ToolStripMenuItem();
            cloneConnectionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            databaseComboBox = new System.Windows.Forms.ComboBox();
            DriverComboBox = new System.Windows.Forms.ComboBox();
            label6 = new System.Windows.Forms.Label();
            rememberAsDefaultCheckBox = new System.Windows.Forms.CheckBox();
            toolTip2 = new System.Windows.Forms.ToolTip(components);
            programName = new System.Windows.Forms.Label();
            label7 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            groupBox1.SuspendLayout();
            cmsHelpConnections.SuspendLayout();
            SuspendLayout();
            //
            // userNameTextBox
            //
            userNameTextBox.Font = new Font("Segoe UI", 10F);
            userNameTextBox.ForeColor = Color.FromArgb(64, 64, 64);
            userNameTextBox.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            userNameTextBox.Location = new Point(159, 26);
            userNameTextBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            userNameTextBox.Name = "userNameTextBox";
            userNameTextBox.Size = new Size(241, 25);
            userNameTextBox.TabIndex = 2;
            userNameTextBox.Text = "username";
            //
            // passwordTextBox
            //
            passwordTextBox.Font = new Font("Segoe UI", 10F);
            passwordTextBox.ForeColor = Color.FromArgb(64, 64, 64);
            passwordTextBox.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            passwordTextBox.Location = new Point(159, 63);
            passwordTextBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            passwordTextBox.Name = "passwordTextBox";
            passwordTextBox.Size = new Size(213, 25);
            passwordTextBox.TabIndex = 3;
            passwordTextBox.Text = "password";
            passwordTextBox.UseSystemPasswordChar = true;
            //
            // serverTextBox
            //
            serverTextBox.Font = new Font("Century Gothic", 11F);
            serverTextBox.ForeColor = Color.Black;
            serverTextBox.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            serverTextBox.Location = new Point(159, 96);
            serverTextBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            serverTextBox.Name = "serverTextBox";
            serverTextBox.Size = new Size(241, 25);
            serverTextBox.TabIndex = 4;
            serverTextBox.Text = "database ip";
            //
            // connectionSelectorComboBox
            //
            connectionSelectorComboBox.BackColor = Color.FromArgb(224, 224, 224);
            connectionSelectorComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            connectionSelectorComboBox.Font = new Font("Century Gothic", 11F);
            connectionSelectorComboBox.FormattingEnabled = true;
            connectionSelectorComboBox.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            connectionSelectorComboBox.Location = new Point(88, 136);
            connectionSelectorComboBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            connectionSelectorComboBox.Name = "connectionSelectorComboBox";
            connectionSelectorComboBox.Size = new Size(324, 28);
            connectionSelectorComboBox.TabIndex = 0;
            connectionSelectorComboBox.SelectedIndexChanged += ConnectionSelectorComboBox_SelectedIndexChanged;
            // 
            // selectDatabaseButton
            // 
            selectDatabaseButton.BackColor = Color.FromArgb(0, 122, 204);
            selectDatabaseButton.FlatAppearance.BorderSize = 0;
            selectDatabaseButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(0, 102, 184);
            selectDatabaseButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(28, 151, 234);
            selectDatabaseButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            selectDatabaseButton.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            selectDatabaseButton.ForeColor = Color.White;
            selectDatabaseButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            selectDatabaseButton.Location = new Point(261, 484);
            selectDatabaseButton.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            selectDatabaseButton.Name = "selectDatabaseButton";
            selectDatabaseButton.Size = new Size(174, 35);
            selectDatabaseButton.TabIndex = 10;
            selectDatabaseButton.Text = "Save && Select";
            selectDatabaseButton.UseVisualStyleBackColor = false;
            selectDatabaseButton.Click += SelectDatabaseButton_Click;
            // 
            // saveBt
            // 
            saveBt.BackColor = Color.FromArgb(108, 117, 125);
            saveBt.FlatAppearance.BorderSize = 0;
            saveBt.FlatAppearance.MouseDownBackColor = Color.FromArgb(88, 97, 105);
            saveBt.FlatAppearance.MouseOverBackColor = Color.FromArgb(128, 137, 145);
            saveBt.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            saveBt.Font = new Font("Segoe UI", 10F);
            saveBt.ForeColor = Color.White;
            saveBt.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            saveBt.Location = new Point(39, 484);
            saveBt.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            saveBt.Name = "saveBt";
            saveBt.Size = new Size(192, 35);
            saveBt.TabIndex = 9;
            saveBt.Text = "Save";
            saveBt.UseVisualStyleBackColor = false;
            saveBt.Click += btSave_Click;
            // 
            // addNewButton
            // 
            addNewButton.BackColor = Color.FromArgb(40, 167, 69);
            addNewButton.FlatAppearance.BorderSize = 0;
            addNewButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(20, 147, 49);
            addNewButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(60, 187, 89);
            addNewButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            addNewButton.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            addNewButton.ForeColor = Color.White;
            addNewButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            addNewButton.Location = new Point(39, 439);
            addNewButton.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            addNewButton.Name = "addNewButton";
            addNewButton.Size = new Size(192, 35);
            addNewButton.TabIndex = 7;
            addNewButton.Text = "Add New";
            addNewButton.UseVisualStyleBackColor = true;
            addNewButton.Click += AddNewButton_Click;
            // 
            // deleteButton
            // 
            deleteButton.BackColor = Color.FromArgb(220, 53, 69);
            deleteButton.FlatAppearance.BorderSize = 0;
            deleteButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(200, 33, 49);
            deleteButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 73, 89);
            deleteButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            deleteButton.Font = new Font("Segoe UI", 10F);
            deleteButton.ForeColor = Color.White;
            deleteButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            deleteButton.Location = new Point(261, 439);
            deleteButton.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            deleteButton.Name = "deleteButton";
            deleteButton.Size = new Size(174, 35);
            deleteButton.TabIndex = 8;
            deleteButton.Text = "Delete";
            deleteButton.UseVisualStyleBackColor = false;
            deleteButton.Click += DeleteButton_Click;
            // 
            // nameTextBox
            // 
            nameTextBox.Font = new Font("Century Gothic", 11F);
            nameTextBox.ForeColor = Color.Black;
            nameTextBox.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            nameTextBox.Location = new Point(88, 174);
            nameTextBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            nameTextBox.Name = "nameTextBox";
            nameTextBox.Size = new Size(324, 25);
            nameTextBox.TabIndex = 1;
            nameTextBox.Text = "connection name";
            // 
            // checkBoxFastLogin
            // 
            checkBoxFastLogin.Font = new Font("Century Gothic", 11F);
            checkBoxFastLogin.ForeColor = Color.Black;
            checkBoxFastLogin.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            checkBoxFastLogin.Location = new Point(105, 533);
            checkBoxFastLogin.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            checkBoxFastLogin.Name = "checkBoxFastLogin";
            checkBoxFastLogin.Size = new Size(282, 28);
            checkBoxFastLogin.TabIndex = 13;
            checkBoxFastLogin.Text = "skip this window next time";
            // 
            // toolTip1
            // 
            toolTip1.ToolTipIcon = System.Windows.Forms.ToolTipIcon.Info;
            toolTip1.ToolTipTitle = "About";
            // 
            // xButton
            // 
            xButton.ForeColor = Color.Gray;
            xButton.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            xButton.Location = new Point(438, 18);
            xButton.Name = "xButton";
            xButton.Size = new Size(26, 23);
            xButton.TabIndex = 25;
            xButton.Text = "X";
            toolTip1.SetToolTip(xButton, "Cancel");
            xButton.UseVisualStyleBackColor = true;
            xButton.Click += XButton_Click;
            // 
            // btReorder
            // 
            btReorder.Image = JustData.Properties.Resources.arrow_refresh_small_grayscale;
            btReorder.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            btReorder.Location = new Point(39, 529);
            btReorder.Name = "btReorder";
            btReorder.Size = new Size(31, 23);
            btReorder.TabIndex = 26;
            toolTip1.SetToolTip(btReorder, "Reorder connections");
            btReorder.UseVisualStyleBackColor = true;
            btReorder.Click += BtReorder_Click;
            // 
            // checkBox1
            // 
            checkBox1.AutoSize = true;
            checkBox1.Location = new Point(379, 74);
            checkBox1.Name = "checkBox1";
            checkBox1.Size = new Size(15, 14);
            checkBox1.TabIndex = 26;
            toolTip2.SetToolTip(checkBox1, "show password");
            checkBox1.UseVisualStyleBackColor = true;
            checkBox1.CheckedChanged += CheckBox1_CheckedChanged;
            // 
            // pictureBox1
            // 
            pictureBox1.ErrorImage = null;
            pictureBox1.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left;
            pictureBox1.Image = JustData.Properties.Resources.icon2;
            pictureBox1.Location = new Point(13, 12);
            pictureBox1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(134, 116);
            pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            pictureBox1.TabIndex = 22;
            pictureBox1.TabStop = false;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Century Gothic", 11F);
            label1.ForeColor = Color.DimGray;
            label1.Location = new Point(14, 31);
            label1.Name = "label1";
            label1.Size = new Size(81, 20);
            label1.TabIndex = 23;
            label1.Text = "username";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Century Gothic", 11F);
            label2.ForeColor = Color.DimGray;
            label2.Location = new Point(14, 66);
            label2.Name = "label2";
            label2.Size = new Size(80, 20);
            label2.TabIndex = 23;
            label2.Text = "password";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Century Gothic", 11F);
            label3.ForeColor = Color.DimGray;
            label3.Location = new Point(14, 101);
            label3.Name = "label3";
            label3.Size = new Size(121, 20);
            label3.TabIndex = 23;
            label3.Text = "server ip/name";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Font = new Font("Century Gothic", 11F);
            label4.ForeColor = Color.DimGray;
            label4.Location = new Point(9, 139);
            label4.Name = "label4";
            label4.Size = new Size(126, 20);
            label4.TabIndex = 23;
            label4.Text = "database name";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Font = new Font("Century Gothic", 11F);
            label5.ForeColor = Color.DimGray;
            label5.Location = new Point(14, 178);
            label5.Name = "label5";
            label5.Size = new Size(52, 20);
            label5.TabIndex = 23;
            label5.Text = "driver";
            // 
            // groupBox1
            // 
            groupBox1.BackColor = Color.Transparent;
            groupBox1.ContextMenuStrip = cmsHelpConnections;
            groupBox1.Controls.Add(checkBox1);
            groupBox1.Controls.Add(databaseComboBox);
            groupBox1.Controls.Add(DriverComboBox);
            groupBox1.Controls.Add(label6);
            groupBox1.Controls.Add(label1);
            groupBox1.Controls.Add(label5);
            groupBox1.Controls.Add(userNameTextBox);
            groupBox1.Controls.Add(label4);
            groupBox1.Controls.Add(label2);
            groupBox1.Controls.Add(label3);
            groupBox1.Controls.Add(passwordTextBox);
            groupBox1.Controls.Add(serverTextBox);
            groupBox1.Font = new Font("Century Gothic", 11F);
            groupBox1.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            groupBox1.Location = new Point(30, 207);
            groupBox1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            groupBox1.Name = "groupBox1";
            groupBox1.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
            groupBox1.Size = new Size(419, 224);
            groupBox1.TabIndex = 24;
            groupBox1.TabStop = false;
            groupBox1.Text = "Login Data";
            // 
            // cmsHelpConnections
            // 
            cmsHelpConnections.ImageScalingSize = new Size(20, 20);
            cmsHelpConnections.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsmiNz, tsmiDB2, tsmiOracle, tsmiAccess, tsmiMsSqlStandard, tsmiMsSqlTrusted, tsmiPostgres, tsmiSQLite, tsmiMySql, cloneConnectionToolStripMenuItem });
            cmsHelpConnections.Name = "cmsHelpConnections";
            cmsHelpConnections.Size = new Size(239, 244);
            // 
            // tsmiNz
            // 
            tsmiNz.Font = new Font("Century Gothic", 11F);
            tsmiNz.Name = "tsmiNz";
            tsmiNz.Size = new Size(238, 24);
            tsmiNz.Text = "Netezza";
            tsmiNz.Click += tsmiDbClick_Click;
            // 
            // tsmiDB2
            // 
            tsmiDB2.Font = new Font("Century Gothic", 11F);
            tsmiDB2.Name = "tsmiDB2";
            tsmiDB2.Size = new Size(238, 24);
            tsmiDB2.Text = "DB2";
            tsmiDB2.Click += tsmiDbClick_Click;
            // 
            // tsmiOracle
            // 
            tsmiOracle.Font = new Font("Century Gothic", 11F);
            tsmiOracle.Name = "tsmiOracle";
            tsmiOracle.Size = new Size(238, 24);
            tsmiOracle.Text = "Oracle";
            tsmiOracle.Click += tsmiDbClick_Click;
            // 
            // tsmiAccess
            // 
            tsmiAccess.Font = new Font("Century Gothic", 11F);
            tsmiAccess.Name = "tsmiAccess";
            tsmiAccess.Size = new Size(238, 24);
            tsmiAccess.Text = "Access";
            tsmiAccess.Click += tsmiDbClick_Click;
            // 
            // tsmiMsSqlStandard
            // 
            tsmiMsSqlStandard.Font = new Font("Century Gothic", 11F);
            tsmiMsSqlStandard.Name = "tsmiMsSqlStandard";
            tsmiMsSqlStandard.Size = new Size(238, 24);
            tsmiMsSqlStandard.Text = "MS SQL";
            tsmiMsSqlStandard.Click += tsmiDbClick_Click;
            // 
            // tsmiMsSqlTrusted
            // 
            tsmiMsSqlTrusted.Font = new Font("Century Gothic", 11F);
            tsmiMsSqlTrusted.Name = "tsmiMsSqlTrusted";
            tsmiMsSqlTrusted.Size = new Size(238, 24);
            tsmiMsSqlTrusted.Text = "MS SQL Windows Auth";
            tsmiMsSqlTrusted.Click += tsmiDbClick_Click;
            // 
            // tsmiPostgres
            // 
            tsmiPostgres.Font = new Font("Century Gothic", 11F);
            tsmiPostgres.Name = "tsmiPostgres";
            tsmiPostgres.Size = new Size(238, 24);
            tsmiPostgres.Text = "Postgres";
            tsmiPostgres.Click += tsmiDbClick_Click;
            // 
            // tsmiSQLite
            // 
            tsmiSQLite.Font = new Font("Century Gothic", 11F);
            tsmiSQLite.Name = "tsmiSQLite";
            tsmiSQLite.Size = new Size(238, 24);
            tsmiSQLite.Text = "SQLite";
            tsmiSQLite.Click += tsmiDbClick_Click;
            // 
            // tsmiMySql
            // 
            tsmiMySql.Font = new Font("Century Gothic", 11F);
            tsmiMySql.Name = "tsmiMySql";
            tsmiMySql.Size = new Size(238, 24);
            tsmiMySql.Text = "MySql";
            tsmiMySql.Click += tsmiDbClick_Click;
            // 
            // cloneConnectionToolStripMenuItem
            // 
            cloneConnectionToolStripMenuItem.Font = new Font("Century Gothic", 11F);
            cloneConnectionToolStripMenuItem.Name = "cloneConnectionToolStripMenuItem";
            cloneConnectionToolStripMenuItem.Size = new Size(238, 24);
            cloneConnectionToolStripMenuItem.Text = "Clone connection";
            cloneConnectionToolStripMenuItem.Click += cloneConnectionToolStripMenuItem_Click;
            // 
            // databaseComboBox
            // 
            databaseComboBox.FormattingEnabled = true;
            databaseComboBox.Location = new Point(159, 133);
            databaseComboBox.Name = "databaseComboBox";
            databaseComboBox.Size = new Size(241, 28);
            databaseComboBox.TabIndex = 25;
            databaseComboBox.DropDown += databaseComboBox_DropDown;
            // 
            // DriverComboBox
            // 
            DriverComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            DriverComboBox.FormattingEnabled = true;
            DriverComboBox.Items.AddRange(new object[] { "NetezzaSQL", "DB2", "Oracle", "Microsoft.ACE.OLEDB.12.0", "MsSqlStd", "MsSqlTrusted", "Postgres", "SQLite" });
            DriverComboBox.Location = new Point(159, 170);
            DriverComboBox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            DriverComboBox.Name = "DriverComboBox";
            DriverComboBox.Size = new Size(241, 28);
            DriverComboBox.TabIndex = 24;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Font = new Font("Century Gothic", 11F);
            label6.ForeColor = Color.DimGray;
            label6.Location = new Point(14, 178);
            label6.Name = "label6";
            label6.Size = new Size(52, 20);
            label6.TabIndex = 23;
            label6.Text = "driver";
            // 
            // rememberAsDefaultCheckBox
            // 
            rememberAsDefaultCheckBox.Checked = true;
            rememberAsDefaultCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            rememberAsDefaultCheckBox.Font = new Font("Century Gothic", 11F);
            rememberAsDefaultCheckBox.ForeColor = Color.Black;
            rememberAsDefaultCheckBox.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            rememberAsDefaultCheckBox.Location = new Point(105, 556);
            rememberAsDefaultCheckBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            rememberAsDefaultCheckBox.Name = "rememberAsDefaultCheckBox";
            rememberAsDefaultCheckBox.Size = new Size(282, 28);
            rememberAsDefaultCheckBox.TabIndex = 13;
            rememberAsDefaultCheckBox.Text = "selected connection as default";
            // 
            // toolTip2
            // 
            toolTip2.ToolTipIcon = System.Windows.Forms.ToolTipIcon.Warning;
            // 
            // programName
            // 
            programName.AutoSize = true;
            programName.BackColor = Color.Transparent;
            programName.Font = new Font("Century Gothic", 38F);
            programName.ForeColor = Color.Black;
            programName.Location = new Point(155, 25);
            programName.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            programName.Name = "programName";
            programName.Size = new Size(247, 62);
            programName.TabIndex = 16;
            programName.Text = "JustyBase";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.BackColor = Color.Transparent;
            label7.Font = new Font("Segoe UI", 11F, FontStyle.Italic);
            label7.ForeColor = Color.FromArgb(120, 120, 120);
            label7.Location = new Point(190, 80);
            label7.Name = "label7";
            label7.Size = new Size(162, 21);
            label7.TabIndex = 27;
            label7.Text = "legacy";
            // 
            // LoginForm
            // 
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            BackColor = Color.FromArgb(248, 249, 250);
            ClientSize = new Size(483, 598);
            Controls.Add(label7);
            Controls.Add(btReorder);
            Controls.Add(xButton);
            Controls.Add(groupBox1);
            Controls.Add(pictureBox1);
            Controls.Add(programName);
            Controls.Add(nameTextBox);
            Controls.Add(addNewButton);
            Controls.Add(deleteButton);
            Controls.Add(saveBt);
            Controls.Add(selectDatabaseButton);
            Controls.Add(connectionSelectorComboBox);
            Controls.Add(rememberAsDefaultCheckBox);
            Controls.Add(checkBoxFastLogin);
            ForeColor = Color.Black;
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "LoginForm";
            Opacity = 0.95D;
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "Login";
            TopMost = true;
            DoubleBuffered = true;
            FormClosing += LoginForm_FormClosing;
            Load += LoginForm_Load;
            MouseDown += LoginForm_MouseDown;
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            cmsHelpConnections.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }




        #endregion
        private System.Windows.Forms.TextBox userNameTextBox;
		private System.Windows.Forms.TextBox passwordTextBox;
		private System.Windows.Forms.TextBox serverTextBox;
		private System.Windows.Forms.ComboBox connectionSelectorComboBox;
		private System.Windows.Forms.Button selectDatabaseButton;
		private System.Windows.Forms.Button saveBt;
		private System.Windows.Forms.Button addNewButton;
		private System.Windows.Forms.Button deleteButton;
		private System.Windows.Forms.TextBox nameTextBox;
		private System.Windows.Forms.CheckBox checkBoxFastLogin;
		private System.Windows.Forms.ToolTip toolTip1;
		private System.Windows.Forms.PictureBox pictureBox1;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.ContextMenuStrip cmsHelpConnections;
		private System.Windows.Forms.ToolStripMenuItem tsmiNz;
		private System.Windows.Forms.ToolStripMenuItem tsmiDB2;
		private System.Windows.Forms.ToolStripMenuItem tsmiOracle;
		private System.Windows.Forms.ToolStripMenuItem tsmiAccess;
		private System.Windows.Forms.ToolStripMenuItem tsmiMsSqlStandard;
		private System.Windows.Forms.ToolStripMenuItem tsmiMsSqlTrusted;
		private System.Windows.Forms.ToolStripMenuItem tsmiPostgres;
		private System.Windows.Forms.ComboBox DriverComboBox;
		private System.Windows.Forms.ToolStripMenuItem tsmiSQLite;
		private System.Windows.Forms.ToolStripMenuItem tsmiMySql;
		private System.Windows.Forms.Button xButton;
		private System.Windows.Forms.CheckBox rememberAsDefaultCheckBox;
		private System.Windows.Forms.Button btReorder;
		private System.Windows.Forms.ComboBox databaseComboBox;
		private System.Windows.Forms.ToolStripMenuItem cloneConnectionToolStripMenuItem;
		private System.Windows.Forms.CheckBox checkBox1;
		private System.Windows.Forms.ToolTip toolTip2;
		private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label programName;
        private System.Windows.Forms.Label label7;
    }
}
