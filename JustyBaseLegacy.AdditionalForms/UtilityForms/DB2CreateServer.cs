using System.Diagnostics;

namespace JustyBaseLegacy.UI.DbForms
{
    public partial class DB2CreateServer : Form
    {
        public DB2CreateServer(Action<Form> DoColorize)
        {
            InitializeComponent();
            cbDataSource.Items.Clear();
            cbDataSource.Items.AddRange(m_server.Keys.ToArray());
            DoColorize(this);
        }

        Dictionary<string, (string, string)> m_server = new Dictionary<string, (string, string)>
        {
{"Amazon Redshift",("REDSHIFT","ODBC")},
{"Apache Hive",("HIVE","ODBC")},
{"Apache Spark",("SPARK","JDBC")},
{"Apache Spark SQL",("SPARK_ODBC","ODBC")},
{"Cloudera Impala",("IMPALA","ODBC")},
{"CouchDB",("COUCHDB","NoSQL")},
{"database.com",("DATABASE.COM","ODBC")},
{"force.com",("FORCE.COM","ODBC")},
{"HDFS parquet",("HDFSPARQUET","NoSQL")},
{"IBM BigInsights",("BIGSQL","DRDA")},
{"IBM Db2® Warehouse on Cloud",("DASHDB","DRDA")},
{"IBM Db2 on Cloud",("DASHDB","DRDA")},
{"IBM Db2 Warehouse",("DASHDB","DRDA")},
{"IBM Db2",("DB2/LUW","DRDA")},
{"Db2 for z/OS®",("DB2/ZOS","DRDA")},
{"Db2 for IBM® i",("DB2/ISERIES","DRDA")},
{"IBM Db2 Hosted",("DB2/LUW","DRDA")},
{"IBM Db2 Server for VSE and VM",("DB2/VM","DRDA")},
{"IBM PureData System for Analytics (formerly Netezza) v1",("PDA","ODBC")},
{"IBM PureData System for Analytics (formerly Netezza) v2",("NETEZZA","ODBC")},
{"IBM PureData System for Operational Analytics",("DB2/LUW","DRDA")},
{"IBM PureData System for Transactions",("DB2/LUW","DRDA")},
{"Informix® (with INFORMIX wrapper)",("INFORMIX","INFORMIX")},
{"Informix (with ODBC wrapper)",("INFORMIX_ODBC","ODBC")},
{"JDBC",("JDBC","JDBC")},
{"MariaDB",("MARIADB","ODBC")},
{"Microsoft Azure",("AZURE","ODBC")},
{"Microsoft SQL Server (with MSSQLODBC3 wrapper)",("MSSQLSERVER","MSSQLODBC3")},
{"Microsoft SQL Server (with ODBC wrapper)",("MSSQL_ODBC","ODBC")},
{"MongoDB",("MONGODBREST1, MONGODRIVER2, RESTHEART3","NoSQL")},
{"ODBC",("ODBC","ODBC")},
{"Oracle (with NET8 wrapper)",("ORACLE","NET8")},
{"Oracle (with ODBC wrapper)",("ORACLE_ODBC","ODBC")},
{"Oracle Cloud",("ORACLE_CLOUD","ODBC")},
{"Oracle MySQL",("MYSQL","ODBC")},
{"Pivotal Greenplum",("GREENPLUM","ODBC")},
{"Pivotal HAWQ",("HAWQ","ODBC")},
{"PostgreSQL",("POSTGRESQL","ODBC")},
{"Progress OpenEdge",("OPENEDGE","ODBC")},
{"Salesforce",("SALESFORCE","ODBC")},
{"SAP HANA",("HANA","ODBC")},
{"SAP Sybase",("SYBASE","CTLIB")},
{"SAP Sybase IQ",("SYBASEIQ","ODBC")},
{"SAP Sybase ASE",("SYBASE_ODBC","ODBC")},
{"Teradata (with TERADATA wrapper)",("TERADATA","TERADATA")},
{"Teradata (with ODBC wrapper)",("TERADATA_ODBC","ODBC")}
        };

        private void cbDataSource_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (m_server.TryGetValue(cbDataSource.SelectedItem.ToString(), out var res))
            {
                tbType.Text = res.Item1;
                tbWrapper.Text = res.Item2;
                tbType.ReadOnly = true;
                tbWrapper.ReadOnly = true;
            }
            else
            {
                tbType.ReadOnly = false;
                tbWrapper.ReadOnly = false;
            }
        }

