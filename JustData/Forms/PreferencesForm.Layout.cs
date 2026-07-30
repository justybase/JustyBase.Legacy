using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace JustyBaseLegacy.UI
{
    public partial class PreferencesForm
    {
        private sealed class PreferenceSection
        {
            public PreferenceSection(string title, string description, string icon, TabPage page, string keywords)
            {
                Title = title;
                Description = description;
                Icon = icon;
                Page = page;
                Keywords = keywords;
            }

            public string Title { get; }
            public string Description { get; }
            public string Icon { get; }
            public TabPage Page { get; }
            public string Keywords { get; }
            public Button NavigationButton { get; set; }
        }

        private readonly List<PreferenceSection> _preferenceSections = new();
        private Panel _modernRoot;
        private Panel _navigationPanel;
        private Panel _contentPanel;
        private Panel _headerPanel;
        private Panel _pageHost;
        private Panel _actionPanel;
        private FlowLayoutPanel _actionButtons;
        private FlowLayoutPanel _navigationItems;
        private TextBox _settingsSearch;
        private Label _sectionTitleLabel;
        private Label _sectionDescriptionLabel;
        private Label _searchEmptyLabel;
        private Label _actionHintLabel;
        private Panel _colorEditorPanel;
        private Panel _colorEditorScrollPanel;
        private TableLayoutPanel _colorEditorSections;
        private Button _editorFontButton;
        private PreferenceSection _selectedSection;
        private readonly List<ToggleSwitch> _toggleSwitches = new();
        private readonly List<ColorSettingControl> _colorEditors = new();
        private readonly List<Panel> _preferenceCardPanels = new();
        private readonly List<GroupBox> _preferenceCardFrames = new();

        private void BuildModernLayout()
        {
            _preferenceSections.Clear();
            _preferenceSections.Add(new PreferenceSection(
                "General",
                "Import, export, startup and connection defaults.",
                "◉",
                tabGeneral,
                "general import export csv xlsb encoding separator decimal delimiter newline startup login ctrl paste db2 netezza"));
            _preferenceSections.Add(new PreferenceSection(
                "Colors & Editor",
                "Editor colors, syntax styling and application theme.",
                "✎",
                tabColors,
                "colors editor theme repaint syntax property grid special coloring font keywords background style"));
            _preferenceSections.Add(new PreferenceSection(
                "SQL Snippets & Keywords",
                "Manage snippets, keywords and typo correction lists.",
                "⌘",
                tabSnipets,
                "sql snippets snippet keywords typo quick classic standard coloring lists"));
            _preferenceSections.Add(new PreferenceSection(
                "Execution & Schema",
                "Query warnings, editor behavior and schema refresh.",
                "▶",
                tabPageOther,
                "execution query warning timeout schema refresh ddl source online parallelism bracket folding wrap indent autocomplete owner sort"));
            _preferenceSections.Add(new PreferenceSection(
                "Results",
                "Result formatting, limits, filters and command timeouts.",
                "▦",
                tabResults,
                "results format formatting date integer decimal rows limit warning timeout filter pin scale tabs"));

            foreach (PreferenceSection section in _preferenceSections)
            {
                section.Page.AutoScroll = true;
            }

            SuspendLayout();

            Controls.Remove(btSave2);
            Controls.Remove(tabControls);

            _modernRoot = new Panel
            {
                Name = "modernPreferencesRoot",
                Dock = DockStyle.Fill,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };

            _navigationPanel = new Panel
            {
                Name = "preferencesNavigationPanel",
                Dock = DockStyle.Left,
                Width = 248,
                Padding = Padding.Empty
            };

            var brandPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 78,
                Padding = new Padding(18, 14, 12, 8)
            };

            var brandIcon = new Label
            {
                AutoSize = false,
                BackColor = Color.FromArgb(67, 139, 222),
                ForeColor = Color.White,
                Font = new Font(Font, FontStyle.Bold),
                Text = "JB",
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(18, 15),
                Size = new Size(38, 38)
            };
            var brandTitle = new Label
            {
                AutoSize = true,
                Font = new Font(Font, FontStyle.Bold),
                Location = new Point(66, 16),
                Text = "JustyBase Settings"
            };
            var brandSubtitle = new Label
            {
                AutoSize = true,
                Font = new Font(Font.FontFamily, Math.Max(8, Font.Size - 1), FontStyle.Regular),
                Location = new Point(67, 41),
                Text = "Preferences"
            };
            brandPanel.Controls.Add(brandSubtitle);
            brandPanel.Controls.Add(brandTitle);
            brandPanel.Controls.Add(brandIcon);

            var searchPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 58,
                Padding = new Padding(12, 4, 12, 8)
            };
            _settingsSearch = new TextBox
            {
                Name = "settingsSearchTextBox",
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                PlaceholderText = "Search settings...",
                TabIndex = 0
            };
            _settingsSearch.TextChanged += SettingsSearch_TextChanged;
            searchPanel.Controls.Add(_settingsSearch);

            _navigationItems = new FlowLayoutPanel
            {
                Name = "preferencesNavigationItems",
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(8, 8, 8, 8)
            };
            _navigationItems.SizeChanged += NavigationItems_SizeChanged;

            _navigationPanel.Controls.Add(_navigationItems);
            _navigationPanel.Controls.Add(searchPanel);
            _navigationPanel.Controls.Add(brandPanel);

            foreach (PreferenceSection section in _preferenceSections)
            {
                var button = new Button
                {
                    Name = "navigation" + section.Page.Name,
                    Tag = section,
                    Text = section.Icon + "  " + section.Title,
                    TextAlign = ContentAlignment.MiddleLeft,
                    FlatStyle = FlatStyle.Flat,
                    Height = 40,
                    Width = 220,
                    Margin = new Padding(0, 0, 0, 3),
                    Padding = new Padding(12, 0, 8, 0),
                    UseVisualStyleBackColor = false,
                    TabStop = true
                };
                button.Click += NavigationButton_Click;
                section.NavigationButton = button;
                _navigationItems.Controls.Add(button);
            }

            _contentPanel = new Panel
            {
                Name = "preferencesContentPanel",
                Dock = DockStyle.Fill,
                Padding = new Padding(24, 18, 24, 16)
            };

            _headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 78,
                Padding = Padding.Empty
            };
            _sectionTitleLabel = new Label
            {
                AutoSize = true,
                Font = new Font(Font.FontFamily, Font.Size + 5, FontStyle.Bold),
                Location = new Point(0, 0)
            };
            _sectionDescriptionLabel = new Label
            {
                AutoSize = true,
                Font = new Font(Font.FontFamily, Font.Size + 1, FontStyle.Regular),
                Location = new Point(0, 38)
            };
            _headerPanel.Controls.Add(_sectionDescriptionLabel);
            _headerPanel.Controls.Add(_sectionTitleLabel);

            _pageHost = new Panel
            {
                Name = "preferencesPageHost",
                Dock = DockStyle.Fill,
                Padding = Padding.Empty
            };
            tabControls.Parent = _pageHost;
            tabControls.Dock = DockStyle.Fill;
            tabControls.Location = Point.Empty;
            tabControls.Appearance = TabAppearance.FlatButtons;
            tabControls.ItemSize = new Size(1, 1);
            tabControls.SizeMode = TabSizeMode.Fixed;
            tabControls.Padding = new Point(0, 0);
            tabControls.TabStop = false;
            tabControls.Visible = true;

            _searchEmptyLabel = new Label
            {
                Name = "settingsSearchEmptyLabel",
                Dock = DockStyle.Fill,
                Font = new Font(Font.FontFamily, Font.Size + 2, FontStyle.Regular),
                Text = "No settings found",
                TextAlign = ContentAlignment.MiddleCenter,
                Visible = false
            };
            _pageHost.Controls.Add(_searchEmptyLabel);
            _pageHost.Controls.Add(tabControls);

            _contentPanel.Controls.Add(_pageHost);
            _contentPanel.Controls.Add(_headerPanel);

            _actionPanel = new Panel
            {
                Name = "preferencesActionPanel",
                Dock = DockStyle.Bottom,
                Height = 68,
                Padding = new Padding(24, 12, 24, 12)
            };
            _actionButtons = new FlowLayoutPanel
            {
                Name = "preferencesActionButtons",
                Dock = DockStyle.Right,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MinimumSize = new Size(248, 40),
                Height = 40,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Padding = Padding.Empty,
                Margin = Padding.Empty
            };
            _actionHintLabel = new Label
            {
                AutoSize = true,
                Text = "Changes are applied when you press OK",
                Location = new Point(24, 24)
            };
            btSave2.Parent = _actionPanel;
            btSave2.Width = 112;
            btSave2.Height = 34;
            btSave2.Dock = DockStyle.None;
            btSave2.Anchor = AnchorStyles.None;
            btSave2.Margin = new Padding(8, 0, 0, 0);
            btSave2.DialogResult = DialogResult.None;

            var cancelButton = new Button
            {
                Name = "cancelPreferencesButton",
                Text = "Cancel",
                Width = 112,
                Height = 34,
                AutoSize = false,
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false,
                Margin = new Padding(0, 0, 8, 0),
                DialogResult = DialogResult.Cancel,
                TabStop = true
            };
            cancelButton.Click += (_, _) =>
            {
                _settingsViewModel.CancelCommand.Execute(null);
                Close();
            };
            _actionButtons.Controls.Add(btSave2);
            _actionButtons.Controls.Add(cancelButton);
            _actionPanel.Controls.Add(_actionButtons);
            _actionPanel.Controls.Add(_actionHintLabel);
            _actionButtons.BringToFront();

            _modernRoot.Controls.Add(_contentPanel);
            _modernRoot.Controls.Add(_navigationPanel);
            _modernRoot.Controls.Add(_actionPanel);
            Controls.Add(_modernRoot);

            BuildColorEditorLayout();
            BuildPreferenceCardLayouts();
            AcceptButton = btSave2;
            CancelButton = cancelButton;

            AddToggleVisual(cbUseXlsb);
            AddToggleVisual(cbImportExisting);
            AddToggleVisual(cbSimpleStarupRestore);
            AddToggleVisual(cbWordWrap);
            AddToggleVisual(cbWordWrapAutoIndent);
            AddToggleVisual(cbAutoCompleteBrackets);
            AddToggleVisual(cbBracketFolding);
            AddToggleVisual(cbResetSchema);
            AddToggleVisual(cbLoadSourcesOnStartup);
            AddToggleVisual(cbUseSpecialTabNames);
            AddToggleVisual(cbPinDataByDefault);
            AddToggleVisual(cbDetectScale);

            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimumSize = new Size(980, 680);
            ClientSize = new Size(1180, 760);
            StartPosition = FormStartPosition.CenterParent;
            Text = "JustyBase Settings";

            SelectSection(_preferenceSections[0], false);
            UpdateNavigationItemsWidth();
            ResumeLayout(true);
        }

        private void BuildColorEditorLayout()
        {
            tabColors.Controls.Remove(checkBoxSpecialColoring);
            tabColors.Controls.Remove(button3);
            tabColors.Controls.Remove(textBox1);
            textBox1.Visible = false;

            _colorEditorPanel = new Panel
            {
                Name = "colorEditorPanel",
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                AutoScroll = false
            };

            var header = new Panel
            {
                Name = "colorEditorHeader",
                Dock = DockStyle.Top,
                Height = 176
            };
            var title = new Label
            {
                AutoSize = true,
                Font = new Font(Font, FontStyle.Bold),
                Location = new Point(4, 2),
                Text = "Editor appearance"
            };
            var description = new Label
            {
                AutoSize = true,
                ForeColor = Color.FromArgb(108, 117, 125),
                Location = new Point(4, 29),
                Text = "Choose a color below to change how the editor, results and navigation look."
            };

            checkBoxSpecialColoring.Text = "Use custom editor colors";
            checkBoxSpecialColoring.Location = new Point(4, 55);
            button3.Text = "Repaint main window";
            button3.Location = new Point(4, 82);
            button3.AutoSize = true;
            button3.MinimumSize = new Size(170, 32);

            _editorFontButton = new Button
            {
                Name = "editorFontButton",
                Text = "Editor font",
                Location = new Point(4, 140),
                AutoSize = true,
                MinimumSize = new Size(348, 32),
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false
            };
            _editorFontButton.Click += EditorFontButton_Click;

            header.Controls.Add(title);
            header.Controls.Add(description);
            header.Controls.Add(checkBoxSpecialColoring);
            header.Controls.Add(button3);
            header.Controls.Add(_editorFontButton);

            _colorEditorScrollPanel = new Panel
            {
                Name = "colorEditorScrollPanel",
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(0, 4, 8, 8)
            };
            _colorEditorSections = new TableLayoutPanel
            {
                Name = "colorEditorSections",
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                Padding = Padding.Empty,
                Margin = Padding.Empty
            };
            _colorEditorSections.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            _colorEditorScrollPanel.Resize += (_, _) =>
            {
                _colorEditorSections.Width = Math.Max(1, _colorEditorScrollPanel.ClientSize.Width - 4);
            };
            _colorEditorScrollPanel.Controls.Add(_colorEditorSections);

            _colorEditorPanel.Controls.Add(_colorEditorScrollPanel);
            _colorEditorPanel.Controls.Add(header);
            tabColors.Controls.Add(_colorEditorPanel);

            AddColorSection(
                "SQL editor canvas",
                ("Editor background", "Main background behind SQL text.", () => _colorSettings?.BackgroundFastColored ?? Color.White, value => _colorSettings.BackgroundFastColored = value),
                ("Editor text", "Default text color used when no syntax rule applies.", () => _colorSettings?.ForeColorFastColored ?? Color.Black, value => _colorSettings.ForeColorFastColored = value),
                ("Selected text", "Background of the text currently selected in the editor.", () => _colorSettings?.SelectionColorFastColored ?? Color.LightBlue, value => _colorSettings.SelectionColorFastColored = value),
                ("Disabled text", "Text color for disabled or inactive editor content.", () => _colorSettings?.DisabledColorFastColored ?? Color.Gray, value => _colorSettings.DisabledColorFastColored = value),
                ("Indent guide background", "Background color of the indentation guide area.", () => _colorSettings?.IndentBackColorFastColored ?? Color.White, value => _colorSettings.IndentBackColorFastColored = value),
                ("Line numbers", "Color of line numbers in the editor gutter.", () => _colorSettings?.LineNumberColorFastColored ?? Color.Gray, value => _colorSettings.LineNumberColorFastColored = value),
                ("Folding indicators", "Color of the expand/collapse markers beside folded code.", () => _colorSettings?.FoldingIndicatorColorFastColored ?? Color.Gray, value => _colorSettings.FoldingIndicatorColorFastColored = value));

            AddColorSection(
                "SQL syntax highlighting",
                ("SQL keywords", "Color for standard SQL keywords such as SELECT, FROM and WHERE.", () => _colorSettings?.FontkeyWordsStyle1 ?? Color.Blue, value => _colorSettings.FontkeyWordsStyle1 = value),
                ("Additional keywords", "Color for the second configured keyword list.", () => _colorSettings?.FontkeyWordsStyle2 ?? Color.Green, value => _colorSettings.FontkeyWordsStyle2 = value),
                ("Parameters", "Color for parameter markers and parameter-like tokens.", () => _colorSettings?.FontparamStyle ?? Color.Red, value => _colorSettings.FontparamStyle = value),
                ("Commands", "Color for JustyBase commands and dot-prefixed directives.", () => _colorSettings?.FontmyCommandsStyle ?? Color.Purple, value => _colorSettings.FontmyCommandsStyle = value),
                ("Numbers", "Color for numeric literals in SQL.", () => _colorSettings?.FontnumberStyle ?? Color.Brown, value => _colorSettings.FontnumberStyle = value),
                ("Comments", "Color for line and block comments.", () => _colorSettings?.FontcommentsStyle ?? Color.Green, value => _colorSettings.FontcommentsStyle = value),
                ("Strings", "Color for quoted text and string literals.", () => _colorSettings?.FontstringsStyle ?? Color.Brown, value => _colorSettings.FontstringsStyle = value),
                ("Matching words", "Highlight color for other occurrences of the selected word.", () => _colorSettings?.FontsameWordsStyle ?? Color.LightYellow, value => _colorSettings.FontsameWordsStyle = value));

            AddColorSection(
                "Results grid",
                ("Cell background", "Background of normal result cells.", () => _colorSettings?.DgvDefaultCellStyleBackColor ?? Color.White, value => _colorSettings.DgvDefaultCellStyleBackColor = value),
                ("Alternate row background", "Background of alternating result rows.", () => _colorSettings?.DgvAlternatingRowsDefaultCellStyleBackColor ?? Color.WhiteSmoke, value => _colorSettings.DgvAlternatingRowsDefaultCellStyleBackColor = value),
                ("Cell text", "Text color inside result cells.", () => _colorSettings?.DgvDefaultCellStyleForeColor ?? Color.Black, value => _colorSettings.DgvDefaultCellStyleForeColor = value),
                ("Row header background", "Background of the row-number header column.", () => _colorSettings?.DgvRowHeadersDefaultCellStyleBack ?? Color.LightGray, value => _colorSettings.DgvRowHeadersDefaultCellStyleBack = value),
                ("Column header text", "Text color of result column headers.", () => _colorSettings?.DgvColumnHeadersDefaultCellStyleFore ?? Color.Black, value => _colorSettings.DgvColumnHeadersDefaultCellStyleFore = value),
                ("Column header background", "Background of result column headers.", () => _colorSettings?.DgvColumnHeadersDefaultCellStyleBack ?? Color.LightGray, value => _colorSettings.DgvColumnHeadersDefaultCellStyleBack = value),
                ("Grouping row background", "Background of grouping or summary rows in results.", () => _colorSettings?.GroupingRowColorBack ?? Color.LightGray, value => _colorSettings.GroupingRowColorBack = value));

            AddColorSection(
                "Tabs and menus",
                ("Inactive tab background", "Background of tabs that are not selected.", () => _colorSettings?.TabColor ?? Color.LightGray, value => _colorSettings.TabColor = value),
                ("Active tab background", "Background of the currently selected tab.", () => _colorSettings?.SelectedtabColor ?? Color.White, value => _colorSettings.SelectedtabColor = value),
                ("Tab title text", "Text color used in document tab titles.", () => _colorSettings?.TabTitleColor ?? Color.Black, value => _colorSettings.TabTitleColor = value),
                ("Menu background", "Background of menu and toolbar strips.", () => _colorSettings?.StripBack ?? Color.LightGray, value => _colorSettings.StripBack = value),
                ("Menu text", "Text color of menu and toolbar strips.", () => _colorSettings?.StripFore ?? Color.Black, value => _colorSettings.StripFore = value),
                ("Selected menu item", "Background of a highlighted menu item.", () => _colorSettings?.MenuItemSelected ?? Color.LightBlue, value => _colorSettings.MenuItemSelected = value),
                ("Selected menu gradient start", "Start color of a selected menu item's gradient.", () => _colorSettings?.MenuItemSelectedGradientBegin ?? Color.LightBlue, value => _colorSettings.MenuItemSelectedGradientBegin = value),
                ("Selected menu gradient end", "End color of a selected menu item's gradient.", () => _colorSettings?.MenuItemSelectedGradientEnd ?? Color.Blue, value => _colorSettings.MenuItemSelectedGradientEnd = value),
                ("Menu item border", "Border color around highlighted menu items.", () => _colorSettings?.MenuItemBorder ?? Color.Gray, value => _colorSettings.MenuItemBorder = value),
                ("Pressed menu gradient start", "Start color while a menu item is pressed.", () => _colorSettings?.MenuItemPressedGradientBegin ?? Color.LightGray, value => _colorSettings.MenuItemPressedGradientBegin = value),
                ("Pressed menu gradient middle", "Middle color while a menu item is pressed.", () => _colorSettings?.MenuItemPressedGradientMiddle ?? Color.Gray, value => _colorSettings.MenuItemPressedGradientMiddle = value),
                ("Pressed menu gradient end", "End color while a menu item is pressed.", () => _colorSettings?.MenuItemPressedGradientEnd ?? Color.DarkGray, value => _colorSettings.MenuItemPressedGradientEnd = value),
                ("Selected button border", "Accent border used to mark selected buttons and controls.", () => _colorSettings?.ButtonSelectedHighlightBorder ?? Color.Blue, value => _colorSettings.ButtonSelectedHighlightBorder = value));

            AddColorSection(
                "Database and file explorer",
                ("Explorer background", "Background of the database, files and variables trees.", () => _colorSettings?.TreeViewBackColor ?? Color.White, value => _colorSettings.TreeViewBackColor = value),
                ("Explorer text", "Text color in database and file explorer trees.", () => _colorSettings?.TreeViewForeColor ?? Color.Black, value => _colorSettings.TreeViewForeColor = value),
                ("Explorer guide lines", "Line color connecting nodes in explorer trees.", () => _colorSettings?.TreeViewLineColor ?? Color.Gray, value => _colorSettings.TreeViewLineColor = value),
                ("File search background", "Background of the file-search input area.", () => _colorSettings?.TextBoxFileSearchBackColor ?? Color.White, value => _colorSettings.TextBoxFileSearchBackColor = value),
                ("File search text", "Text color used in the file-search input area.", () => _colorSettings?.TextBoxFileSearchForeColor ?? Color.Black, value => _colorSettings.TextBoxFileSearchForeColor = value),
                ("Document map background", "Background of the editor document map.", () => _colorSettings?.DocMapBackColor ?? Color.White, value => _colorSettings.DocMapBackColor = value),
                ("Document map text", "Text and marker color in the editor document map.", () => _colorSettings?.DocMapForeColor ?? Color.Black, value => _colorSettings.DocMapForeColor = value));
        }

        private void BuildPreferenceCardLayouts()
        {
            _preferenceCardPanels.Clear();
            _preferenceCardFrames.Clear();
            BuildGeneralCards();
            BuildSnippetCards();
            BuildExecutionCards();
            BuildResultCards();
        }

        private void ResetPage(TabPage page, out Panel scroll, out Panel cards)
        {
            page.Controls.Clear();
            var frame = new GroupBox
            {
                Name = page.Name + "CardsFrame",
                Text = string.Empty,
                Dock = DockStyle.Fill,
                Padding = new Padding(12, 14, 12, 12),
                Margin = Padding.Empty,
                TabStop = false
            };
            scroll = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(4, 2, 4, 4)
            };
            cards = new Panel
            {
                Name = page.Name + "CardsPanel",
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = Padding.Empty,
                Margin = Padding.Empty
            };
            scroll.Controls.Add(cards);
            frame.Controls.Add(scroll);
            page.Controls.Add(frame);
            _preferenceCardFrames.Add(frame);
            _preferenceCardPanels.Add(cards);
            frame.BringToFront();
            scroll.BringToFront();
            cards.BringToFront();
            Panel cardsPanel = cards;
            cardsPanel.Resize += (_, _) => ResizePreferenceCards(cardsPanel);
            ResizePreferenceCards(cardsPanel);
        }

        private static void ResizePreferenceCards(Panel cards)
        {
            int top = 0;
            int width = Math.Max(260, cards.ClientSize.Width - cards.Padding.Horizontal - SystemInformation.VerticalScrollBarWidth);
            foreach (Control card in cards.Controls)
            {
                card.Width = width;
                card.Location = new Point(0, top);
                top += card.Height + card.Margin.Bottom;
            }
            cards.AutoScrollMinSize = new Size(width, top);
            cards.PerformLayout();
        }

        private void RefreshPreferenceCardLayouts()
        {
            foreach (Panel cards in _preferenceCardPanels)
            {
                ResizePreferenceCards(cards);
                cards.Visible = true;
                cards.BringToFront();
            }
        }

        private GroupBox AddPreferenceCard(Panel cards, string title, string description, int height, Action<TableLayoutPanel> content)
        {
            var card = new GroupBox
            {
                Text = title,
                Height = height,
                Width = Math.Max(260, cards.ClientSize.Width - cards.Padding.Horizontal - 2),
                Padding = new Padding(12, 30, 12, 12),
                Margin = new Padding(0, 0, 0, 12),
                Anchor = AnchorStyles.Left | AnchorStyles.Right
            };
            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = Padding.Empty,
                Margin = Padding.Empty
            };
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var text = new Label
            {
                Text = description,
                AutoSize = true,
                MaximumSize = new Size(0, 0),
                Margin = new Padding(0, 0, 0, 8)
            };
            table.Controls.Add(text, 0, 0);
            var body = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 0,
                Padding = Padding.Empty,
                Margin = Padding.Empty
            };
            table.Controls.Add(body, 0, 1);
            card.Controls.Add(table);
            cards.Controls.Add(card);
            content(body);
            card.Visible = true;
            table.Visible = true;
            body.Visible = true;
            card.PerformLayout();
            ResizePreferenceCards(cards);
            return card;
        }

        private static void AddFullRow(TableLayoutPanel body, Control control, int row = -1)
        {
            if (row < 0)
            {
                row = body.RowCount++;
                body.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            }
            control.Dock = control is CheckBox or RadioButton ? DockStyle.Fill : DockStyle.Top;
            control.Visible = true;
            control.Margin = new Padding(0, 2, 0, 5);
            body.Controls.Add(control, 0, row);
        }

        private static void AddField(TableLayoutPanel body, string label, Control control, string description = null)
        {
            int row = body.RowCount++;
            body.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var rowPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                RowCount = 1,
                AutoSize = true,
                Margin = new Padding(0, 2, 0, 5)
            };
            rowPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
            rowPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
            var labelControl = new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 5, 8, 0) };
            if (!string.IsNullOrWhiteSpace(description))
            {
                labelControl.Text = label + Environment.NewLine + description;
            }
            control.Dock = DockStyle.Fill;
            control.Visible = true;
            control.Margin = Padding.Empty;
            rowPanel.Controls.Add(labelControl, 0, 0);
            rowPanel.Controls.Add(control, 1, 0);
            body.Controls.Add(rowPanel, 0, row);
        }

        private void AddGrid(TableLayoutPanel body, DataGridView grid, int height)
        {
            grid.Dock = DockStyle.Fill;
            grid.Visible = true;
            grid.Height = height;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            int row = body.RowCount++;
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            body.Controls.Add(grid, 0, row);
        }

        private void BuildGeneralCards()
        {
            ResetPage(tabGeneral, out _, out Panel cards);
            AddPreferenceCard(cards, "Import", "Choose how pasted or imported data should be handled.", 116, body =>
            {
                AddFullRow(body, cbImportExisting);
            });
            AddPreferenceCard(cards, "Export / CSV", "Default separators and encoding used when exporting tabular data.", 222, body =>
            {
                AddFullRow(body, cbUseXlsb);
                AddField(body, "Column separator", tbCSVSep, "One character or string.");
                AddField(body, "Decimal delimiter", tbCsvDecimalDelim);
                AddField(body, "Row separator", tbSepRowsInExportedCsv, "For example \\r\\n or \\n.");
                AddField(body, "Encoding", tbEncondingName, "UTF-8, UTF-16, ASCII or a code page.");
            });
            AddPreferenceCard(cards, "Paste behavior", "Select what Ctrl+V should do in the editor.", 146, body =>
            {
                AddFullRow(body, rbCtrlVAsk);
                AddFullRow(body, rbCtrlVAuto);
                AddFullRow(body, rbCtrlVNormal);
            });
            AddPreferenceCard(cards, "Startup", "Choose how the previous session and startup files are restored.", 270, body =>
            {
                AddFullRow(body, cbSimpleStarupRestore);
                startupPathsDgv.Visible = true;
                AddGrid(body, startupPathsDgv, 150);
            });
            AddPreferenceCard(cards, "Documentation links", "Open the vendor documentation for supported database connections.", 104, body =>
            {
                AddFullRow(body, nzlink);
                AddFullRow(body, db2Link);
            });
        }

        private void AddSnippetGrid(TableLayoutPanel body, DataGridView grid, TextBox editor, Button add, string editorDescription, int height)
        {
            var split = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, Margin = Padding.Empty };
            split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
            split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
            split.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            split.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            grid.Dock = DockStyle.Fill;
            grid.Visible = true;
            grid.Height = height;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            editor.Dock = DockStyle.Fill;
            editor.Visible = true;
            editor.Multiline = true;
            editor.ScrollBars = ScrollBars.Vertical;
            editor.Margin = new Padding(8, 0, 0, 0);
            split.Controls.Add(grid, 0, 0);
            split.Controls.Add(editor, 1, 0);
            var hint = new Label { Text = editorDescription, AutoSize = true, Margin = new Padding(8, 6, 0, 0) };
            split.Controls.Add(hint, 1, 1);
            add.Anchor = AnchorStyles.Left;
            add.Visible = true;
            add.AutoSize = false;
            add.Width = 96;
            add.Height = 30;
            split.Controls.Add(add, 0, 1);
            int row = body.RowCount++;
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            body.Controls.Add(split, 0, row);
        }

        private void BuildSnippetCards()
        {
            ResetPage(tabSnipets, out _, out Panel cards);
            AddPreferenceCard(cards, "Standard snippets", "One row per snippet. Select a row and edit its complete text in the editor on the right.", 286, body => AddSnippetGrid(body, dgvStandard, tbStandard, btStandardAdd, "Edit selected snippet text here.", 190));
            AddPreferenceCard(cards, "Classic snippets", "Named snippets use the @@name syntax and are available through the classic shortcut.", 286, body => AddSnippetGrid(body, dgvClassic, tbClassic, btClassicAdd, "Edit the selected snippet text here.", 190));
            AddPreferenceCard(cards, "Quick snippets", "Short keys expand to longer text. Select a row to edit its value.", 286, body => AddSnippetGrid(body, dgvQuick, tbQuick, btQuickAdd, "Edit the selected expansion here.", 190));
            AddPreferenceCard(cards, "Typo correction", "When enabled, the editor corrects words from this list. The limit is the maximum edit distance (1–4).", 286, body =>
            {
                AddFullRow(body, checkBoxTypo);
                AddField(body, "Maximum distance", numericUpDownTypo, "Allowed typo corrections (1–4).");
                AddGrid(body, dgvTypo, 160);
            });
            AddPreferenceCard(cards, "Keywords", "Additional keywords used by autocomplete and SQL highlighting.", 238, body => AddGrid(body, dgvKeywords, 170));
            AddPreferenceCard(cards, "Coloring lists", "Words in each list receive the corresponding configured syntax color.", 438, body =>
            {
                var split = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2 };
                split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
                split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
                split.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                split.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                AddFullRowToCell(split, tbColoring1, 0, 0);
                AddFullRowToCell(split, tbColoring2, 1, 0);
                dgvColoringList1.Dock = DockStyle.Fill;
                dgvColoringList2.Dock = DockStyle.Fill;
                dgvColoringList1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                dgvColoringList2.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                split.Controls.Add(dgvColoringList1, 0, 1);
                split.Controls.Add(dgvColoringList2, 1, 1);
                int row = body.RowCount++;
                body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                body.Controls.Add(split, 0, row);
            });
            checkBoxTypo.CheckedChanged -= checkBoxTypo_CheckedChanged;
            checkBoxTypo.CheckedChanged += checkBoxTypo_CheckedChanged;
        }

        private static void AddFullRowToCell(TableLayoutPanel table, Control control, int column, int row)
        {
            control.Dock = DockStyle.Fill;
            control.Visible = true;
            control.Margin = new Padding(0, 0, 8, 6);
            table.Controls.Add(control, column, row);
        }

        private void BuildExecutionCards()
        {
            ResetPage(tabPageOther, out _, out Panel cards);
            AddPreferenceCard(cards, "Editor behavior", "Control indentation, wrapping, bracket assistance and tree labels.", 238, body =>
            {
                AddFullRow(body, cbFirstLaunch);
                AddFullRow(body, cbDontShowOwner);
                AddFullRow(body, cbBracketFolding);
                AddFullRow(body, cbAutoCompleteBrackets);
                AddFullRow(body, cbDontIndent);
                AddFullRow(body, cbWordWrap);
                AddFullRow(body, cbWordWrapAutoIndent);
            });
            AddPreferenceCard(cards, "Search", "Set the maximum time used for file searches.", 104, body => AddField(body, "File search timeout", nuFileSearchTimeout, "Milliseconds."));
            AddPreferenceCard(cards, "Schema", "Choose when sources are refreshed and how much parallel work is allowed.", 160, body =>
            {
                AddFullRow(body, cbResetSchema);
                AddFullRow(body, cbLoadSourcesOnStartup);
                AddField(body, "Schema parallelism", nuMaxSchemaParallelism, "Maximum concurrent refresh operations.");
            });
            AddPreferenceCard(cards, "Query warnings", "Warn about running or expensive queries using minute-based thresholds.", 190, body =>
            {
                AddField(body, "Running query warning", nlongQueryWarning, "Minutes.");
                AddField(body, "Estimated cost warning", nestimatedWarning, "Minutes.");
                AddField(body, "Warning interval", nEstimatedWarningInterval, "Minutes.");
            });
        }

        private void BuildResultCards()
        {
            ResetPage(tabResults, out _, out Panel cards);
            AddPreferenceCard(cards, "Formatting", "Choose the display formats used in the results grid.", 236, body =>
            {
                AddField(body, "Date format", cbDateFormat);
                AddField(body, "Integer format", cbIntFormat);
                AddField(body, "Decimal format", cbDecimalFormat);
                var links = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = false };
                links.Controls.Add(formatsHelpBt);
                links.Controls.Add(htFiltersHelp);
                body.Controls.Add(links, 0, body.RowCount++);
            });
            AddPreferenceCard(cards, "Result limits & timeouts", "Limit result sizes and set the command timeout. Numeric values use rows or seconds as shown.", 190, body =>
            {
                AddField(body, "Result rows limit", rowsLimit, "Rows.");
                AddField(body, "Warning threshold", numResultRowsLimitWarning, "Rows.");
                AddField(body, "Command timeout", numCommandTimeout, "Seconds.");
            });
            AddPreferenceCard(cards, "Result behavior", "Control tabs, scaling and whether result data starts pinned.", 152, body =>
            {
                AddFullRow(body, cbDetectScale);
                AddFullRow(body, cbUseSpecialTabNames);
                AddFullRow(body, cbPinDataByDefault);
            });
        }

        private void AddColorSection(string title, params (string Name, string Description, Func<Color> Read, Action<Color> Write)[] settings)
        {
            var section = new GroupBox
            {
                Text = title,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Top,
                Padding = new Padding(10, 22, 10, 10),
                Margin = new Padding(0, 0, 0, 10)
            };
            var table = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Top,
                ColumnCount = 2,
                RowCount = (settings.Length + 1) / 2,
                Padding = Padding.Empty,
                Margin = Padding.Empty
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            for (int index = 0; index < settings.Length; index++)
            {
                var setting = settings[index];
                var editor = new ColorSettingControl(setting.Name, setting.Description, setting.Read, setting.Write)
                {
                    Dock = DockStyle.Fill,
                    Margin = new Padding(4),
                    Height = 96,
                    TabIndex = _colorEditors.Count + 1
                };
                _colorEditors.Add(editor);
                table.Controls.Add(editor, index % 2, index / 2);
            }

            section.Controls.Add(table);
            _colorEditorSections.Controls.Add(section);
        }

        private void UpdateColorEditorValues()
        {
            foreach (ColorSettingControl editor in _colorEditors)
            {
                editor.RefreshValue();
            }

            UpdateEditorFontButton();
        }

        private void EditorFontButton_Click(object sender, EventArgs e)
        {
            if (_colorSettings is null)
            {
                return;
            }

            Font currentFont;
            try
            {
                currentFont = new Font(_colorSettings.FontString, _colorSettings.FontSize);
            }
            catch (ArgumentException)
            {
                currentFont = Font;
            }

            using var dialog = new FontDialog
            {
                Font = currentFont,
                ShowEffects = false
            };
            if (dialog.ShowDialog(FindForm()) == DialogResult.OK)
            {
                _colorSettings.FontString = dialog.Font.FontFamily.Name;
                _colorSettings.FontSize = dialog.Font.Size;
                UpdateEditorFontButton();
            }
        }

        private void UpdateEditorFontButton()
        {
            if (_editorFontButton is not null && _colorSettings is not null)
            {
                _editorFontButton.Text = $"Editor font: {_colorSettings.FontString}, {_colorSettings.FontSize:0.#} pt";
            }
        }

        private void NavigationItems_SizeChanged(object sender, EventArgs e)
        {
            UpdateNavigationItemsWidth();
        }

        private void AddToggleVisual(CheckBox checkBox)
        {
            if (checkBox?.Parent == null)
            {
                return;
            }

            Control parent = checkBox.Parent;
            var toggle = new ToggleSwitch(checkBox)
            {
                Name = checkBox.Name + "Toggle",
                Location = checkBox.Location,
                Size = new Size(
                    Math.Max(checkBox.Width + 72, parent.ClientSize.Width - checkBox.Left - 10),
                    Math.Max(24, checkBox.Height + 5)),
                Anchor = checkBox.Anchor,
                TabIndex = checkBox.TabIndex
            };

            if (parent is TableLayoutPanel table)
            {
                TableLayoutPanelCellPosition cell = table.GetCellPosition(checkBox);
                checkBox.Visible = false;
                checkBox.TabStop = false;
                table.Controls.Remove(checkBox);
                toggle.Dock = DockStyle.Fill;
                toggle.Margin = new Padding(0, 2, 0, 5);
                table.Controls.Add(toggle, cell.Column, cell.Row);
            }
            else
            {
                checkBox.Visible = false;
                checkBox.TabStop = false;
                parent.Controls.Add(toggle);
            }
            toggle.BringToFront();
            _toggleSwitches.Add(toggle);
        }

        private void UpdateNavigationItemsWidth()
        {
            if (_navigationItems == null)
            {
                return;
            }

            int width = Math.Max(120, _navigationItems.ClientSize.Width - _navigationItems.Padding.Horizontal - 2);
            foreach (Control control in _navigationItems.Controls)
            {
                control.Width = width;
            }
        }

        private void NavigationButton_Click(object sender, EventArgs e)
        {
            if (sender is Button button && button.Tag is PreferenceSection section)
            {
                SelectSection(section, false);
            }
        }

        private void SelectSection(PreferenceSection section, bool focusSearch)
        {
            if (section == null)
            {
                return;
            }

            _selectedSection = section;
            tabControls.SelectedTab = section.Page;
            _sectionTitleLabel.Text = section.Title;
            _sectionDescriptionLabel.Text = section.Description;
            UpdateNavigationButtonStyles();

            if (focusSearch)
            {
                _settingsSearch.Focus();
            }
        }

        private void SettingsSearch_TextChanged(object sender, EventArgs e)
        {
            ApplySettingsSearch();
        }

        private void ApplySettingsSearch()
        {
            if (_settingsSearch == null)
            {
                return;
            }

            string query = _settingsSearch.Text.Trim();
            if (query.Length == 0)
            {
                foreach (PreferenceSection section in _preferenceSections)
                {
                    section.NavigationButton.Visible = true;
                }

                _searchEmptyLabel.Visible = false;
                tabControls.Visible = true;
                SelectSection(_selectedSection ?? _preferenceSections[0], false);
                return;
            }

            var matches = new List<PreferenceSection>();
            foreach (PreferenceSection section in _preferenceSections)
            {
                bool match = SectionMatches(section, query);
                section.NavigationButton.Visible = match;
                if (match)
                {
                    matches.Add(section);
                }
            }

            if (matches.Count == 0)
            {
                tabControls.Visible = false;
                _searchEmptyLabel.Visible = true;
                _sectionTitleLabel.Text = "No settings found";
                _sectionDescriptionLabel.Text = "Try a different setting name or keyword.";
                UpdateNavigationButtonStyles();
                return;
            }

            _searchEmptyLabel.Visible = false;
            tabControls.Visible = true;
            SelectSection(matches[0], false);
        }

        private static bool SectionMatches(PreferenceSection section, string query)
        {
            string searchableText = section.Title + " " + section.Description + " " + section.Keywords + " " + GetControlSearchText(section.Page);
            string[] terms = query.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string term in terms)
            {
                if (searchableText.IndexOf(term, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    return false;
                }
            }

            return true;
        }

        private static string GetControlSearchText(Control control)
        {
            string text = control.Name + " " + control.Text;
            if (control is DataGridView grid)
            {
                foreach (DataGridViewColumn column in grid.Columns)
                {
                    text += " " + column.Name + " " + column.HeaderText;
                }
            }

            foreach (Control child in control.Controls)
            {
                text += " " + GetControlSearchText(child);
            }

            return text;
        }

        private void UpdateNavigationButtonStyles()
        {
            if (_navigationItems == null)
            {
                return;
            }

            bool dark = _config.UseSpecialColoring;
            Color accent = GetAccentColor(dark);
            Color background = dark ? _colorize.MainBack : Color.FromArgb(248, 249, 251);
            Color navigationBack = dark ? Mix(background, Color.Black, 0.12) : Color.FromArgb(244, 246, 249);
            Color selectedBackground = dark ? Mix(background, accent, 0.28) : Mix(Color.White, accent, 0.12);
            Color fore = dark ? _colorize.MainFore : SystemColors.ControlText;

            foreach (PreferenceSection section in _preferenceSections)
            {
                bool selected = ReferenceEquals(section, _selectedSection) && !_searchEmptyLabel.Visible;
                Button button = section.NavigationButton;
                button.BackColor = selected ? selectedBackground : navigationBack;
                button.ForeColor = selected ? (dark ? Color.White : accent) : fore;
                button.FlatAppearance.BorderSize = 0;
                button.FlatAppearance.BorderColor = selected ? selectedBackground : navigationBack;
                button.FlatAppearance.MouseOverBackColor = dark ? Mix(background, accent, 0.18) : Mix(Color.White, accent, 0.07);
                button.FlatAppearance.MouseDownBackColor = selectedBackground;
                button.Font = new Font(Font, selected ? FontStyle.Bold : FontStyle.Regular);
            }
        }

        private void ApplyModernTheme()
        {
            if (_modernRoot == null)
            {
                return;
            }

            bool dark = _config.UseSpecialColoring;
            Color mainBack = dark ? _colorize.MainBack : Color.FromArgb(248, 249, 251);
            // Prefer configured theme colors, but never allow dark-on-dark body text.
            Color configuredFore = dark ? _colorize.MainFore : SystemColors.ControlText;
            Color mainFore = dark && IsDarkColor(configuredFore)
                ? Color.FromArgb(236, 236, 236)
                : configuredFore;
            Color mutedFore = dark ? Color.FromArgb(198, 198, 198) : Color.FromArgb(108, 117, 125);
            Color navigationBack = dark ? Mix(mainBack, Color.Black, 0.12) : Color.FromArgb(244, 246, 249);
            Color contentBack = dark ? mainBack : Color.FromArgb(248, 249, 251);
            Color cardBack = dark ? Mix(mainBack, Color.White, 0.055) : Color.White;
            Color fieldBack = dark ? Mix(mainBack, Color.White, 0.10) : SystemColors.Window;
            Color fieldFore = dark ? Color.FromArgb(240, 240, 240) : SystemColors.ControlText;
            Color border = dark ? Mix(mainBack, Color.White, 0.32) : Color.FromArgb(222, 226, 230);
            Color accent = GetAccentColor(dark);
            Color cardsFrameBack = dark ? Mix(mainBack, Color.White, 0.04) : Color.FromArgb(252, 252, 253);

            BackColor = mainBack;
            ForeColor = mainFore;
            _modernRoot.BackColor = mainBack;
            _navigationPanel.BackColor = navigationBack;
            _contentPanel.BackColor = contentBack;
            _headerPanel.BackColor = contentBack;
            _pageHost.BackColor = contentBack;
            tabControls.BackColor = contentBack;
            foreach (TabPage page in tabControls.TabPages)
            {
                page.BackColor = contentBack;
                page.BorderStyle = BorderStyle.None;
                page.UseVisualStyleBackColor = false;
            }

            foreach (GroupBox frame in _preferenceCardFrames)
            {
                frame.BackColor = cardsFrameBack;
                // GroupBox draws its frame using ForeColor — keep it soft, not pure white.
                frame.ForeColor = border;
                if (frame.Controls.Count > 0)
                {
                    frame.Controls[0].BackColor = cardsFrameBack;
                }
            }

            _actionPanel.BackColor = cardBack;
            _actionPanel.BorderStyle = BorderStyle.FixedSingle;
            _actionHintLabel.ForeColor = mutedFore;
            _sectionTitleLabel.ForeColor = mainFore;
            _sectionDescriptionLabel.ForeColor = mutedFore;
            _searchEmptyLabel.ForeColor = mutedFore;

            _settingsSearch.BackColor = fieldBack;
            _settingsSearch.ForeColor = fieldFore;
            _settingsSearch.BorderStyle = BorderStyle.FixedSingle;
            if (dark)
            {
                // Keep placeholder readable on near-black surfaces.
                _settingsSearch.BackColor = Mix(mainBack, Color.White, 0.10);
            }
            btSave2.BackColor = accent;
            btSave2.ForeColor = GetContrastColor(accent);
            btSave2.FlatStyle = FlatStyle.Flat;
            btSave2.FlatAppearance.BorderColor = accent;
            btSave2.FlatAppearance.MouseOverBackColor = Mix(accent, Color.White, dark ? 0.12 : 0.08);

            if (_actionButtons != null)
            {
                _actionButtons.BackColor = cardBack;
                foreach (Control actionButton in _actionButtons.Controls)
                {
                    if (actionButton is Button button && !ReferenceEquals(button, btSave2))
                    {
                        button.BackColor = cardBack;
                        button.ForeColor = mainFore;
                        button.FlatStyle = FlatStyle.Flat;
                        button.FlatAppearance.BorderColor = border;
                        button.FlatAppearance.MouseOverBackColor = Mix(cardBack, accent, 0.12);
                    }
                }
            }

            foreach (ToggleSwitch toggle in _toggleSwitches)
            {
                toggle.BackColor = cardBack;
                toggle.ForeColor = mainFore;
                toggle.AccentColor = accent;
            }

            foreach (ColorSettingControl editor in _colorEditors)
            {
                editor.ApplyTheme(cardBack, mainFore, border, accent);
            }

            foreach (PreferenceSection section in _preferenceSections)
            {
                section.Page.BackColor = contentBack;
                section.Page.ForeColor = mainFore;
                ApplyControlTheme(section.Page, cardBack, fieldBack, fieldFore, mainFore, border, accent, mutedFore);
            }

            // Re-apply after ApplyControlTheme so the outer cards frame keeps a soft border color.
            foreach (GroupBox frame in _preferenceCardFrames)
            {
                frame.BackColor = cardsFrameBack;
                frame.ForeColor = border;
                if (frame.Controls.Count > 0)
                {
                    frame.Controls[0].BackColor = cardsFrameBack;
                }
            }

            // Outer frame ForeColor is the border color — force readable text on descendants again.
            foreach (PreferenceSection section in _preferenceSections)
            {
                EnsureReadableText(section.Page, mainFore, mutedFore, dark);
            }

            foreach (GroupBox frame in _preferenceCardFrames)
            {
                frame.BackColor = cardsFrameBack;
                frame.ForeColor = border;
            }

            UpdateNavigationButtonStyles();
        }

        private void ApplyControlTheme(
            Control parent,
            Color cardBack,
            Color fieldBack,
            Color fieldFore,
            Color fore,
            Color border,
            Color accent,
            Color mutedFore)
        {
            foreach (Control control in parent.Controls)
            {
                if (control is DataGridView grid)
                {
                    _colorize.ColorDataGridView(grid);
                    continue;
                }

                if (control is ColorSettingControl colorSetting)
                {
                    colorSetting.ApplyTheme(cardBack, fore, border, accent);
                    continue;
                }

                if (control is GroupBox groupBox && _preferenceCardFrames.Contains(groupBox))
                {
                    // Outer content frame — themed separately after recursion.
                    ApplyControlTheme(control, cardBack, fieldBack, fieldFore, fore, border, accent, mutedFore);
                    continue;
                }

                if (control is GroupBox)
                {
                    // Title + ambient children need light text; border stays light in dark mode.
                    control.BackColor = cardBack;
                    control.ForeColor = fore;
                }
                else if (control is TextBoxBase || control is ComboBox || control is NumericUpDown)
                {
                    control.BackColor = fieldBack;
                    control.ForeColor = fieldFore;
                }
                else if (control is CheckBox checkBox)
                {
                    checkBox.UseVisualStyleBackColor = false;
                    checkBox.BackColor = cardBack;
                    checkBox.ForeColor = fore;
                }
                else if (control is RadioButton radioButton)
                {
                    radioButton.UseVisualStyleBackColor = false;
                    radioButton.BackColor = cardBack;
                    radioButton.ForeColor = fore;
                }
                else if (control is Button button)
                {
                    button.BackColor = cardBack;
                    button.ForeColor = fore;
                    button.FlatStyle = FlatStyle.Flat;
                    button.FlatAppearance.BorderColor = border;
                    button.FlatAppearance.MouseOverBackColor = Mix(cardBack, accent, 0.12);
                }
                else if (control is Label label)
                {
                    label.BackColor = Color.Transparent;
                    label.ForeColor = IsDescriptionLabel(label) ? mutedFore : fore;
                }
                else
                {
                    control.BackColor = cardBack;
                    control.ForeColor = fore;
                }

                if (control is LinkLabel link)
                {
                    link.LinkColor = accent;
                    link.ActiveLinkColor = Mix(accent, Color.White, 0.2);
                }

                ApplyControlTheme(control, cardBack, fieldBack, fieldFore, fore, border, accent, mutedFore);
            }
        }

        private static void EnsureReadableText(Control parent, Color fore, Color mutedFore, bool dark)
        {
            foreach (Control control in parent.Controls)
            {
                if (control is GroupBox groupBox && groupBox.Name.EndsWith("CardsFrame", StringComparison.Ordinal))
                {
                    EnsureReadableText(control, fore, mutedFore, dark);
                    continue;
                }

                if (control is Label label)
                {
                    label.ForeColor = IsDescriptionLabel(label) ? mutedFore : fore;
                }
                else if (control is CheckBox checkBox)
                {
                    checkBox.UseVisualStyleBackColor = false;
                    checkBox.ForeColor = fore;
                }
                else if (control is RadioButton radioButton)
                {
                    radioButton.UseVisualStyleBackColor = false;
                    radioButton.ForeColor = fore;
                }
                else if (control is GroupBox)
                {
                    control.ForeColor = fore;
                }

                if (control.HasChildren)
                {
                    EnsureReadableText(control, fore, mutedFore, dark);
                }
            }
        }

        private static bool IsDescriptionLabel(Label label)
        {
            string text = label.Text ?? string.Empty;
            return text.Length > 42
                || text.Contains("Choose how", StringComparison.OrdinalIgnoreCase)
                || text.Contains("Default separators", StringComparison.OrdinalIgnoreCase)
                || text.Contains("Select what", StringComparison.OrdinalIgnoreCase)
                || text.Contains("One character", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsDarkColor(Color color)
        {
            double luminance = (0.299 * color.R) + (0.587 * color.G) + (0.114 * color.B);
            return luminance < 140;
        }

        private Color GetAccentColor(bool dark)
        {
            IList<byte> configuredAccent = _config.ButtonSelectedHighlightBorder;
            if (configuredAccent != null && configuredAccent.Count >= 3)
            {
                return Color.FromArgb(
                    ClampColor(configuredAccent[0]),
                    ClampColor(configuredAccent[1]),
                    ClampColor(configuredAccent[2]));
            }

            return dark ? Color.FromArgb(67, 139, 222) : SystemColors.Highlight;
        }

        private static int ClampColor(int value)
        {
            return Math.Max(0, Math.Min(255, value));
        }

        private static Color Mix(Color first, Color second, double secondWeight)
        {
            double weight = Math.Max(0, Math.Min(1, secondWeight));
            return Color.FromArgb(
                (int)Math.Round(first.R + ((second.R - first.R) * weight)),
                (int)Math.Round(first.G + ((second.G - first.G) * weight)),
                (int)Math.Round(first.B + ((second.B - first.B) * weight)));
        }

        private static Color GetContrastColor(Color color)
        {
            double luminance = (0.299 * color.R) + (0.587 * color.G) + (0.114 * color.B);
            return luminance > 160 ? Color.Black : Color.White;
        }

        private sealed class ColorSettingControl : Control
        {
            private readonly string _description;
            private readonly Func<Color> _readValue;
            private readonly Action<Color> _writeValue;
            private Color _value;
            private Color _borderColor = Color.LightGray;
            private Color _accentColor = Color.FromArgb(67, 139, 222);

            public ColorSettingControl(
                string title,
                string description,
                Func<Color> readValue,
                Action<Color> writeValue)
            {
                Text = title;
                _description = description;
                _readValue = readValue;
                _writeValue = writeValue;
                _value = readValue();
                Cursor = Cursors.Hand;
                TabStop = true;
                SetStyle(
                    ControlStyles.UserPaint |
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.ResizeRedraw, true);
            }

            public void RefreshValue()
            {
                _value = _readValue();
                Invalidate();
            }

            public void ApplyTheme(Color background, Color foreground, Color border, Color accent)
            {
                BackColor = background;
                ForeColor = foreground;
                _borderColor = border;
                _accentColor = accent;
                Invalidate();
            }

            protected override void OnClick(EventArgs e)
            {
                base.OnClick(e);
                if (!Enabled)
                {
                    return;
                }

                using var dialog = new ColorDialog
                {
                    Color = _value,
                    FullOpen = true,
                    AnyColor = true,
                    SolidColorOnly = false
                };
                if (dialog.ShowDialog(FindForm()) == DialogResult.OK)
                {
                    // ColorDialog does not edit alpha. Preserve it so existing
                    // configurations that use transparency remain lossless.
                    _value = Color.FromArgb(_value.A, dialog.Color.R, dialog.Color.G, dialog.Color.B);
                    _writeValue(_value);
                    Invalidate();
                }
            }

            protected override void OnKeyDown(KeyEventArgs e)
            {
                if (e.KeyCode is Keys.Enter or Keys.Space)
                {
                    OnClick(EventArgs.Empty);
                    e.Handled = true;
                }

                base.OnKeyDown(e);
            }

            protected override void OnFontChanged(EventArgs e)
            {
                base.OnFontChanged(e);
                Height = Math.Max(78, (Font.Height * 4) + 8);
                Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                e.Graphics.Clear(BackColor);

                int swatchSize = Math.Min(34, Math.Max(24, Height - 20));
                int swatchX = Math.Max(0, Width - swatchSize - 12);
                int textWidth = Math.Max(1, swatchX - 18);
                int titleY = 6;
                using var titleFont = new Font(Font, FontStyle.Bold);
                using var descriptionFont = new Font(Font.FontFamily, Math.Max(7, Font.Size - 1), FontStyle.Regular);
                using var valueFont = new Font(Font.FontFamily, Math.Max(7, Font.Size - 2), FontStyle.Regular);
                int titleHeight = titleFont.Height;
                int valueHeight = valueFont.Height;
                int valueY = Math.Max(titleY + titleHeight + 4, Height - valueHeight - 5);
                int descriptionY = titleY + titleHeight + 3;
                int descriptionHeight = Math.Max(16, valueY - descriptionY - 3);
                var titleRectangle = new Rectangle(10, titleY, textWidth, titleHeight);
                var descriptionRectangle = new Rectangle(10, descriptionY, textWidth, descriptionHeight);
                var swatchRectangle = new Rectangle(swatchX, titleY, swatchSize, swatchSize);

                TextRenderer.DrawText(
                    e.Graphics,
                    Text,
                    titleFont,
                    titleRectangle,
                    Enabled ? ForeColor : SystemColors.GrayText,
                    TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
                TextRenderer.DrawText(
                    e.Graphics,
                    _description,
                    descriptionFont,
                    descriptionRectangle,
                    Enabled ? Mix(ForeColor, BackColor, 0.35) : SystemColors.GrayText,
                    TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

                using var swatchBrush = new SolidBrush(Enabled ? _value : SystemColors.Control);
                using var swatchPen = new Pen(_borderColor);
                e.Graphics.FillRectangle(swatchBrush, swatchRectangle);
                e.Graphics.DrawRectangle(swatchPen, swatchRectangle);

                string rgb = $"RGB { _value.R}, { _value.G}, { _value.B}";
                TextRenderer.DrawText(
                    e.Graphics,
                    rgb,
                    valueFont,
                    new Rectangle(swatchX - 112, valueY, 112, valueHeight),
                    Enabled ? Mix(ForeColor, BackColor, 0.2) : SystemColors.GrayText,
                    TextFormatFlags.Right | TextFormatFlags.NoPrefix);

                if (Focused)
                {
                    using var focusPen = new Pen(_accentColor);
                    var focusRectangle = new Rectangle(1, 1, Math.Max(0, Width - 3), Math.Max(0, Height - 3));
                    e.Graphics.DrawRectangle(focusPen, focusRectangle);
                }
            }
        }

        protected override void OnDpiChanged(DpiChangedEventArgs e)
        {
            base.OnDpiChanged(e);
            if (_navigationPanel == null)
            {
                return;
            }

            float scale = e.DeviceDpiNew / 96f;
            _navigationPanel.Width = (int)Math.Round(248 * scale);
            _navigationItems.Padding = new Padding((int)Math.Round(8 * scale));
            foreach (Control control in _navigationItems.Controls)
            {
                control.Height = (int)Math.Round(40 * scale);
            }

            UpdateNavigationItemsWidth();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            RefreshPreferenceCardLayouts();
            if (!_documentHosted)
            {
                FitToWorkingArea();
            }
        }

        private void FitToWorkingArea()
        {
            Screen screen = Screen.FromControl(this);
            Rectangle workingArea = screen.WorkingArea;
            const int margin = 12;

            int maximumWidth = Math.Max(1, workingArea.Width - (margin * 2));
            int maximumHeight = Math.Max(1, workingArea.Height - (margin * 2));
            int width = Math.Min(Bounds.Width, maximumWidth);
            int height = Math.Min(Bounds.Height, maximumHeight);

            if (MinimumSize.Width > width || MinimumSize.Height > height)
            {
                MinimumSize = new Size(
                    Math.Min(MinimumSize.Width, width),
                    Math.Min(MinimumSize.Height, height));
            }

            if (width != Bounds.Width || height != Bounds.Height)
            {
                Size = new Size(width, height);
            }

            int left = workingArea.Left + Math.Max(0, (workingArea.Width - Width) / 2);
            int top = workingArea.Top + Math.Max(0, (workingArea.Height - Height) / 2);
            Location = new Point(left, top);
        }

        private sealed class ToggleSwitch : Control
        {
            private readonly CheckBox _checkBox;

            public ToggleSwitch(CheckBox checkBox)
            {
                _checkBox = checkBox;
                Text = checkBox.Text;
                Font = checkBox.Font;
                Cursor = Cursors.Hand;
                TabStop = true;
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
                _checkBox.CheckedChanged += CheckBox_CheckedChanged;
            }

            public Color AccentColor { get; set; } = Color.FromArgb(67, 139, 222);

            protected override void OnClick(EventArgs e)
            {
                base.OnClick(e);
                _checkBox.Checked = !_checkBox.Checked;
                Focus();
            }

            protected override void OnKeyDown(KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Space)
                {
                    _checkBox.Checked = !_checkBox.Checked;
                    e.Handled = true;
                }

                base.OnKeyDown(e);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                int trackWidth = 38;
                int trackHeight = 18;
                int trackX = Math.Max(0, Width - trackWidth - 5);
                int trackY = Math.Max(0, (Height - trackHeight) / 2);
                Color trackColor;
                if (!Enabled)
                {
                    trackColor = Color.Gray;
                }
                else if (_checkBox.Checked)
                {
                    trackColor = AccentColor;
                }
                else
                {
                    trackColor = Color.FromArgb(130, 140, 150);
                }

                using (var trackBrush = new SolidBrush(trackColor))
                using (var knobBrush = new SolidBrush(Color.White))
                using (var outlinePen = new Pen(Color.FromArgb(80, 80, 80)))
                {
                    using GraphicsPath trackPath = RoundedRectangle(new Rectangle(trackX, trackY, trackWidth, trackHeight), trackHeight);
                    e.Graphics.FillPath(trackBrush, trackPath);
                    if (!_checkBox.Checked)
                    {
                        e.Graphics.DrawPath(outlinePen, trackPath);
                    }

                    int knobSize = 12;
                    int knobX = _checkBox.Checked ? trackX + trackWidth - knobSize - 3 : trackX + 3;
                    int knobY = trackY + 3;
                    e.Graphics.FillEllipse(knobBrush, new Rectangle(knobX, knobY, knobSize, knobSize));
                }

                Rectangle textRectangle = new Rectangle(0, 0, Math.Max(0, trackX - 10), Height);
                TextRenderer.DrawText(e.Graphics, Text, Font, textRectangle, ForeColor,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            }

            private void CheckBox_CheckedChanged(object sender, EventArgs e)
            {
                Invalidate();
            }

            private static GraphicsPath RoundedRectangle(Rectangle rectangle, int radius)
            {
                int diameter = Math.Min(radius, Math.Min(rectangle.Width, rectangle.Height));
                var path = new GraphicsPath();
                path.AddArc(rectangle.X, rectangle.Y, diameter, diameter, 180, 90);
                path.AddArc(rectangle.Right - diameter, rectangle.Y, diameter, diameter, 270, 90);
                path.AddArc(rectangle.Right - diameter, rectangle.Bottom - diameter, diameter, diameter, 0, 90);
                path.AddArc(rectangle.X, rectangle.Bottom - diameter, diameter, diameter, 90, 90);
                path.CloseFigure();
                return path;
            }
        }
    }
}
