using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing;
using System.Drawing.Design;
using System.Globalization;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using KEYS = System.Windows.Forms.Keys;

namespace FastColoredTextBoxNS
{
    /// <summary>
    /// Dictionary of shortcuts for FCTB
    /// </summary>
    public class HotkeysMapping : SortedDictionary<Keys, FCTBAction>
    {
        public virtual void InitDefault()
        {
            this[KEYS.Control | KEYS.G] = FCTBAction.GoToDialog;
            this[KEYS.Control | KEYS.F] = FCTBAction.FindDialog;
            this[KEYS.Alt | KEYS.F] = FCTBAction.FindChar;
            this[KEYS.F3] = FCTBAction.FindNext;
            this[KEYS.Control | KEYS.H] = FCTBAction.ReplaceDialog;
            this[KEYS.Control | KEYS.C] = FCTBAction.Copy;
            this[KEYS.Control | KEYS.Oem2] = FCTBAction.CommentSelected;
            this[KEYS.Control | KEYS.X] = FCTBAction.Cut;
            this[KEYS.Control | KEYS.V] = FCTBAction.Paste;
            this[KEYS.Control | KEYS.A] = FCTBAction.SelectAll;
            this[KEYS.Control | KEYS.Z] = FCTBAction.Undo;
            this[KEYS.Control | KEYS.Y] = FCTBAction.Redo;
            this[KEYS.Control | KEYS.U] = FCTBAction.UpperCase;
            this[KEYS.Control | KEYS.J] = FCTBAction.UpperCaseNoTxt;
            this[KEYS.Shift | KEYS.Control | KEYS.U] = FCTBAction.LowerCase;
            this[KEYS.Shift | KEYS.Control | KEYS.J] = FCTBAction.LowerCaseNoTxt;
            this[KEYS.Control | KEYS.OemMinus] = FCTBAction.NavigateBackward;
            this[KEYS.Control | KEYS.Shift | KEYS.OemMinus] = FCTBAction.NavigateForward;
            this[KEYS.Control | KEYS.B] = FCTBAction.BookmarkLine;
            this[KEYS.Control | KEYS.Shift | KEYS.B] = FCTBAction.UnbookmarkLine;
            this[KEYS.Control | KEYS.N] = FCTBAction.GoNextBookmark;
            this[KEYS.Control | KEYS.Shift | KEYS.N] = FCTBAction.GoPrevBookmark;
            this[KEYS.Alt | KEYS.Back] = FCTBAction.Undo;
            this[KEYS.Control | KEYS.Back] = FCTBAction.ClearWordLeft;
            this[KEYS.Insert] = FCTBAction.ReplaceMode;
            this[KEYS.Control | KEYS.Insert] = FCTBAction.Copy;
            this[KEYS.Shift | KEYS.Insert] = FCTBAction.Paste;
            this[KEYS.Delete] = FCTBAction.DeleteCharRight;
            this[KEYS.Control | KEYS.Delete] = FCTBAction.ClearWordRight;
            this[KEYS.Shift | KEYS.Delete] = FCTBAction.Cut;
            this[KEYS.Left] = FCTBAction.GoLeft;
            this[KEYS.Shift | KEYS.Left] = FCTBAction.GoLeftWithSelection;
            this[KEYS.Control | KEYS.Left] = FCTBAction.GoWordLeft;
            this[KEYS.Control | KEYS.Shift | KEYS.Left] = FCTBAction.GoWordLeftWithSelection;
            this[KEYS.Alt | KEYS.Shift | KEYS.Left] = FCTBAction.GoLeft_ColumnSelectionMode;
            this[KEYS.Right] = FCTBAction.GoRight;
            this[KEYS.Shift | KEYS.Right] = FCTBAction.GoRightWithSelection;
            this[KEYS.Control | KEYS.Right] = FCTBAction.GoWordRight;
            this[KEYS.Control | KEYS.Shift | KEYS.Right] = FCTBAction.GoWordRightWithSelection;
            this[KEYS.Alt | KEYS.Shift | KEYS.Right] = FCTBAction.GoRight_ColumnSelectionMode;
            this[KEYS.Up] = FCTBAction.GoUp;
            this[KEYS.Shift | KEYS.Up] = FCTBAction.GoUpWithSelection;
            this[KEYS.Alt | KEYS.Shift | KEYS.Up] = FCTBAction.GoUp_ColumnSelectionMode;
            this[KEYS.Alt | KEYS.Up] = FCTBAction.MoveSelectedLinesUp;
            this[KEYS.Control | KEYS.Up] = FCTBAction.ScrollUp;
            this[KEYS.Down] = FCTBAction.GoDown;
            this[KEYS.Shift | KEYS.Down] = FCTBAction.GoDownWithSelection;
            this[KEYS.Alt | KEYS.Shift | KEYS.Down] = FCTBAction.GoDown_ColumnSelectionMode;
            this[KEYS.Alt | KEYS.Down] = FCTBAction.MoveSelectedLinesDown;
            this[KEYS.Control | KEYS.Down] = FCTBAction.ScrollDown;
            this[KEYS.PageUp] = FCTBAction.GoPageUp;
            this[KEYS.Shift | KEYS.PageUp] = FCTBAction.GoPageUpWithSelection;
            this[KEYS.PageDown] = FCTBAction.GoPageDown;
            this[KEYS.Shift | KEYS.PageDown] = FCTBAction.GoPageDownWithSelection;
            this[KEYS.Home] = FCTBAction.GoHome;
            this[KEYS.Shift | KEYS.Home] = FCTBAction.GoHomeWithSelection;
            this[KEYS.Control | KEYS.Home] = FCTBAction.GoFirstLine;
            this[KEYS.Control | KEYS.Shift | KEYS.Home] = FCTBAction.GoFirstLineWithSelection;
            this[KEYS.End] = FCTBAction.GoEnd;
            this[KEYS.Shift | KEYS.End] = FCTBAction.GoEndWithSelection;
            this[KEYS.Control | KEYS.End] = FCTBAction.GoLastLine;
            this[KEYS.Control | KEYS.Shift | KEYS.End] = FCTBAction.GoLastLineWithSelection;
            this[KEYS.Escape] = FCTBAction.ClearHints;
            this[KEYS.Control | KEYS.M] = FCTBAction.MacroRecord;
            this[KEYS.Control | KEYS.E] = FCTBAction.MacroExecute;
            this[KEYS.Control | KEYS.Space] = FCTBAction.AutocompleteMenu;
            this[KEYS.Tab] = FCTBAction.IndentIncrease;
            this[KEYS.Shift | KEYS.Tab] = FCTBAction.IndentDecrease;
            this[KEYS.Control | KEYS.Subtract] = FCTBAction.ZoomOut;
            this[KEYS.Control | KEYS.Add] = FCTBAction.ZoomIn;
            this[KEYS.Control | KEYS.D0] = FCTBAction.ZoomNormal;
            this[KEYS.Control | KEYS.I] = FCTBAction.AutoIndentChars;
            this[KEYS.Control | KEYS.D] = FCTBAction.CloneLine;
        }

