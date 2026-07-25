using System.Diagnostics;

namespace JustyBaseLegacy.UI.DbForms
{
    public class ProvideCode : Form
    {
        private FastColoredTextBoxNS.FastColoredTextBox _codeTextBox;
        private Button _saveButton;
        private Button _cancelButton;
        private LinkLabel _docsLink;

        public string GetCode { get; private set; }

        public ProvideCode(Action<Form> colorizeAction = null)
        {
            InitializeComponents();
            colorizeAction?.Invoke(this);
        }

        private void InitializeComponents()
        {
            SuspendLayout();

            // Create and configure the code text box
            _codeTextBox = new FastColoredTextBoxNS.FastColoredTextBox
            {
                Language = FastColoredTextBoxNS.Language.SQL,
                Location = new Point(1, 1),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            // Create buttons
            _saveButton = new Button
            {
                Text = "Save",
                Size = new Size(75, 23),
                UseVisualStyleBackColor = true,
                TabIndex = 1
            };

            _cancelButton = new Button
            {
                Text = "Cancel",
                Size = new Size(75, 23),
                UseVisualStyleBackColor = true,
                TabIndex = 2
            };

            // Create docs link
            _docsLink = new LinkLabel
            {
                Text = "Docs",
                AutoSize = true,
                TabIndex = 3
            };

            // Configure form
            Text = "Enter Code";
            Size = new Size(800, 450);
            StartPosition = FormStartPosition.CenterParent;
            KeyPreview = true;

            // Add controls
            Controls.Add(_codeTextBox);
            Controls.Add(_saveButton);
            Controls.Add(_cancelButton);
            Controls.Add(_docsLink);

            // Set up event handlers
            _saveButton.Click += SaveButton_Click;
            _cancelButton.Click += CancelButton_Click;
            _docsLink.LinkClicked += DocsLink_LinkClicked;
            KeyDown += ProvideCode_KeyDown;
            Resize += ProvideCode_Resize;

            // Initial layout
            UpdateLayout();

            ResumeLayout(false);
            PerformLayout();
        }

        private void UpdateLayout()
        {
            const int margin = 10;
            const int buttonHeight = 23;
            const int buttonWidth = 75;

            // Position code text box
            _codeTextBox.Location = new Point(1, 1);
            _codeTextBox.Size = new Size(ClientSize.Width - 2, ClientSize.Height - buttonHeight - margin * 2);

            // Position buttons at the bottom
            int buttonY = ClientSize.Height - buttonHeight - margin;
            _saveButton.Location = new Point(ClientSize.Width - buttonWidth * 2 - margin * 2, buttonY);
            _cancelButton.Location = new Point(ClientSize.Width - buttonWidth - margin, buttonY);
            _docsLink.Location = new Point(margin, buttonY + 4);
        }

        private void ProvideCode_Resize(object sender, EventArgs e)
        {
            UpdateLayout();
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            GetCode = _codeTextBox.Text;
            DialogResult = DialogResult.OK;
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        private void DocsLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo("https://www.ibm.com/docs/en/netezza?topic=reference-create-view")
                    {
                        UseShellExecute = true
                    }
                };
                process.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open documentation: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ProvideCode_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
            }
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ProvideCode));
            SuspendLayout();
            // 
            // ProvideCode
            // 
            ClientSize = new Size(284, 261);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "ProvideCode";
            ResumeLayout(false);

        }

        /// <summary>
        /// Gets the FastColoredTextBox for external access if needed
        /// </summary>
        public FastColoredTextBoxNS.FastColoredTextBox CodeTextBox => _codeTextBox;

        /// <summary>
        /// Static helper method to show the dialog and get code
        /// </summary>
        public static string GetCodeFromUser(Action<Form> colorizeAction = null, string defaultCode = "")
        {
            using (var form = new ProvideCode(colorizeAction))
            {
                form._codeTextBox.Text = defaultCode;
                if (form.ShowDialog() == DialogResult.OK)
                {
                    return form.GetCode;
                }
            }
            return null;
        }
    }
}
