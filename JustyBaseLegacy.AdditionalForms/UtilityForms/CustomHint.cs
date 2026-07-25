using AppBase.Common;
using AppBase.Common.Enums;
using AppBase.Common.Interfaces;
using DatabaseDataGridView.WinForms.Coloring;
using FastColoredTextBoxNS;

namespace JustyBaseLegacy.UI
{
    public partial class CustomHint : UserControl
    {
        private readonly string _objectName;
        private readonly string _db;
        private readonly string _tableOrView;

        private readonly FastColoredTextBox _fctb;
        private readonly IObjectExplorerNavigationHost _baseWindow;
        private readonly INetezzaDdlCodeProvider _ddlCodeProvider;
        private readonly TypeInDatabase _dbType;
        private readonly IColorTheme _colorTheme;

        public CustomHint(string tableName, FastColoredTextBox fctb,
            IObjectExplorerNavigationHost baseWindow,
            INetezzaDdlCodeProvider ddlCodeProvider, string db, string tableOrView, TypeInDatabase dbType,
            IColorTheme colorTheme)
        {
            InitializeComponent();
            _objectName = tableName;

            _fctb = fctb;
            _baseWindow = baseWindow;
            textBox1.Text = tableName;
            _ddlCodeProvider = ddlCodeProvider;
            _db = db;
            _tableOrView = tableOrView;
            _dbType = dbType;
            _colorTheme = colorTheme;

            // Apply theming if available
            ApplyTheming();

            // Configure button states based on database object type
            ConfigureButtonStates();

            // Add subtle shadow effect
            ApplyVisualEffects();
        }

        private void ApplyTheming()
        {
            try
            {
                // Try to get the color theme from the base window if available
                if (_colorTheme != null)
                {
                    // Apply theme colors to the control
                    this.BackColor = _colorTheme.MainBack;
                    headerPanel.BackColor = _colorTheme.TreeViewBackColor;
                    textBox1.BackColor = headerPanel.BackColor;
                    textBox1.ForeColor = _colorTheme.TreeViewForeColor;

                    // Apply theme to all buttons
                    ApplyButtonTheming(_colorTheme);
                }
            }
            catch
            {
                // Fallback to default styling if theming fails
            }
        }

        private void ApplyButtonTheming(IColorTheme theme)
        {
            var buttons = new[] { button1, button2, button3, button4, btDDL, jumpToBt, btGroom, btRecreate };

            foreach (var btn in buttons)
            {
                btn.BackColor = theme.ButtonBackColor;
                btn.ForeColor = theme.ButtonForeColor;

                // Adjust disabled button appearance
                if (!btn.Enabled)
                {
                    btn.ForeColor = Color.FromArgb(120, btn.ForeColor);
                }
            }
        }

        private void ConfigureButtonStates()
        {
            if (_dbType == TypeInDatabase.view)
            {
                btGroom.Enabled = false;
                btDDL.Enabled = false;
                btRecreate.Enabled = false;
            }
            else if (_dbType == TypeInDatabase.thisExternal)
            {
                btGroom.Enabled = false;
                btRecreate.Enabled = false;
            }
        }

        private void ApplyVisualEffects()
        {
            // Add a subtle border to the control
            this.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(200, 210, 220), 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, this.Width - 1, this.Height - 1);
                }
            };
        }

        private void doAction(string act, string comment = "--")
        {
            int l = _fctb.Selection.End.iLine;

            _fctb.Selection.Start = new Place(_fctb.Lines[l].Length, l);
            _fctb.Selection.End = new Place(_fctb.Lines[l].Length, l);
            _fctb.InsertText($"{Environment.NewLine}{comment}{act}{Environment.NewLine}");
            if (comment != "")
            {
                _fctb.Selection.Start = new Place(2, l + 1);
                _fctb.Selection.End = new Place(act.Length + 2, l + 1);
            }
            _fctb.Focus();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            switch (_dbType)
            {
                case TypeInDatabase.table:
                case TypeInDatabase.thisExternal:
                    doAction($"DROP TABLE {_objectName};");
                    return;
                case TypeInDatabase.view:
                    doAction($"DROP VIEW {_objectName};");
                    return;
                default:
                    return;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            switch (_dbType)
            {
                case TypeInDatabase.table:
                case TypeInDatabase.thisExternal:
                    doAction($"ALTER TABLE {_objectName} RENAME TO ABC;");
                    return;
                case TypeInDatabase.view:
                    doAction($"ALTER VIEW {_objectName} RENAME TO ABC;");
                    return;
                default:
                    return;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            doAction($"CREATE TABLE ABC AS (SELECT * FROM {_objectName} ) DISTRIBUTE ON RANDOM;");
        }

        private void button4_Click(object sender, EventArgs e)
        {
            doAction($"SELECT T1.* FROM {_objectName} T1;");
        }

        private void btGroom_Click(object sender, EventArgs e)
        {
            doAction($"GROOM TABLE {_objectName} VERSIONS; GROOM TABLE {_objectName} RECLAIM BACKUPSET NONE;");
        }

        private void jumpToBt_Click(object sender, EventArgs e) //go to
        {
            _fctb.ClearHints();

            switch (_dbType)
            {
                case TypeInDatabase.table:
                    _baseWindow.ExpandBaseToTable(_db, _tableOrView, "Tables", null);
                    return;
                case TypeInDatabase.thisExternal:
                    _baseWindow.ExpandBaseToTable(_db, _tableOrView, "External Tables", null);
                    return;
                case TypeInDatabase.view:
                    _baseWindow.ExpandBaseToTable(_db, _tableOrView, "Views", null);
                    return;
                default:
                    return;
            }
        }

        private async void btDDL_Click(object sender, EventArgs e)
        {
            string connectionName = TabConnectionCache.Default.TryGet(_fctb, out var cd) ? cd.ConnectionName : null;

            switch (_dbType)
            {
                case TypeInDatabase.table:
                    doAction(await _ddlCodeProvider.GetTableCodeByName(_db, _tableOrView, connectionName), comment: "");
                    return;
                case TypeInDatabase.thisExternal:
                    doAction(await _ddlCodeProvider.GetExternaTableCodeByName(_db, _tableOrView, connectionName), comment: "");
                    return;
                default:
                    return;
            }
        }

        private async void BtRecreate_Click(object sender, EventArgs e)
        {
            string connectionName = TabConnectionCache.Default.TryGet(_fctb, out var cd) ? cd.ConnectionName : null;

            switch (this._dbType)
            {
                case TypeInDatabase.table:
                    doAction(await _ddlCodeProvider.GetRecreateTableCodeByName(_db, _tableOrView, connectionName), comment: "");
                    return;
                default:
                    return;
            }
        }
    }
}
