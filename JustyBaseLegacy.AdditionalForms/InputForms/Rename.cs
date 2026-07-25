using JustyBaseLegacy.UI.InputForms;

namespace JustyBaseLegacy.UI
{
    public class Rename : BaseInputForm
    {
        private readonly TabPage _tabPage;

        public Rename(TabPage tabPage, Action<Form> doColorize) : base(doColorize)
        {
            _tabPage = tabPage;
            Text = "Rename";
            InputText = tabPage.Text;

            // Focus the text box and select all text for easy editing
            inputTextBox.Focus();
            inputTextBox.SelectAll();
        }

        protected override void OnInputAccepted()
        {
            _tabPage.Text = InputText;
        }

        private void InitializeComponent()
        {

        }

        protected override bool ValidateInput()
        {
            return !string.IsNullOrWhiteSpace(InputText);
        }
    }
}
