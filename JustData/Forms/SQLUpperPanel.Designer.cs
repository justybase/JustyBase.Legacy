namespace JustyBaseLegacy.UI.DbForms
{
    partial class SQLUpperPanel
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SQLUpperPanel));
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.newToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.openToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.saveToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.btRunToolStrip = new System.Windows.Forms.ToolStripSplitButton();
            this.runF5ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.singleBatchCtrlF5ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.resultsToXlsxCtrlF6ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.resultsToCsvCtrlF6ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.runToCursorCtrlF10ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            //this.sqlLiteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.scriptModeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.btStop = new System.Windows.Forms.ToolStripButton();
            this.tsbImport = new System.Windows.Forms.ToolStripButton();
            this.tsbKeepConnection = new System.Windows.Forms.ToolStripButton();
            this.tsbContinueOnError = new System.Windows.Forms.ToolStripButton();
            this.tsbFormatSql = new System.Windows.Forms.ToolStripButton();
            this.commentSelectedLinesToolStripMenuItem = new System.Windows.Forms.ToolStripButton();
            this.uncommentSelectedLinesToolStripMenuItem = new System.Windows.Forms.ToolStripButton();
            this.cbConnections = new System.Windows.Forms.ToolStripComboBox();
            this.cbDatabases = new System.Windows.Forms.ToolStripComboBox();
            this.toolStripSeparator = new System.Windows.Forms.ToolStripSeparator();
            this.cutToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.copyToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.pasteToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.printToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStrip1
            // 
            this.toolStrip1.Dock = System.Windows.Forms.DockStyle.Top;
            this.toolStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newToolStripButton,
            this.openToolStripButton,
            this.saveToolStripButton,
            this.btRunToolStrip,
            this.btStop,
            this.tsbImport,
            this.tsbKeepConnection,
            this.tsbContinueOnError,
            this.tsbFormatSql,
            this.commentSelectedLinesToolStripMenuItem,
            this.uncommentSelectedLinesToolStripMenuItem,
            this.cbConnections,
            this.cbDatabases,
            this.toolStripSeparator,
            this.cutToolStripButton,
            this.copyToolStripButton,
            this.pasteToolStripButton,
            this.toolStripSeparator1,
            this.printToolStripButton});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(618, 25);
            this.toolStrip1.TabIndex = 7;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // newToolStripButton
            // 
            this.newToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.newToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("newToolStripButton.Image")));
            this.newToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.newToolStripButton.Name = "newToolStripButton";
            this.newToolStripButton.Size = new System.Drawing.Size(23, 22);
            this.newToolStripButton.Text = "&New";
            this.newToolStripButton.Click += new System.EventHandler(this.NewToolStripButton_Click);
            // 
            // openToolStripButton
            // 
            this.openToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.openToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("openToolStripButton.Image")));
            this.openToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.openToolStripButton.Name = "openToolStripButton";
            this.openToolStripButton.Size = new System.Drawing.Size(23, 22);
            this.openToolStripButton.Text = "&Open";
            this.openToolStripButton.Click += new System.EventHandler(this.OpenToolStripButton_Click);
            // 
            // saveToolStripButton
            // 
            this.saveToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.saveToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("saveToolStripButton.Image")));
            this.saveToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.saveToolStripButton.Name = "saveToolStripButton";
            this.saveToolStripButton.Size = new System.Drawing.Size(23, 22);
            this.saveToolStripButton.Text = "&Save";
            this.saveToolStripButton.Click += new System.EventHandler(this.SaveToolStripButton_Click);
            // 
            // btRunToolStrip
            // 
            this.btRunToolStrip.DefaultItem = this.runF5ToolStripMenuItem;
            this.btRunToolStrip.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btRunToolStrip.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.runF5ToolStripMenuItem,
            this.singleBatchCtrlF5ToolStripMenuItem,
            this.resultsToXlsxCtrlF6ToolStripMenuItem,
            this.resultsToCsvCtrlF6ToolStripMenuItem,
            this.runToCursorCtrlF10ToolStripMenuItem,
           // this.sqlLiteToolStripMenuItem,
            this.scriptModeToolStripMenuItem});
            this.btRunToolStrip.Image = global::JustData.Properties.Resources.run2;
            this.btRunToolStrip.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btRunToolStrip.Name = "btRunToolStrip";
            this.btRunToolStrip.Size = new System.Drawing.Size(32, 22);
            this.btRunToolStrip.Text = "Run query";
            // 
            // runF5ToolStripMenuItem
            // 
            this.runF5ToolStripMenuItem.Name = "runF5ToolStripMenuItem";
            this.runF5ToolStripMenuItem.Size = new System.Drawing.Size(207, 22);
            this.runF5ToolStripMenuItem.Text = "Run [F5]";
            this.runF5ToolStripMenuItem.Click += new System.EventHandler(this.RunToolStrip_Click);
            // 
            // singleBatchCtrlF5ToolStripMenuItem
            // 
            this.singleBatchCtrlF5ToolStripMenuItem.Name = "singleBatchCtrlF5ToolStripMenuItem";
            this.singleBatchCtrlF5ToolStripMenuItem.Size = new System.Drawing.Size(207, 22);
            this.singleBatchCtrlF5ToolStripMenuItem.Text = "Single Batch [Ctrl + F5]";
            this.singleBatchCtrlF5ToolStripMenuItem.Click += new System.EventHandler(this.RunCtrlF5_Click);
            // 
            // resultsToXlsxCtrlF6ToolStripMenuItem
            // 
            this.resultsToXlsxCtrlF6ToolStripMenuItem.Name = "resultsToXlsxCtrlF6ToolStripMenuItem";
            this.resultsToXlsxCtrlF6ToolStripMenuItem.Size = new System.Drawing.Size(207, 22);
            this.resultsToXlsxCtrlF6ToolStripMenuItem.Text = "Results to Xlsx [Ctrl + F7]";
            this.resultsToXlsxCtrlF6ToolStripMenuItem.Click += new System.EventHandler(this.RunExcel_Click);
            // 
            // resultsToCsvCtrlF6ToolStripMenuItem
            // 
            this.resultsToCsvCtrlF6ToolStripMenuItem.Name = "resultsToCsvCtrlF6ToolStripMenuItem";
            this.resultsToCsvCtrlF6ToolStripMenuItem.Size = new System.Drawing.Size(207, 22);
            this.resultsToCsvCtrlF6ToolStripMenuItem.Text = "Results to Csv [Ctrl + F8]";
            this.resultsToCsvCtrlF6ToolStripMenuItem.Click += new System.EventHandler(this.RunCSV_Click);
            // 
            // runToCursorCtrlF10ToolStripMenuItem
            // 
            this.runToCursorCtrlF10ToolStripMenuItem.Name = "runToCursorCtrlF10ToolStripMenuItem";
            this.runToCursorCtrlF10ToolStripMenuItem.Size = new System.Drawing.Size(207, 22);
            this.runToCursorCtrlF10ToolStripMenuItem.Text = "Run to cursor [Ctrl + F10]";
            this.runToCursorCtrlF10ToolStripMenuItem.Click += new System.EventHandler(this.runToCursorToolStripMenuItem_Click);
            //// 
            //// sqlLiteToolStripMenuItem
            //// 
            //this.sqlLiteToolStripMenuItem.Name = "sqlLiteToolStripMenuItem";
            //this.sqlLiteToolStripMenuItem.Size = new System.Drawing.Size(207, 22);
            //this.sqlLiteToolStripMenuItem.Text = "Sql Lite";
            //this.sqlLiteToolStripMenuItem.Click += new System.EventHandler(this.sqlLiteToolStripMenuItem_Click);
            // 
            // scriptModeToolStripMenuItem
            // 
            this.scriptModeToolStripMenuItem.Name = "scriptModeToolStripMenuItem";
            this.scriptModeToolStripMenuItem.Size = new System.Drawing.Size(207, 22);
            this.scriptModeToolStripMenuItem.Text = "Script Mode";
            this.scriptModeToolStripMenuItem.Click += new System.EventHandler(this.scriptModeToolStripMenuItem_Click);
            // 
            // btStop
            // 
            this.btStop.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btStop.Image = global::JustData.Properties.Resources.stop3;
            this.btStop.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btStop.Name = "btStop";
            this.btStop.Size = new System.Drawing.Size(23, 22);
            this.btStop.Text = "Abort All";
            this.btStop.Click += new System.EventHandler(this.btStop_Click);
            // 
            // tsbImport
            // 
            this.tsbImport.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsbImport.Image = ((System.Drawing.Image)(resources.GetObject("tsbImport.Image")));
            this.tsbImport.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbImport.Name = "tsbImport";
            this.tsbImport.Size = new System.Drawing.Size(23, 22);
            this.tsbImport.Text = "Import Data";
            this.tsbImport.Click += new System.EventHandler(this.tsbImport_Click);
            // 
            // tsbKeepConnection
            // 
            this.tsbKeepConnection.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsbKeepConnection.Image = ((System.Drawing.Image)(resources.GetObject("tsbKeepConnection.Image")));
            this.tsbKeepConnection.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbKeepConnection.Name = "tsbKeepConnection";
            this.tsbKeepConnection.Size = new System.Drawing.Size(23, 22);
            this.tsbKeepConnection.Text = "Keep connection Open";
            this.tsbKeepConnection.Click += new System.EventHandler(this.tsbKeepConnection_Click);
            // 
            // tsbContinueOnError
            // 
            this.tsbContinueOnError.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsbContinueOnError.Image = ((System.Drawing.Image)(resources.GetObject("tsbContinueOnError.Image")));
            this.tsbContinueOnError.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbContinueOnError.Name = "tsbContinueOnError";
            this.tsbContinueOnError.Size = new System.Drawing.Size(23, 22);
            this.tsbContinueOnError.Text = "Continue On Error";
            this.tsbContinueOnError.Click += new System.EventHandler(this.tsbContinueOnError_Click);
            // 
            // tsbFormatSql
            // 
            this.tsbFormatSql.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsbFormatSql.Image = global::JustData.Properties.Resources.script_lightning;
            this.tsbFormatSql.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbFormatSql.Name = "tsbFormatSql";
            this.tsbFormatSql.Size = new System.Drawing.Size(23, 22);
            this.tsbFormatSql.ToolTipText = "Format SQL [Ctrl + Shift + F]";
            this.tsbFormatSql.Click += new System.EventHandler(this.tsbFormatSql_Click);
            // 
            // commentSelectedLinesToolStripMenuItem
            // 
            this.commentSelectedLinesToolStripMenuItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.commentSelectedLinesToolStripMenuItem.Image = global::JustData.Properties.Resources.to_Comment;
            this.commentSelectedLinesToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.commentSelectedLinesToolStripMenuItem.Name = "commentSelectedLinesToolStripMenuItem";
            this.commentSelectedLinesToolStripMenuItem.Size = new System.Drawing.Size(23, 22);
            this.commentSelectedLinesToolStripMenuItem.Text = "Comment selected lines [Ctrl + /]";
            this.commentSelectedLinesToolStripMenuItem.Click += new System.EventHandler(this.commentSelectedLinesToolStripMenuItemClick);
            // 
            // uncommentSelectedLinesToolStripMenuItem
            // 
            this.uncommentSelectedLinesToolStripMenuItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.uncommentSelectedLinesToolStripMenuItem.Image = global::JustData.Properties.Resources.toUncomment;
            this.uncommentSelectedLinesToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.uncommentSelectedLinesToolStripMenuItem.Name = "uncommentSelectedLinesToolStripMenuItem";
            this.uncommentSelectedLinesToolStripMenuItem.Size = new System.Drawing.Size(23, 22);
            this.uncommentSelectedLinesToolStripMenuItem.Text = "Uncomment selected lines [Ctrl + /]";
            this.uncommentSelectedLinesToolStripMenuItem.Click += new System.EventHandler(this.uncommentSelectedLinesToolStripMenuItemClick);
            // 
            // cbConnections
            // 
            this.cbConnections.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbConnections.Name = "cbConnections";
            this.cbConnections.Size = new System.Drawing.Size(121, 25);
            this.cbConnections.DropDown += new System.EventHandler(this.CbConnections_DropDown);
            this.cbConnections.SelectedIndexChanged += new System.EventHandler(this.cbConnections_SelectedIndexChanged);
            // 
            // cbDatabases
            // 
            this.cbDatabases.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbDatabases.Name = "cbDatabases";
            this.cbDatabases.Size = new System.Drawing.Size(121, 25);
            this.cbDatabases.DropDown += new System.EventHandler(this.CbDatabases_DropDown);
            // 
            // toolStripSeparator
            // 
            this.toolStripSeparator.Name = "toolStripSeparator";
            this.toolStripSeparator.Size = new System.Drawing.Size(6, 25);
            // 
            // cutToolStripButton
            // 
            this.cutToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.cutToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("cutToolStripButton.Image")));
            this.cutToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.cutToolStripButton.Name = "cutToolStripButton";
            this.cutToolStripButton.Size = new System.Drawing.Size(23, 22);
            this.cutToolStripButton.Text = "C&ut";
            this.cutToolStripButton.Click += new System.EventHandler(this.CutToolStripButton_Click);
            // 
            // copyToolStripButton
            // 
            this.copyToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.copyToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("copyToolStripButton.Image")));
            this.copyToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.copyToolStripButton.Name = "copyToolStripButton";
            this.copyToolStripButton.Size = new System.Drawing.Size(23, 22);
            this.copyToolStripButton.Text = "&Copy";
            this.copyToolStripButton.Click += new System.EventHandler(this.CopyToolStripButton_Click);
            // 
            // pasteToolStripButton
            // 
            this.pasteToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.pasteToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("pasteToolStripButton.Image")));
            this.pasteToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.pasteToolStripButton.Name = "pasteToolStripButton";
            this.pasteToolStripButton.Size = new System.Drawing.Size(23, 22);
            this.pasteToolStripButton.Text = "&Paste";
            this.pasteToolStripButton.Click += new System.EventHandler(this.PasteToolStripButton_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
            // 
            // printToolStripButton
            // 
            this.printToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.printToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("printToolStripButton.Image")));
            this.printToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.printToolStripButton.Name = "printToolStripButton";
            this.printToolStripButton.Size = new System.Drawing.Size(23, 22);
            this.printToolStripButton.Text = "&Print";
            this.printToolStripButton.Click += new System.EventHandler(this.PrintToolStripButton_Click);
            // 
            // SQLUpperPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.DoubleBuffered = true;
            this.Margin = new System.Windows.Forms.Padding(0);
            this.Name = "SQLUpperPanel";
            this.Size = new System.Drawing.Size(618, 226);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripSplitButton btRunToolStrip;
        private System.Windows.Forms.ToolStripMenuItem runF5ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem singleBatchCtrlF5ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem resultsToXlsxCtrlF6ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem resultsToCsvCtrlF6ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem runToCursorCtrlF10ToolStripMenuItem;
        private System.Windows.Forms.ToolStripButton btStop;
        private System.Windows.Forms.ToolStripComboBox cbConnections;
        private System.Windows.Forms.ToolStripComboBox cbDatabases;
        private FastColoredTextBoxNS.FastColoredTextBox fastColoredTextBox1;
        //private System.Windows.Forms.ToolStripMenuItem sqlLiteToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem scriptModeToolStripMenuItem;
        private System.Windows.Forms.ToolStripButton tsbImport;
        private System.Windows.Forms.ToolStripButton tsbKeepConnection;
        private System.Windows.Forms.ToolStripButton tsbContinueOnError;
        private System.Windows.Forms.ToolStripButton tsbFormatSql;
        private System.Windows.Forms.ToolStripButton commentSelectedLinesToolStripMenuItem;
        private System.Windows.Forms.ToolStripButton uncommentSelectedLinesToolStripMenuItem;
        private System.Windows.Forms.ToolStripButton newToolStripButton;
        private System.Windows.Forms.ToolStripButton openToolStripButton;
        private System.Windows.Forms.ToolStripButton saveToolStripButton;
        private System.Windows.Forms.ToolStripButton printToolStripButton;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator;
        private System.Windows.Forms.ToolStripButton cutToolStripButton;
        private System.Windows.Forms.ToolStripButton copyToolStripButton;
        private System.Windows.Forms.ToolStripButton pasteToolStripButton;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
    }
}
