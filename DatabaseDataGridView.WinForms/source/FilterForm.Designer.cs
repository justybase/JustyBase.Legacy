
namespace DatabaseDataGridView.WinForms
{
    partial class FilterForm
    {
        /// <summary> 
        /// Wymagana zmienna projektanta.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Dispose all used resources.
        /// </summary>
        /// <param name="disposing">True to dispose managed resources; otherwise false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support; do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.tbFind = new System.Windows.Forms.TextBox();
            this.listView1 = new System.Windows.Forms.ListView();
            this.btConfirm = new System.Windows.Forms.Button();
            this.searchTimer = new System.Windows.Forms.Timer(this.components);
            this.button1 = new System.Windows.Forms.Button();
            this.btNull = new System.Windows.Forms.Button();
            this.btNotNull = new System.Windows.Forms.Button();
            this.lbInfo = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // tbFind
            // 
            this.tbFind.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbFind.Location = new System.Drawing.Point(0, 0);
            this.tbFind.Margin = new System.Windows.Forms.Padding(0);
            this.tbFind.Name = "tbFind";
            this.tbFind.Size = new System.Drawing.Size(151, 23);
            this.tbFind.TabIndex = 0;
            this.tbFind.TextChanged += new System.EventHandler(this.TbFind_TextChanged);
            // 
            // listView1
            // 
            this.listView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listView1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.listView1.FullRowSelect = true;
            this.listView1.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.listView1.Location = new System.Drawing.Point(3, 100);
            this.listView1.Name = "listView1";
            this.listView1.ShowGroups = false;
            this.listView1.Size = new System.Drawing.Size(145, 229);
            this.listView1.TabIndex = 1;
            this.listView1.UseCompatibleStateImageBehavior = false;
            this.listView1.View = System.Windows.Forms.View.Details;
            this.listView1.VirtualMode = true;
            this.listView1.ItemSelectionChanged += new System.Windows.Forms.ListViewItemSelectionChangedEventHandler(this.ListView1_ItemSelectionChanged);
            // 
            // btConfirm
            // 
            this.btConfirm.Location = new System.Drawing.Point(0, 23);
            this.btConfirm.Margin = new System.Windows.Forms.Padding(0);
            this.btConfirm.Name = "btConfirm";
            this.btConfirm.Size = new System.Drawing.Size(60, 23);
            this.btConfirm.TabIndex = 4;
            this.btConfirm.Text = "Apply";
            this.btConfirm.UseVisualStyleBackColor = true;
            this.btConfirm.Click += new System.EventHandler(this.BtConfirm_Click);
            // 
            // searchTimer
            // 
            this.searchTimer.Interval = 300;
            this.searchTimer.Tick += new System.EventHandler(this.Timer1_Tick);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(63, 23);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(67, 23);
            this.button1.TabIndex = 5;
            this.button1.Text = "Clear";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.Button1_Click);
            // 
            // btNull
            // 
            this.btNull.Location = new System.Drawing.Point(0, 46);
            this.btNull.Name = "btNull";
            this.btNull.Size = new System.Drawing.Size(60, 23);
            this.btNull.TabIndex = 6;
            this.btNull.Text = "Is null";
            this.btNull.UseVisualStyleBackColor = true;
            this.btNull.Click += new System.EventHandler(this.BtNull_Click);
            // 
            // btNotNull
            // 
            this.btNotNull.Location = new System.Drawing.Point(63, 46);
            this.btNotNull.Name = "btNotNull";
            this.btNotNull.Size = new System.Drawing.Size(67, 23);
            this.btNotNull.TabIndex = 6;
            this.btNotNull.Text = "Is not null";
            this.btNotNull.UseVisualStyleBackColor = true;
            this.btNotNull.Click += new System.EventHandler(this.BtNotNull_Click);
            // 
            // lbInfo
            // 
            this.lbInfo.AutoSize = true;
            this.lbInfo.Location = new System.Drawing.Point(7, 76);
            this.lbInfo.Name = "lbInfo";
            this.lbInfo.Size = new System.Drawing.Size(38, 15);
            this.lbInfo.TabIndex = 7;
            this.lbInfo.Text = "label1";
            // 
            // FilterForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.Controls.Add(this.lbInfo);
            this.Controls.Add(this.btNotNull);
            this.Controls.Add(this.btNull);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.btConfirm);
            this.Controls.Add(this.listView1);
            this.Controls.Add(this.tbFind);
            this.DoubleBuffered = true;
            this.MinimumSize = new System.Drawing.Size(200, 200);
            this.Name = "FilterForm";
            this.Size = new System.Drawing.Size(240, 329);
            this.Load += new System.EventHandler(this.FilterForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox tbFind;
        private System.Windows.Forms.ListView listView1;
        private System.Windows.Forms.Button btConfirm;
        private System.Windows.Forms.Timer searchTimer;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button btNull;
        private System.Windows.Forms.Button btNotNull;
        private System.Windows.Forms.Label lbInfo;
    }
}
