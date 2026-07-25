using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace FastColoredTextBoxNS
{
    public partial class HotkeysEditorForm : Form
    {
        readonly BindingList<HotkeyWrapper> wrappers = new BindingList<HotkeyWrapper>();

        public HotkeysEditorForm(HotkeysMapping hotkeys, string message = "")
        {
            InitializeComponent();
            if (!String.IsNullOrWhiteSpace(message))
            {
                this.Text += (" " + message);
            }

            BuildWrappers(hotkeys);
            dgv.DataSource = wrappers;
        }

        int CompereKeys(Keys key1, Keys key2)
        {
            var res = ((int)key1 & 0xff).CompareTo((int)key2 & 0xff);
            if (res == 0)
                res = key1.CompareTo(key2);

            return res;
        }

        private void BuildWrappers(HotkeysMapping hotkeys)
        {
            var keys = new List<Keys>(hotkeys.Keys);
            keys.Sort(CompereKeys);

            wrappers.Clear();
            foreach (var k in keys)
                wrappers.Add(new HotkeyWrapper(k, hotkeys[k]));
        }

        /// <summary>
        /// Returns edited hotkey map
        /// </summary>
        /// <returns></returns>
        public HotkeysMapping GetHotkeys()
        {
            var result = new HotkeysMapping();
            foreach (var w in wrappers)
                result[w.ToKeyData()] = w.Action;

            return result;
        }

        private void btAdd_Click(object sender, EventArgs e)
        {
            wrappers.Add(new HotkeyWrapper(Keys.None, FCTBAction.None));
        }

        private void dgv_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            var cell = (dgv[0, e.RowIndex] as DataGridViewComboBoxCell);
            if(cell.Items.Count == 0)
            foreach(var item in new string[]{"", "Ctrl", "Ctrl + Shift", "Ctrl + Alt", "Shift", "Shift + Alt", "Alt", "Ctrl + Shift + Alt"})
                cell.Items.Add(item);

            cell = (dgv[1, e.RowIndex] as DataGridViewComboBoxCell);
            if (cell.Items.Count == 0)
            {
                // Use static array instead of Enum.GetValues() for AOT compatibility
                Keys[] allKeys = GetAllKeysValues();
                foreach (var item in allKeys)
                    cell.Items.Add(item);
            }

            cell = (dgv[2, e.RowIndex] as DataGridViewComboBoxCell);
            if (cell.Items.Count == 0)
            {
                // Use static array instead of Enum.GetValues() for AOT compatibility
                FCTBAction[] allActions = HotkeysMapping.GetAllFCTBActionValues();
                foreach (var item in allActions)
                    cell.Items.Add(item);
            }
        }

        private void btResore_Click(object sender, EventArgs e)
        {
            HotkeysMapping h = new HotkeysMapping();
            h.InitDefault();
            BuildWrappers(h);
        }

        private void btRemove_Click(object sender, EventArgs e)
        {
            for (int i = dgv.RowCount - 1; i >= 0; i--)
                if (dgv.Rows[i].Selected) dgv.Rows.RemoveAt(i);
        }

        private void HotkeysEditorForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if(DialogResult == System.Windows.Forms.DialogResult.OK)
            {
                var actions = GetUnAssignedActions();
                if (!string.IsNullOrEmpty(actions))
                {
                    if (MessageBox.Show("Some actions are not assigned!\r\nActions: " + actions + "\r\nPress Yes to save and exit, press No to continue editing", "Some actions is not assigned", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == System.Windows.Forms.DialogResult.No)
                        e.Cancel = true;
                }
            }
        }

        private string GetUnAssignedActions()
        {
            StringBuilder sb = new StringBuilder();
            var dic = new Dictionary<FCTBAction, FCTBAction>();

            foreach (var w in wrappers)
                dic[w.Action] = w.Action;

            // Use static array instead of Enum.GetValues() for AOT compatibility
            FCTBAction[] allActions = HotkeysMapping.GetAllFCTBActionValues();
            foreach (var item in allActions)
            if (item != FCTBAction.None)
            if(!item.ToString().StartsWith("CustomAction"))
            {
                if(!dic.ContainsKey(item))
                    sb.Append(item+", ");
            }

            return sb.ToString().TrimEnd(' ', ',');
        }

        /// <summary>
        /// Gets all Keys enum values for AOT compatibility
        /// </summary>
        private static Keys[] GetAllKeysValues()
        {
            return new Keys[]
            {
                Keys.None, Keys.LButton, Keys.RButton, Keys.Cancel, Keys.MButton, Keys.XButton1, Keys.XButton2,
                Keys.Back, Keys.Tab, Keys.LineFeed, Keys.Clear, Keys.Return, Keys.Enter, Keys.ShiftKey,
                Keys.ControlKey, Keys.Menu, Keys.Pause, Keys.Capital, Keys.CapsLock, Keys.KanaMode,
                Keys.HanguelMode, Keys.HangulMode, Keys.JunjaMode, Keys.FinalMode, Keys.HanjaMode,
                Keys.KanjiMode, Keys.Escape, Keys.IMEConvert, Keys.IMENonconvert, Keys.IMEAccept,
                Keys.IMEAceept, Keys.IMEModeChange, Keys.Space, Keys.Prior, Keys.PageUp, Keys.Next,
                Keys.PageDown, Keys.End, Keys.Home, Keys.Left, Keys.Up, Keys.Right, Keys.Down,
                Keys.Select, Keys.Print, Keys.Execute, Keys.Snapshot, Keys.PrintScreen, Keys.Insert,
                Keys.Delete, Keys.Help, Keys.D0, Keys.D1, Keys.D2, Keys.D3, Keys.D4, Keys.D5,
                Keys.D6, Keys.D7, Keys.D8, Keys.D9, Keys.A, Keys.B, Keys.C, Keys.D, Keys.E,
                Keys.F, Keys.G, Keys.H, Keys.I, Keys.J, Keys.K, Keys.L, Keys.M, Keys.N, Keys.O,
                Keys.P, Keys.Q, Keys.R, Keys.S, Keys.T, Keys.U, Keys.V, Keys.W, Keys.X, Keys.Y,
                Keys.Z, Keys.LWin, Keys.RWin, Keys.Apps, Keys.Sleep, Keys.NumPad0, Keys.NumPad1,
                Keys.NumPad2, Keys.NumPad3, Keys.NumPad4, Keys.NumPad5, Keys.NumPad6, Keys.NumPad7,
                Keys.NumPad8, Keys.NumPad9, Keys.Multiply, Keys.Add, Keys.Separator, Keys.Subtract,
                Keys.Decimal, Keys.Divide, Keys.F1, Keys.F2, Keys.F3, Keys.F4, Keys.F5, Keys.F6,
                Keys.F7, Keys.F8, Keys.F9, Keys.F10, Keys.F11, Keys.F12, Keys.F13, Keys.F14,
                Keys.F15, Keys.F16, Keys.F17, Keys.F18, Keys.F19, Keys.F20, Keys.F21, Keys.F22,
                Keys.F23, Keys.F24, Keys.NumLock, Keys.Scroll, Keys.LShiftKey, Keys.RShiftKey,
                Keys.LControlKey, Keys.RControlKey, Keys.LMenu, Keys.RMenu, Keys.BrowserBack,
                Keys.BrowserForward, Keys.BrowserRefresh, Keys.BrowserStop, Keys.BrowserSearch,
                Keys.BrowserFavorites, Keys.BrowserHome, Keys.VolumeMute, Keys.VolumeDown,
                Keys.VolumeUp, Keys.MediaNextTrack, Keys.MediaPreviousTrack, Keys.MediaStop,
                Keys.MediaPlayPause, Keys.LaunchMail, Keys.SelectMedia, Keys.LaunchApplication1,
                Keys.LaunchApplication2, Keys.OemSemicolon, Keys.Oem1, Keys.Oemplus, Keys.Oemcomma,
                Keys.OemMinus, Keys.OemPeriod, Keys.OemQuestion, Keys.Oem2, Keys.Oemtilde,
                Keys.Oem3, Keys.OemOpenBrackets, Keys.Oem4, Keys.OemPipe, Keys.Oem5,
                Keys.OemCloseBrackets, Keys.Oem6, Keys.OemQuotes, Keys.Oem7, Keys.Oem8,
                Keys.OemBackslash, Keys.Oem102, Keys.ProcessKey, Keys.Packet, Keys.Attn,
                Keys.Crsel, Keys.Exsel, Keys.EraseEof, Keys.Play, Keys.Zoom, Keys.NoName,
                Keys.Pa1, Keys.OemClear, Keys.KeyCode, Keys.Shift, Keys.Control, Keys.Alt
            };
        }
    }

    internal class HotkeyWrapper
    {
        public HotkeyWrapper(Keys keyData, FCTBAction action)
        {
            KeyEventArgs a = new KeyEventArgs(keyData);
            Ctrl = a.Control;
            Shift = a.Shift;
            Alt = a.Alt;

            Key = a.KeyCode;
            Action = action;
        }

        public Keys ToKeyData()
        {
            var res = Key;
            if (Ctrl) res |= Keys.Control;
            if (Alt) res |= Keys.Alt;
            if (Shift) res |= Keys.Shift;

            return res;
        }

        bool Ctrl;
        bool Shift;
        bool Alt;
        
        public string Modifiers
        {
            get
            {
                var res = "";
                if (Ctrl) res += "Ctrl + ";
                if (Shift) res += "Shift + ";
                if (Alt) res += "Alt + ";

                return res.Trim(' ', '+');
            }
            set
            {
                if (value == null)
                {
                    Ctrl = Alt = Shift = false;
                }
                else
                {
                    Ctrl = value.Contains("Ctrl");
                    Shift = value.Contains("Shift");
                    Alt = value.Contains("Alt");
                }
            }
        }

        public Keys Key { get; set; }
        public FCTBAction Action { get; set; }
    }
}
