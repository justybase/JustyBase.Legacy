using AppBase.Common;
using JustDataAdditionalForms;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace JustyBaseLegacy.UI;

public partial class CsvFastImport : Form
{
    public CsvFastImport(
        IFastNetezzaCsvImport fastNetezzaCsvImport,
        string connectionString, Action<Form> DoColorize, Action<DataGridView> DoubleBuff,
        Func<string, DbConnection> getConnection, string configDirectory)
    {
        InitializeComponent();
        import = fastNetezzaCsvImport;
        _getConnection = getConnection;
        DoubleBuff(dgvTypes);

        DoColorize(this);

        this.customProgressBar1.SetState(3);
        this.progessDownloaded.SetState(3);
        this.progressProceded.SetState(3);
        this.connectionString = connectionString;
        this.tbLogDir.Text = $@"{configDirectory}\data\";
        string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        this.tbTable.Text = "imp_" + DateTime.Now.ToString("yyMMdd_HHmm") + new string(Enumerable.Repeat(letters, 10).Select(s => s[Random.Shared.Next(s.Length)]).ToArray());
    }

    Func<string, DbConnection> _getConnection;

    readonly string connectionString;

    private void btOpenFile_Click(object sender, EventArgs e)
    {
        var d = openFileDialog1.ShowDialog();
        if (d == DialogResult.OK)
        {
            string path = openFileDialog1.FileName;
            tbFilePath.Text = path;
            if (File.Exists(path))
            {
                FileBytes = new FileInfo(tbFilePath.Text).Length;
                this.tbFileSize.Text = ((double)FileBytes / 1024 / 1024).ToString("N1") + " MB";
            }
        }

    }
    readonly Stopwatch stopwatch = new Stopwatch();
    private IFastNetezzaCsvImport import;

    long FileBytes;

    private char getDelim()
    {
        char delim = tbDelimiter.Text[0];
        if (tbDelimiter.Text == "\\t")
        {
            delim = '\t';
        }
        return delim;
    }

