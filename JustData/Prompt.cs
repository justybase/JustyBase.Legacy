using JustData.Properties;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace JustyBaseLegacy.UI;

public static class Prompt
{
    static Button _bt;
    public static DialogResult ShowDialog(Dictionary<string, string> textList, string caption, out List<string> result)
    {
        int count = textList.Count;
        result = new List<string>();

        Form prompt = new Form()
        {
            Height = 130 + 25 * (count - 1),
            FormBorderStyle = FormBorderStyle.FixedSingle,
            MaximizeBox = false,
            MinimizeBox = false,
            Text = caption,
            StartPosition = FormStartPosition.CenterScreen,
            AutoScaleMode = AutoScaleMode.Font,
            MinimumSize = new Size(350, 150),
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Icon = Resources.icon2ico
        };

        List<TextBox> textBoxes = new List<TextBox>();

        int index = 0;
        int maxWidth = 0;
        foreach (var item in textList)
        {
            int textWidth = TextRenderer.MeasureText(item.Key, prompt.Font).Width;
            if (textWidth > maxWidth)
            {
                maxWidth = textWidth;
            }

            Label textLabel = new Label() { Left = 10, Top = 20 + index * 25, Text = item.Key, Width = textWidth + 10 };
            if (textLabel.Width > maxWidth)
            {
                maxWidth = textLabel.Width;
            }
            TextBox textBox = new TextBox()
            {
                Top = 20 + index * 25,
                Text = item.Value,
                Width = 150,
                Left = 80
            };
            textBox.Anchor = AnchorStyles.Top | AnchorStyles.Left;


            textBoxes.Add(textBox);
            prompt.Controls.Add(textLabel);
            prompt.Controls.Add(textBox);
            textBox.KeyDown += TextBox_KeyDown;
            index++;
        }

        foreach (TextBox item in prompt.Controls.OfType<TextBox>())
        {
            item.Left = maxWidth + 200 - 170;
        }


        Button confirmation = new Button()
        {
            Text = "OK"
            ,
            Width = 80
            ,
            DialogResult = DialogResult.OK
        };
        confirmation.Click += (sender, e) => { prompt.Close(); };
        confirmation.Location = new Point(prompt.Width / 2 - 40, prompt.Height - 70);
        confirmation.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;


        prompt.Controls.Add(confirmation);
        prompt.AcceptButton = confirmation;
        _bt = confirmation;

        prompt.Width = maxWidth + 200;

        DialogResult dialogResult = prompt.ShowDialog();

        if (dialogResult == DialogResult.OK)
        {
            int i = 0;
            foreach (var item in textList.Keys)
            {
                result.Add(textBoxes[i++].Text);
            }
        }
        return dialogResult;
    }
    private static void TextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.F5)
        {
            Prompt._bt.PerformClick();
        }
        else if (e.KeyCode == Keys.Escape)
        {
            ((sender as TextBox).Parent as Form).Close();
        }
    }
}
