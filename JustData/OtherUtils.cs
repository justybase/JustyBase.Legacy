using FastColoredTextBoxNS;
using System.Windows.Forms;

namespace JustyBaseLegacy.UI;

public static class OtherUtils
{
    public static TabPage FindAncestorTabPage(this Control control)
    {
        for (Control current = control; current != null; current = current.Parent)
        {
            if (current is TabPage tabPage)
            {
                return tabPage;
            }
        }

        return null;
    }


    public static void OnlyNzMesage(Form form)
    {
        TaskDialog.ShowDialog(form, new TaskDialogPage()
        {
            Text = @$"This feature is currently available only for Netezza",
            Heading = "Information",
            Caption = "Information",
            Buttons =
            {
                TaskDialogButton.OK
            },
            Icon = TaskDialogIcon.Information,
            DefaultButton = TaskDialogButton.OK
        }); ;
    }


    public static void AboutMessage(Form form)
    {
        TaskDialogButton result = TaskDialog.ShowDialog(form, new TaskDialogPage()
        {
            Text = "JustyBaseLegacy - SQL Editor",
            Heading = "About",
            Caption = "About",
            Buttons =
                {
                    TaskDialogButton.OK
                },
            DefaultButton = TaskDialogButton.OK
        });
    }
}
