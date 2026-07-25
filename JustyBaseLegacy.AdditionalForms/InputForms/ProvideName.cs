using JustyBaseLegacy.UI.InputForms;

namespace JustyBaseLegacy.UI.DbForms
{
    public class ProvideName : BaseInputForm
    {
        public string ProvidedName { get; private set; }

        public ProvideName(Action<Form> doColorize) : base(doColorize)
        {
            Text = "Enter name";
            okButton.Text = "Save";
        }

        protected override void OnInputAccepted()
        {
            ProvidedName = InputText;
        }

        protected override bool ValidateInput()
        {
            return !string.IsNullOrWhiteSpace(InputText);
        }

        private void InitializeComponent()
        {

        }

        /// <summary>
        /// Static helper method to show the dialog and get the name
        /// </summary>
        public static string GetNameFromUser(Action<Form> colorizeAction = null, string defaultName = "")
        {
            using (var form = new ProvideName(colorizeAction))
            {
                form.InputText = defaultName;
                if (form.ShowDialog() == DialogResult.OK)
                {
                    return form.ProvidedName;
                }
            }
            return null;
        }
    }
}
