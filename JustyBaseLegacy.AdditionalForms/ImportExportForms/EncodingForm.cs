using System.Text;

namespace JustyBaseLegacy.UI.DbForms
{
    public partial class EncodingForm : Form
    {
        string _path;
        byte[] buffer;

        public EncodingForm(string path)
        {
            InitializeComponent();
            buffer = new byte[tbPreview.MaxLength];
            _path = path;
            comboBox1.SelectedIndex = 0;
            ToolTip toolTip = new ToolTip();
            toolTip.ToolTipIcon = ToolTipIcon.Info;
            toolTip.SetToolTip(comboBox1, "chose file encoding");
        }
        public Encoding GetEncoding { get; set; }
        private void button1_Click(object sender, EventArgs e)
        {
            GetEncoding = getEncoding(comboBox1.Text);
            DialogResult = DialogResult.OK;
        }
        private static Encoding getEncoding(string name)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            return name switch
            {
                "UTF8" => new UTF8Encoding(false),
                "UTF8 + bom" => new UTF8Encoding(true),
                "ASCII" => Encoding.ASCII,
                "UNICODE" => Encoding.Unicode,
                "UTF32" => Encoding.UTF32,
                "1250" => Encoding.GetEncoding(1250),
                "1252" => Encoding.GetEncoding(1252),
                "Latin1" => Encoding.Latin1,
                "ISO-8859-1" => Encoding.GetEncoding("ISO-8859-1"),
                _ => Encoding.UTF8,
            };
        }

        int readed = -1;
        bool doPreview()
        {
            try
            {
                tbPreview.Enabled = false;
                GetEncoding = getEncoding(comboBox1.Text);
                if (readed == -1)
                {
                    try
                    {
                        using (FileStream sr = new FileStream(_path, FileMode.Open))
                        {
                            readed = sr.Read(buffer, 0, tbPreview.MaxLength);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Cannot open file", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }
                }
                tbPreview.Text = GetEncoding.GetString(buffer.AsSpan().Slice(0, readed));
            }
            finally
            {
                tbPreview.Enabled = true;
            }
            return true;
        }
        private void button2_Click(object sender, EventArgs e)
        {
            doPreview();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!doPreview())
            {
                this.button1.Enabled = false;
            }

        }
    }
}
