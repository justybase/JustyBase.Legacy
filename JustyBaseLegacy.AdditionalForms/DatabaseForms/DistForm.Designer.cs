namespace JustyBaseLegacy.UI.DbForms
{
    partial class DistForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DistForm));
            lbAvaiable = new ListBox();
            lbDist = new ListBox();
            btToDist = new Button();
            btRemoveFromDist = new Button();
            label1 = new Label();
            label2 = new Label();
            linkLabel1 = new LinkLabel();
            btSave = new Button();
            btCancel = new Button();
            SuspendLayout();
            // 
            // lbAvaiable
            // 
            lbAvaiable.FormattingEnabled = true;
            lbAvaiable.Location = new Point(12, 37);
            lbAvaiable.Name = "lbAvaiable";
            lbAvaiable.SelectionMode = SelectionMode.MultiSimple;
            lbAvaiable.Size = new Size(232, 319);
            lbAvaiable.TabIndex = 0;
            // 
            // lbDist
            // 
            lbDist.FormattingEnabled = true;
            lbDist.Location = new Point(306, 37);
            lbDist.Name = "lbDist";
            lbDist.SelectionMode = SelectionMode.MultiSimple;
            lbDist.Size = new Size(234, 319);
            lbDist.TabIndex = 0;
            // 
            // btToDist
            // 
            btToDist.Image = AdditionalForms.Properties.Resources.arrow_right;
            btToDist.Location = new Point(250, 107);
            btToDist.Name = "btToDist";
            btToDist.Size = new Size(50, 23);
            btToDist.TabIndex = 1;
            btToDist.UseVisualStyleBackColor = true;
            btToDist.Click += btToDist_Click;
            // 
            // btRemoveFromDist
            // 
            btRemoveFromDist.Image = AdditionalForms.Properties.Resources.arrow_left;
            btRemoveFromDist.Location = new Point(250, 136);
            btRemoveFromDist.Name = "btRemoveFromDist";
            btRemoveFromDist.Size = new Size(50, 23);
            btRemoveFromDist.TabIndex = 1;
            btRemoveFromDist.UseVisualStyleBackColor = true;
            btRemoveFromDist.Click += btRemoveFromDist_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 19);
            label1.Name = "label1";
            label1.Size = new Size(106, 15);
            label1.TabIndex = 2;
            label1.Text = "Available Columns";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(306, 21);
            label2.Name = "label2";
            label2.Size = new Size(118, 15);
            label2.TabIndex = 2;
            label2.Text = "Distribution columns";
            // 
            // linkLabel1
            // 
            linkLabel1.AutoSize = true;
            linkLabel1.Location = new Point(12, 398);
            linkLabel1.Name = "linkLabel1";
            linkLabel1.Size = new Size(33, 15);
            linkLabel1.TabIndex = 3;
            linkLabel1.TabStop = true;
            linkLabel1.Text = "Docs";
            linkLabel1.LinkClicked += linkLabel1_LinkClicked;
            // 
            // btSave
            // 
            btSave.Location = new Point(366, 390);
            btSave.Name = "btSave";
            btSave.Size = new Size(75, 23);
            btSave.TabIndex = 4;
            btSave.Text = "Save";
            btSave.UseVisualStyleBackColor = true;
            btSave.Click += btSave_Click;
            // 
            // btCancel
            // 
            btCancel.Location = new Point(465, 390);
            btCancel.Name = "btCancel";
            btCancel.Size = new Size(75, 23);
            btCancel.TabIndex = 4;
            btCancel.Text = "Cancel";
            btCancel.UseVisualStyleBackColor = true;
            btCancel.Click += btCancel_Click;
            // 
            // DistForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(560, 423);
            Controls.Add(btCancel);
            Controls.Add(btSave);
            Controls.Add(linkLabel1);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(btRemoveFromDist);
            Controls.Add(btToDist);
            Controls.Add(lbDist);
            Controls.Add(lbAvaiable);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximumSize = new Size(576, 462);
            MinimumSize = new Size(576, 462);
            Name = "DistForm";
            Text = "Change distribution";
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox lbAvaiable;
        private System.Windows.Forms.ListBox lbDist;
        private System.Windows.Forms.Button btToDist;
        private System.Windows.Forms.Button btRemoveFromDist;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.LinkLabel linkLabel1;
        private System.Windows.Forms.Button btSave;
        private System.Windows.Forms.Button btCancel;
    }
}