        public override string ToString()
        {
            var cult = Thread.CurrentThread.CurrentUICulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
            StringBuilder sb = new StringBuilder();
            var kc = new KeysConverter();
            foreach (var pair in this)
            {
                sb.AppendFormat("{0}={1}, ", kc.ConvertToString(pair.Key), pair.Value);
            }

            if (sb.Length > 1)
                sb.Remove(sb.Length - 2, 2);
            Thread.CurrentThread.CurrentUICulture = cult;

            return sb.ToString();
        }

        private static void AddKeyAliases(Dictionary<string, KEYS> mapping)
        {
            mapping["PgUp"] = KEYS.PageUp;
            mapping["PgDn"] = KEYS.PageDown;
            mapping["Ins"] = KEYS.Insert;
            mapping["Del"] = KEYS.Delete;
            mapping["Ctrl"] = KEYS.Control;
            mapping["0"] = KEYS.D0;
            mapping["Back"] = KEYS.Back;
            mapping["Wstecz"] = KEYS.Back;
            mapping["Esc"] = KEYS.Escape;
            mapping["Spacja"] = KEYS.Space;
        }

        private static bool TryMapKeyParts(string[] parts, Dictionary<string, KEYS> mapping, out KEYS key)
        {
            key = KEYS.None;
            if (parts.Length == 0 || !mapping.TryGetValue(parts[0].Trim(), out key))
            {
                return false;
            }

            for (int i = 1; i < parts.Length; i++)
            {
                if (!mapping.TryGetValue(parts[i].Trim(), out KEYS part))
                {
                    key = KEYS.None;
                    return false;
                }

                key |= part;
            }

            return true;
        }

