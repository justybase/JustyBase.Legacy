
using JustDataAdditionalForms;

namespace JustyBaseLegacy.UI
{
    partial class CsvFastImport
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
            DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle2 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle3 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle4 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle5 = new DataGridViewCellStyle();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CsvFastImport));
            tbInfo = new TextBox();
            tbFilePath = new TextBox();
            btOpenFile = new Button();
            openFileDialog1 = new OpenFileDialog();
            btGo = new Button();
            btAbort = new Button();
            tbFilterRow = new TextBox();
            label1 = new Label();
            tbTranferRegex = new TextBox();
            label2 = new Label();
            tbTransferedValue = new TextBox();
            label3 = new Label();
            tbTable = new TextBox();
            label4 = new Label();
            cbExisting = new CheckBox();
            customProgressBar1 = new CustomProgressBar();
            numProgressUnit = new NumericUpDown();
            label6 = new Label();
            tbDelimiter = new TextBox();
            label7 = new Label();
            numSkipRows = new NumericUpDown();
            cbStopWhenEmpty = new CheckBox();
            tbExtimatedSecs = new TextBox();
            cbSingleColumn = new CheckBox();
            cbAdvancedMode = new CheckBox();
            cbTop1000 = new CheckBox();
            label9 = new Label();
            label10 = new Label();
            progessDownloaded = new CustomProgressBar();
            label11 = new Label();
            progressProceded = new CustomProgressBar();
            label12 = new Label();
            tbRowsInserted = new TextBox();
            button1 = new Button();
            dgvTypes = new ThemedDataGridView();
            colName = new DataGridViewTextBoxColumn();
            colType = new DataGridViewComboBoxColumn();
            colLength = new DataGridViewTextBoxColumn();
            colPrecision = new DataGridViewTextBoxColumn();
            colScale = new DataGridViewTextBoxColumn();
            btLoadColumns = new Button();
            label13 = new Label();
            label14 = new Label();
            groupBox1 = new GroupBox();
            cbIncludeZeroSeconds = new CheckBox();
            cbIncludeHeader = new CheckBox();
            cbIgnoreZero = new CheckBox();
            cbCompress = new CheckBox();
            cbLFinString = new CheckBox();
            cbFillRecord = new CheckBox();
            cbCtrlChars = new CheckBox();
            cbTimeRoundNanos = new CheckBox();
            cbRequireQuotes = new CheckBox();
            cbCRinString = new CheckBox();
            cbTruncString = new CheckBox();
            numSocketBufSize = new NumericUpDown();
            numMaxRows = new NumericUpDown();
            tbTimestyle = new TextBox();
            tbEnconding = new TextBox();
            tbNullValue = new TextBox();
            tbRemoteSource = new TextBox();
            tbLogDir = new TextBox();
            linkLabel1 = new LinkLabel();
            label8 = new Label();
            tbEscapeChar = new TextBox();
            label19 = new Label();
            tbRecordDelim = new TextBox();
            tbDecimalDelim = new TextBox();
            label23 = new Label();
            label18 = new Label();
            label22 = new Label();
            label24 = new Label();
            label16 = new Label();
            label5 = new Label();
            label21 = new Label();
            label20 = new Label();
            label17 = new Label();
            tbReject = new TextBox();
            label15 = new Label();
            tbFileSize = new TextBox();
            ((System.ComponentModel.ISupportInitialize)numProgressUnit).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numSkipRows).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dgvTypes).BeginInit();
            groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numSocketBufSize).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numMaxRows).BeginInit();
            SuspendLayout();
            // 
            // tbInfo
            // 
            tbInfo.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            tbInfo.Location = new Point(12, 398);
            tbInfo.Multiline = true;
            tbInfo.Name = "tbInfo";
            tbInfo.Size = new Size(537, 84);
            tbInfo.TabIndex = 21;
            tbInfo.Text = "SELECT * FROM _v_load_status;\r\n";
            // 
            // tbFilePath
            // 
            tbFilePath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            tbFilePath.Location = new Point(101, 13);
            tbFilePath.Name = "tbFilePath";
            tbFilePath.Size = new Size(493, 23);
            tbFilePath.TabIndex = 0;
            // 
            // btOpenFile
            // 
            btOpenFile.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btOpenFile.Location = new Point(608, 13);
            btOpenFile.Name = "btOpenFile";
            btOpenFile.Size = new Size(38, 23);
            btOpenFile.TabIndex = 27;
            btOpenFile.Text = "...";
            btOpenFile.UseVisualStyleBackColor = true;
            btOpenFile.Click += btOpenFile_Click;
            // 
            // openFileDialog1
            // 
            openFileDialog1.FileName = "openFileDialog1";
            // 
            // btGo
            // 
            btGo.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btGo.Location = new Point(13, 367);
            btGo.Name = "btGo";
            btGo.Size = new Size(172, 25);
            btGo.TabIndex = 19;
            btGo.Text = "Go!";
            btGo.UseVisualStyleBackColor = true;
            btGo.Click += btGo_Click;
            // 
            // btAbort
            // 
            btAbort.Location = new Point(194, 367);
            btAbort.Name = "btAbort";
            btAbort.Size = new Size(172, 25);
            btAbort.TabIndex = 20;
            btAbort.Text = "Abort";
            btAbort.UseVisualStyleBackColor = true;
            btAbort.Click += BtAbort_Click;
            // 
            // tbFilterRow
            // 
            tbFilterRow.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            tbFilterRow.Location = new Point(101, 42);
            tbFilterRow.Name = "tbFilterRow";
            tbFilterRow.Size = new Size(175, 23);
            tbFilterRow.TabIndex = 1;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(25, 45);
            label1.Name = "label1";
            label1.Size = new Size(73, 15);
            label1.TabIndex = 6;
            label1.Text = "accept regex";
            // 
            // tbTranferRegex
            // 
            tbTranferRegex.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            tbTranferRegex.Location = new Point(102, 71);
            tbTranferRegex.Name = "tbTranferRegex";
            tbTranferRegex.Size = new Size(175, 23);
            tbTranferRegex.TabIndex = 3;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(13, 74);
            label2.Name = "label2";
            label2.Size = new Size(76, 15);
            label2.TabIndex = 6;
            label2.Text = "replace regex";
            // 
            // tbTransferedValue
            // 
            tbTransferedValue.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            tbTransferedValue.Location = new Point(364, 71);
            tbTransferedValue.Name = "tbTransferedValue";
            tbTransferedValue.Size = new Size(230, 23);
            tbTransferedValue.TabIndex = 4;
            // 
            // label3
            // 
            label3.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            label3.AutoSize = true;
            label3.Location = new Point(282, 74);
            label3.Name = "label3";
            label3.Size = new Size(76, 15);
            label3.TabIndex = 6;
            label3.Text = "replace value";
            // 
            // tbTable
            // 
            tbTable.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            tbTable.Location = new Point(101, 100);
            tbTable.Name = "tbTable";
            tbTable.Size = new Size(175, 23);
            tbTable.TabIndex = 5;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            label4.Location = new Point(22, 103);
            label4.Name = "label4";
            label4.Size = new Size(66, 15);
            label4.TabIndex = 6;
            label4.Text = "tablename";
            // 
            // cbExisting
            // 
            cbExisting.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            cbExisting.AutoSize = true;
            cbExisting.Enabled = false;
            cbExisting.Location = new Point(917, 13);
            cbExisting.Name = "cbExisting";
            cbExisting.Size = new Size(148, 19);
            cbExisting.TabIndex = 31;
            cbExisting.Text = "import to existing table";
            cbExisting.UseVisualStyleBackColor = true;
            // 
            // customProgressBar1
            // 
            customProgressBar1.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            customProgressBar1.Location = new Point(235, 491);
            customProgressBar1.Name = "customProgressBar1";
            customProgressBar1.Size = new Size(314, 23);
            customProgressBar1.TabIndex = 22;
            // 
            // numProgressUnit
            // 
            numProgressUnit.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            numProgressUnit.Increment = new decimal(new int[] { 1000, 0, 0, 0 });
            numProgressUnit.Location = new Point(782, 15);
            numProgressUnit.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
            numProgressUnit.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numProgressUnit.Name = "numProgressUnit";
            numProgressUnit.Size = new Size(106, 23);
            numProgressUnit.TabIndex = 30;
            numProgressUnit.ThousandsSeparator = true;
            numProgressUnit.Value = new decimal(new int[] { 5000, 0, 0, 0 });
            // 
            // label6
            // 
            label6.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            label6.AutoSize = true;
            label6.Location = new Point(782, 45);
            label6.Name = "label6";
            label6.Size = new Size(76, 15);
            label6.TabIndex = 6;
            label6.Text = "progress unit";
            // 
            // tbDelimiter
            // 
            tbDelimiter.Location = new Point(9, 48);
            tbDelimiter.Margin = new Padding(2);
            tbDelimiter.MaxLength = 2;
            tbDelimiter.Name = "tbDelimiter";
            tbDelimiter.Size = new Size(34, 23);
            tbDelimiter.TabIndex = 9;
            tbDelimiter.Text = "|";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(49, 56);
            label7.Name = "label7";
            label7.Size = new Size(55, 15);
            label7.TabIndex = 6;
            label7.Text = "Delimiter";
            // 
            // numSkipRows
            // 
            numSkipRows.Location = new Point(9, 23);
            numSkipRows.Maximum = new decimal(new int[] { 1000000, 0, 0, 0 });
            numSkipRows.Name = "numSkipRows";
            numSkipRows.Size = new Size(93, 23);
            numSkipRows.TabIndex = 7;
            numSkipRows.ThousandsSeparator = true;
            // 
            // cbStopWhenEmpty
            // 
            cbStopWhenEmpty.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            cbStopWhenEmpty.AutoSize = true;
            cbStopWhenEmpty.Checked = true;
            cbStopWhenEmpty.CheckState = CheckState.Checked;
            cbStopWhenEmpty.Location = new Point(916, 38);
            cbStopWhenEmpty.Name = "cbStopWhenEmpty";
            cbStopWhenEmpty.Size = new Size(126, 19);
            cbStopWhenEmpty.TabIndex = 32;
            cbStopWhenEmpty.Text = "stop on empty row";
            cbStopWhenEmpty.UseVisualStyleBackColor = true;
            // 
            // tbExtimatedSecs
            // 
            tbExtimatedSecs.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            tbExtimatedSecs.Location = new Point(934, 572);
            tbExtimatedSecs.Name = "tbExtimatedSecs";
            tbExtimatedSecs.ReadOnly = true;
            tbExtimatedSecs.Size = new Size(206, 23);
            tbExtimatedSecs.TabIndex = 13;
            // 
            // cbSingleColumn
            // 
            cbSingleColumn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            cbSingleColumn.AutoSize = true;
            cbSingleColumn.Checked = true;
            cbSingleColumn.CheckState = CheckState.Checked;
            cbSingleColumn.Location = new Point(916, 110);
            cbSingleColumn.Name = "cbSingleColumn";
            cbSingleColumn.Size = new Size(138, 19);
            cbSingleColumn.TabIndex = 35;
            cbSingleColumn.Text = "Single Column mode";
            cbSingleColumn.UseVisualStyleBackColor = true;
            // 
            // cbAdvancedMode
            // 
            cbAdvancedMode.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            cbAdvancedMode.AutoSize = true;
            cbAdvancedMode.Location = new Point(916, 88);
            cbAdvancedMode.Name = "cbAdvancedMode";
            cbAdvancedMode.Size = new Size(113, 19);
            cbAdvancedMode.TabIndex = 34;
            cbAdvancedMode.Text = "Advanced Mode";
            cbAdvancedMode.UseVisualStyleBackColor = true;
            // 
            // cbTop1000
            // 
            cbTop1000.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            cbTop1000.AutoSize = true;
            cbTop1000.Location = new Point(915, 64);
            cbTop1000.Name = "cbTop1000";
            cbTop1000.Size = new Size(73, 19);
            cbTop1000.TabIndex = 33;
            cbTop1000.Text = "Top 1000";
            cbTop1000.UseVisualStyleBackColor = true;
            // 
            // label9
            // 
            label9.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            label9.AutoSize = true;
            label9.Location = new Point(13, 498);
            label9.Name = "label9";
            label9.Size = new Size(157, 15);
            label9.TabIndex = 18;
            label9.Text = "Rows readed (based on disk)";
            // 
            // label10
            // 
            label10.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            label10.AutoSize = true;
            label10.Location = new Point(13, 527);
            label10.Name = "label10";
            label10.Size = new Size(148, 15);
            label10.TabIndex = 18;
            label10.Text = "Netezza bytes downloaded";
            // 
            // progessDownloaded
            // 
            progessDownloaded.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            progessDownloaded.Location = new Point(235, 519);
            progessDownloaded.Name = "progessDownloaded";
            progessDownloaded.Size = new Size(314, 23);
            progessDownloaded.TabIndex = 23;
            // 
            // label11
            // 
            label11.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            label11.AutoSize = true;
            label11.Location = new Point(13, 554);
            label11.Name = "label11";
            label11.Size = new Size(135, 15);
            label11.TabIndex = 18;
            label11.Text = "Netezza bytes processed";
            // 
            // progressProceded
            // 
            progressProceded.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            progressProceded.Location = new Point(235, 548);
            progressProceded.Name = "progressProceded";
            progressProceded.Size = new Size(314, 23);
            progressProceded.TabIndex = 24;
            // 
            // label12
            // 
            label12.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            label12.AutoSize = true;
            label12.Location = new Point(13, 580);
            label12.Name = "label12";
            label12.Size = new Size(121, 15);
            label12.TabIndex = 18;
            label12.Text = "Netezza rows inserted";
            // 
            // tbRowsInserted
            // 
            tbRowsInserted.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            tbRowsInserted.Location = new Point(235, 577);
            tbRowsInserted.Name = "tbRowsInserted";
            tbRowsInserted.ReadOnly = true;
            tbRowsInserted.Size = new Size(177, 23);
            tbRowsInserted.TabIndex = 25;
            // 
            // button1
            // 
            button1.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            button1.Location = new Point(449, 577);
            button1.Name = "button1";
            button1.Size = new Size(114, 23);
            button1.TabIndex = 26;
            button1.Text = "Refresh netezza stats";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // dgvTypes
            // 
            dgvTypes.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            dgvTypes.BackgroundColor = SystemColors.ButtonFace;
            dgvTypes.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvTypes.Columns.AddRange(new DataGridViewColumn[] { colName, colType, colLength, colPrecision, colScale });
            dgvTypes.Location = new Point(608, 139);
            dgvTypes.Name = "dgvTypes";
            dgvTypes.Size = new Size(532, 403);
            dgvTypes.TabIndex = 36;
            // 
            // colName
            // 
            dataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleCenter;
            colName.DefaultCellStyle = dataGridViewCellStyle1;
            colName.HeaderText = "Name";
            colName.MaxInputLength = 64;
            colName.Name = "colName";
            colName.Width = 150;
            // 
            // colType
            // 
            dataGridViewCellStyle2.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle2.Format = "N0";
            dataGridViewCellStyle2.NullValue = "NULL";
            colType.DefaultCellStyle = dataGridViewCellStyle2;
            colType.HeaderText = "Type";
            colType.Items.AddRange(new object[] { "INTEGER", "BIGINT", "CHAR", "VARCHAR", "NVARCHAR", "DATE", "TIMESTAMP", "FLOAT", "DOUBLE", "NUMERIC" });
            colType.Name = "colType";
            colType.Width = 125;
            // 
            // colLength
            // 
            dataGridViewCellStyle3.Alignment = DataGridViewContentAlignment.MiddleLeft;
            colLength.DefaultCellStyle = dataGridViewCellStyle3;
            colLength.HeaderText = "Length";
            colLength.MaxInputLength = 5;
            colLength.Name = "colLength";
            colLength.Width = 70;
            // 
            // colPrecision
            // 
            dataGridViewCellStyle4.Format = "N0";
            colPrecision.DefaultCellStyle = dataGridViewCellStyle4;
            colPrecision.HeaderText = "Precision";
            colPrecision.MaxInputLength = 3;
            colPrecision.Name = "colPrecision";
            colPrecision.Width = 70;
            // 
            // colScale
            // 
            dataGridViewCellStyle5.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle5.Format = "N0";
            colScale.DefaultCellStyle = dataGridViewCellStyle5;
            colScale.HeaderText = "Scale";
            colScale.MaxInputLength = 3;
            colScale.Name = "colScale";
            colScale.Width = 70;
            // 
            // btLoadColumns
            // 
            btLoadColumns.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btLoadColumns.Location = new Point(608, 546);
            btLoadColumns.Name = "btLoadColumns";
            btLoadColumns.Size = new Size(532, 23);
            btLoadColumns.TabIndex = 22;
            btLoadColumns.Text = "Load columns";
            btLoadColumns.UseVisualStyleBackColor = true;
            btLoadColumns.Click += btLoadColumns_Click;
            // 
            // label13
            // 
            label13.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            label13.AutoSize = true;
            label13.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            label13.Location = new Point(781, 116);
            label13.Name = "label13";
            label13.Size = new Size(107, 15);
            label13.TabIndex = 23;
            label13.Text = "Types are optional";
            // 
            // label14
            // 
            label14.AutoSize = true;
            label14.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            label14.Location = new Point(35, 19);
            label14.Name = "label14";
            label14.Size = new Size(53, 15);
            label14.TabIndex = 6;
            label14.Text = "file path";
            // 
            // groupBox1
            // 
            groupBox1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            groupBox1.Controls.Add(cbIncludeZeroSeconds);
            groupBox1.Controls.Add(cbIncludeHeader);
            groupBox1.Controls.Add(cbIgnoreZero);
            groupBox1.Controls.Add(cbCompress);
            groupBox1.Controls.Add(cbLFinString);
            groupBox1.Controls.Add(cbFillRecord);
            groupBox1.Controls.Add(cbCtrlChars);
            groupBox1.Controls.Add(cbTimeRoundNanos);
            groupBox1.Controls.Add(cbRequireQuotes);
            groupBox1.Controls.Add(cbCRinString);
            groupBox1.Controls.Add(cbTruncString);
            groupBox1.Controls.Add(numSocketBufSize);
            groupBox1.Controls.Add(numMaxRows);
            groupBox1.Controls.Add(tbTimestyle);
            groupBox1.Controls.Add(tbEnconding);
            groupBox1.Controls.Add(tbNullValue);
            groupBox1.Controls.Add(tbRemoteSource);
            groupBox1.Controls.Add(tbLogDir);
            groupBox1.Controls.Add(linkLabel1);
            groupBox1.Controls.Add(label8);
            groupBox1.Controls.Add(numSkipRows);
            groupBox1.Controls.Add(tbEscapeChar);
            groupBox1.Controls.Add(label19);
            groupBox1.Controls.Add(tbRecordDelim);
            groupBox1.Controls.Add(tbDecimalDelim);
            groupBox1.Controls.Add(tbDelimiter);
            groupBox1.Controls.Add(label23);
            groupBox1.Controls.Add(label18);
            groupBox1.Controls.Add(label22);
            groupBox1.Controls.Add(label24);
            groupBox1.Controls.Add(label16);
            groupBox1.Controls.Add(label5);
            groupBox1.Controls.Add(label21);
            groupBox1.Controls.Add(label20);
            groupBox1.Controls.Add(label17);
            groupBox1.Controls.Add(label7);
            groupBox1.Location = new Point(13, 129);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(581, 232);
            groupBox1.TabIndex = 24;
            groupBox1.TabStop = false;
            groupBox1.Text = "Netezza options";
            // 
            // cbIncludeZeroSeconds
            // 
            cbIncludeZeroSeconds.AutoSize = true;
            cbIncludeZeroSeconds.Location = new Point(94, 206);
            cbIncludeZeroSeconds.Name = "cbIncludeZeroSeconds";
            cbIncludeZeroSeconds.Size = new Size(133, 19);
            cbIncludeZeroSeconds.TabIndex = 18;
            cbIncludeZeroSeconds.Text = "IncludeZeroSeconds";
            cbIncludeZeroSeconds.UseVisualStyleBackColor = true;
            // 
            // cbIncludeHeader
            // 
            cbIncludeHeader.AutoSize = true;
            cbIncludeHeader.Location = new Point(94, 181);
            cbIncludeHeader.Name = "cbIncludeHeader";
            cbIncludeHeader.Size = new Size(103, 19);
            cbIncludeHeader.TabIndex = 18;
            cbIncludeHeader.Text = "IncludeHeader";
            cbIncludeHeader.UseVisualStyleBackColor = true;
            // 
            // cbIgnoreZero
            // 
            cbIgnoreZero.AutoSize = true;
            cbIgnoreZero.Location = new Point(94, 156);
            cbIgnoreZero.Name = "cbIgnoreZero";
            cbIgnoreZero.Size = new Size(84, 19);
            cbIgnoreZero.TabIndex = 18;
            cbIgnoreZero.Text = "IgnoreZero";
            cbIgnoreZero.UseVisualStyleBackColor = true;
            // 
            // cbCompress
            // 
            cbCompress.AutoSize = true;
            cbCompress.Location = new Point(9, 133);
            cbCompress.Name = "cbCompress";
            cbCompress.Size = new Size(79, 19);
            cbCompress.TabIndex = 18;
            cbCompress.Text = "Compress";
            cbCompress.UseVisualStyleBackColor = true;
            // 
            // cbLFinString
            // 
            cbLFinString.AutoSize = true;
            cbLFinString.Location = new Point(226, 131);
            cbLFinString.Name = "cbLFinString";
            cbLFinString.Size = new Size(79, 19);
            cbLFinString.TabIndex = 18;
            cbLFinString.Text = "LFinString";
            cbLFinString.UseVisualStyleBackColor = true;
            // 
            // cbFillRecord
            // 
            cbFillRecord.AutoSize = true;
            cbFillRecord.Location = new Point(9, 206);
            cbFillRecord.Name = "cbFillRecord";
            cbFillRecord.Size = new Size(78, 19);
            cbFillRecord.TabIndex = 18;
            cbFillRecord.Text = "FillRecord";
            cbFillRecord.UseVisualStyleBackColor = true;
            // 
            // cbCtrlChars
            // 
            cbCtrlChars.AutoSize = true;
            cbCtrlChars.Location = new Point(9, 182);
            cbCtrlChars.Name = "cbCtrlChars";
            cbCtrlChars.Size = new Size(75, 19);
            cbCtrlChars.TabIndex = 18;
            cbCtrlChars.Text = "CtrlChars";
            cbCtrlChars.UseVisualStyleBackColor = true;
            // 
            // cbTimeRoundNanos
            // 
            cbTimeRoundNanos.AutoSize = true;
            cbTimeRoundNanos.Location = new Point(94, 133);
            cbTimeRoundNanos.Name = "cbTimeRoundNanos";
            cbTimeRoundNanos.Size = new Size(122, 19);
            cbTimeRoundNanos.TabIndex = 18;
            cbTimeRoundNanos.Text = "TimeRoundNanos";
            cbTimeRoundNanos.UseVisualStyleBackColor = true;
            // 
            // cbRequireQuotes
            // 
            cbRequireQuotes.AutoSize = true;
            cbRequireQuotes.Location = new Point(226, 181);
            cbRequireQuotes.Name = "cbRequireQuotes";
            cbRequireQuotes.Size = new Size(104, 19);
            cbRequireQuotes.TabIndex = 18;
            cbRequireQuotes.Text = "RequireQuotes";
            cbRequireQuotes.UseVisualStyleBackColor = true;
            // 
            // cbCRinString
            // 
            cbCRinString.AutoSize = true;
            cbCRinString.Location = new Point(226, 156);
            cbCRinString.Name = "cbCRinString";
            cbCRinString.Size = new Size(82, 19);
            cbCRinString.TabIndex = 18;
            cbCRinString.Text = "CRinString";
            cbCRinString.UseVisualStyleBackColor = true;
            // 
            // cbTruncString
            // 
            cbTruncString.AutoSize = true;
            cbTruncString.Location = new Point(9, 157);
            cbTruncString.Name = "cbTruncString";
            cbTruncString.Size = new Size(87, 19);
            cbTruncString.TabIndex = 18;
            cbTruncString.Text = "TruncString";
            cbTruncString.UseVisualStyleBackColor = true;
            // 
            // numSocketBufSize
            // 
            numSocketBufSize.Increment = new decimal(new int[] { 1024, 0, 0, 0 });
            numSocketBufSize.Location = new Point(168, 75);
            numSocketBufSize.Margin = new Padding(2);
            numSocketBufSize.Maximum = new decimal(new int[] { int.MinValue, 0, 0, 0 });
            numSocketBufSize.Minimum = new decimal(new int[] { 65536, 0, 0, 0 });
            numSocketBufSize.Name = "numSocketBufSize";
            numSocketBufSize.Size = new Size(100, 23);
            numSocketBufSize.TabIndex = 13;
            numSocketBufSize.ThousandsSeparator = true;
            numSocketBufSize.Value = new decimal(new int[] { 8388608, 0, 0, 0 });
            // 
            // numMaxRows
            // 
            numMaxRows.Location = new Point(168, 48);
            numMaxRows.Margin = new Padding(2);
            numMaxRows.Name = "numMaxRows";
            numMaxRows.Size = new Size(100, 23);
            numMaxRows.TabIndex = 10;
            // 
            // tbTimestyle
            // 
            tbTimestyle.Location = new Point(371, 102);
            tbTimestyle.Margin = new Padding(2);
            tbTimestyle.MaxLength = 50;
            tbTimestyle.Name = "tbTimestyle";
            tbTimestyle.Size = new Size(119, 23);
            tbTimestyle.TabIndex = 17;
            tbTimestyle.Text = "24HOUR";
            // 
            // tbEnconding
            // 
            tbEnconding.Location = new Point(371, 75);
            tbEnconding.Margin = new Padding(2);
            tbEnconding.MaxLength = 50;
            tbEnconding.Name = "tbEnconding";
            tbEnconding.Size = new Size(119, 23);
            tbEnconding.TabIndex = 14;
            tbEnconding.Text = "utf-8";
            // 
            // tbNullValue
            // 
            tbNullValue.Location = new Point(168, 101);
            tbNullValue.Margin = new Padding(2);
            tbNullValue.MaxLength = 50;
            tbNullValue.Name = "tbNullValue";
            tbNullValue.Size = new Size(100, 23);
            tbNullValue.TabIndex = 16;
            // 
            // tbRemoteSource
            // 
            tbRemoteSource.Location = new Point(371, 48);
            tbRemoteSource.Margin = new Padding(2);
            tbRemoteSource.MaxLength = 50;
            tbRemoteSource.Name = "tbRemoteSource";
            tbRemoteSource.Size = new Size(119, 23);
            tbRemoteSource.TabIndex = 11;
            tbRemoteSource.Text = "odbc";
            // 
            // tbLogDir
            // 
            tbLogDir.Location = new Point(168, 21);
            tbLogDir.Margin = new Padding(2);
            tbLogDir.Name = "tbLogDir";
            tbLogDir.Size = new Size(322, 23);
            tbLogDir.TabIndex = 8;
            // 
            // linkLabel1
            // 
            linkLabel1.AutoSize = true;
            linkLabel1.Location = new Point(485, 207);
            linkLabel1.Name = "linkLabel1";
            linkLabel1.Size = new Size(90, 15);
            linkLabel1.TabIndex = 7;
            linkLabel1.TabStop = true;
            linkLabel1.Text = "Documentation";
            linkLabel1.LinkClicked += linkLabel1_LinkClicked;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(106, 29);
            label8.Name = "label8";
            label8.Size = new Size(57, 15);
            label8.TabIndex = 6;
            label8.Text = "SkipRows";
            // 
            // tbEscapeChar
            // 
            tbEscapeChar.Location = new Point(395, 152);
            tbEscapeChar.Margin = new Padding(2);
            tbEscapeChar.MaxLength = 1;
            tbEscapeChar.Name = "tbEscapeChar";
            tbEscapeChar.Size = new Size(34, 23);
            tbEscapeChar.TabIndex = 15;
            tbEscapeChar.Text = "\\";
            // 
            // label19
            // 
            label19.AutoSize = true;
            label19.Location = new Point(273, 78);
            label19.Name = "label19";
            label19.Size = new Size(80, 15);
            label19.TabIndex = 6;
            label19.Text = "SocketBufSize";
            // 
            // tbRecordDelim
            // 
            tbRecordDelim.Location = new Point(9, 102);
            tbRecordDelim.Margin = new Padding(2);
            tbRecordDelim.MaxLength = 1;
            tbRecordDelim.Name = "tbRecordDelim";
            tbRecordDelim.Size = new Size(34, 23);
            tbRecordDelim.TabIndex = 12;
            tbRecordDelim.Text = "\\r\\n";
            // 
            // tbDecimalDelim
            // 
            tbDecimalDelim.Location = new Point(9, 74);
            tbDecimalDelim.Margin = new Padding(2);
            tbDecimalDelim.MaxLength = 1;
            tbDecimalDelim.Name = "tbDecimalDelim";
            tbDecimalDelim.Size = new Size(34, 23);
            tbDecimalDelim.TabIndex = 12;
            tbDecimalDelim.Text = ".";
            // 
            // label23
            // 
            label23.AutoSize = true;
            label23.Location = new Point(495, 110);
            label23.Name = "label23";
            label23.Size = new Size(59, 15);
            label23.TabIndex = 6;
            label23.Text = "TimeStyle";
            // 
            // label18
            // 
            label18.AutoSize = true;
            label18.Location = new Point(273, 56);
            label18.Name = "label18";
            label18.Size = new Size(57, 15);
            label18.TabIndex = 6;
            label18.Text = "MaxRows";
            // 
            // label22
            // 
            label22.AutoSize = true;
            label22.Location = new Point(496, 82);
            label22.Name = "label22";
            label22.Size = new Size(57, 15);
            label22.TabIndex = 6;
            label22.Text = "Encoding";
            // 
            // label24
            // 
            label24.AutoSize = true;
            label24.Location = new Point(273, 105);
            label24.Name = "label24";
            label24.Size = new Size(57, 15);
            label24.TabIndex = 6;
            label24.Text = "NullValue";
            // 
            // label16
            // 
            label16.AutoSize = true;
            label16.Location = new Point(434, 160);
            label16.Name = "label16";
            label16.Size = new Size(68, 15);
            label16.TabIndex = 6;
            label16.Text = "EscapeChar";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(49, 110);
            label5.Name = "label5";
            label5.Size = new Size(75, 15);
            label5.TabIndex = 6;
            label5.Text = "RecordDelim";
            // 
            // label21
            // 
            label21.AutoSize = true;
            label21.Location = new Point(496, 55);
            label21.Name = "label21";
            label21.Size = new Size(84, 15);
            label21.TabIndex = 6;
            label21.Text = "RemoteSource";
            // 
            // label20
            // 
            label20.AutoSize = true;
            label20.Location = new Point(48, 82);
            label20.Name = "label20";
            label20.Size = new Size(81, 15);
            label20.TabIndex = 6;
            label20.Text = "DecimalDelim";
            // 
            // label17
            // 
            label17.AutoSize = true;
            label17.Location = new Point(495, 23);
            label17.Name = "label17";
            label17.Size = new Size(42, 15);
            label17.TabIndex = 6;
            label17.Text = "LogDir";
            // 
            // tbReject
            // 
            tbReject.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            tbReject.Location = new Point(364, 42);
            tbReject.Name = "tbReject";
            tbReject.Size = new Size(230, 23);
            tbReject.TabIndex = 2;
            // 
            // label15
            // 
            label15.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            label15.AutoSize = true;
            label15.Location = new Point(290, 45);
            label15.Name = "label15";
            label15.Size = new Size(67, 15);
            label15.TabIndex = 6;
            label15.Text = "reject regex";
            // 
            // tbFileSize
            // 
            tbFileSize.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            tbFileSize.Location = new Point(290, 100);
            tbFileSize.Name = "tbFileSize";
            tbFileSize.ReadOnly = true;
            tbFileSize.Size = new Size(304, 23);
            tbFileSize.TabIndex = 6;
            tbFileSize.Text = "File size";
            // 
            // CsvFastImport
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1152, 603);
            Controls.Add(tbFileSize);
            Controls.Add(groupBox1);
            Controls.Add(label13);
            Controls.Add(btLoadColumns);
            Controls.Add(dgvTypes);
            Controls.Add(button1);
            Controls.Add(tbRowsInserted);
            Controls.Add(label12);
            Controls.Add(label11);
            Controls.Add(label10);
            Controls.Add(label9);
            Controls.Add(cbTop1000);
            Controls.Add(tbExtimatedSecs);
            Controls.Add(numProgressUnit);
            Controls.Add(progressProceded);
            Controls.Add(progessDownloaded);
            Controls.Add(customProgressBar1);
            Controls.Add(cbStopWhenEmpty);
            Controls.Add(cbAdvancedMode);
            Controls.Add(cbSingleColumn);
            Controls.Add(cbExisting);
            Controls.Add(tbTable);
            Controls.Add(label4);
            Controls.Add(label6);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(label14);
            Controls.Add(label15);
            Controls.Add(label1);
            Controls.Add(tbTransferedValue);
            Controls.Add(tbReject);
            Controls.Add(tbTranferRegex);
            Controls.Add(tbFilterRow);
            Controls.Add(btAbort);
            Controls.Add(btGo);
            Controls.Add(btOpenFile);
            Controls.Add(tbFilePath);
            Controls.Add(tbInfo);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MinimumSize = new Size(1168, 642);
            Name = "CsvFastImport";
            Text = "Advanced csv import";
            Load += CsvFastImport_Load;
            ((System.ComponentModel.ISupportInitialize)numProgressUnit).EndInit();
            ((System.ComponentModel.ISupportInitialize)numSkipRows).EndInit();
            ((System.ComponentModel.ISupportInitialize)dgvTypes).EndInit();
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numSocketBufSize).EndInit();
            ((System.ComponentModel.ISupportInitialize)numMaxRows).EndInit();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox tbInfo;
        private System.Windows.Forms.TextBox tbFilePath;
        private System.Windows.Forms.Button btOpenFile;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.Button btGo;
        private System.Windows.Forms.Button btAbort;
        private System.Windows.Forms.TextBox tbFilterRow;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox tbTranferRegex;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox tbTransferedValue;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox tbTable;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.CheckBox cbExisting;
        private CustomProgressBar customProgressBar1;
        private System.Windows.Forms.NumericUpDown numProgressUnit;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox tbDelimiter;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.NumericUpDown numSkipRows;
        private System.Windows.Forms.CheckBox cbStopWhenEmpty;
        private System.Windows.Forms.TextBox tbExtimatedSecs;
        private System.Windows.Forms.CheckBox cbSingleColumn;
        private System.Windows.Forms.CheckBox cbAdvancedMode;
        private System.Windows.Forms.CheckBox cbTop1000;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label10;
        private CustomProgressBar progessDownloaded;
        private System.Windows.Forms.Label label11;
        private CustomProgressBar progressProceded;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.TextBox tbRowsInserted;
        private System.Windows.Forms.Button button1;
        private ThemedDataGridView dgvTypes;
        private System.Windows.Forms.Button btLoadColumns;
        private System.Windows.Forms.DataGridViewTextBoxColumn colName;
        private System.Windows.Forms.DataGridViewComboBoxColumn colType;
        private System.Windows.Forms.DataGridViewTextBoxColumn colLength;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPrecision;
        private System.Windows.Forms.DataGridViewTextBoxColumn colScale;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox tbReject;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.TextBox tbFileSize;
        private System.Windows.Forms.LinkLabel linkLabel1;
        private System.Windows.Forms.TextBox tbLogDir;
        private System.Windows.Forms.TextBox tbEscapeChar;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.NumericUpDown numMaxRows;
        private System.Windows.Forms.Label label18;
        private System.Windows.Forms.NumericUpDown numSocketBufSize;
        private System.Windows.Forms.Label label19;
        private System.Windows.Forms.TextBox tbDecimalDelim;
        private System.Windows.Forms.Label label20;
        private System.Windows.Forms.TextBox tbRemoteSource;
        private System.Windows.Forms.Label label21;
        private System.Windows.Forms.TextBox tbEnconding;
        private System.Windows.Forms.Label label22;
        private System.Windows.Forms.TextBox tbTimestyle;
        private System.Windows.Forms.Label label23;
        private System.Windows.Forms.CheckBox cbTruncString;
        private System.Windows.Forms.TextBox tbNullValue;
        private System.Windows.Forms.Label label24;
        private System.Windows.Forms.CheckBox cbCRinString;
        private System.Windows.Forms.CheckBox cbCtrlChars;
        private System.Windows.Forms.CheckBox cbFillRecord;
        private System.Windows.Forms.CheckBox cbIgnoreZero;
        private System.Windows.Forms.CheckBox cbIncludeHeader;
        private System.Windows.Forms.CheckBox cbIncludeZeroSeconds;
        private System.Windows.Forms.CheckBox cbCompress;
        private System.Windows.Forms.CheckBox cbLFinString;
        private System.Windows.Forms.CheckBox cbRequireQuotes;
        private System.Windows.Forms.CheckBox cbTimeRoundNanos;
        private System.Windows.Forms.TextBox tbRecordDelim;
        private System.Windows.Forms.Label label5;
    }
}
