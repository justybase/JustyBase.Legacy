namespace JustyBaseLegacy.UI
{
    partial class History
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
            DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle2 = new DataGridViewCellStyle();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(History));
            textBox1 = new TextBox();
            splitContainer1 = new SplitContainer();
            historyDataGridView = new ThemedDataGridView();
            fastColoredTextBox2 = new FastColoredTextBoxNS.FastColoredTextBox();
            labelSqlPreview = new Label();
            fastColoredTextBox1 = new FastColoredTextBoxNS.FastColoredTextBox();
            button1 = new Button();
            panelHeader = new Panel();
            panelSearch = new Panel();
            labelSearch = new Label();
            labelResults = new Label();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)historyDataGridView).BeginInit();
            ((System.ComponentModel.ISupportInitialize)fastColoredTextBox2).BeginInit();
            ((System.ComponentModel.ISupportInitialize)fastColoredTextBox1).BeginInit();
            panelHeader.SuspendLayout();
            panelSearch.SuspendLayout();
            SuspendLayout();
            // 
            // textBox1
            // 
            textBox1.BorderStyle = BorderStyle.FixedSingle;
            textBox1.Font = new Font("Segoe UI", 11F);
            textBox1.ForeColor = Color.FromArgb(108, 117, 125);
            textBox1.Location = new Point(20, 35);
            textBox1.Name = "textBox1";
            textBox1.PlaceholderText = "Search in history...";
            textBox1.Size = new Size(350, 27);
            textBox1.TabIndex = 0;
            textBox1.MouseClick += TextBox1_MouseClick;
            // 
            // splitContainer1
            // 
            splitContainer1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            splitContainer1.BackColor = Color.FromArgb(233, 236, 239);
            splitContainer1.Cursor = Cursors.HSplit;
            splitContainer1.Location = new Point(20, 200);
            splitContainer1.Name = "splitContainer1";
            splitContainer1.Orientation = Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(historyDataGridView);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(fastColoredTextBox2);
            splitContainer1.Panel2.Controls.Add(labelSqlPreview);
            splitContainer1.Size = new Size(1130, 488);
            splitContainer1.SplitterDistance = 242;
            splitContainer1.SplitterWidth = 8;
            splitContainer1.TabIndex = 7;
            // 
            // historyDataGridView
            // 
            historyDataGridView.AllowUserToAddRows = false;
            historyDataGridView.AllowUserToDeleteRows = false;
            historyDataGridView.BackgroundColor = Color.White;
            historyDataGridView.BorderStyle = BorderStyle.None;
            historyDataGridView.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            historyDataGridView.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = Color.FromArgb(248, 249, 250);
            dataGridViewCellStyle1.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            dataGridViewCellStyle1.ForeColor = Color.FromArgb(73, 80, 87);
            dataGridViewCellStyle1.SelectionBackColor = Color.FromArgb(248, 249, 250);
            dataGridViewCellStyle1.SelectionForeColor = Color.FromArgb(73, 80, 87);
            dataGridViewCellStyle1.WrapMode = DataGridViewTriState.True;
            historyDataGridView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            historyDataGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle2.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = Color.White;
            dataGridViewCellStyle2.Font = new Font("Segoe UI", 9F);
            dataGridViewCellStyle2.ForeColor = Color.FromArgb(73, 80, 87);
            dataGridViewCellStyle2.SelectionBackColor = Color.FromArgb(0, 123, 255);
            dataGridViewCellStyle2.SelectionForeColor = Color.White;
            dataGridViewCellStyle2.WrapMode = DataGridViewTriState.False;
            historyDataGridView.DefaultCellStyle = dataGridViewCellStyle2;
            historyDataGridView.Dock = DockStyle.Fill;
            historyDataGridView.EnableHeadersVisualStyles = false;
            historyDataGridView.GridColor = Color.FromArgb(233, 236, 239);
            historyDataGridView.Location = new Point(0, 0);
            historyDataGridView.Name = "historyDataGridView";
            historyDataGridView.ReadOnly = true;
            historyDataGridView.RowHeadersVisible = false;
            historyDataGridView.RowTemplate.Height = 32;
            historyDataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            historyDataGridView.Size = new Size(1130, 242);
            historyDataGridView.TabIndex = 2;
            // 
            // fastColoredTextBox2
            // 
            fastColoredTextBox2.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            fastColoredTextBox2.AutoCompleteBracketsList = new char[]
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
            fastColoredTextBox2.AutoIndentCharsPatterns = "";
            fastColoredTextBox2.AutoScrollMinSize = new Size(47, 35);
            fastColoredTextBox2.BackBrush = null;
            fastColoredTextBox2.BackColor = Color.FromArgb(248, 249, 250);
            fastColoredTextBox2.BracketsHighlightStrategy = FastColoredTextBoxNS.BracketsHighlightStrategy.Strategy1;
            fastColoredTextBox2.CharHeight = 15;
            fastColoredTextBox2.CharWidth = 8;
            fastColoredTextBox2.CommentPrefix = "--";
            fastColoredTextBox2.Cursor = Cursors.IBeam;
            fastColoredTextBox2.DisabledColor = Color.FromArgb(100, 180, 180, 180);
            fastColoredTextBox2.Font = new Font("Consolas", 10F);
            fastColoredTextBox2.HighlightingRangeType = FastColoredTextBoxNS.HighlightingRangeType.ChangedRange;
            fastColoredTextBox2.IsReplaceMode = false;
            fastColoredTextBox2.Language = FastColoredTextBoxNS.Language.SQL;
            fastColoredTextBox2.LeftBracket = '(';
            fastColoredTextBox2.Location = new Point(0, 25);
            fastColoredTextBox2.MaxBracketSearchIterations = 1000;
            fastColoredTextBox2.Name = "fastColoredTextBox2";
            fastColoredTextBox2.Paddings = new Padding(10);
            fastColoredTextBox2.ReadOnly = true;
            fastColoredTextBox2.RightBracket = ')';
            fastColoredTextBox2.SelectionColor = Color.FromArgb(60, 100, 150, 255);
            fastColoredTextBox2.ServiceColors = (FastColoredTextBoxNS.ServiceColors)resources.GetObject("fastColoredTextBox2.ServiceColors");
            fastColoredTextBox2.Size = new Size(1130, 197);
            fastColoredTextBox2.TabIndex = 0;
            fastColoredTextBox2.TextAreaBorder = FastColoredTextBoxNS.TextAreaBorderType.None;
            fastColoredTextBox2.useUtf8WithoutBoom = false;
            fastColoredTextBox2.WordWrapMode = FastColoredTextBoxNS.WordWrapMode.WordWrapControlWidth;
            fastColoredTextBox2.Zoom = 100;
            // 
            // labelSqlPreview
            // 
            labelSqlPreview.AutoSize = true;
            labelSqlPreview.BackColor = Color.FromArgb(248, 249, 250);
            labelSqlPreview.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            labelSqlPreview.ForeColor = Color.FromArgb(73, 80, 87);
            labelSqlPreview.Location = new Point(10, 5);
            labelSqlPreview.Name = "labelSqlPreview";
            labelSqlPreview.Size = new Size(93, 19);
            labelSqlPreview.TabIndex = 15;
            labelSqlPreview.Text = "SQL Preview";
            // 
            // fastColoredTextBox1
            // 
            fastColoredTextBox1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            fastColoredTextBox1.AutoCompleteBrackets = true;
            fastColoredTextBox1.AutoCompleteBracketsList = new char[]
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
            fastColoredTextBox1.AutoIndentCharsPatterns = "";
            fastColoredTextBox1.AutoScrollMinSize = new Size(210, 31);
            fastColoredTextBox1.BackBrush = null;
            fastColoredTextBox1.BorderStyle = BorderStyle.FixedSingle;
            fastColoredTextBox1.BracketsHighlightStrategy = FastColoredTextBoxNS.BracketsHighlightStrategy.Strategy1;
            fastColoredTextBox1.CharHeight = 15;
            fastColoredTextBox1.CharWidth = 8;
            fastColoredTextBox1.CommentPrefix = "--";
            fastColoredTextBox1.Cursor = Cursors.IBeam;
            fastColoredTextBox1.DisabledColor = Color.FromArgb(100, 180, 180, 180);
            fastColoredTextBox1.Font = new Font("Consolas", 10F);
            fastColoredTextBox1.HighlightingRangeType = FastColoredTextBoxNS.HighlightingRangeType.ChangedRange;
            fastColoredTextBox1.IsReplaceMode = false;
            fastColoredTextBox1.Language = FastColoredTextBoxNS.Language.SQL;
            fastColoredTextBox1.LeftBracket = '(';
            fastColoredTextBox1.Location = new Point(23, 106);
            fastColoredTextBox1.MaxBracketSearchIterations = 1000;
            fastColoredTextBox1.Name = "fastColoredTextBox1";
            fastColoredTextBox1.Paddings = new Padding(8);
            fastColoredTextBox1.RightBracket = ')';
            fastColoredTextBox1.SelectionColor = Color.FromArgb(60, 100, 150, 255);
            fastColoredTextBox1.ServiceColors = (FastColoredTextBoxNS.ServiceColors)resources.GetObject("fastColoredTextBox1.ServiceColors");
            fastColoredTextBox1.ShowLineNumbers = false;
            fastColoredTextBox1.Size = new Size(1130, 60);
            fastColoredTextBox1.TabIndex = 5;
            fastColoredTextBox1.Text = "--and DBNAME = 'SOME_DB'";
            fastColoredTextBox1.TextAreaBorder = FastColoredTextBoxNS.TextAreaBorderType.None;
            fastColoredTextBox1.useUtf8WithoutBoom = false;
            fastColoredTextBox1.WordWrapMode = FastColoredTextBoxNS.WordWrapMode.WordWrapControlWidth;
            fastColoredTextBox1.Zoom = 100;
            fastColoredTextBox1.KeyDown += FastColoredTextBox1_KeyDown;
            // 
            // button1
            // 
            button1.BackColor = Color.FromArgb(0, 123, 255);
            button1.FlatAppearance.BorderSize = 0;
            button1.FlatStyle = FlatStyle.Flat;
            button1.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            button1.ForeColor = Color.White;
            button1.Location = new Point(390, 35);
            button1.Name = "button1";
            button1.Size = new Size(100, 35);
            button1.TabIndex = 8;
            button1.Text = "Search";
            button1.UseVisualStyleBackColor = false;
            button1.Click += Search_Click;
            // 
            // panelHeader
            // 
            panelHeader.BackColor = Color.White;
            panelHeader.Controls.Add(panelSearch);
            panelHeader.Dock = DockStyle.Top;
            panelHeader.Location = new Point(0, 0);
            panelHeader.Name = "panelHeader";
            panelHeader.Padding = new Padding(20);
            panelHeader.Size = new Size(1200, 120);
            panelHeader.TabIndex = 9;
            // 
            // panelSearch
            // 
            panelSearch.Controls.Add(labelSearch);
            panelSearch.Controls.Add(textBox1);
            panelSearch.Controls.Add(button1);
            panelSearch.Dock = DockStyle.Left;
            panelSearch.Location = new Point(20, 20);
            panelSearch.Name = "panelSearch";
            panelSearch.Size = new Size(520, 80);
            panelSearch.TabIndex = 10;
            // 
            // labelSearch
            // 
            labelSearch.AutoSize = true;
            labelSearch.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            labelSearch.ForeColor = Color.FromArgb(73, 80, 87);
            labelSearch.Location = new Point(20, 8);
            labelSearch.Name = "labelSearch";
            labelSearch.Size = new Size(61, 21);
            labelSearch.TabIndex = 12;
            labelSearch.Text = "Search";
            // 
            // labelResults
            // 
            labelResults.AutoSize = true;
            labelResults.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            labelResults.ForeColor = Color.FromArgb(73, 80, 87);
            labelResults.Location = new Point(20, 175);
            labelResults.Name = "labelResults";
            labelResults.Size = new Size(64, 21);
            labelResults.TabIndex = 14;
            labelResults.Text = "Results";
            // 
            // History
            // 
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Color.FromArgb(248, 249, 250);
            ClientSize = new Size(1200, 700);
            Controls.Add(labelResults);
            Controls.Add(fastColoredTextBox1);
            Controls.Add(splitContainer1);
            Controls.Add(panelHeader);
            Font = new Font("Segoe UI", 9F);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MinimumSize = new Size(1000, 600);
            Name = "History";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Query History";
            WindowState = FormWindowState.Maximized;
            DoubleBuffered = true;
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)historyDataGridView).EndInit();
            ((System.ComponentModel.ISupportInitialize)fastColoredTextBox2).EndInit();
            ((System.ComponentModel.ISupportInitialize)fastColoredTextBox1).EndInit();
            panelHeader.ResumeLayout(false);
            panelSearch.ResumeLayout(false);
            panelSearch.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private ThemedDataGridView historyDataGridView;
        private FastColoredTextBoxNS.FastColoredTextBox fastColoredTextBox1;
        private System.Windows.Forms.Button button1;
        private FastColoredTextBoxNS.FastColoredTextBox fastColoredTextBox2;
        private System.Windows.Forms.Panel panelHeader;
        private System.Windows.Forms.Panel panelSearch;
        private System.Windows.Forms.Label labelSearch;
        private System.Windows.Forms.Label labelResults;
        private System.Windows.Forms.Label labelSqlPreview;
    }
}
