
namespace JustyBaseLegacy.UI
{
    partial class ContexScripts
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ContexScripts));
            fctbPre = new FastColoredTextBoxNS.FastColoredTextBox();
            fctbMain = new FastColoredTextBoxNS.FastColoredTextBox();
            fctbPost = new FastColoredTextBoxNS.FastColoredTextBox();
            label1 = new Label();
            label2 = new Label();
            label3 = new Label();
            btSave = new Button();
            tbName = new TextBox();
            label4 = new Label();
            groupBox1 = new GroupBox();
            cbSyn = new CheckBox();
            cbExt = new CheckBox();
            cbProc = new CheckBox();
            cbViews = new CheckBox();
            cbTables = new CheckBox();
            listBox1 = new ListBox();
            btAdd = new Button();
            btDelete = new Button();
            button1 = new Button();
            button2 = new Button();
            button3 = new Button();
            button4 = new Button();
            button5 = new Button();
            panelMain = new Panel();
            panelSidebar = new Panel();
            panelHeader = new Panel();
            panelVariables = new Panel();
            ((System.ComponentModel.ISupportInitialize)fctbPre).BeginInit();
            ((System.ComponentModel.ISupportInitialize)fctbMain).BeginInit();
            ((System.ComponentModel.ISupportInitialize)fctbPost).BeginInit();
            groupBox1.SuspendLayout();
            panelMain.SuspendLayout();
            panelSidebar.SuspendLayout();
            panelHeader.SuspendLayout();
            panelVariables.SuspendLayout();
            SuspendLayout();
            // 
            // fctbPre
            // 
            fctbPre.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            fctbPre.AutoCompleteBrackets = true;
            fctbPre.AutoCompleteBracketsList = new char[]
    {
    '(',
    ')',
    '{',
    '}',
    '[',
    ']',
    '"',
    '"',
    '\'',
    '\''
    };
            fctbPre.AutoIndentCharsPatterns = "";
            fctbPre.AutoScrollMinSize = new Size(43, 31);
            fctbPre.BackBrush = null;
            fctbPre.BorderStyle = BorderStyle.FixedSingle;
            fctbPre.BracketsHighlightStrategy = FastColoredTextBoxNS.BracketsHighlightStrategy.Strategy1;
            fctbPre.CharHeight = 15;
            fctbPre.CharWidth = 8;
            fctbPre.CommentPrefix = "--";
            fctbPre.Cursor = Cursors.IBeam;
            fctbPre.DisabledColor = Color.FromArgb(100, 180, 180, 180);
            fctbPre.Font = new Font("Consolas", 10F);
            fctbPre.HighlightingRangeType = FastColoredTextBoxNS.HighlightingRangeType.ChangedRange;
            fctbPre.Hotkeys = resources.GetString("fctbPre.Hotkeys");
            fctbPre.IsReplaceMode = false;
            fctbPre.Language = FastColoredTextBoxNS.Language.SQL;
            fctbPre.LeftBracket = '(';
            fctbPre.Location = new Point(20, 40);
            fctbPre.Margin = new Padding(4);
            fctbPre.MaxBracketSearchIterations = 1000;
            fctbPre.Name = "fctbPre";
            fctbPre.Paddings = new Padding(8);
            fctbPre.RightBracket = ')';
            fctbPre.SelectionColor = Color.FromArgb(60, 100, 150, 255);
            fctbPre.ServiceColors = (FastColoredTextBoxNS.ServiceColors)resources.GetObject("fctbPre.ServiceColors");
            fctbPre.Size = new Size(680, 80);
            fctbPre.TabIndex = 0;
            fctbPre.TextAreaBorder = FastColoredTextBoxNS.TextAreaBorderType.None;
            fctbPre.useUtf8WithoutBoom = false;
            fctbPre.WordWrapMode = FastColoredTextBoxNS.WordWrapMode.WordWrapControlWidth;
            fctbPre.Zoom = 100;
            // 
            // fctbMain
            // 
            fctbMain.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            fctbMain.AutoCompleteBrackets = true;
            fctbMain.AutoCompleteBracketsList = new char[]
    {
    '(',
    ')',
    '{',
    '}',
    '[',
    ']',
    '"',
    '"',
    '\'',
    '\''
    };
            fctbMain.AutoIndentCharsPatterns = "";
            fctbMain.AutoScrollMinSize = new Size(43, 31);
            fctbMain.BackBrush = null;
            fctbMain.BorderStyle = BorderStyle.FixedSingle;
            fctbMain.BracketsHighlightStrategy = FastColoredTextBoxNS.BracketsHighlightStrategy.Strategy1;
            fctbMain.CharHeight = 15;
            fctbMain.CharWidth = 8;
            fctbMain.CommentPrefix = "--";
            fctbMain.Cursor = Cursors.IBeam;
            fctbMain.DisabledColor = Color.FromArgb(100, 180, 180, 180);
            fctbMain.Font = new Font("Consolas", 10F);
            fctbMain.HighlightingRangeType = FastColoredTextBoxNS.HighlightingRangeType.ChangedRange;
            fctbMain.Hotkeys = resources.GetString("fctbMain.Hotkeys");
            fctbMain.IsReplaceMode = false;
            fctbMain.Language = FastColoredTextBoxNS.Language.SQL;
            fctbMain.LeftBracket = '(';
            fctbMain.Location = new Point(20, 160);
            fctbMain.Margin = new Padding(4);
            fctbMain.MaxBracketSearchIterations = 1000;
            fctbMain.Name = "fctbMain";
            fctbMain.Paddings = new Padding(8);
            fctbMain.RightBracket = ')';
            fctbMain.SelectionColor = Color.FromArgb(60, 100, 150, 255);
            fctbMain.ServiceColors = (FastColoredTextBoxNS.ServiceColors)resources.GetObject("fctbMain.ServiceColors");
            fctbMain.Size = new Size(680, 150);
            fctbMain.TabIndex = 1;
            fctbMain.TextAreaBorder = FastColoredTextBoxNS.TextAreaBorderType.None;
            fctbMain.useUtf8WithoutBoom = false;
            fctbMain.WordWrapMode = FastColoredTextBoxNS.WordWrapMode.WordWrapControlWidth;
            fctbMain.Zoom = 100;
            // 
            // fctbPost
            // 
            fctbPost.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            fctbPost.AutoCompleteBrackets = true;
            fctbPost.AutoCompleteBracketsList = new char[]
    {
    '(',
    ')',
    '{',
    '}',
    '[',
    ']',
    '"',
    '"',
    '\'',
    '\''
    };
            fctbPost.AutoIndentCharsPatterns = "";
            fctbPost.AutoScrollMinSize = new Size(43, 31);
            fctbPost.BackBrush = null;
            fctbPost.BorderStyle = BorderStyle.FixedSingle;
            fctbPost.BracketsHighlightStrategy = FastColoredTextBoxNS.BracketsHighlightStrategy.Strategy1;
            fctbPost.CharHeight = 15;
            fctbPost.CharWidth = 8;
            fctbPost.CommentPrefix = "--";
            fctbPost.Cursor = Cursors.IBeam;
            fctbPost.DisabledColor = Color.FromArgb(100, 180, 180, 180);
            fctbPost.Font = new Font("Consolas", 10F);
            fctbPost.HighlightingRangeType = FastColoredTextBoxNS.HighlightingRangeType.ChangedRange;
            fctbPost.Hotkeys = resources.GetString("fctbPost.Hotkeys");
            fctbPost.IsReplaceMode = false;
            fctbPost.Language = FastColoredTextBoxNS.Language.SQL;
            fctbPost.LeftBracket = '(';
            fctbPost.Location = new Point(20, 370);
            fctbPost.Margin = new Padding(4);
            fctbPost.MaxBracketSearchIterations = 1000;
            fctbPost.Name = "fctbPost";
            fctbPost.Paddings = new Padding(8);
            fctbPost.RightBracket = ')';
            fctbPost.SelectionColor = Color.FromArgb(60, 100, 150, 255);
            fctbPost.ServiceColors = (FastColoredTextBoxNS.ServiceColors)resources.GetObject("fctbPost.ServiceColors");
            fctbPost.Size = new Size(680, 80);
            fctbPost.TabIndex = 2;
            fctbPost.TextAreaBorder = FastColoredTextBoxNS.TextAreaBorderType.None;
            fctbPost.useUtf8WithoutBoom = false;
            fctbPost.WordWrapMode = FastColoredTextBoxNS.WordWrapMode.WordWrapControlWidth;
            fctbPost.Zoom = 100;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            label1.ForeColor = Color.FromArgb(64, 64, 64);
            label1.Location = new Point(20, 16);
            label1.Name = "label1";
            label1.Size = new Size(75, 19);
            label1.TabIndex = 1;
            label1.Text = "Pre Script";
            // 
            // label2
            // 
            label2.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            label2.ForeColor = Color.FromArgb(64, 64, 64);
            label2.Location = new Point(20, 330);
            label2.Name = "label2";
            label2.Size = new Size(81, 19);
            label2.TabIndex = 1;
            label2.Text = "Post Script";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            label3.ForeColor = Color.FromArgb(64, 64, 64);
            label3.Location = new Point(20, 136);
            label3.Name = "label3";
            label3.Size = new Size(85, 19);
            label3.TabIndex = 1;
            label3.Text = "Main Script";
            // 
            // btSave
            // 
            btSave.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btSave.BackColor = Color.FromArgb(0, 122, 204);
            btSave.FlatAppearance.BorderSize = 0;
            btSave.FlatStyle = FlatStyle.Flat;
            btSave.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btSave.ForeColor = Color.White;
            btSave.Location = new Point(20, 20);
            btSave.Name = "btSave";
            btSave.Size = new Size(120, 40);
            btSave.TabIndex = 2;
            btSave.Text = "Save Changes";
            btSave.UseVisualStyleBackColor = false;
            btSave.Click += BtSave_Click;
            // 
            // tbName
            // 
            tbName.BorderStyle = BorderStyle.FixedSingle;
            tbName.Font = new Font("Segoe UI", 10F);
            tbName.Location = new Point(80, 16);
            tbName.Name = "tbName";
            tbName.Size = new Size(250, 25);
            tbName.TabIndex = 3;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            label4.ForeColor = Color.FromArgb(64, 64, 64);
            label4.Location = new Point(20, 19);
            label4.Name = "label4";
            label4.Size = new Size(53, 19);
            label4.TabIndex = 4;
            label4.Text = "Name:";
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(cbSyn);
            groupBox1.Controls.Add(cbExt);
            groupBox1.Controls.Add(cbProc);
            groupBox1.Controls.Add(cbViews);
            groupBox1.Controls.Add(cbTables);
            groupBox1.FlatStyle = FlatStyle.Flat;
            groupBox1.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            groupBox1.ForeColor = Color.FromArgb(64, 64, 64);
            groupBox1.Location = new Point(20, 80);
            groupBox1.Name = "groupBox1";
            groupBox1.Padding = new Padding(15, 10, 15, 15);
            groupBox1.Size = new Size(200, 180);
            groupBox1.TabIndex = 6;
            groupBox1.TabStop = false;
            groupBox1.Text = "Applicable To";
            // 
            // cbSyn
            // 
            cbSyn.AutoSize = true;
            cbSyn.FlatStyle = FlatStyle.Flat;
            cbSyn.Font = new Font("Segoe UI", 9F);
            cbSyn.Location = new Point(20, 140);
            cbSyn.Name = "cbSyn";
            cbSyn.Size = new Size(78, 19);
            cbSyn.TabIndex = 8;
            cbSyn.Text = "Synonyms";
            cbSyn.UseVisualStyleBackColor = true;
            // 
            // cbExt
            // 
            cbExt.AutoSize = true;
            cbExt.FlatStyle = FlatStyle.Flat;
            cbExt.Font = new Font("Segoe UI", 9F);
            cbExt.Location = new Point(20, 115);
            cbExt.Name = "cbExt";
            cbExt.Size = new Size(100, 19);
            cbExt.TabIndex = 9;
            cbExt.Text = "External Tables";
            cbExt.UseVisualStyleBackColor = true;
            // 
            // cbProc
            // 
            cbProc.AutoSize = true;
            cbProc.FlatStyle = FlatStyle.Flat;
            cbProc.Font = new Font("Segoe UI", 9F);
            cbProc.Location = new Point(20, 90);
            cbProc.Name = "cbProc";
            cbProc.Size = new Size(82, 19);
            cbProc.TabIndex = 10;
            cbProc.Text = "Procedures";
            cbProc.UseVisualStyleBackColor = true;
            // 
            // cbViews
            // 
            cbViews.AutoSize = true;
            cbViews.FlatStyle = FlatStyle.Flat;
            cbViews.Font = new Font("Segoe UI", 9F);
            cbViews.Location = new Point(20, 65);
            cbViews.Name = "cbViews";
            cbViews.Size = new Size(53, 19);
            cbViews.TabIndex = 11;
            cbViews.Text = "Views";
            cbViews.UseVisualStyleBackColor = true;
            // 
            // cbTables
            // 
            cbTables.AutoSize = true;
            cbTables.FlatStyle = FlatStyle.Flat;
            cbTables.Font = new Font("Segoe UI", 9F);
            cbTables.Location = new Point(20, 40);
            cbTables.Name = "cbTables";
            cbTables.Size = new Size(56, 19);
            cbTables.TabIndex = 12;
            cbTables.Text = "Tables";
            cbTables.UseVisualStyleBackColor = true;
            // 
            // listBox1
            // 
            listBox1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            listBox1.BorderStyle = BorderStyle.FixedSingle;
            listBox1.Font = new Font("Segoe UI", 9F);
            listBox1.FormattingEnabled = true;
            listBox1.Location = new Point(20, 280);
            listBox1.Name = "listBox1";
            listBox1.Size = new Size(200, 152);
            listBox1.TabIndex = 7;
            listBox1.SelectedIndexChanged += ListBox1_SelectedIndexChanged;
            // 
            // btAdd
            // 
            btAdd.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btAdd.BackColor = Color.FromArgb(46, 204, 113);
            btAdd.FlatAppearance.BorderSize = 0;
            btAdd.FlatStyle = FlatStyle.Flat;
            btAdd.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btAdd.ForeColor = Color.White;
            btAdd.Location = new Point(20, 460);
            btAdd.Name = "btAdd";
            btAdd.Size = new Size(90, 35);
            btAdd.TabIndex = 8;
            btAdd.Text = "Add";
            btAdd.UseVisualStyleBackColor = false;
            btAdd.Click += BtAdd_Click;
            // 
            // btDelete
            // 
            btDelete.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btDelete.BackColor = Color.FromArgb(231, 76, 60);
            btDelete.FlatAppearance.BorderSize = 0;
            btDelete.FlatStyle = FlatStyle.Flat;
            btDelete.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btDelete.ForeColor = Color.White;
            btDelete.Location = new Point(130, 460);
            btDelete.Name = "btDelete";
            btDelete.Size = new Size(90, 35);
            btDelete.TabIndex = 8;
            btDelete.Text = "Delete";
            btDelete.UseVisualStyleBackColor = false;
            btDelete.Click += BtDelete_Click;
            // 
            // button1
            // 
            button1.BackColor = Color.FromArgb(108, 117, 125);
            button1.FlatAppearance.BorderSize = 0;
            button1.FlatStyle = FlatStyle.Flat;
            button1.Font = new Font("Consolas", 9F);
            button1.ForeColor = Color.White;
            button1.Location = new Point(100, 10);
            button1.Name = "button1";
            button1.Size = new Size(80, 30);
            button1.TabIndex = 10;
            button1.Text = "$schema";
            button1.UseVisualStyleBackColor = false;
            button1.Click += Button1_Click;
            // 
            // button2
            // 
            button2.BackColor = Color.FromArgb(108, 117, 125);
            button2.FlatAppearance.BorderSize = 0;
            button2.FlatStyle = FlatStyle.Flat;
            button2.Font = new Font("Consolas", 9F);
            button2.ForeColor = Color.White;
            button2.Location = new Point(190, 10);
            button2.Name = "button2";
            button2.Size = new Size(80, 30);
            button2.TabIndex = 10;
            button2.Text = "$name";
            button2.UseVisualStyleBackColor = false;
            button2.Click += Button1_Click;
            // 
            // button3
            // 
            button3.BackColor = Color.FromArgb(108, 117, 125);
            button3.FlatAppearance.BorderSize = 0;
            button3.FlatStyle = FlatStyle.Flat;
            button3.Font = new Font("Consolas", 9F);
            button3.ForeColor = Color.White;
            button3.Location = new Point(280, 10);
            button3.Name = "button3";
            button3.Size = new Size(80, 30);
            button3.TabIndex = 10;
            button3.Text = "$columns";
            button3.UseVisualStyleBackColor = false;
            button3.Click += Button1_Click;
            // 
            // button4
            // 
            button4.BackColor = Color.FromArgb(108, 117, 125);
            button4.FlatAppearance.BorderSize = 0;
            button4.FlatStyle = FlatStyle.Flat;
            button4.Font = new Font("Consolas", 9F);
            button4.ForeColor = Color.White;
            button4.Location = new Point(370, 10);
            button4.Name = "button4";
            button4.Size = new Size(80, 30);
            button4.TabIndex = 10;
            button4.Text = "$signature";
            button4.UseVisualStyleBackColor = false;
            button4.Click += Button1_Click;
            // 
            // button5
            // 
            button5.BackColor = Color.FromArgb(108, 117, 125);
            button5.FlatAppearance.BorderSize = 0;
            button5.FlatStyle = FlatStyle.Flat;
            button5.Font = new Font("Consolas", 9F);
            button5.ForeColor = Color.White;
            button5.Location = new Point(10, 10);
            button5.Name = "button5";
            button5.Size = new Size(80, 30);
            button5.TabIndex = 10;
            button5.Text = "$db";
            button5.UseVisualStyleBackColor = false;
            button5.Click += Button1_Click;
            // 
            // panelMain
            // 
            panelMain.Controls.Add(fctbPre);
            panelMain.Controls.Add(fctbMain);
            panelMain.Controls.Add(fctbPost);
            panelMain.Controls.Add(label1);
            panelMain.Controls.Add(label2);
            panelMain.Controls.Add(label3);
            panelMain.Dock = DockStyle.Fill;
            panelMain.Location = new Point(280, 60);
            panelMain.Name = "panelMain";
            panelMain.Padding = new Padding(20);
            panelMain.Size = new Size(720, 470);
            panelMain.TabIndex = 11;
            // 
            // panelSidebar
            // 
            panelSidebar.BackColor = Color.FromArgb(248, 249, 250);
            panelSidebar.Controls.Add(groupBox1);
            panelSidebar.Controls.Add(listBox1);
            panelSidebar.Controls.Add(btAdd);
            panelSidebar.Controls.Add(btDelete);
            panelSidebar.Controls.Add(btSave);
            panelSidebar.Dock = DockStyle.Left;
            panelSidebar.Location = new Point(0, 60);
            panelSidebar.Name = "panelSidebar";
            panelSidebar.Padding = new Padding(20);
            panelSidebar.Size = new Size(280, 520);
            panelSidebar.TabIndex = 12;
            // 
            // panelHeader
            // 
            panelHeader.BackColor = Color.FromArgb(255, 255, 255);
            panelHeader.Controls.Add(label4);
            panelHeader.Controls.Add(tbName);
            panelHeader.Dock = DockStyle.Top;
            panelHeader.Location = new Point(0, 0);
            panelHeader.Name = "panelHeader";
            panelHeader.Padding = new Padding(20);
            panelHeader.Size = new Size(1000, 60);
            panelHeader.TabIndex = 13;
            // 
            // panelVariables
            // 
            panelVariables.BackColor = Color.FromArgb(233, 236, 239);
            panelVariables.Controls.Add(button5);
            panelVariables.Controls.Add(button1);
            panelVariables.Controls.Add(button2);
            panelVariables.Controls.Add(button3);
            panelVariables.Controls.Add(button4);
            panelVariables.Dock = DockStyle.Bottom;
            panelVariables.Location = new Point(280, 530);
            panelVariables.Name = "panelVariables";
            panelVariables.Padding = new Padding(10);
            panelVariables.Size = new Size(720, 50);
            panelVariables.TabIndex = 14;
            // 
            // ContexScripts
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            ClientSize = new Size(1000, 580);
            Controls.Add(panelMain);
            Controls.Add(panelVariables);
            Controls.Add(panelSidebar);
            Controls.Add(panelHeader);
            Font = new Font("Segoe UI", 9F);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MinimumSize = new Size(900, 600);
            Name = "ContexScripts";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Script Manager";
            ((System.ComponentModel.ISupportInitialize)fctbPre).EndInit();
            ((System.ComponentModel.ISupportInitialize)fctbMain).EndInit();
            ((System.ComponentModel.ISupportInitialize)fctbPost).EndInit();
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            panelMain.ResumeLayout(false);
            panelMain.PerformLayout();
            panelSidebar.ResumeLayout(false);
            panelHeader.ResumeLayout(false);
            panelHeader.PerformLayout();
            panelVariables.ResumeLayout(false);
            ResumeLayout(false);

        }

        #endregion

        private FastColoredTextBoxNS.FastColoredTextBox fctbPre;
        private FastColoredTextBoxNS.FastColoredTextBox fctbMain;
        private FastColoredTextBoxNS.FastColoredTextBox fctbPost;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button btSave;
        private System.Windows.Forms.TextBox tbName;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.CheckBox cbSyn;
        private System.Windows.Forms.CheckBox cbExt;
        private System.Windows.Forms.CheckBox cbProc;
        private System.Windows.Forms.CheckBox cbViews;
        private System.Windows.Forms.CheckBox cbTables;
        private System.Windows.Forms.ListBox listBox1;
        private System.Windows.Forms.Button btAdd;
        private System.Windows.Forms.Button btDelete;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.Button button5;
        private System.Windows.Forms.Panel panelMain;
        private System.Windows.Forms.Panel panelSidebar;
        private System.Windows.Forms.Panel panelHeader;
        private System.Windows.Forms.Panel panelVariables;
    }
}