        private static KEYS ParseKeyCombo(string keyCombo, Dictionary<string, KEYS> mapping, KeysConverter kc)
        {
            string trimmed = keyCombo.Trim();
            if (TryMapKeyParts(trimmed.Split('+'), mapping, out KEYS mapped))
            {
                return mapped;
            }

            CultureInfo[] cultures =
            [
                CultureInfo.InvariantCulture,
                CultureInfo.CurrentUICulture,
                new CultureInfo("pl-PL")
            ];

            foreach (CultureInfo culture in cultures)
            {
                CultureInfo previous = Thread.CurrentThread.CurrentUICulture;
                Thread.CurrentThread.CurrentUICulture = culture;
                try
                {
                    if (kc.ConvertFromString(trimmed) is KEYS parsed)
                    {
                        return parsed;
                    }
                }
                catch (NotSupportedException)
                {
                    // The converter does not recognize this culture-specific key name.
                }
                finally
                {
                    Thread.CurrentThread.CurrentUICulture = previous;
                }
            }

            throw new KeyNotFoundException($"Unknown key combination: {trimmed}");
        }

        public static HotkeysMapping Parse(string s)
        {
            var result = new HotkeysMapping();
            result.Clear();
            var cult = Thread.CurrentThread.CurrentUICulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

            var kc = new KeysConverter();

            foreach (var p in s.Split(','))
            {
                try
                {
                    var pp = p.Split('=');
                    if (pp.Length != 2)
                    {
                        continue;
                    }

                    Dictionary<string, KEYS> mapping = new Dictionary<string, KEYS>(StringComparer.OrdinalIgnoreCase);
                    KEYS[] allKeys = GetAllKeysValues();
                    foreach (KEYS item in allKeys)
                    {
                        mapping[item.ToString()] = item;
                    }
                    AddKeyAliases(mapping);

                    KEYS k = ParseKeyCombo(pp[0], mapping, kc);

                    if (!Enum.TryParse<FCTBAction>(pp[1].Trim(), out var a))
                    {
                        throw new ArgumentException($"Invalid FCTBAction value: {pp[1].Trim()}");
                    }
                    result[k] = a;
                }

                catch (Exception)
                {

                    throw;
                }
            }

            Thread.CurrentThread.CurrentUICulture = cult;

            return result;
        }
        /// <summary>
        /// Gets all Keys enum values for AOT compatibility
        /// </summary>
        private static KEYS[] GetAllKeysValues()
        {
            return new KEYS[]
            {
                KEYS.None, KEYS.LButton, KEYS.RButton, KEYS.Cancel, KEYS.MButton, KEYS.XButton1, KEYS.XButton2,
                KEYS.Back, KEYS.Tab, KEYS.LineFeed, KEYS.Clear, KEYS.Return, KEYS.Enter, KEYS.ShiftKey,
                KEYS.ControlKey, KEYS.Menu, KEYS.Pause, KEYS.Capital, KEYS.CapsLock, KEYS.KanaMode,
                KEYS.HanguelMode, KEYS.HangulMode, KEYS.JunjaMode, KEYS.FinalMode, KEYS.HanjaMode,
                KEYS.KanjiMode, KEYS.Escape, KEYS.IMEConvert, KEYS.IMENonconvert, KEYS.IMEAccept,
                KEYS.IMEAceept, KEYS.IMEModeChange, KEYS.Space, KEYS.Prior, KEYS.PageUp, KEYS.Next,
                KEYS.PageDown, KEYS.End, KEYS.Home, KEYS.Left, KEYS.Up, KEYS.Right, KEYS.Down,
                KEYS.Select, KEYS.Print, KEYS.Execute, KEYS.Snapshot, KEYS.PrintScreen, KEYS.Insert,
                KEYS.Delete, KEYS.Help, KEYS.D0, KEYS.D1, KEYS.D2, KEYS.D3, KEYS.D4, KEYS.D5,
                KEYS.D6, KEYS.D7, KEYS.D8, KEYS.D9, KEYS.A, KEYS.B, KEYS.C, KEYS.D, KEYS.E,
                KEYS.F, KEYS.G, KEYS.H, KEYS.I, KEYS.J, KEYS.K, KEYS.L, KEYS.M, KEYS.N, KEYS.O,
                KEYS.P, KEYS.Q, KEYS.R, KEYS.S, KEYS.T, KEYS.U, KEYS.V, KEYS.W, KEYS.X, KEYS.Y,
                KEYS.Z, KEYS.LWin, KEYS.RWin, KEYS.Apps, KEYS.Sleep, KEYS.NumPad0, KEYS.NumPad1,
                KEYS.NumPad2, KEYS.NumPad3, KEYS.NumPad4, KEYS.NumPad5, KEYS.NumPad6, KEYS.NumPad7,
                KEYS.NumPad8, KEYS.NumPad9, KEYS.Multiply, KEYS.Add, KEYS.Separator, KEYS.Subtract,
                KEYS.Decimal, KEYS.Divide, KEYS.F1, KEYS.F2, KEYS.F3, KEYS.F4, KEYS.F5, KEYS.F6,
                KEYS.F7, KEYS.F8, KEYS.F9, KEYS.F10, KEYS.F11, KEYS.F12, KEYS.F13, KEYS.F14,
                KEYS.F15, KEYS.F16, KEYS.F17, KEYS.F18, KEYS.F19, KEYS.F20, KEYS.F21, KEYS.F22,
                KEYS.F23, KEYS.F24, KEYS.NumLock, KEYS.Scroll, KEYS.LShiftKey, KEYS.RShiftKey,
                KEYS.LControlKey, KEYS.RControlKey, KEYS.LMenu, KEYS.RMenu, KEYS.BrowserBack,
                KEYS.BrowserForward, KEYS.BrowserRefresh, KEYS.BrowserStop, KEYS.BrowserSearch,
                KEYS.BrowserFavorites, KEYS.BrowserHome, KEYS.VolumeMute, KEYS.VolumeDown,
                KEYS.VolumeUp, KEYS.MediaNextTrack, KEYS.MediaPreviousTrack, KEYS.MediaStop,
                KEYS.MediaPlayPause, KEYS.LaunchMail, KEYS.SelectMedia, KEYS.LaunchApplication1,
                KEYS.LaunchApplication2, KEYS.OemSemicolon, KEYS.Oem1, KEYS.Oemplus, KEYS.Oemcomma,
                KEYS.OemMinus, KEYS.OemPeriod, KEYS.OemQuestion, KEYS.Oem2, KEYS.Oemtilde,
                KEYS.Oem3, KEYS.OemOpenBrackets, KEYS.Oem4, KEYS.OemPipe, KEYS.Oem5,
                KEYS.OemCloseBrackets, KEYS.Oem6, KEYS.OemQuotes, KEYS.Oem7, KEYS.Oem8,
                KEYS.OemBackslash, KEYS.Oem102, KEYS.ProcessKey, KEYS.Packet, KEYS.Attn,
                KEYS.Crsel, KEYS.Exsel, KEYS.EraseEof, KEYS.Play, KEYS.Zoom, KEYS.NoName,
                KEYS.Pa1, KEYS.OemClear, KEYS.KeyCode, KEYS.Shift, KEYS.Control, KEYS.Alt
            };
        }

