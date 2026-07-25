namespace JustyBaseLegacy.UI.DbForms
{
    partial class ColumnEditNetezzaForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ColumnEditNetezzaForm));
            groupBox1 = new GroupBox();
            tbColumnDesc = new TextBox();
            button1 = new Button();
            button2 = new Button();
            linkLabel1 = new LinkLabel();
            groupBox1.SuspendLayout();
            SuspendLayout();
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(tbColumnDesc);
            groupBox1.Location = new Point(2, 5);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(466, 153);
            groupBox1.TabIndex = 0;
            groupBox1.TabStop = false;
            groupBox1.Text = "Edit description";
            // 
            // tbColumnDesc
            // 
            tbColumnDesc.Dock = DockStyle.Fill;
            tbColumnDesc.Location = new Point(3, 19);
            tbColumnDesc.Multiline = true;
            tbColumnDesc.Name = "tbColumnDesc";
            tbColumnDesc.Size = new Size(460, 131);
            tbColumnDesc.TabIndex = 0;
            // 
            // button1
            // 
            button1.Location = new Point(312, 164);
            button1.Name = "button1";
            button1.Size = new Size(75, 23);
            button1.TabIndex = 1;
            button1.Text = "Save";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // button2
            // 
            button2.Location = new Point(393, 164);
            button2.Name = "button2";
            button2.Size = new Size(75, 23);
            button2.TabIndex = 1;
            button2.Text = "Cancel";
            button2.UseVisualStyleBackColor = true;
            button2.Click += button2_Click;
            // 
            // linkLabel1
            // 
            linkLabel1.AutoSize = true;
            linkLabel1.Location = new Point(4, 166);
            linkLabel1.Name = "linkLabel1";
            linkLabel1.Size = new Size(33, 15);
            linkLabel1.TabIndex = 2;
            linkLabel1.TabStop = true;
            linkLabel1.Text = "Docs";
            linkLabel1.LinkClicked += linkLabel1_LinkClicked;
            // 
            // ColumnEditNetezzaForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(480, 191);
            Controls.Add(linkLabel1);
            Controls.Add(button2);
            Controls.Add(button1);
            Controls.Add(groupBox1);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "ColumnEditNetezzaForm";
            ShowInTaskbar = false;
            Text = "Edit Column Description";
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.TextBox tbColumnDesc;
        private System.Windows.Forms.LinkLabel linkLabel1;
    }
}
