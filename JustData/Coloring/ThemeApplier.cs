using AppBase.Common;
using AppBase.Common.Interfaces;
using DatabaseDataGridView.WinForms;
using DatabaseDataGridView.WinForms.Coloring;
using FastColoredTextBoxNS;
using JustyBaseLegacy.UI.Controls;
using JustyBaseLegacy.UI.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace JustyBaseLegacy.UI.Coloring
{
    public static class ThemeApplier
    {
        public static void RePaintMainWindowX(BaseWindow control, IColorTheme _colorTheme, IApplicationSettingsContext applicationSettingsContext)
        {
            _colorTheme.InitColors();
            bool dark = applicationSettingsContext.Config.UseSpecialColoring;

            control.ForeColor = _colorTheme.MainFore;
            control.BackColor = _colorTheme.MainBack;

            if (applicationSettingsContext.Config.UseSpecialColoring)
            {
                control.CbSearchDb.DrawMode = DrawMode.OwnerDrawFixed;
                control.CbWhatDb.DrawMode = DrawMode.OwnerDrawFixed;
            }
            else
            {
                control.CbSearchDb.DrawMode = DrawMode.Normal;
                control.CbWhatDb.DrawMode = DrawMode.Normal;
            }

            Color subtleBorder = DarkChromeHelper.SubtleBorder(_colorTheme.MainBack, _colorTheme.MainFore);

            Stack<Component> componentStack = new Stack<Component>();
            foreach (Control item in control.Controls.OfType<Component>())
            {
                componentStack.Push(item);
            }

            while (componentStack.Count > 0)
            {
                var component = componentStack.Pop();

                try
                {
                    if (component is MenuStrip menuStrip)
                    {
                        if (ReferenceEquals(menuStrip, control.MenuStrip1))
                        {
                            continue;
                        }

                        menuStrip.ForeColor = _colorTheme.MainFore;
                        menuStrip.BackColor = Color.Transparent;
                        menuStrip.Renderer = _colorTheme.GetRenderer();
                    }
                    else if (component is ToolStripMenuItem menuItem) // ? ToolStripItem ? 
                    {
                        if (!control.MenuStrip1.Items.Contains(menuItem))
                        {
                            // OK, OK
                            menuItem.BackColor = _colorTheme.MainBack;
                            menuItem.ForeColor = _colorTheme.MainFore;
                        }
                    }
                    else if (component is ToolStrip toolStrip)
                    {
                        toolStrip.Renderer = _colorTheme.GetRenderer();
                        toolStrip.BackColor = _colorTheme.MainBack;
                        toolStrip.ForeColor = _colorTheme.MainFore;
                    }
                    else if (component is ToolStripComboBox toolStripComboBox)
                    {
                        DarkChromeHelper.ApplyToolStripComboBox(
                            toolStripComboBox,
                            _colorTheme.CbBackColor,
                            _colorTheme.CbForeColor,
                            dark);
                    }
                    else if (component is SplitContainer splitContainer)
                    {
                        if (dark)
                        {
                            DarkChromeHelper.ApplySplitContainer(
                                splitContainer,
                                _colorTheme.MainBack,
                                _colorTheme.MainFore,
                                subtleBorder);
                        }
                        else
                        {
                            splitContainer.BackColor = _colorTheme.MainBack;
                            splitContainer.ForeColor = _colorTheme.MainFore;
                            splitContainer.Panel1.BackColor = _colorTheme.MainBack;
                            splitContainer.Panel2.BackColor = _colorTheme.MainBack;
                            splitContainer.Panel1.ForeColor = _colorTheme.MainFore;
                            splitContainer.Panel2.ForeColor = _colorTheme.MainFore;
                        }
                    }
                    else if (component is TextBox textBox)
                    {
                        if (dark)
                        {
                            DarkChromeHelper.ApplyTextBox(
                                textBox,
                                _colorTheme.TextBoxBackColor,
                                _colorTheme.TextBoxForeColor,
                                subtleBorder);
                        }
                        else
                        {
                            textBox.ForeColor = _colorTheme.TextBoxForeColor;
                            textBox.BackColor = _colorTheme.TextBoxBackColor;
                        }
                    }
                    else if (component is Button button)
                    {
                        button.ForeColor = _colorTheme.ButtonForeColor;
                        button.BackColor = _colorTheme.ButtonBackColor;
                    }
                    else if (component is TreeView treeView)
                    {
                        treeView.BackColor = _colorTheme.TreeViewBackColor;
                        treeView.ForeColor = _colorTheme.TreeViewForeColor;
                        treeView.LineColor = _colorTheme.TreeViewLineColor;
                        treeView.Invalidate();
                    }
                    else if (component is PropertyGrid propertyGrid)
                    {
                        propertyGrid.BackColor = _colorTheme.PropertyBackColor;
                        propertyGrid.ForeColor = _colorTheme.PropertyForeColor;
                    }
                    else if (component is Control propertyGridView && propertyGridView.Text == "PropertyGridView")
                    {
                        propertyGridView.BackColor = _colorTheme.PropertyBackViewColor;
                        propertyGridView.ForeColor = _colorTheme.PropertyForeViewColor;
                    }
                    else if (component is TabPage tabPage)
                    {
                        tabPage.BackColor = _colorTheme.TabPageBackColor;
                        tabPage.ForeColor = _colorTheme.TabPageForeColor;
                    }
                    else if (component is Panel panel)
                    {
                        if (dark)
                        {
                            if (panel.Dock == DockStyle.Bottom && panel.Controls.Count > 0
                                && panel.Controls[0] is TextBox)
                            {
                                panel.BackColor = _colorTheme.MainBack;
                                panel.ForeColor = _colorTheme.MainFore;
                                panel.BorderStyle = BorderStyle.None;
                                DarkChromeHelper.ApplyStatusPanelChildBorders(panel, subtleBorder);
                            }
                            else
                            {
                                DarkChromeHelper.ApplyPanel(panel, _colorTheme.MainBack, _colorTheme.MainFore, subtleBorder);
                            }
                        }
                        else
                        {
                            panel.BackColor = _colorTheme.MainBack;
                            panel.ForeColor = _colorTheme.MainFore;
                        }
                    }
                    else if (component is SqlExecutionLogControl sqlLog)
                    {
                        sqlLog.ApplyTheme(_colorTheme);
                        sqlLog.SetErrorBackColor(MyColors.LogErrorStdColor);
                        continue;
                    }
                    else if (component is CustomDataGridView customDataGridView)
                    {
                        _colorTheme.ColorMyDataGridView(customDataGridView);
                        customDataGridView.InnerDataGridView.Invalidate();
                        customDataGridView.Invalidate();
                        continue;
                    }
                    else if (component is DataGridView dataGridView)
                    {
                        _colorTheme.ColorDataGridView(dataGridView);
                        dataGridView.Invalidate();
                        continue;
                    }
                    else if (component is FastColoredTextBox fastColoredTextBox)
                    {
                        fastColoredTextBox.SelectionColor = _colorTheme.CurrentFctbColors.FctbSelectionColor;
                        fastColoredTextBox.DisabledColor = _colorTheme.CurrentFctbColors.FctbDisabledColor;
                        fastColoredTextBox.BackColor = _colorTheme.CurrentFctbColors.FctbBackColor;
                        fastColoredTextBox.ForeColor = _colorTheme.CurrentFctbColors.FctbForeColor;
                        fastColoredTextBox.IndentBackColor = _colorTheme.CurrentFctbColors.FctbIndentBackColor;
                        fastColoredTextBox.LineNumberColor = _colorTheme.CurrentFctbColors.FctbLineNumberColor;
                        fastColoredTextBox.FoldingIndicatorColor = _colorTheme.CurrentFctbColors.FctbFoldingIndicatorColor;
                        continue;
                    }
                    else if (component is TabControl tabControl)
                    {
                        //tabControl.BackColor = Colorize.mainBack;
                        //tabControl.ForeColor = Colorize.mainFore;
                    }
                    else if (component is ComboBox comboBox)
                    {
                        if (dark)
                        {
                            bool ownerDraw = comboBox.DrawMode == DrawMode.OwnerDrawFixed;
                            DarkChromeHelper.ApplyComboBox(
                                comboBox,
                                _colorTheme.CbBackColor,
                                _colorTheme.CbForeColor,
                                ownerDraw);
                        }
                        else
                        {
                            comboBox.BackColor = _colorTheme.CbBackColor;
                            comboBox.ForeColor = _colorTheme.CbForeColor;
                            comboBox.FlatStyle = FlatStyle.Standard;
                            GridThemingHelper.ApplyScrollbarTheme(comboBox, false);
                        }
                    }
                    else if (component is ScrollBar scrollBar)
                    {
                        scrollBar.BackColor = _colorTheme.MainBack;
                        scrollBar.ForeColor = _colorTheme.MainFore;
                    }
                    else if (component is PictureBox pictureBox)
                    {
                        pictureBox.BackColor = Color.Transparent;
                        pictureBox.ForeColor = Color.Black;
                    }
                    else if (component is Control otherControl) // any other - risky..
                    {
                        otherControl.BackColor = _colorTheme.MainBack;
                        otherControl.ForeColor = _colorTheme.MainFore;
                    }
                }
                catch (Exception exception)
                {
                    System.Diagnostics.Trace.WriteLine($"Applying a control theme failed: {exception.GetType().Name}");
                }

                if (component is Control controlWithChildren)
                {
                    if (controlWithChildren.Controls.Count > 0)
                    {
                        foreach (Control subComponent in controlWithChildren.Controls.OfType<Control>())
                        {
                            componentStack.Push(subComponent);
                        }
                    }
                    if (controlWithChildren is MenuStrip menuStripWithItems && menuStripWithItems.Items.Count > 0)
                    {
                        if (!ReferenceEquals(menuStripWithItems, control.MenuStrip1))
                        {
                            foreach (ToolStripMenuItem subComponent in menuStripWithItems.Items.OfType<ToolStripMenuItem>())
                            {
                                componentStack.Push(subComponent);
                            }
                        }
                    }
                    else if (controlWithChildren is ToolStrip toolStripWithItems && toolStripWithItems.Items.Count > 0)
                    {
                        foreach (ToolStripItem subComponent in toolStripWithItems.Items.OfType<ToolStripItem>())
                        {
                            componentStack.Push(subComponent);
                        }
                    }
                }
                else if (component is ToolStripMenuItem toolStripMenuItem)
                {
                    foreach (var dropDownItem in toolStripMenuItem.DropDownItems.OfType<ToolStripItem>())
                    {
                        componentStack.Push(dropDownItem);
                    }
                }
                else if (component is ToolStripSplitButton toolStripSplitButton)
                {
                    foreach (ToolStripMenuItem dropDownItem in toolStripSplitButton.DropDownItems.OfType<ToolStripMenuItem>())
                    {
                        componentStack.Push(dropDownItem);
                    }
                }
            }

            foreach (var item in control.Components.Components)
            {
                if (item is ContextMenuStrip contextMenuStrip)
                {
                    contextMenuStrip.Renderer = _colorTheme.GetRenderer();

                    Stack<ToolStripMenuItem> menuItemStack = new Stack<ToolStripMenuItem>();
                    foreach (ToolStripItem toolStripMenuItem in contextMenuStrip.Items.OfType<ToolStripItem>())
                    {
                        toolStripMenuItem.BackColor = _colorTheme.MainBack;
                        toolStripMenuItem.ForeColor = _colorTheme.MainFore;
                        if (toolStripMenuItem is ToolStripMenuItem menuItem)
                        {
                            menuItemStack.Push(menuItem);
                        }
                    }
                    while (menuItemStack.Count > 0)
                    {
                        var menuItemX = menuItemStack.Pop();
                        menuItemX.BackColor = _colorTheme.MainBack;
                        menuItemX.ForeColor = _colorTheme.MainFore;
                        foreach (ToolStripItem toolStripMenuItem2 in menuItemX.DropDownItems.OfType<ToolStripItem>())
                        {
                            toolStripMenuItem2.BackColor = _colorTheme.MainBack;
                            toolStripMenuItem2.ForeColor = _colorTheme.MainFore;
                            if (toolStripMenuItem2 is ToolStripMenuItem menuItem)
                            {
                                menuItemStack.Push(menuItem);
                            }
                        }
                    }
                }
                else if (item is ToolStripMenuItem menuItem)
                {
                    menuItem.BackColor = _colorTheme.MainBack;
                    menuItem.ForeColor = _colorTheme.MainFore;

                    foreach (ToolStripMenuItem level1MenuItem in menuItem.DropDownItems.OfType<ToolStripMenuItem>())
                    {
                        level1MenuItem.BackColor = _colorTheme.MainBack;
                        level1MenuItem.ForeColor = _colorTheme.MainFore;
                    }
                }
            }
        }
    }
}