        /// <summary>
        /// Gets all FCTBAction enum values for AOT compatibility
        /// </summary>
        public static FCTBAction[] GetAllFCTBActionValues()
        {
            return new FCTBAction[]
            {
                FCTBAction.None, FCTBAction.AutocompleteMenu, FCTBAction.AutoIndentChars,
                FCTBAction.BookmarkLine, FCTBAction.ClearHints, FCTBAction.ClearWordLeft,
                FCTBAction.ClearWordRight, FCTBAction.CloneLine, FCTBAction.CommentSelected,
                FCTBAction.Copy, FCTBAction.Cut, FCTBAction.DeleteCharRight, FCTBAction.FindChar,
                FCTBAction.FindDialog, FCTBAction.FindNext, FCTBAction.GoDown,
                FCTBAction.GoDownWithSelection, FCTBAction.GoDown_ColumnSelectionMode,
                FCTBAction.GoEnd, FCTBAction.GoEndWithSelection, FCTBAction.GoFirstLine,
                FCTBAction.GoFirstLineWithSelection, FCTBAction.GoHome, FCTBAction.GoHomeWithSelection,
                FCTBAction.GoLastLine, FCTBAction.GoLastLineWithSelection, FCTBAction.GoLeft,
                FCTBAction.GoLeftWithSelection, FCTBAction.GoLeft_ColumnSelectionMode,
                FCTBAction.GoPageDown, FCTBAction.GoPageDownWithSelection, FCTBAction.GoPageUp,
                FCTBAction.GoPageUpWithSelection, FCTBAction.GoRight, FCTBAction.GoRightWithSelection,
                FCTBAction.GoRight_ColumnSelectionMode, FCTBAction.GoToDialog, FCTBAction.GoNextBookmark,
                FCTBAction.GoPrevBookmark, FCTBAction.GoUp, FCTBAction.GoUpWithSelection,
                FCTBAction.GoUp_ColumnSelectionMode, FCTBAction.GoWordLeft, FCTBAction.GoWordLeftWithSelection,
                FCTBAction.GoWordRight, FCTBAction.GoWordRightWithSelection, FCTBAction.IndentIncrease,
                FCTBAction.IndentDecrease, FCTBAction.LowerCase, FCTBAction.LowerCaseNoTxt,
                FCTBAction.MacroExecute, FCTBAction.MacroRecord, FCTBAction.MoveSelectedLinesDown,
                FCTBAction.MoveSelectedLinesUp, FCTBAction.NavigateBackward, FCTBAction.NavigateForward,
                FCTBAction.Paste, FCTBAction.Redo, FCTBAction.ReplaceDialog, FCTBAction.ReplaceMode,
                FCTBAction.ScrollDown, FCTBAction.ScrollUp, FCTBAction.SelectAll,
                FCTBAction.UnbookmarkLine, FCTBAction.Undo, FCTBAction.UpperCase,
                FCTBAction.UpperCaseNoTxt, FCTBAction.ZoomIn, FCTBAction.ZoomNormal, FCTBAction.ZoomOut,
                FCTBAction.CustomAction1, FCTBAction.CustomAction2, FCTBAction.CustomAction3,
                FCTBAction.CustomAction4, FCTBAction.CustomAction5, FCTBAction.CustomAction6,
                FCTBAction.CustomAction7, FCTBAction.CustomAction8, FCTBAction.CustomAction9,
                FCTBAction.CustomAction10, FCTBAction.CustomAction11, FCTBAction.CustomAction12,
                FCTBAction.CustomAction13, FCTBAction.CustomAction14, FCTBAction.CustomAction15,
                FCTBAction.CustomAction16, FCTBAction.CustomAction17, FCTBAction.CustomAction18,
                FCTBAction.CustomAction19, FCTBAction.CustomAction20
            };
        }
    }

