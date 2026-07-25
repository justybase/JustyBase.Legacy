namespace JustyBaseLegacy.UI.DbForms
{
    partial class AddIndexForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AddIndexForm));
            label1 = new Label();
            tbSchema = new TextBox();
            label2 = new Label();
            tbTabName = new TextBox();
            label3 = new Label();
            label4 = new Label();
            tbIndName = new TextBox();
            label5 = new Label();
            cbUnique = new CheckBox();
            dataGridView1 = new ThemedDataGridView();
            colName = new DataGridViewComboBoxColumn();
            colSort = new DataGridViewComboBoxColumn();
            btAdd = new Button();
            btMinus = new Button();
            button3 = new Button();
            button4 = new Button();
            label6 = new Label();
            cbCluster = new CheckBox();
            radioButton1 = new RadioButton();
            radioButton2 = new RadioButton();
            radioButton3 = new RadioButton();
            button1 = new Button();
            button2 = new Button();
            tbSql = new TextBox();
            label8 = new Label();
            cbCompress = new CheckBox();
            label9 = new Label();
            cbPartitioned = new CheckBox();
            label10 = new Label();
            cbIncludeNulls = new CheckBox();
            linkLabel1 = new LinkLabel();
            label11 = new Label();
            cbSpecification = new CheckBox();
            cbStats = new ComboBox();
            label12 = new Label();
            groupBox1 = new GroupBox();
            tbIndexSchema = new TextBox();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            groupBox1.SuspendLayout();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 20);
            label1.Name = "label1";
            label1.Size = new Size(49, 15);
            label1.TabIndex = 0;
            label1.Text = "Schema";
            // 
            // tbSchema
            // 
            tbSchema.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            tbSchema.Location = new Point(100, 12);
            tbSchema.Name = "tbSchema";
            tbSchema.ReadOnly = true;
            tbSchema.Size = new Size(454, 23);
            tbSchema.TabIndex = 1;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(12, 49);
            label2.Name = "label2";
            label2.Size = new Size(35, 15);
            label2.TabIndex = 0;
            label2.Text = "Table";
            // 
            // tbTabName
            // 
            tbTabName.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            tbTabName.Location = new Point(100, 41);
            tbTabName.Name = "tbTabName";
            tbTabName.ReadOnly = true;
            tbTabName.Size = new Size(454, 23);
            tbTabName.TabIndex = 1;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(12, 78);
            label3.Name = "label3";
            label3.Size = new Size(80, 15);
            label3.TabIndex = 0;
            label3.Text = "Index Schema";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(12, 107);
            label4.Name = "label4";
            label4.Size = new Size(70, 15);
            label4.TabIndex = 0;
            label4.Text = "Index Name";
            // 
            // tbIndName
            // 
            tbIndName.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            tbIndName.Location = new Point(100, 99);
            tbIndName.Name = "tbIndName";
            tbIndName.Size = new Size(454, 23);
            tbIndName.TabIndex = 1;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(16, 139);
            label5.Name = "label5";
            label5.Size = new Size(45, 15);
            label5.TabIndex = 0;
            label5.Text = "Unique";
            // 
            // cbUnique
            // 
            cbUnique.AutoSize = true;
            cbUnique.Location = new Point(99, 139);
            cbUnique.Name = "cbUnique";
            cbUnique.Size = new Size(15, 14);
            cbUnique.TabIndex = 3;
            cbUnique.UseVisualStyleBackColor = true;
            // 
            // dataGridView1
            // 
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.AllowUserToDeleteRows = false;
            dataGridView1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            dataGridView1.BackgroundColor = SystemColors.Control;
            dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView1.Columns.AddRange(new DataGridViewColumn[] { colName, colSort });
            dataGridView1.Location = new Point(12, 171);
            dataGridView1.Name = "dataGridView1";
            dataGridView1.RowHeadersVisible = false;
            dataGridView1.Size = new Size(494, 121);
            dataGridView1.TabIndex = 4;
            // 
            // colName
            // 
            colName.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            colName.HeaderText = "Column Name";
            colName.Name = "colName";
            colName.Resizable = DataGridViewTriState.True;
            colName.SortMode = DataGridViewColumnSortMode.Automatic;
            // 
            // colSort
            // 
            colSort.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            colSort.HeaderText = "Sort Order";
            colSort.Items.AddRange(new object[] { "ASC", "DESC", "RANDOM", "Default" });
            colSort.Name = "colSort";
            colSort.Resizable = DataGridViewTriState.True;
            colSort.SortMode = DataGridViewColumnSortMode.Automatic;
            // 
            // btAdd
            // 
            btAdd.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btAdd.Location = new Point(512, 171);
            btAdd.Name = "btAdd";
            btAdd.Size = new Size(48, 23);
            btAdd.TabIndex = 5;
            btAdd.Text = "+";
            btAdd.UseVisualStyleBackColor = true;
            btAdd.Click += btAdd_Click;
            // 
            // btMinus
            // 
            btMinus.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btMinus.Location = new Point(512, 200);
            btMinus.Name = "btMinus";
            btMinus.Size = new Size(48, 23);
            btMinus.TabIndex = 5;
            btMinus.Text = "-";
            btMinus.UseVisualStyleBackColor = true;
            btMinus.Click += btMinus_Click;
            // 
            // button3
            // 
            button3.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            button3.Location = new Point(512, 229);
            button3.Name = "button3";
            button3.Size = new Size(48, 23);
            button3.TabIndex = 5;
            button3.Text = "up";
            button3.UseVisualStyleBackColor = true;
            button3.Click += button3_Click;
            // 
            // button4
            // 
            button4.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            button4.Location = new Point(512, 258);
            button4.Name = "button4";
            button4.Size = new Size(48, 23);
            button4.TabIndex = 5;
            button4.Text = "down";
            button4.UseVisualStyleBackColor = true;
            button4.Click += button4_Click;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(240, 311);
            label6.Name = "label6";
            label6.Size = new Size(44, 15);
            label6.TabIndex = 0;
            label6.Text = "Cluster";
            // 
            // cbCluster
            // 
            cbCluster.AutoSize = true;
            cbCluster.Location = new Point(294, 311);
            cbCluster.Name = "cbCluster";
            cbCluster.Size = new Size(15, 14);
            cbCluster.TabIndex = 3;
            cbCluster.UseVisualStyleBackColor = true;
            // 
            // radioButton1
            // 
            radioButton1.AutoSize = true;
            radioButton1.Checked = true;
            radioButton1.Location = new Point(16, 16);
            radioButton1.Name = "radioButton1";
            radioButton1.Size = new Size(63, 19);
            radioButton1.TabIndex = 6;
            radioButton1.TabStop = true;
            radioButton1.Text = "Default";
            radioButton1.UseVisualStyleBackColor = true;
            // 
            // radioButton2
            // 
            radioButton2.AutoSize = true;
            radioButton2.Location = new Point(85, 16);
            radioButton2.Name = "radioButton2";
            radioButton2.Size = new Size(55, 19);
            radioButton2.TabIndex = 6;
            radioButton2.Text = "Allow";
            radioButton2.UseVisualStyleBackColor = true;
            // 
            // radioButton3
            // 
            radioButton3.AutoSize = true;
            radioButton3.Location = new Point(146, 16);
            radioButton3.Name = "radioButton3";
            radioButton3.Size = new Size(69, 19);
            radioButton3.TabIndex = 6;
            radioButton3.Text = "Disallow";
            radioButton3.UseVisualStyleBackColor = true;
            // 
            // button1
            // 
            button1.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            button1.Location = new Point(395, 516);
            button1.Name = "button1";
            button1.Size = new Size(75, 23);
            button1.TabIndex = 7;
            button1.Text = "Save";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // button2
            // 
            button2.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            button2.Location = new Point(479, 516);
            button2.Name = "button2";
            button2.Size = new Size(75, 23);
            button2.TabIndex = 7;
            button2.Text = "Cancel";
            button2.UseVisualStyleBackColor = true;
            button2.Click += button2_Click;
            // 
            // tbSql
            // 
            tbSql.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            tbSql.Location = new Point(17, 378);
            tbSql.Multiline = true;
            tbSql.Name = "tbSql";
            tbSql.ScrollBars = ScrollBars.Both;
            tbSql.Size = new Size(537, 132);
            tbSql.TabIndex = 8;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(324, 311);
            label8.Name = "label8";
            label8.Size = new Size(60, 15);
            label8.TabIndex = 0;
            label8.Text = "Compress";
            // 
            // cbCompress
            // 
            cbCompress.AutoSize = true;
            cbCompress.Checked = true;
            cbCompress.CheckState = CheckState.Indeterminate;
            cbCompress.Location = new Point(390, 311);
            cbCompress.Name = "cbCompress";
            cbCompress.Size = new Size(15, 14);
            cbCompress.TabIndex = 3;
            cbCompress.ThreeState = true;
            cbCompress.UseVisualStyleBackColor = true;
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new Point(15, 311);
            label9.Name = "label9";
            label9.Size = new Size(61, 15);
            label9.TabIndex = 0;
            label9.Text = "Paritioned";
            // 
            // cbPartitioned
            // 
            cbPartitioned.AutoSize = true;
            cbPartitioned.Checked = true;
            cbPartitioned.CheckState = CheckState.Indeterminate;
            cbPartitioned.Location = new Point(82, 311);
            cbPartitioned.Name = "cbPartitioned";
            cbPartitioned.Size = new Size(15, 14);
            cbPartitioned.TabIndex = 3;
            cbPartitioned.ThreeState = true;
            cbPartitioned.UseVisualStyleBackColor = true;
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Location = new Point(411, 311);
            label10.Name = "label10";
            label10.Size = new Size(95, 15);
            label10.TabIndex = 0;
            label10.Text = "Include null keys";
            // 
            // cbIncludeNulls
            // 
            cbIncludeNulls.AutoSize = true;
            cbIncludeNulls.Checked = true;
            cbIncludeNulls.CheckState = CheckState.Indeterminate;
            cbIncludeNulls.Location = new Point(512, 311);
            cbIncludeNulls.Name = "cbIncludeNulls";
            cbIncludeNulls.Size = new Size(15, 14);
            cbIncludeNulls.TabIndex = 3;
            cbIncludeNulls.ThreeState = true;
            cbIncludeNulls.UseVisualStyleBackColor = true;
            // 
            // linkLabel1
            // 
            linkLabel1.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            linkLabel1.AutoSize = true;
            linkLabel1.Location = new Point(18, 527);
            linkLabel1.Name = "linkLabel1";
            linkLabel1.Size = new Size(33, 15);
            linkLabel1.TabIndex = 9;
            linkLabel1.TabStop = true;
            linkLabel1.Text = "Docs";
            linkLabel1.LinkClicked += linkLabel1_LinkClicked;
            // 
            // label11
            // 
            label11.AutoSize = true;
            label11.Location = new Point(103, 310);
            label11.Name = "label11";
            label11.Size = new Size(103, 15);
            label11.TabIndex = 0;
            label11.Text = "Specification Only";
            // 
            // cbSpecification
            // 
            cbSpecification.AutoSize = true;
            cbSpecification.Location = new Point(212, 311);
            cbSpecification.Name = "cbSpecification";
            cbSpecification.Size = new Size(15, 14);
            cbSpecification.TabIndex = 3;
            cbSpecification.UseVisualStyleBackColor = true;
            // 
            // cbStats
            // 
            cbStats.FormattingEnabled = true;
            cbStats.Items.AddRange(new object[] { "Default", "COLLECT STATISTICS", "COLLECT DETAILED STATISTICS", "COLLECT SAMPLED DETAILED STATISTICS", "COLLECT UNSAMPLED DETAILED STATISTICS" });
            cbStats.Location = new Point(82, 343);
            cbStats.Name = "cbStats";
            cbStats.Size = new Size(227, 23);
            cbStats.TabIndex = 10;
            cbStats.Text = "Default";
            // 
            // label12
            // 
            label12.AutoSize = true;
            label12.Location = new Point(18, 349);
            label12.Name = "label12";
            label12.Size = new Size(50, 15);
            label12.TabIndex = 0;
            label12.Text = "Collect: ";
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(radioButton1);
            groupBox1.Controls.Add(radioButton2);
            groupBox1.Controls.Add(radioButton3);
            groupBox1.Location = new Point(324, 329);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(230, 43);
            groupBox1.TabIndex = 11;
            groupBox1.TabStop = false;
            groupBox1.Text = "Reverse Scan";
            // 
            // tbIndexSchema
            // 
            tbIndexSchema.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            tbIndexSchema.Location = new Point(100, 70);
            tbIndexSchema.Name = "tbIndexSchema";
            tbIndexSchema.Size = new Size(452, 23);
            tbIndexSchema.TabIndex = 12;
            // 
            // AddIndexForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(566, 551);
            Controls.Add(tbIndexSchema);
            Controls.Add(groupBox1);
            Controls.Add(cbStats);
            Controls.Add(linkLabel1);
            Controls.Add(tbSql);
            Controls.Add(button2);
            Controls.Add(button1);
            Controls.Add(button4);
            Controls.Add(button3);
            Controls.Add(btMinus);
            Controls.Add(btAdd);
            Controls.Add(dataGridView1);
            Controls.Add(cbIncludeNulls);
            Controls.Add(cbCompress);
            Controls.Add(cbSpecification);
            Controls.Add(cbPartitioned);
            Controls.Add(cbCluster);
            Controls.Add(cbUnique);
            Controls.Add(tbIndName);
            Controls.Add(label12);
            Controls.Add(label10);
            Controls.Add(label8);
            Controls.Add(label11);
            Controls.Add(label9);
            Controls.Add(label6);
            Controls.Add(label5);
            Controls.Add(label4);
            Controls.Add(label3);
            Controls.Add(tbTabName);
            Controls.Add(label2);
            Controls.Add(tbSchema);
            Controls.Add(label1);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "AddIndexForm";
            Text = "Create Index";
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox tbSchema;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox tbTabName;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox tbIndName;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.CheckBox cbUnique;
        private ThemedDataGridView dataGridView1;
        private System.Windows.Forms.DataGridViewComboBoxColumn colName;
        private System.Windows.Forms.DataGridViewComboBoxColumn colSort;
        private System.Windows.Forms.Button btAdd;
        private System.Windows.Forms.Button btMinus;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.CheckBox cbCluster;
        private System.Windows.Forms.RadioButton radioButton1;
        private System.Windows.Forms.RadioButton radioButton2;
        private System.Windows.Forms.RadioButton radioButton3;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.TextBox tbSql;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.CheckBox cbCompress;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.CheckBox cbPartitioned;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.CheckBox cbIncludeNulls;
        private System.Windows.Forms.LinkLabel linkLabel1;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.CheckBox cbSpecification;
        private System.Windows.Forms.ComboBox cbStats;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TextBox tbIndexSchema;
    }
}