        private void btGenerateSql_Click(object sender, EventArgs e)
        {
            string auth = "";
            if (cbAuthorization.Checked)
            {
                auth = $@"
AUTHORIZATION '{tbUser.Text}'
PASSWORD '{tbPassword.Text}'
";
            }

            List<string> opt = new List<string>(dgvOptions.Rows.Count);
            for (int i = 0; i < dgvOptions.Rows.Count; i++)
            {
                object optionName = dgvOptions.Rows[i].Cells[0].Value;
                if (optionName is null || optionName == DBNull.Value)
                {
                    break;
                }
                opt.Add($"    {optionName} {dgvOptions.Rows[i].Cells[1].Value}");
            }


            tbSql1.Text = $@"CREATE SERVER {tbServerName.Text}
TYPE {tbType.Text}
VERSION {tbVersion.Text}
WRAPPER {tbWrapper.Text}{auth}
OPTIONS
(
{String.Join("\r\n", opt)}
);

GRANT PASSTHRU ON SERVER {tbServerName.Text} TO USER <TYPE D2 USER HERE>;

-- REMEMBER TO INFORM USER ABOUT CREATION OF USER MAPPING
-- SAMPLE CODE
CREATE USER MAPPING FOR <TYPE D2 USER HERE>
SERVER {tbServerName.Text}
OPTIONS 
(
    REMOTE_AUTHID '<TYPE REMOTE USER HERE>',
    REMOTE_PASSWORD '<TYPE REMOTE PASSWORD HERE>'
);



";
        }


        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var p = new Process();
            p.StartInfo = new ProcessStartInfo("https://www.ibm.com/docs/en/db2/11.5?topic=reference-data-source-options")
            {
                UseShellExecute = true
            };
            p.Start();
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var p = new Process();
            p.StartInfo = new ProcessStartInfo("https://www.ibm.com/docs/en/db2/11.5?topic=statements-create-server")
            {
                UseShellExecute = true
            };
            p.Start();
        }

        private void cbAuthorization_CheckedChanged(object sender, EventArgs e)
        {
            lbUser.Enabled = cbAuthorization.Checked;
            lbPass.Enabled = cbAuthorization.Checked;
            tbUser.Enabled = cbAuthorization.Checked;
            tbPassword.Enabled = cbAuthorization.Checked;
            tbAuthInfo.Enabled = cbAuthorization.Checked;
        }

        private void BtLoadSampleOptions_Click(object sender, EventArgs e)
        {
            dgvOptions.Rows.Clear();

            if (cbDataSource.SelectedItem is null)
            {
                return;
            }


            dgvOptions.Rows.Add(new string[] { "HOST", "'host.name'" });

            string port = "123456";

            if (cbDataSource.SelectedItem.ToString().Contains("Microsoft"))
            {
                port = "1433";
            }
            else if (cbDataSource.SelectedItem.ToString().Contains("Db2"))
            {
                port = "50000";
            }
            else if (cbDataSource.SelectedItem.ToString().Contains("Netezza"))
            {
                port = "5480";
            }
            else if (cbDataSource.SelectedItem.ToString().Contains("Oracle"))
            {
                port = "1521";
            }
            else if (cbDataSource.SelectedItem.ToString() == "Amazon Redshift")
            {
                port = "5439";
            }
            else if (cbDataSource.SelectedItem.ToString() == "Apache Hive")
            {
                port = "10000";
            }
            else if (cbDataSource.SelectedItem.ToString().Contains("Apache Spark"))
            {
                port = "10001";
            }

            dgvOptions.Rows.Add(new string[] { "PORT", port });

            if (cbDataSource.SelectedItem.ToString().Contains("Oracle"))
            {
                dgvOptions.Rows.Add(new string[] { "SERVICE_NAME ", "'default'" });
            }
            else
            {
                dgvOptions.Rows.Add(new string[] { "DBNAME ", "'default'" });
            }
        }

        private void btCopySql1_Click_1(object sender, EventArgs e)
        {
            Clipboard.SetText(tbSql1.Text);
            DialogResult = DialogResult.OK;
        }
    }
}
