namespace JustyBaseLegacy.UI.DbForms
{
    partial class SortConnections
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SortConnections));
            dataGridView1 = new System.Windows.Forms.DataGridView();
            ClNum = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ColName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ColDefault = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            btCancel = new System.Windows.Forms.Button();
            btOK = new System.Windows.Forms.Button();
            btUp = new System.Windows.Forms.Button();
            btDown = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            SuspendLayout();
            // 
            // dataGridView1
            // 
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.AllowUserToOrderColumns = true;
            dataGridView1.BackgroundColor = System.Drawing.SystemColors.Control;
            dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView1.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { ClNum, ColName, ColDefault });
            dataGridView1.Location = new System.Drawing.Point(7, 10);
            dataGridView1.Name = "dataGridView1";
            dataGridView1.Size = new System.Drawing.Size(325, 195);
            dataGridView1.TabIndex = 0;
            dataGridView1.CellBeginEdit += dataGridView1_CellBeginEdit;
            dataGridView1.DefaultValuesNeeded += dataGridView1_DefaultValuesNeeded;
            // 
            // ClNum
            // 
            ClNum.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            ClNum.FillWeight = 30F;
            ClNum.HeaderText = "#";
            ClNum.Name = "ClNum";
            ClNum.ReadOnly = true;
            // 
            // ColName
            // 
            ColName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            ColName.HeaderText = "Name";
            ColName.Name = "ColName";
            ColName.ReadOnly = true;
            // 
            // ColDefault
            // 
            ColDefault.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            ColDefault.FillWeight = 30F;
            ColDefault.HeaderText = "Default";
            ColDefault.Name = "ColDefault";
            // 
            // btCancel
            // 
            btCancel.Location = new System.Drawing.Point(250, 211);
            btCancel.Name = "btCancel";
            btCancel.Size = new System.Drawing.Size(75, 26);
            btCancel.TabIndex = 1;
            btCancel.Text = "Cancel";
            btCancel.UseVisualStyleBackColor = true;
            btCancel.Click += btCancel_Click;
            // 
            // btOK
            // 
            btOK.Location = new System.Drawing.Point(164, 211);
            btOK.Name = "btOK";
            btOK.Size = new System.Drawing.Size(75, 26);
            btOK.TabIndex = 1;
            btOK.Text = "OK";
            btOK.UseVisualStyleBackColor = true;
            btOK.Click += btOK_Click;
            // 
            // btUp
            // 
            btUp.Location = new System.Drawing.Point(16, 214);
            btUp.Name = "btUp";
            btUp.Size = new System.Drawing.Size(48, 23);
            btUp.TabIndex = 2;
            btUp.Text = "Up";
            btUp.UseVisualStyleBackColor = true;
            btUp.Click += btUp_Click;
            // 
            // btDown
            // 
            btDown.Location = new System.Drawing.Point(70, 214);
            btDown.Name = "btDown";
            btDown.Size = new System.Drawing.Size(57, 23);
            btDown.TabIndex = 2;
            btDown.Text = "Down";
            btDown.UseVisualStyleBackColor = true;
            btDown.Click += btDown_Click;
            // 
            // SortConnections
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(337, 244);
            Controls.Add(btDown);
            Controls.Add(btUp);
            Controls.Add(btOK);
            Controls.Add(btCancel);
            Controls.Add(dataGridView1);
            Font = new System.Drawing.Font("Century Gothic", 9F);
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MaximumSize = new System.Drawing.Size(353, 283);
            MinimizeBox = false;
            MinimumSize = new System.Drawing.Size(353, 283);
            Name = "SortConnections";
            Opacity = 0.95D;
            Text = "Sort Connections";
            TopMost = true;
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.Button btCancel;
        private System.Windows.Forms.Button btOK;
        private System.Windows.Forms.DataGridViewTextBoxColumn ClNum;
        private System.Windows.Forms.DataGridViewTextBoxColumn ColName;
        private System.Windows.Forms.DataGridViewCheckBoxColumn ColDefault;
        private System.Windows.Forms.Button btUp;
        private System.Windows.Forms.Button btDown;
    }
}
