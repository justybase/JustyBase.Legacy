namespace JustyBaseLegacy.UI.InputForms
{
    /// <summary>
    /// Base class for input forms that eliminates code duplication
    /// Provides common functionality for text input dialogs
    /// </summary>
    public partial class BaseInputForm : Form
    {
        protected TextBox inputTextBox;
        protected Button okButton;
        protected Button cancelButton;

        public string InputText
        {
            get => inputTextBox?.Text ?? string.Empty;
            set
            {
                if (inputTextBox != null)
                    inputTextBox.Text = value;
            }
        }

        public BaseInputForm()
        {
            InitializeBaseComponents();
            SetupEventHandlers();
        }

        public BaseInputForm(Action<Form> colorizeAction) : this()
        {
            colorizeAction?.Invoke(this);
        }

        private void InitializeBaseComponents()
        {
            // Create controls
            inputTextBox = new TextBox();
            okButton = new Button();
            cancelButton = new Button();

            SuspendLayout();

            // Configure TextBox
            inputTextBox.Location = new System.Drawing.Point(12, 12);
            inputTextBox.Name = "inputTextBox";
            inputTextBox.Size = new System.Drawing.Size(200, 23);
            inputTextBox.TabIndex = 0;

            // Configure OK Button
            okButton.Location = new System.Drawing.Point(12, 50);
            okButton.Name = "okButton";
            okButton.Size = new System.Drawing.Size(75, 23);
            okButton.TabIndex = 1;
            okButton.Text = "OK";
            okButton.UseVisualStyleBackColor = true;

            // Configure Cancel Button
            cancelButton.Location = new System.Drawing.Point(100, 50);
            cancelButton.Name = "cancelButton";
            cancelButton.Size = new System.Drawing.Size(75, 23);
            cancelButton.TabIndex = 2;
            cancelButton.Text = "Cancel";
            cancelButton.UseVisualStyleBackColor = true;

            // Configure Form
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(224, 85);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            KeyPreview = true;

            // Add controls
            Controls.Add(cancelButton);
            Controls.Add(okButton);
            Controls.Add(inputTextBox);

            ResumeLayout(false);
            PerformLayout();
        }

        private void SetupEventHandlers()
        {
            okButton.Click += OkButton_Click;
            cancelButton.Click += CancelButton_Click;
            KeyDown += BaseInputForm_KeyDown;
        }

        protected virtual void OkButton_Click(object sender, EventArgs e)
        {
            if (ValidateInput())
            {
                OnInputAccepted();
                DialogResult = DialogResult.OK;
            }
        }

        protected virtual void CancelButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        protected virtual void BaseInputForm_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Escape:
                    DialogResult = DialogResult.Cancel;
                    break;
                case Keys.Return or Keys.Enter:
                    if (ValidateInput())
                    {
                        OnInputAccepted();
                        DialogResult = DialogResult.OK;
                    }
                    break;
            }
        }

        /// <summary>
        /// Override this method to add custom validation logic
        /// </summary>
        protected virtual bool ValidateInput()
        {
            return !string.IsNullOrWhiteSpace(InputText);
        }

        /// <summary>
        /// Override this method to handle input acceptance (e.g., save data)
        /// </summary>
        protected virtual void OnInputAccepted()
        {
            // Base implementation does nothing
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BaseInputForm));
            SuspendLayout();
            // 
            // BaseInputForm
            // 
            ClientSize = new Size(284, 261);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "BaseInputForm";
            ResumeLayout(false);

        }

        /// <summary>
        /// Helper method to show the dialog and return the input text
        /// </summary>
        public static string ShowInputDialog(string title, string defaultText = "", Action<Form> colorizeAction = null)
        {
            using (var form = new BaseInputForm(colorizeAction))
            {
                form.Text = title;
                form.InputText = defaultText;

                if (form.ShowDialog() == DialogResult.OK)
                {
                    return form.InputText;
                }
            }
            return null;
        }
    }
}