    private async void btGo_Click(object sender, EventArgs e)
    {
        try
        {
            if (dgvTypes.RowCount == 0)
            {
                refreshColumns();
            }
            else
            {
                var row = dgvTypes.Rows[0];
                if (row.Cells[0].Value == null || row.Cells[0].Value == DBNull.Value)
                {
                    refreshColumns();
                }
            }
        }
        catch (IOException ioexp)
        {
            MessageBox.Show(ioexp.Message, "Cannot open file", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (this.customProgressBar1.InvokeRequired)
        {
            this.customProgressBar1.Invoke(() =>
            {
                this.customProgressBar1.SetState(3);
                this.progessDownloaded.SetState(3);
                this.progressProceded.SetState(3);
                this.customProgressBar1.Value = 0;
                this.tbInfo.AppendText($"Started: {DateTime.Now}{Environment.NewLine}");
            });
        }
        else
        {
            this.customProgressBar1.SetState(3);
            this.progessDownloaded.SetState(3);
            this.progressProceded.SetState(3);
            this.customProgressBar1.Value = 0;
            this.tbInfo.AppendText($"Started: {DateTime.Now}{Environment.NewLine}");
        }
        times.Clear();

        import.GetCollumnsFun = this.getColumns;
        import.ConnectionString = connectionString;
        import.Tablename = this.tbTable.Text;
        import.FilePath = this.tbFilePath.Text;
        import.ImportToExisting = this.cbExisting.Checked;

        import.ProgessUnit = (long)this.numProgressUnit.Value;
        import.SkipRows = (long)this.numSkipRows.Value;
        import.StopOnEmpty = this.cbStopWhenEmpty.Checked;
        if (cbTop1000.Checked)
        {
            import.Limit1000 = true;
        }
        else
        {
            import.Limit1000 = false;
        }

        import.Progress += Import_Progress;
        import.ForcedStop += Import_ForcedStop;

        if (!string.IsNullOrWhiteSpace(tbFilterRow.Text))
        {
            import.FilterRow = true;
            import.RxFilter = new Regex(tbFilterRow.Text, RegexOptions.Compiled);
        }
        if (!string.IsNullOrWhiteSpace(tbReject.Text))
        {
            import.RejectRow = true;
            import.RxReject = new Regex(tbReject.Text, RegexOptions.Compiled);
        }

        if (!string.IsNullOrWhiteSpace(tbTranferRegex.Text))
        {
            import.TransformRow = true;
            import.RxTransform = new Regex(tbTranferRegex.Text, RegexOptions.Compiled);
            import.RelaceValue = tbTransferedValue.Text;
        }

        import.escapechar = tbEscapeChar.Text[0];

        import.ColumnDelimiter = getDelim();

        import.DECIMALDELIM = tbDecimalDelim.Text[0];
        import.RecordDelim = tbRecordDelim.Text;
        import.REMOTESOURCE = tbRemoteSource.Text;
        import.NULLVALUE = tbNullValue.Text;
        import.ENCODING = tbEnconding.Text;
        import.TIMESTYLE = tbTimestyle.Text;
        import.LOGDIR = tbLogDir.Text;
        import.MAXROWS = (long)numMaxRows.Value;
        import.SocketBufSize = (long)numSocketBufSize.Value;
        import.TruncString = cbTruncString.Checked;
        import.SkipRows = (long)numSkipRows.Value;
        import.SingleColumnMode = cbSingleColumn.Checked;
        import.TruncString = cbTruncString.Checked;
        import.CRinString = cbCRinString.Checked;
        import.LFinString = cbLFinString.Checked;
        import.CtrlChars = cbCtrlChars.Checked;
        import.FillRecord = cbFillRecord.Checked;
        import.IgnoreZero = cbIgnoreZero.Checked;
        import.IncludeHeader = cbIncludeHeader.Checked;
        import.IncludeZeroSeconds = cbIncludeZeroSeconds.Checked;
        import.Compress = cbCompress.Checked;
        import.RequireQuotes = cbRequireQuotes.Checked;
        import.TimeRoundNanos = cbTimeRoundNanos.Checked;

        if (!cbAdvancedMode.Checked)
        {
            stopwatch.Start();
            await Task.Run(() =>
            {
                try
                {
                    string serverName = import.StartServer();
                    import.MakeImport(serverName, 4 * 60 * 60, _getConnection);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            });
            stopwatch.Reset();
            Import_Finish();
        }
        else
        {
            string serverName = import.StartServer();
            var (_, _, fullCreate) = import.GetCodes(tbTable.Text, serverName);
            this.tbInfo.AppendText(fullCreate);
        }
    }

    private void Import_ForcedStop()
    {
        if (this.customProgressBar1.InvokeRequired)
        {
            this.customProgressBar1.Invoke(() =>
            {
                this.customProgressBar1.SetState(2);
                this.progessDownloaded.SetState(2);
                this.progressProceded.SetState(2);
                this.tbInfo.AppendText($"Stoped: {DateTime.Now}{Environment.NewLine}");
            });
        }
        else
        {
            this.customProgressBar1.SetState(2);
            this.progessDownloaded.SetState(2);
            this.progressProceded.SetState(2);
            this.tbInfo.AppendText($"Stoped: {DateTime.Now}{Environment.NewLine}");
        }
    }

    private void Import_Finish()
    {
        if (this.customProgressBar1.InvokeRequired)
        {
            this.customProgressBar1.Invoke(() =>
            {
                this.customProgressBar1.SetState(1);
                this.customProgressBar1.Value = 100;

                this.progessDownloaded.SetState(1);
                this.progessDownloaded.Value = 100;

                this.progressProceded.SetState(1);
                this.progressProceded.Value = 100;
                this.tbInfo.AppendText($"Finished: {DateTime.Now}{Environment.NewLine}");
            });
        }
        else
        {
            this.customProgressBar1.SetState(1);
            this.customProgressBar1.Value = 100;

            this.progessDownloaded.SetState(1);
            this.progessDownloaded.Value = 100;

            this.progressProceded.SetState(1);
            this.progressProceded.Value = 100;
        }
    }

    readonly Queue<double> times = new Queue<double>();

    private void Import_Progress(long obj)
    {
        int progres1 = (int)((double)100.0 * obj / FileBytes);
        double progresDouble = ((double)obj / FileBytes);
        if (progres1 > 100)
        {
            progres1 = 100;
        }

        if (progresDouble > 1.0)
        {
            progresDouble = 1.0;
        }


        if (this.customProgressBar1.InvokeRequired)
        {
            this.customProgressBar1.Invoke(() =>
            {
                this.customProgressBar1.Value = progres1;
                if (progresDouble >= 0.01)
                {
                    double time = stopwatch.Elapsed.Seconds / progresDouble - stopwatch.Elapsed.Seconds;
                    if (times.Count == 10)
                    {
                        times.Dequeue();
                    }
                    times.Enqueue(time);
                    double avgTime = times.Average();
                    this.tbExtimatedSecs.Text = avgTime.ToString("# sec");
                }
            });
        }
        else
        {
            this.customProgressBar1.Value = progres1;
        }
        //this.customProgressBar1.Invalidate();
    }

    private void BtAbort_Click(object sender, EventArgs e)
    {
        if (import != null)
        {
            import.StopTask = true;
        }


        if (this.customProgressBar1.InvokeRequired)
        {
            this.customProgressBar1.Invoke(() =>
            {
                this.customProgressBar1.SetState(2);
                this.progessDownloaded.SetState(2);
                this.progressProceded.SetState(2);
            });
        }
        else
        {
            this.customProgressBar1.SetState(2);
            this.progessDownloaded.SetState(2);
            this.progressProceded.SetState(2);
        }
    }

    readonly string statsSql = @"SELECT 
            PLANID  -- int32
            , DATABASENAME -- string
            , TABLENAME -- string
            , SCHEMANAME -- string
            , USERNAME -- string
            , BYTESPROCESSED --int64
            , ROWSINSERTED --int64
            , ROWSREJECTED --int64
            , BYTESDOWNLOADED --int64
        FROM 
            _v_load_status
        WHERE
            TABLENAME = ";

    private async void button1_Click(object sender, EventArgs e)
    {
        await Task.Run(() =>
        {
            try
            {
                using (DbConnection conn = _getConnection(connectionString))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = statsSql + $"'{import.Tablename.ToUpper()}'";
                        var rdr = cmd.ExecuteReader();
                        if (rdr.Read())
                        {
                            tbRowsInserted.Invoke(() =>
                            {
                                tbRowsInserted.Text = rdr.GetString(6);
                                long bytesprocessed = rdr.GetInt64(5);
                                if (bytesprocessed < FileBytes && FileBytes > 0)
                                {
                                    progressProceded.Value = (int)(100 * bytesprocessed / FileBytes);
                                }
                                long bytesDownloaded = rdr.GetInt64(8);
                                if (bytesDownloaded < FileBytes && FileBytes > 0)
                                {
                                    progessDownloaded.Value = (int)(100 * bytesDownloaded / FileBytes);
                                }
                            });
                        }
                    }
                    conn.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        });
    }

    private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
    {
        Process.Start(new ProcessStartInfo("cmd", $"/c start {"https://www.ibm.com/docs/en/netezza?topic=options-option-summary".Replace("&", "^&")}") { CreateNoWindow = true });
    }

    public string[] getColumns()
    {
        var cols = new List<string>();
        this.Invoke(() =>
        {
            for (int i = 0; i < dgvTypes.RowCount; i++)
            {
                var row = dgvTypes.Rows[i];
                if (row.Cells[0].Value == null || row.Cells[0].Value == DBNull.Value)
                {
                    continue;
                }
                string name = row.Cells[0].Value.ToString();
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                string typeAdn = $"{row.Cells[2].Value}";
                string precision = $"{row.Cells[3].Value}";
                string scale = $"{row.Cells[4].Value}";

                if (!string.IsNullOrWhiteSpace(typeAdn))
                {
                    typeAdn = "(" + typeAdn + ")";
                }
                else if (!string.IsNullOrWhiteSpace(precision))
                {
                    typeAdn = "(" + precision + "," + scale + ")";
                }

                string nullAdn = "";
                cols.Add(name + " " + row.Cells[1].Value + typeAdn + " " + nullAdn);
            }
        });
        return cols.ToArray();
    }

    private void refreshColumns()
    {
        dgvTypes.Rows.Clear();
        string line;

        using (StreamReader streamReader = new StreamReader(tbFilePath.Text))
        {
            line = streamReader.ReadLine();
            streamReader.Close();
        }

        if (!string.IsNullOrWhiteSpace(tbTranferRegex.Text))
        {
            line = Regex.Replace(line, tbTranferRegex.Text, tbTransferedValue.Text);
        }
        int n = line.Split(getDelim()).Length;
        if (n == 0 || cbSingleColumn.Checked)
        {
            int rn = dgvTypes.Rows.Add(new object[] { "COL0", DBNull.Value, 1024, DBNull.Value, DBNull.Value });
            dgvTypes.Rows[rn].Cells[1].Value = "NVARCHAR";
        }
        else
        {
            for (int i = 0; i < n; i++)
            {
                int rn = dgvTypes.Rows.Add(new object[] { $"COL{i}", DBNull.Value, 1024, DBNull.Value, DBNull.Value });
                dgvTypes.Rows[rn].Cells[1].Value = "NVARCHAR";
            }
        }
    }

    private void btLoadColumns_Click(object sender, EventArgs e)
    {
        try
        {
            refreshColumns();
        }
        catch (IOException ioexp)
        {
            MessageBox.Show(ioexp.Message, "Cannot open file", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
    }

    private void CsvFastImport_Load(object sender, EventArgs e)
    {
        ToolTip toolTip1 = new ToolTip();

        // Set up the delays for the ToolTip.
        toolTip1.AutoPopDelay = 5000;
        toolTip1.InitialDelay = 1000;
        toolTip1.ReshowDelay = 500;
        // Force the ToolTip text to be displayed whether or not the form is active.
        toolTip1.ShowAlways = true;

        // Set up the ToolTip text for the Button and Checkbox.
        toolTip1.SetToolTip(this.cbTruncString, @"Specifies how to process strings that are longer than their declared storage.
A value of True causes the system to truncate any string value that exceeds its declared char or varchar storage. 
A value of False causes the system to report an error when a string exceeds its declared storage. 
If you do not specify the option, the default value is False. 
If you specify the option with no value, the default value is True.
This option is not supported for the fixed-length format.");

        toolTip1.SetToolTip(this.cbFillRecord, @"Specifies whether to allow an input line with fewer columns than that of the table definition. 
If you do not specify the option, an input line with fewer columns than that of the table definition is not allowed.
By default, the system expects one input field for every column in the schema of a target table and rejects a row with fewer fields. 
If you specify the FillRecord option, the system allows the omission of one or more trailing (rightmost) fields if all corresponding columns can be null.
This option is not supported for the fixed-length format.");

        toolTip1.SetToolTip(this.cbCompress, "Specifies whether the source data file data is compressed.");
        toolTip1.SetToolTip(this.cbCtrlChars, "Specifies whether to allow an ASCII value of 1 - 31 in char, varchar, nchar, and nvarchar fields.");
        toolTip1.SetToolTip(this.cbTimeRoundNanos, @"Rounds the time value to six fractional seconds digits. 
You can use the timeRoundNanos option to specify that the system allows and rounds non-zero digits with smaller than microsecond precision. 
The option is also referred to as the TimeExtraZeros option.");
        toolTip1.SetToolTip(this.cbIgnoreZero, "Specifies whether to discard binary value zero in char and varchar fields.");
        toolTip1.SetToolTip(this.cbIncludeHeader, "Specifies whether to include the table column names as headers in the external table file.");
        toolTip1.SetToolTip(this.cbIncludeZeroSeconds, @"If set to true or specified with no value (in which case, the default is true),
specifies that ""00"" seconds values are unloaded into the external table.
For example, a time value such as 12:34:00 or 12:34 is unloaded into the external table in the format 12:34:00.");
        toolTip1.SetToolTip(this.cbLFinString, @"Specifies whether an embedded newline value that is also the record delimiter is treated as real data.
Acceptable values are true or false.The default value is false.Do not put quotation marks around the value.
By default, the newline value is the record delimiter.You can change the record delimiter to another value by using the -recDelim option.");
        toolTip1.SetToolTip(this.cbCRinString, @"Specifies whether to allow unescaped carriage returns in char, varchar, nchar, and nvarchar fields.
Acceptable values are as follows:
true or false
on or off
Do not put quotation marks around the value. The default value is false;
in this case and in the case of off, all CR or CRLF control characters are treated as end of record. 
If the value is true or on, unescaped CR control characters are accepted in char and varchar fields (the LF control character becomes only end of row).");


        toolTip1.SetToolTip(this.cbRequireQuotes, @"Specifies whether quotation marks are mandatory.
If you do not specify the option, the default is false. 
If you set the option to true or specify the option with no value (in which case, the default is true), you must set the QuotedValue option to YES, SINGLE, or DOUBLE.");
    }
}