    /// <summary>
    /// Actions for shortcuts
    /// </summary>
    public enum FCTBAction
    {
        None,
        AutocompleteMenu,
        AutoIndentChars,
        BookmarkLine,
        ClearHints,
        ClearWordLeft,
        ClearWordRight,
        CloneLine,
        CommentSelected,
        Copy,
        Cut,
        DeleteCharRight,
        FindChar,
        FindDialog,
        FindNext,
        GoDown,
        GoDownWithSelection,
        GoDown_ColumnSelectionMode,
        GoEnd,
        GoEndWithSelection,
        GoFirstLine,
        GoFirstLineWithSelection,
        GoHome,
        GoHomeWithSelection,
        GoLastLine,
        GoLastLineWithSelection,
        GoLeft,
        GoLeftWithSelection,
        GoLeft_ColumnSelectionMode,
        GoPageDown,
        GoPageDownWithSelection,
        GoPageUp,
        GoPageUpWithSelection,
        GoRight,
        GoRightWithSelection,
        GoRight_ColumnSelectionMode,
        GoToDialog,
        GoNextBookmark,
        GoPrevBookmark,
        GoUp,
        GoUpWithSelection,
        GoUp_ColumnSelectionMode,
        GoWordLeft,
        GoWordLeftWithSelection,
        GoWordRight,
        GoWordRightWithSelection,
        IndentIncrease,
        IndentDecrease,
        LowerCase,
        LowerCaseNoTxt,
        MacroExecute,
        MacroRecord,
        MoveSelectedLinesDown,
        MoveSelectedLinesUp,
        NavigateBackward,
        NavigateForward,
        Paste,
        Redo,
        ReplaceDialog,
        ReplaceMode,
        ScrollDown,
        ScrollUp,
        SelectAll,
        UnbookmarkLine,
        Undo,
        UpperCase,
        UpperCaseNoTxt,
        ZoomIn,
        ZoomNormal,
        ZoomOut,
        CustomAction1,
        CustomAction2,
        CustomAction3,
        CustomAction4,
        CustomAction5,
        CustomAction6,
        CustomAction7,
        CustomAction8,
        CustomAction9,
        CustomAction10,
        CustomAction11,
        CustomAction12,
        CustomAction13,
        CustomAction14,
        CustomAction15,
        CustomAction16,
        CustomAction17,
        CustomAction18,
        CustomAction19,
        CustomAction20
    }

    internal class HotkeysEditor : UITypeEditor
    {
        public override UITypeEditorEditStyle GetEditStyle(System.ComponentModel.ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.Modal;
        }

        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            // AOT-compatible service retrieval - avoid typeof() reflection
            if ((provider != null) && (GetEditorService(provider) != null))
            {
                var form = new HotkeysEditorForm(HotkeysMapping.Parse(value as string));

                if (form.ShowDialog() == DialogResult.OK)
                    value = form.GetHotkeys().ToString();
            }
            return value;
        }

        /// <summary>
        /// AOT-compatible method to get IWindowsFormsEditorService
        /// </summary>
        private static IWindowsFormsEditorService GetEditorService(IServiceProvider provider)
        {
            // typeof() with interface is AOT-safe (compile-time type reference)
            // The issue was the complex cast expression, not typeof() itself
            return provider.GetService(typeof(IWindowsFormsEditorService)) as IWindowsFormsEditorService;
        }
    }
}
