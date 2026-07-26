using AppBase.Common.Interfaces;
using DatabaseDataGridView.WinForms.Coloring;
using JustData.Properties;
using System.Drawing;
using System.Windows.Forms;

namespace JustyBaseLegacy.UI
{
    partial class BaseWindow
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BaseWindow));
            DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
            menuStrip1 = new MenuStrip();
            toolStripMenuPlik = new ToolStripMenuItem();
            fileOpenMenuItem = new ToolStripMenuItem();
            fileOpenManyMenuItem = new ToolStripMenuItem();
            toolStripSeparator1 = new CustomToolStripSeparator(_applicationSettingsContext.Config.UseSpecialColoring, Color.FromArgb(_applicationSettingsContext.Config.StripFore[0], _applicationSettingsContext.Config.StripFore[1], _applicationSettingsContext.Config.StripFore[2]), Color.FromArgb(_applicationSettingsContext.Config.StripBack[0], _applicationSettingsContext.Config.StripBack[1], _applicationSettingsContext.Config.StripBack[2]));
            recentFilesMenu = new ToolStripMenuItem();
            recentManyFilesMenu = new ToolStripMenuItem();
            toolStripSeparator6 = new CustomToolStripSeparator(_applicationSettingsContext.Config.UseSpecialColoring, Color.FromArgb(_applicationSettingsContext.Config.StripFore[0], _applicationSettingsContext.Config.StripFore[1], _applicationSettingsContext.Config.StripFore[2]), Color.FromArgb(_applicationSettingsContext.Config.StripBack[0], _applicationSettingsContext.Config.StripBack[1], _applicationSettingsContext.Config.StripBack[2]));
            fileSaveMenuItem = new ToolStripMenuItem();
            fileSaveAsMenuItem = new ToolStripMenuItem();
            fileSaveManyMenuItem = new ToolStripMenuItem();
            toolStripSeparator5 = new CustomToolStripSeparator(_applicationSettingsContext.Config.UseSpecialColoring, Color.FromArgb(_applicationSettingsContext.Config.StripFore[0], _applicationSettingsContext.Config.StripFore[1], _applicationSettingsContext.Config.StripFore[2]), Color.FromArgb(_applicationSettingsContext.Config.StripBack[0], _applicationSettingsContext.Config.StripBack[1], _applicationSettingsContext.Config.StripBack[2]));
            fileExitMenuItem = new ToolStripMenuItem();
            editToolStripMenuItem = new ToolStripMenuItem();
            findToolStripMenuItem = new ToolStripMenuItem();
            replaceToolStripMenuItem = new ToolStripMenuItem();
            toolStripMenuItem1 = new CustomToolStripSeparator(_applicationSettingsContext.Config.UseSpecialColoring, Color.FromArgb(_applicationSettingsContext.Config.StripFore[0], _applicationSettingsContext.Config.StripFore[1], _applicationSettingsContext.Config.StripFore[2]), Color.FromArgb(_applicationSettingsContext.Config.StripBack[0], _applicationSettingsContext.Config.StripBack[1], _applicationSettingsContext.Config.StripBack[2]));
            setSelectedAsReadonlyToolStripMenuItem = new ToolStripMenuItem();
            setSelectedAsWritableToolStripMenuItem = new ToolStripMenuItem();
            toolStripMenuItem8 = new CustomToolStripSeparator(_applicationSettingsContext.Config.UseSpecialColoring, Color.FromArgb(_applicationSettingsContext.Config.StripFore[0], _applicationSettingsContext.Config.StripFore[1], _applicationSettingsContext.Config.StripFore[2]), Color.FromArgb(_applicationSettingsContext.Config.StripBack[0], _applicationSettingsContext.Config.StripBack[1], _applicationSettingsContext.Config.StripBack[2]));
            collapseSelectedBlockToolStripMenuItem = new ToolStripMenuItem();
            toolStripMenuItem3 = new CustomToolStripSeparator(_applicationSettingsContext.Config.UseSpecialColoring, Color.FromArgb(_applicationSettingsContext.Config.StripFore[0], _applicationSettingsContext.Config.StripFore[1], _applicationSettingsContext.Config.StripFore[2]), Color.FromArgb(_applicationSettingsContext.Config.StripBack[0], _applicationSettingsContext.Config.StripBack[1], _applicationSettingsContext.Config.StripBack[2]));
            collapseAllregionToolStripMenuItem = new ToolStripMenuItem();
            exapndAllregionToolStripMenuItem = new ToolStripMenuItem();
            toolStripMenuItem2 = new CustomToolStripSeparator(_applicationSettingsContext.Config.UseSpecialColoring, Color.FromArgb(_applicationSettingsContext.Config.StripFore[0], _applicationSettingsContext.Config.StripFore[1], _applicationSettingsContext.Config.StripFore[2]), Color.FromArgb(_applicationSettingsContext.Config.StripBack[0], _applicationSettingsContext.Config.StripBack[1], _applicationSettingsContext.Config.StripBack[2]));
            increaseIndentSiftTabToolStripMenuItem = new ToolStripMenuItem();
            decreaseIndentTabToolStripMenuItem = new ToolStripMenuItem();
            toolStripMenuItem4 = new CustomToolStripSeparator(_applicationSettingsContext.Config.UseSpecialColoring, Color.FromArgb(_applicationSettingsContext.Config.StripFore[0], _applicationSettingsContext.Config.StripFore[1], _applicationSettingsContext.Config.StripFore[2]), Color.FromArgb(_applicationSettingsContext.Config.StripBack[0], _applicationSettingsContext.Config.StripBack[1], _applicationSettingsContext.Config.StripBack[2]));
            goBackwardCtrlToolStripMenuItem = new ToolStripMenuItem();
            goForwardCtrlShiftToolStripMenuItem = new ToolStripMenuItem();
            toolStripMenuItem5 = new CustomToolStripSeparator(_applicationSettingsContext.Config.UseSpecialColoring, Color.FromArgb(_applicationSettingsContext.Config.StripFore[0], _applicationSettingsContext.Config.StripFore[1], _applicationSettingsContext.Config.StripFore[2]), Color.FromArgb(_applicationSettingsContext.Config.StripBack[0], _applicationSettingsContext.Config.StripBack[1], _applicationSettingsContext.Config.StripBack[2]));
            autoIndentToolStripMenuItem = new ToolStripMenuItem();
            toolStripMenuItem6 = new CustomToolStripSeparator(_applicationSettingsContext.Config.UseSpecialColoring, Color.FromArgb(_applicationSettingsContext.Config.StripFore[0], _applicationSettingsContext.Config.StripFore[1], _applicationSettingsContext.Config.StripFore[2]), Color.FromArgb(_applicationSettingsContext.Config.StripBack[0], _applicationSettingsContext.Config.StripBack[1], _applicationSettingsContext.Config.StripBack[2]));
            goLeftBracketToolStripMenuItem = new ToolStripMenuItem();
            goRightBracketToolStripMenuItem = new ToolStripMenuItem();
            toolStripMenuItem7 = new CustomToolStripSeparator(_applicationSettingsContext.Config.UseSpecialColoring, Color.FromArgb(_applicationSettingsContext.Config.StripFore[0], _applicationSettingsContext.Config.StripFore[1], _applicationSettingsContext.Config.StripFore[2]), Color.FromArgb(_applicationSettingsContext.Config.StripBack[0], _applicationSettingsContext.Config.StripBack[1], _applicationSettingsContext.Config.StripBack[2]));
            miPrint = new ToolStripMenuItem();
            databaseToolStripMenuItem = new ToolStripMenuItem();
            addNewConnectionToolStripMenuItem = new ToolStripMenuItem();
            queryWatchToolStripMenuItem = new ToolStripMenuItem();
            historyToolStripMenuItem = new ToolStripMenuItem();
            ImportToolStripMenuItem = new ToolStripMenuItem();
            XLSXtoolStripMenuItem = new ToolStripMenuItem();
            recentXlsx = new ToolStripMenuItem();
            changeHotkeysToolStripMenuItem = new ToolStripMenuItem();
            optionsToolStripMenuItem = new ToolStripMenuItem();
            mapToolStripMenuItem = new ToolStripMenuItem();
            settingsToolStripMenuItem = new ToolStripMenuItem();
            showDataFolderMenuItem = new ToolStripMenuItem();
            clearDataFolderMenuItem = new ToolStripMenuItem();
            tsmLicence = new ToolStripMenuItem();
            themeToolStripMenuItem = new ToolStripMenuItem();
            cSharpbuiltinHighlighterToolStripMenuItem = new ToolStripMenuItem();
            splitContainer1 = new SplitContainer();
            _leftTabs = new TabControl();
            databaseTabPage = new TabPage();
            databaseContextMenuStrip = new ContextMenuStrip(components);
            refreshTableListItem = new ToolStripMenuItem();
            collapseDatabaseMenuItem = new ToolStripMenuItem();
            groomDatabaseToolStripMenuItem = new ToolStripMenuItem();
            DbSizeToolStripMenuItem = new ToolStripMenuItem();
            imageList1 = new ImageList(components);
            tabPageFiles = new TabPage();
            tabPageVariables = new TabPage();
            tabPageLegend = new TabPage();
            _tabControlMain = new TabControl();
            tabContextMenuStrip = new ContextMenuStrip(components);
            closeAllTabsMenuItem = new ToolStripMenuItem();
            closeOtherTabsMenuItem = new ToolStripMenuItem();
            cmsOpenInExplorer = new ToolStripMenuItem();
            cmsSave = new ToolStripMenuItem();
            cmsRenameTab = new ToolStripMenuItem();
            runToCursorToolStripMenuItem = new ToolStripMenuItem();
            imageListFiles = new ImageList(components);

            ofdMain = new OpenFileDialog();
            tabPage1 = new TabPage();
            saveFileDialogSQL = new SaveFileDialog();
            cmResults = new ContextMenuStrip(components);
            zamknijKarte = new ToolStripMenuItem();
            zamknijWszytkieKarty = new ToolStripMenuItem();
            zamknijReszte = new ToolStripMenuItem();
            renameTab = new ToolStripMenuItem();
            verticalHorizontalToolStripMenuItem = new ToolStripMenuItem();
            cmMain = new ContextMenuStrip(components);
            cutToolStripMenuItem = new ToolStripMenuItem();
            copyToolStripMenuItem = new ToolStripMenuItem();
            copyRawToolStripMenuItem = new ToolStripMenuItem();
            pasteToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator3 = new CustomToolStripSeparator(_applicationSettingsContext.Config.UseSpecialColoring, Color.FromArgb(_applicationSettingsContext.Config.StripFore[0], _applicationSettingsContext.Config.StripFore[1], _applicationSettingsContext.Config.StripFore[2]), Color.FromArgb(_applicationSettingsContext.Config.StripBack[0], _applicationSettingsContext.Config.StripBack[1], _applicationSettingsContext.Config.StripBack[2]));
            zakomentujToolStrip = new ToolStripMenuItem();
            odkomentujToolStrip = new ToolStripMenuItem();
            toolStripSeparator4 = new CustomToolStripSeparator(_applicationSettingsContext.Config.UseSpecialColoring, Color.FromArgb(_applicationSettingsContext.Config.StripFore[0], _applicationSettingsContext.Config.StripFore[1], _applicationSettingsContext.Config.StripFore[2]), Color.FromArgb(_applicationSettingsContext.Config.StripBack[0], _applicationSettingsContext.Config.StripBack[1], _applicationSettingsContext.Config.StripBack[2]));
            EksportytoolStripMenuItem11 = new ToolStripMenuItem();
            CSVtoolStripMenuItem12 = new ToolStripMenuItem();
            XLSXStripMenuItem12 = new ToolStripMenuItem();
            addDollarSignMenuItem = new ToolStripMenuItem();
            formatSQL = new ToolStripMenuItem();
            addAliases = new ToolStripMenuItem();
            makeCodeToTempTable = new ToolStripMenuItem();
            saveAsSnippet = new ToolStripMenuItem();
            toolStripSeparator9 = new CustomToolStripSeparator(_applicationSettingsContext.Config.UseSpecialColoring, Color.FromArgb(_applicationSettingsContext.Config.StripFore[0], _applicationSettingsContext.Config.StripFore[1], _applicationSettingsContext.Config.StripFore[2]), Color.FromArgb(_applicationSettingsContext.Config.StripBack[0], _applicationSettingsContext.Config.StripBack[1], _applicationSettingsContext.Config.StripBack[2]));
            pasteAsSelect = new ToolStripMenuItem();
            importFromClipboard = new ToolStripMenuItem();
            inRaw = new ToolStripMenuItem();
            inText = new ToolStripMenuItem();
            tsmiWordWrap = new ToolStripMenuItem();
            manySaveFileDialog = new SaveFileDialog();
            manyOpenFileDialog = new OpenFileDialog();
            cmGridContextMenuStrip1 = new ContextMenuStrip(components);
            copyWithHeadersStripMenuItem = new ToolStripMenuItem();
            copyWithOutHeadersStripMenuItem = new ToolStripMenuItem();
            clearFilters = new ToolStripMenuItem();
            singleRowStripMenuItem = new ToolStripMenuItem();
            pokazSQLMenuItem = new ToolStripMenuItem();
            copyAsXlsxToClipboardMenuItem = new ToolStripMenuItem();
            cmGridContextMenuStripRowView = new ContextMenuStrip(components);
            showDiff = new ToolStripMenuItem();

            saveFileCSV = new SaveFileDialog();
            saveFileJson = new SaveFileDialog();
            saveSQLLite = new SaveFileDialog();
            saveFileXlsx = new SaveFileDialog();
            openFileXlsx = new OpenFileDialog();

            toolStripMenuItem11 = new ToolStripMenuItem();
            toolStripMenuItem12 = new ToolStripMenuItem();
            cmAllTables = new ContextMenuStrip(components);
            refreshTableListToolStripMenuItem = new ToolStripMenuItem();
            tcmDDLALLNz = new ToolStripMenuItem();
            tcmRecreateALL = new ToolStripMenuItem();
            showTablesSizes = new ToolStripMenuItem();
            showQueryHistory = new ToolStripMenuItem();
            showUserSessions = new ToolStripMenuItem();
            tcmChangeSorting = new ToolStripMenuItem();
            createNewTableToolStripMenuItem = new ToolStripMenuItem();
            cmSynonyms = new ContextMenuStrip(components);
            addNewSynonym = new ToolStripMenuItem();
            cmAllProcsNetezza = new ContextMenuStrip(components);
            menuItemDdlProcsNetezza = new ToolStripMenuItem();
            searchInProcs = new ToolStripMenuItem();
            netezzaProcExample = new ToolStripMenuItem();
            toolStripMenuItemAddProcedureNetezza = new ToolStripMenuItem();
            tcmViewsSearchNetezza = new ToolStripMenuItem();
            cmColumns = new ContextMenuStrip(components);
            tcmAddColumn = new ToolStripMenuItem();
            cmConstraints = new ContextMenuStrip(components);
            tcmAddConstraint = new ToolStripMenuItem();
            cmIndexes = new ContextMenuStrip(components);
            tcmAddIndex = new ToolStripMenuItem();
            cmPartitions = new ContextMenuStrip(components);
            tcmAddPartitio = new ToolStripMenuItem();
            cmTriggers = new ContextMenuStrip(components);
            tcmAddTrigger = new ToolStripMenuItem();
            cmAllViews = new ContextMenuStrip(components);
            tcmAllViews = new ToolStripMenuItem();
            addViewToolStripMenuItem = new ToolStripMenuItem();
            panel1 = new Panel();
            cursorPositionTextBox = new TextBox();
            mainTextBox = new TextBox();
            statusTextBox = new TextBox();
            createNewSequenceToolStripMenuItem = new ToolStripMenuItem();
            contextMenuStripNetezzaSequences = new ContextMenuStrip(components);
            contextMenuStripNetezzaUsersOrGroups = new ContextMenuStrip(components);
            tsmiAddNetezzaUser = new ToolStripMenuItem();
            cmsDB2Server = new ContextMenuStrip(components);
            tsmiCreateServerDB2 = new ToolStripMenuItem();
            cmsSynonyms = new ContextMenuStrip(components);
            tsmiDDLSynonyms = new ToolStripMenuItem();
            tsmiValidateSynonyms = new ToolStripMenuItem();
            menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            _leftTabs.SuspendLayout();
            databaseTabPage.SuspendLayout();
            databaseContextMenuStrip.SuspendLayout();
            tabContextMenuStrip.SuspendLayout();
            cmResults.SuspendLayout();
            cmMain.SuspendLayout();
            cmGridContextMenuStrip1.SuspendLayout();
            cmGridContextMenuStripRowView.SuspendLayout();
            cmAllTables.SuspendLayout();
            cmSynonyms.SuspendLayout();
            cmAllProcsNetezza.SuspendLayout();
            cmColumns.SuspendLayout();
            cmConstraints.SuspendLayout();
            cmIndexes.SuspendLayout();
            cmPartitions.SuspendLayout();
            cmTriggers.SuspendLayout();
            cmAllViews.SuspendLayout();
            panel1.SuspendLayout();
            contextMenuStripNetezzaSequences.SuspendLayout();
            contextMenuStripNetezzaUsersOrGroups.SuspendLayout();
            cmsDB2Server.SuspendLayout();
            cmsSynonyms.SuspendLayout();
            SuspendLayout();
            // 
            // menuStrip1
            // 
            menuStrip1.Dock = DockStyle.None;
            menuStrip1.GripMargin = new Padding(0);
            menuStrip1.ImageScalingSize = new Size(23, 23);
            menuStrip1.Items.AddRange(new ToolStripItem[] { toolStripMenuPlik, editToolStripMenuItem, databaseToolStripMenuItem, ImportToolStripMenuItem, changeHotkeysToolStripMenuItem, settingsToolStripMenuItem, optionsToolStripMenuItem, tsmLicence, themeToolStripMenuItem });
            menuStrip1.Location = new Point(8, 8);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Padding = new Padding(0);
            menuStrip1.Size = new Size(446, 27);
            menuStrip1.TabIndex = 4;
            menuStrip1.Text = "menuStrip1";
            menuStrip1.MouseDown += Form1_MouseDown;
            // 
            // toolStripMenuPlik
            // 
            toolStripMenuPlik.DropDownItems.AddRange(new ToolStripItem[] { fileOpenMenuItem, fileOpenManyMenuItem, toolStripSeparator1, recentFilesMenu, recentManyFilesMenu, toolStripSeparator6, fileSaveMenuItem, fileSaveAsMenuItem, fileSaveManyMenuItem, toolStripSeparator5, fileExitMenuItem });
            toolStripMenuPlik.ImageAlign = ContentAlignment.TopLeft;
            toolStripMenuPlik.ImageScaling = ToolStripItemImageScaling.None;
            toolStripMenuPlik.Name = "toolStripMenuPlik";
            toolStripMenuPlik.Padding = new Padding(0);
            toolStripMenuPlik.Size = new Size(29, 27);
            toolStripMenuPlik.Text = "File";
            toolStripMenuPlik.TextImageRelation = TextImageRelation.Overlay;
            // 
            // fileOpenMenuItem
            // 
            fileOpenMenuItem.Name = "fileOpenMenuItem";
            fileOpenMenuItem.Size = new Size(191, 22);
            fileOpenMenuItem.Text = "Open [Ctrl + O]";
            fileOpenMenuItem.Click += OpenToolStripMenuItem_Click;
            // 
            // fileOpenManyMenuItem
            // 
            fileOpenManyMenuItem.Name = "fileOpenManyMenuItem";
            fileOpenManyMenuItem.Size = new Size(191, 22);
            fileOpenManyMenuItem.Text = "Open SQL set";
            fileOpenManyMenuItem.Click += OpenManyToolStripMenuItem_Click;
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(188, 6);
            // 
            // recentFilesMenu
            // 
            recentFilesMenu.Name = "recentFilesMenu";
            recentFilesMenu.Size = new Size(191, 22);
            recentFilesMenu.Text = "Recent files";
            // 
            // recentManyFilesMenu
            // 
            recentManyFilesMenu.Name = "recentManyFilesMenu";
            recentManyFilesMenu.Size = new Size(191, 22);
            recentManyFilesMenu.Text = "Recent Many SQL files";
            // 
            // toolStripSeparator6
            // 
            toolStripSeparator6.Name = "toolStripSeparator6";
            toolStripSeparator6.Size = new Size(188, 6);
            // 
            // fileSaveMenuItem
            // 
            fileSaveMenuItem.Image = (Image)resources.GetObject("fileSaveMenuItem.Image");
            fileSaveMenuItem.ImageScaling = ToolStripItemImageScaling.None;
            fileSaveMenuItem.Name = "fileSaveMenuItem";
            fileSaveMenuItem.Size = new Size(191, 22);
            fileSaveMenuItem.Text = "Save  [Ctrl + S]";
            fileSaveMenuItem.Click += SaveOnTabEventHandler;
            // 
            // fileSaveAsMenuItem
            // 
            fileSaveAsMenuItem.Image = (Image)resources.GetObject("fileSaveAsMenuItem.Image");
            fileSaveAsMenuItem.ImageScaling = ToolStripItemImageScaling.None;
            fileSaveAsMenuItem.Name = "fileSaveAsMenuItem";
            fileSaveAsMenuItem.Size = new Size(191, 22);
            fileSaveAsMenuItem.Text = "Save as...";
            fileSaveAsMenuItem.Click += SaveAsToolStripMenuItem_Click;
            // 
            // fileSaveManyMenuItem
            // 
            fileSaveManyMenuItem.Name = "fileSaveManyMenuItem";
            fileSaveManyMenuItem.Size = new Size(191, 22);
            fileSaveManyMenuItem.Text = "Save Many SQL";
            fileSaveManyMenuItem.Click += SaveManyStrip_Click;
            // 
            // toolStripSeparator5
            // 
            toolStripSeparator5.Name = "toolStripSeparator5";
            toolStripSeparator5.Size = new Size(188, 6);
            // 
            // fileExitMenuItem
            // 
            fileExitMenuItem.Image = (Image)resources.GetObject("fileExitMenuItem.Image");
            fileExitMenuItem.ImageScaling = ToolStripItemImageScaling.None;
            fileExitMenuItem.Name = "fileExitMenuItem";
            fileExitMenuItem.Size = new Size(191, 22);
            fileExitMenuItem.Text = "Exit";
            fileExitMenuItem.Click += QuitToolStripMenuItem_Click;
            // 
            // editToolStripMenuItem
            // 
            editToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { findToolStripMenuItem, replaceToolStripMenuItem, toolStripMenuItem1, setSelectedAsReadonlyToolStripMenuItem, setSelectedAsWritableToolStripMenuItem, toolStripMenuItem8, collapseSelectedBlockToolStripMenuItem, toolStripMenuItem3, collapseAllregionToolStripMenuItem, exapndAllregionToolStripMenuItem, toolStripMenuItem2, increaseIndentSiftTabToolStripMenuItem, decreaseIndentTabToolStripMenuItem, toolStripMenuItem4, goBackwardCtrlToolStripMenuItem, goForwardCtrlShiftToolStripMenuItem, toolStripMenuItem5, autoIndentToolStripMenuItem, toolStripMenuItem6, goLeftBracketToolStripMenuItem, goRightBracketToolStripMenuItem, toolStripMenuItem7, miPrint });
            editToolStripMenuItem.ImageAlign = ContentAlignment.TopLeft;
            editToolStripMenuItem.ImageScaling = ToolStripItemImageScaling.None;
            editToolStripMenuItem.Name = "editToolStripMenuItem";
            editToolStripMenuItem.Padding = new Padding(0);
            editToolStripMenuItem.Size = new Size(31, 27);
            editToolStripMenuItem.Text = "Edit";
            editToolStripMenuItem.TextImageRelation = TextImageRelation.Overlay;
            // 
            // findToolStripMenuItem
            // 
            findToolStripMenuItem.Name = "findToolStripMenuItem";
            findToolStripMenuItem.Size = new Size(232, 22);
            findToolStripMenuItem.Text = "&Find [Ctrl+F]";
            findToolStripMenuItem.Click += FindToolStripMenuItem_Click;
            // 
            // replaceToolStripMenuItem
            // 
            replaceToolStripMenuItem.Name = "replaceToolStripMenuItem";
            replaceToolStripMenuItem.Size = new Size(232, 22);
            replaceToolStripMenuItem.Text = "&Replace [Ctrl+H]";
            replaceToolStripMenuItem.Click += ReplaceToolStripMenuItem_Click;
            // 
            // toolStripMenuItem1
            // 
            toolStripMenuItem1.Name = "toolStripMenuItem1";
            toolStripMenuItem1.Size = new Size(229, 6);
            // 
            // setSelectedAsReadonlyToolStripMenuItem
            // 
            setSelectedAsReadonlyToolStripMenuItem.Name = "setSelectedAsReadonlyToolStripMenuItem";
            setSelectedAsReadonlyToolStripMenuItem.Size = new Size(232, 22);
            setSelectedAsReadonlyToolStripMenuItem.Text = "Set selected as readonly";
            setSelectedAsReadonlyToolStripMenuItem.Click += SetSelectedAsReadonlyToolStripMenuItem_Click;
            // 
            // setSelectedAsWritableToolStripMenuItem
            // 
            setSelectedAsWritableToolStripMenuItem.Name = "setSelectedAsWritableToolStripMenuItem";
            setSelectedAsWritableToolStripMenuItem.Size = new Size(232, 22);
            setSelectedAsWritableToolStripMenuItem.Text = "Set selected as writable";
            setSelectedAsWritableToolStripMenuItem.Click += SetSelectedAsWritableToolStripMenuItem_Click;
            // 
            // toolStripMenuItem8
            // 
            toolStripMenuItem8.Name = "toolStripMenuItem8";
            toolStripMenuItem8.Size = new Size(229, 6);
            // 
            // collapseSelectedBlockToolStripMenuItem
            // 
            collapseSelectedBlockToolStripMenuItem.Name = "collapseSelectedBlockToolStripMenuItem";
            collapseSelectedBlockToolStripMenuItem.Size = new Size(232, 22);
            collapseSelectedBlockToolStripMenuItem.Text = "Collapse selected block";
            collapseSelectedBlockToolStripMenuItem.Click += CollapseSelectedBlockToolStripMenuItem_Click;
            // 
            // toolStripMenuItem3
            // 
            toolStripMenuItem3.Name = "toolStripMenuItem3";
            toolStripMenuItem3.Size = new Size(229, 6);
            // 
            // collapseAllregionToolStripMenuItem
            // 
            collapseAllregionToolStripMenuItem.Name = "collapseAllregionToolStripMenuItem";
            collapseAllregionToolStripMenuItem.Size = new Size(232, 22);
            collapseAllregionToolStripMenuItem.Text = "Collapse all --region [Ctrl + R]";
            collapseAllregionToolStripMenuItem.Click += CollapseAllregionToolStripMenuItem_Click;
            // 
            // exapndAllregionToolStripMenuItem
            // 
            exapndAllregionToolStripMenuItem.Name = "exapndAllregionToolStripMenuItem";
            exapndAllregionToolStripMenuItem.Size = new Size(232, 22);
            exapndAllregionToolStripMenuItem.Text = "Expand all --region [Ctrl + E]";
            exapndAllregionToolStripMenuItem.Click += ExapndAllregionToolStripMenuItem_Click;
            // 
            // toolStripMenuItem2
            // 
            toolStripMenuItem2.Name = "toolStripMenuItem2";
            toolStripMenuItem2.Size = new Size(229, 6);
            // 
            // increaseIndentSiftTabToolStripMenuItem
            // 
            increaseIndentSiftTabToolStripMenuItem.Name = "increaseIndentSiftTabToolStripMenuItem";
            increaseIndentSiftTabToolStripMenuItem.Size = new Size(232, 22);
            increaseIndentSiftTabToolStripMenuItem.Text = "Increase Indent [Tab]";
            increaseIndentSiftTabToolStripMenuItem.Click += IncreaseIndentSiftTabToolStripMenuItem_Click;
            // 
            // decreaseIndentTabToolStripMenuItem
            // 
            decreaseIndentTabToolStripMenuItem.Name = "decreaseIndentTabToolStripMenuItem";
            decreaseIndentTabToolStripMenuItem.Size = new Size(232, 22);
            decreaseIndentTabToolStripMenuItem.Text = "Decrease Indent [Shift + Tab]";
            decreaseIndentTabToolStripMenuItem.Click += DecreaseIndentTabToolStripMenuItem_Click;
            // 
            // toolStripMenuItem4
            // 
            toolStripMenuItem4.Name = "toolStripMenuItem4";
            toolStripMenuItem4.Size = new Size(229, 6);
            // 
            // goBackwardCtrlToolStripMenuItem
            // 
            goBackwardCtrlToolStripMenuItem.Name = "goBackwardCtrlToolStripMenuItem";
            goBackwardCtrlToolStripMenuItem.Size = new Size(232, 22);
            goBackwardCtrlToolStripMenuItem.Text = "Go Backward [Ctrl+ -]";
            goBackwardCtrlToolStripMenuItem.Click += GoBackwardCtrlToolStripMenuItem_Click;
            // 
            // goForwardCtrlShiftToolStripMenuItem
            // 
            goForwardCtrlShiftToolStripMenuItem.Name = "goForwardCtrlShiftToolStripMenuItem";
            goForwardCtrlShiftToolStripMenuItem.Size = new Size(232, 22);
            goForwardCtrlShiftToolStripMenuItem.Text = "Go Forward [Ctrl+Shift+ -]";
            goForwardCtrlShiftToolStripMenuItem.Click += GoForwardCtrlShiftToolStripMenuItem_Click;
            // 
            // toolStripMenuItem5
            // 
            toolStripMenuItem5.Name = "toolStripMenuItem5";
            toolStripMenuItem5.Size = new Size(229, 6);
            // 
            // autoIndentToolStripMenuItem
            // 
            autoIndentToolStripMenuItem.Name = "autoIndentToolStripMenuItem";
            autoIndentToolStripMenuItem.Size = new Size(232, 22);
            autoIndentToolStripMenuItem.Text = "Auto Indent selected text";
            autoIndentToolStripMenuItem.Click += AutoIndentToolStripMenuItem_Click;
            // 
            // toolStripMenuItem6
            // 
            toolStripMenuItem6.Name = "toolStripMenuItem6";
            toolStripMenuItem6.Size = new Size(229, 6);
            // 
            // goLeftBracketToolStripMenuItem
            // 
            goLeftBracketToolStripMenuItem.Name = "goLeftBracketToolStripMenuItem";
            goLeftBracketToolStripMenuItem.Size = new Size(232, 22);
            goLeftBracketToolStripMenuItem.Text = "Go Left Bracket";
            goLeftBracketToolStripMenuItem.Click += GoLeftBracketToolStripMenuItem_Click;
            // 
            // goRightBracketToolStripMenuItem
            // 
            goRightBracketToolStripMenuItem.Name = "goRightBracketToolStripMenuItem";
            goRightBracketToolStripMenuItem.Size = new Size(232, 22);
            goRightBracketToolStripMenuItem.Text = "Go Right Bracket";
            goRightBracketToolStripMenuItem.Click += GoRightBracketToolStripMenuItem_Click;
            // 
            // toolStripMenuItem7
            // 
            toolStripMenuItem7.Name = "toolStripMenuItem7";
            toolStripMenuItem7.Size = new Size(229, 6);
            // 
            // miPrint
            // 
            miPrint.Name = "miPrint";
            miPrint.Size = new Size(232, 22);
            miPrint.Text = "Print...";
            miPrint.Click += MiPrint_Click;
            // 
            // databaseToolStripMenuItem
            // 
            databaseToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { addNewConnectionToolStripMenuItem, queryWatchToolStripMenuItem, historyToolStripMenuItem });
            databaseToolStripMenuItem.ImageAlign = ContentAlignment.TopLeft;
            databaseToolStripMenuItem.ImageScaling = ToolStripItemImageScaling.None;
            databaseToolStripMenuItem.Name = "databaseToolStripMenuItem";
            databaseToolStripMenuItem.Padding = new Padding(0);
            databaseToolStripMenuItem.Size = new Size(59, 27);
            databaseToolStripMenuItem.Text = "Database";
            databaseToolStripMenuItem.TextImageRelation = TextImageRelation.Overlay;
            // 
            // addNewConnectionToolStripMenuItem
            // 
            addNewConnectionToolStripMenuItem.Image = JustData.Properties.Resources.Create;
            addNewConnectionToolStripMenuItem.ImageScaling = ToolStripItemImageScaling.None;
            addNewConnectionToolStripMenuItem.Name = "addNewConnectionToolStripMenuItem";
            addNewConnectionToolStripMenuItem.Size = new Size(184, 22);
            addNewConnectionToolStripMenuItem.Text = "Add new connection";
            addNewConnectionToolStripMenuItem.Click += AddNewConnectionToolStripMenuItem_Click;
            // 
            // queryWatchToolStripMenuItem
            // 
            queryWatchToolStripMenuItem.Image = JustData.Properties.Resources.magnifier;
            queryWatchToolStripMenuItem.ImageScaling = ToolStripItemImageScaling.None;
            queryWatchToolStripMenuItem.Name = "queryWatchToolStripMenuItem";
            queryWatchToolStripMenuItem.Size = new Size(184, 22);
            queryWatchToolStripMenuItem.Text = "Query Watch";
            queryWatchToolStripMenuItem.Click += QueryWatchToolStripMenuItem_Click;
            // 
            // historyToolStripMenuItem
            // 
            historyToolStripMenuItem.Image = (Image)resources.GetObject("historyToolStripMenuItem.Image");
            historyToolStripMenuItem.ImageScaling = ToolStripItemImageScaling.None;
            historyToolStripMenuItem.Name = "historyToolStripMenuItem";
            historyToolStripMenuItem.Size = new Size(184, 22);
            historyToolStripMenuItem.Text = "Query History";
            historyToolStripMenuItem.Click += HistoryToolStripMenuItem_Click;
            // 
            // ImportToolStripMenuItem
            // 
            ImportToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { XLSXtoolStripMenuItem, recentXlsx });
            ImportToolStripMenuItem.ImageAlign = ContentAlignment.TopLeft;
            ImportToolStripMenuItem.ImageScaling = ToolStripItemImageScaling.None;
            ImportToolStripMenuItem.Name = "ImportToolStripMenuItem";
            ImportToolStripMenuItem.Padding = new Padding(0);
            ImportToolStripMenuItem.Size = new Size(76, 27);
            ImportToolStripMenuItem.Text = "Import from";
            ImportToolStripMenuItem.TextImageRelation = TextImageRelation.Overlay;
            // 
            // XLSXtoolStripMenuItem
            // 
            XLSXtoolStripMenuItem.Name = "XLSXtoolStripMenuItem";
            XLSXtoolStripMenuItem.Size = new Size(147, 22);
            XLSXtoolStripMenuItem.Text = "xlsx/xlsb/csv";
            XLSXtoolStripMenuItem.Click += XLSXtoolStripMenuItem_Click;
            // 
            // recentXlsx
            // 
            recentXlsx.Name = "recentXlsx";
            recentXlsx.Size = new Size(147, 22);
            recentXlsx.Text = "Recent file";
            // 
            // changeHotkeysToolStripMenuItem
            // 
            changeHotkeysToolStripMenuItem.ImageAlign = ContentAlignment.TopLeft;
            changeHotkeysToolStripMenuItem.ImageScaling = ToolStripItemImageScaling.None;
            changeHotkeysToolStripMenuItem.Name = "changeHotkeysToolStripMenuItem";
            changeHotkeysToolStripMenuItem.Padding = new Padding(0);
            changeHotkeysToolStripMenuItem.Size = new Size(75, 27);
            changeHotkeysToolStripMenuItem.Text = "Get Hotkeys";
            changeHotkeysToolStripMenuItem.TextImageRelation = TextImageRelation.Overlay;
            changeHotkeysToolStripMenuItem.Visible = false;
            changeHotkeysToolStripMenuItem.Click += ChangeHotkeysToolStripMenuItem_Click;
            // 
            // optionsToolStripMenuItem
            // 
            optionsToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { mapToolStripMenuItem, showDataFolderMenuItem, clearDataFolderMenuItem });
            optionsToolStripMenuItem.ImageAlign = ContentAlignment.TopLeft;
            optionsToolStripMenuItem.ImageScaling = ToolStripItemImageScaling.None;
            optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
            optionsToolStripMenuItem.Padding = new Padding(0);
            optionsToolStripMenuItem.Size = new Size(53, 27);
            optionsToolStripMenuItem.Text = "Settings";
            optionsToolStripMenuItem.TextImageRelation = TextImageRelation.Overlay;
            // 
            // mapToolStripMenuItem
            // 
            mapToolStripMenuItem.Name = "mapToolStripMenuItem";
            mapToolStripMenuItem.Size = new Size(216, 22);
            mapToolStripMenuItem.Text = "Show/hide document map";
            mapToolStripMenuItem.Click += Map_Click;
            // 
            // settingsToolStripMenuItem
            // 
            settingsToolStripMenuItem.Image = JustData.Properties.Resources.wrench;
            settingsToolStripMenuItem.ImageAlign = ContentAlignment.TopLeft;
            settingsToolStripMenuItem.ImageScaling = ToolStripItemImageScaling.None;
            settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
            settingsToolStripMenuItem.Padding = new Padding(0);
            settingsToolStripMenuItem.Size = new Size(75, 27);
            settingsToolStripMenuItem.Text = "Preferences";
            settingsToolStripMenuItem.TextImageRelation = TextImageRelation.Overlay;
            settingsToolStripMenuItem.Click += Ustaw_Click;
            // 
            // showDataFolderMenuItem
            // 
            showDataFolderMenuItem.Image = (Image)resources.GetObject("showDataFolderMenuItem.Image");
            showDataFolderMenuItem.ImageScaling = ToolStripItemImageScaling.None;
            showDataFolderMenuItem.Name = "showDataFolderMenuItem";
            showDataFolderMenuItem.Size = new Size(216, 22);
            showDataFolderMenuItem.Text = "Show data folder";
            showDataFolderMenuItem.Click += ShowDataFolderMenuItem_Click;
            // 
            // clearDataFolderMenuItem
            // 
            clearDataFolderMenuItem.Image = (Image)resources.GetObject("clearDataFolderMenuItem.Image");
            clearDataFolderMenuItem.ImageScaling = ToolStripItemImageScaling.None;
            clearDataFolderMenuItem.Name = "clearDataFolderMenuItem";
            clearDataFolderMenuItem.Size = new Size(216, 22);
            clearDataFolderMenuItem.Text = "Clean up data folder";
            clearDataFolderMenuItem.Click += ClearDataFolderMenuItem_Click;
            // 
            // tsmLicence
            // 
            tsmLicence.ImageAlign = ContentAlignment.TopLeft;
            tsmLicence.ImageScaling = ToolStripItemImageScaling.None;
            tsmLicence.Name = "tsmLicence";
            tsmLicence.Padding = new Padding(0);
            tsmLicence.Size = new Size(107, 27);
            tsmLicence.Text = "About JustyBaseLegacy";
            tsmLicence.TextImageRelation = TextImageRelation.Overlay;
            tsmLicence.Click += TsmAbout_Click;
            // 
            // themeToolStripMenuItem
            // 
            themeToolStripMenuItem.DisplayStyle = ToolStripItemDisplayStyle.Image;
            themeToolStripMenuItem.Image = (Image)resources.GetObject("themeToolStripMenuItem.Image");
            themeToolStripMenuItem.ImageAlign = ContentAlignment.TopCenter;
            themeToolStripMenuItem.Name = "themeToolStripMenuItem";
            themeToolStripMenuItem.Padding = new Padding(0);
            themeToolStripMenuItem.Size = new Size(27, 27);
            themeToolStripMenuItem.Text = "Theme";
            themeToolStripMenuItem.ToolTipText = "Change theme";
            themeToolStripMenuItem.Click += ThemeToolStripMenuItem_Click;
            // 
            // cSharpbuiltinHighlighterToolStripMenuItem
            // 
            cSharpbuiltinHighlighterToolStripMenuItem.Name = "cSharpbuiltinHighlighterToolStripMenuItem";
            cSharpbuiltinHighlighterToolStripMenuItem.Size = new Size(32, 19);
            // 
            // splitContainer1
            // 
            splitContainer1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            splitContainer1.Location = new Point(3, 36);
            splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(_leftTabs);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(_tabControlMain);
            splitContainer1.SplitterDistance = 143;
            splitContainer1.TabIndex = 11;
            splitContainer1.Text = "splitContainer1";
            // 
            // ZakladkiPoLewej
            // 
            _leftTabs.Alignment = TabAlignment.Left;
            _leftTabs.Controls.Add(databaseTabPage);
            _leftTabs.Controls.Add(tabPageFiles);
            _leftTabs.Controls.Add(tabPageVariables);
            _leftTabs.Controls.Add(tabPageLegend);
            _leftTabs.Dock = DockStyle.Fill;
            _leftTabs.DrawMode = TabDrawMode.OwnerDrawFixed;
            _leftTabs.ImeMode = ImeMode.Hiragana;
            _leftTabs.Location = new Point(0, 0);
            _leftTabs.Margin = new Padding(3, 3, 0, 3);
            _leftTabs.Multiline = true;
            _leftTabs.Name = "ZakladkiPoLewej";
            _leftTabs.Padding = new Point(0, 0);
            _leftTabs.SelectedIndex = 0;
            _leftTabs.Size = new Size(143, 661);
            _leftTabs.TabIndex = 1;
            _leftTabs.DrawItem += _tabControlDrawingHandler.LeftTabsDrawItem;
            _leftTabs.SelectedIndexChanged += _leftTabsSelectedIndexChanged;
            // 
            // databaseTabPage
            // 
            //databaseTabPage.Location = new Point(27, 4);
            databaseTabPage.Name = "databaseTabPage";
            databaseTabPage.Padding = new Padding(0);
            databaseTabPage.Size = new Size(275, 653);
            databaseTabPage.TabIndex = 0;
            databaseTabPage.Text = "Database";
            databaseTabPage.UseVisualStyleBackColor = true;
            // 
            // databaseExplorerControl removed — migrated to MvvmDatabaseExplorerControl
            //
            // 
            // databaseContextMenuStrip
            // 
            databaseContextMenuStrip.Items.AddRange(new ToolStripItem[] { refreshTableListItem, collapseDatabaseMenuItem, groomDatabaseToolStripMenuItem, DbSizeToolStripMenuItem });
            databaseContextMenuStrip.Name = "databaseContextMenuStrip";
            databaseContextMenuStrip.Size = new Size(163, 92);
            // 
            // refreshTableListItem
            // 
            refreshTableListItem.Name = "refreshTableListItem";
            refreshTableListItem.Size = new Size(162, 22);
            refreshTableListItem.Text = "Refresh table list";
            refreshTableListItem.Click += RefreshTableList;
            // 
            // collapseDatabaseMenuItem
            // 
            collapseDatabaseMenuItem.Name = "collapseDatabaseMenuItem";
            collapseDatabaseMenuItem.Size = new Size(162, 22);
            collapseDatabaseMenuItem.Text = "Collapse All";
            collapseDatabaseMenuItem.Click += CollapseDatabaseMenuItem_Click;
            // 
            // groomDatabaseToolStripMenuItem
            // 
            groomDatabaseToolStripMenuItem.Name = "groomDatabaseToolStripMenuItem";
            groomDatabaseToolStripMenuItem.Size = new Size(162, 22);
            groomDatabaseToolStripMenuItem.Text = "Groom Database";
            // DbSizeToolStripMenuItem
            // 
            DbSizeToolStripMenuItem.Name = "DbSizeToolStripMenuItem";
            DbSizeToolStripMenuItem.Size = new Size(162, 22);
            DbSizeToolStripMenuItem.Text = "Database Size";
            // 
            // imageList1
            // 
            imageList1.ColorDepth = ColorDepth.Depth32Bit;
            imageList1.TransparentColor = Color.Transparent;
            imageList1.Images.Add("database.png", JustData.Properties.Resources.database);
            imageList1.Images.Add("database_table.png", JustData.Properties.Resources.database_table);
            imageList1.Images.Add("application_view_columns.png", JustData.Properties.Resources.application_view_columns);
            imageList1.Images.Add("bug.png", JustData.Properties.Resources.bug);
            imageList1.Images.Add("table_key.png", JustData.Properties.Resources.table_key);
            imageList1.Images.Add("bullet_white.png", JustData.Properties.Resources.bullet_white);
            imageList1.Images.Add("weather_sun.png", JustData.Properties.Resources.weather_sun);
            imageList1.Images.Add("Table.bmp", JustData.Properties.Resources.table);
            imageList1.Images.Add("application_view_tile.png", JustData.Properties.Resources.application_view_tile);
            imageList1.Images.Add("table_link.png", JustData.Properties.Resources.table_link);
            imageList1.Images.Add("text_columns.png", JustData.Properties.Resources.text_columns);
            imageList1.Images.Add("bullet_blue.png", Resources.bullet_blue);
            imageList1.Images.Add("arrow_switch.png", Resources.arrow_switch);
            imageList1.Images.Add("car.png", Resources.car);
            imageList1.Images.Add("application_lightning.png", Resources.application_lightning);
            imageList1.Images.Add("sum.png", Resources.sum);
            imageList1.Images.Add("arrow_right.png", Resources.arrow_right);
            imageList1.Images.Add("arrow_rotate_anticlockwise.png", Resources.arrow_rotate_anticlockwise);
            imageList1.Images.Add("monitor_lightning.png", Resources.monitor_lightning);
            imageList1.Images.Add("arrow_rotate_clockwise.png", Resources.arrow_rotate_clockwise);
            imageList1.Images.Add("server_database.png", Resources.server_database);
            imageList1.Images.Add("folder_user.png", Resources.folder_user);
            imageList1.Images.Add("box.png", Resources.box);
            imageList1.Images.Add("server_chart.png", Resources.server_chart);
            imageList1.Images.Add("netezza_icon16.png", Resources.netezza_icon16);
            imageList1.Images.Add("db2v2.png", Resources.db2v2);
            imageList1.Images.Add("oracle.png", Resources.oracle);
            imageList1.Images.Add("PostgreSQL.png", Resources.PostgreSQL);
            imageList1.Images.Add("SQLite.png", Resources.SQLite);
            imageList1.Images.Add("MySql.png", Resources.MySql);
            imageList1.Images.Add("MSSQL16x16.png", Resources.MSSQL16x16);
            imageList1.Images.Add("Folder.png", Resources.folder);
            imageList1.Images.Add("Key.png", Resources.Key);
            imageList1.Images.Add("folder_magnify.png", Resources.folder_magnify);
            imageList1.Images.Add("server_connect.png", Resources.server_connect);
            imageList1.Images.Add("table_column.png", Resources.table_column);
            imageList1.Images.Add("msaccess_icon16x16.png", Resources.msaccess_icon16x16);
            imageList1.Images.Add("hourglass.png", Resources.Hourglass);
            // 
            // 
            // tabPagePliki
            // 
            tabPageFiles.Location = new Point(27, 4);
            tabPageFiles.Name = "tabPageFiles";
            tabPageFiles.Padding = new Padding(3);
            tabPageFiles.TabIndex = 1;
            tabPageFiles.Text = "Files";
            tabPageFiles.UseVisualStyleBackColor = true;
            // 
            // tabPageVariables
            // 
            tabPageVariables.Location = new Point(27, 4);
            tabPageVariables.Name = "tabPageVariables";
            tabPageVariables.Padding = new Padding(3);
            tabPageVariables.Size = new Size(275, 653);
            tabPageVariables.TabIndex = 3;
            tabPageVariables.Text = "Variables";
            tabPageVariables.UseVisualStyleBackColor = true;
            // 
            // tabPageLegenda
            // 
            tabPageLegend.Location = new Point(27, 4);
            tabPageLegend.Name = "tabPageLegenda";
            tabPageLegend.Padding = new Padding(3);
            tabPageLegend.Size = new Size(275, 653);
            tabPageLegend.TabIndex = 2;
            tabPageLegend.Text = "Outline";
            tabPageLegend.UseVisualStyleBackColor = true;
            // 
            // 
            // tabControlMain
            // 
            _tabControlMain.AllowDrop = true;
            _tabControlMain.ContextMenuStrip = tabContextMenuStrip;
            _tabControlMain.Dock = DockStyle.Fill;
            _tabControlMain.DrawMode = TabDrawMode.OwnerDrawFixed;
            _tabControlMain.Location = new Point(0, 0);
            _tabControlMain.Multiline = true;
            _tabControlMain.Name = "tabControlMain";
            _tabControlMain.Padding = new Point(20, 4);
            _tabControlMain.SelectedIndex = 0;
            _tabControlMain.Size = new Size(919, 661);
            _tabControlMain.TabIndex = 0;
            _tabControlMain.SelectedIndexChanged += TabControlMain_SelectedIndexChanged;
            _tabControlMain.ControlRemoved += tabControlMain_ControlRemoved;
            _tabControlMain.DragDrop += tabControlMain_DragDrop;
            _tabControlMain.DragOver += tabControlMainDragOver;
            _tabControlMain.KeyDown += tabControlMain_KeyDown;
            _tabControlMain.MouseDown += tabControlMain_MouseDown;

            // 
            // tabContextMenuStrip
            // 
            tabContextMenuStrip.Items.AddRange(new ToolStripItem[] { closeAllTabsMenuItem, closeOtherTabsMenuItem, cmsOpenInExplorer, cmsSave, cmsRenameTab });
            tabContextMenuStrip.Name = "tabContextMenuStrip";
            tabContextMenuStrip.Size = new Size(175, 114);
            // 
            // closeAllTabsMenuItem
            // 
            closeAllTabsMenuItem.Name = "closeAllTabsMenuItem";
            closeAllTabsMenuItem.Size = new Size(174, 22);
            closeAllTabsMenuItem.Text = "Close All";
            closeAllTabsMenuItem.Click += RemoveAllTabsEventHandler;
            // 
            // closeOtherTabsMenuItem
            // 
            closeOtherTabsMenuItem.Name = "closeOtherTabsMenuItem";
            closeOtherTabsMenuItem.Size = new Size(174, 22);
            closeOtherTabsMenuItem.Text = "Close except active";
            closeOtherTabsMenuItem.Click += CloseOtherTabsEventHandler;
            // 
            // cmsOpenInExplorer
            // 
            cmsOpenInExplorer.Name = "cmsOpenInExplorer";
            cmsOpenInExplorer.Size = new Size(174, 22);
            cmsOpenInExplorer.Text = "Open in explorer";
            cmsOpenInExplorer.Click += OpenInExplorerEvenHandler;
            // 
            // cmsSave
            // 
            cmsSave.Name = "cmsSave";
            cmsSave.Size = new Size(174, 22);
            cmsSave.Text = "Save";
            cmsSave.Click += SaveOnTabEventHandler;
            // 
            // cmsRenameTab
            // 
            cmsRenameTab.Name = "cmsRenameTab";
            cmsRenameTab.Size = new Size(174, 22);
            cmsRenameTab.Text = "Rename Tab";
            cmsRenameTab.Click += RenameTabEvenHandler;
            // 
            // runToCursorToolStripMenuItem
            // 
            runToCursorToolStripMenuItem.Name = "runToCursorToolStripMenuItem";
            runToCursorToolStripMenuItem.Size = new Size(262, 22);
            runToCursorToolStripMenuItem.Text = "Run To Cursor [Ctrl + F10]";
            runToCursorToolStripMenuItem.Click += runToCursorToolStripMenuItem_Click;
            // 
            // imageListFiles
            // 
            imageListFiles.ColorDepth = ColorDepth.Depth32Bit;
            imageListFiles.TransparentColor = Color.Transparent;
            imageListFiles.Images.AddRange(new Image[] {
            Resources.folder
              ,Resources.page_white_code
              ,Resources.bullet_red
              ,Resources.folder_heart
              ,Resources.folder_add
              ,Resources.add
              ,Resources.delete
              ,Resources.calendar
              ,Resources.script
              ,Resources.clock
              ,Resources.script_add
              ,Resources.script_delete
              ,Resources.clock_add
              ,Resources.clock_delete
              ,Resources.clock_edit
              ,Resources.clock_error
              ,Resources.clock_go
              ,Resources.clock_link
              ,Resources.clock_pause
              ,Resources.clock_play
              ,Resources.clock_red
              ,Resources.clock_stop
              ,Resources.script_code
              ,Resources.script_code_red
              ,Resources.script_edit
              ,Resources.script_error
              ,Resources.script_gear
              ,Resources.script_go
              ,Resources.script_key
              ,Resources.script_lightning
              ,Resources.script_link
              ,Resources.script_palette
              ,Resources.script_save
              ,Resources.folder });
            // 
            // ofdMain
            // 
            ofdMain.DefaultExt = "sql";
            ofdMain.Filter = "SQL files(*.sql)|*.sql";
            // 
            // tabPage1
            // 
            tabPage1.Location = new Point(4, 24);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new Padding(3);
            tabPage1.Size = new Size(755, 432);
            tabPage1.TabIndex = 0;
            tabPage1.Text = "tabPage1";
            tabPage1.UseVisualStyleBackColor = true;
            // 
            // saveFileDialogSQL
            // 
            saveFileDialogSQL.DefaultExt = "sql";
            saveFileDialogSQL.Filter = "SQL files(*.sql)|*.sql";
            // 
            // cmResults
            // 
            cmResults.Items.AddRange(new ToolStripItem[] { zamknijKarte, zamknijWszytkieKarty, zamknijReszte, renameTab, verticalHorizontalToolStripMenuItem });
            cmResults.Name = "contextMenuWyniki";
            cmResults.Size = new Size(184, 136);
            // 
            // 
            // zamknijKarte
            // 
            zamknijKarte.Name = "zamknijKarte";
            zamknijKarte.Size = new Size(183, 22);
            zamknijKarte.Text = "Close";
            zamknijKarte.Click += DeleteTabEventHandler;
            // 
            // zamknijWszytkieKarty
            // 
            zamknijWszytkieKarty.Name = "zamknijWszytkieKarty";
            zamknijWszytkieKarty.Size = new Size(183, 22);
            zamknijWszytkieKarty.Text = "Close All";
            zamknijWszytkieKarty.Click += CloseAllResultTabsEventHandler;
            // 
            // zamknijReszte
            // 
            zamknijReszte.Name = "zamknijReszte";
            zamknijReszte.Size = new Size(183, 22);
            zamknijReszte.Text = "Close Others";
            zamknijReszte.Click += CloseOthersEventHandler;
            // 
            // renameTab
            // 
            renameTab.Name = "renameTab";
            renameTab.Size = new Size(183, 22);
            renameTab.Text = "Rename selected tab";
            renameTab.Click += RenameResultTabEventHandler;
            // 
            // verticalHorizontalToolStripMenuItem
            // 
            verticalHorizontalToolStripMenuItem.Name = "verticalHorizontalToolStripMenuItem";
            verticalHorizontalToolStripMenuItem.Size = new Size(183, 22);
            verticalHorizontalToolStripMenuItem.Text = "Vertical/Horizontal";
            verticalHorizontalToolStripMenuItem.Click += VerticalHorizontalToolStripMenuItem_Click;
            // 
            // cmMain
            // 
            cmMain.Items.AddRange(new ToolStripItem[] { cutToolStripMenuItem, copyToolStripMenuItem, copyRawToolStripMenuItem, pasteToolStripMenuItem, toolStripSeparator3, zakomentujToolStrip, odkomentujToolStrip, toolStripSeparator4, EksportytoolStripMenuItem11, addDollarSignMenuItem, formatSQL, addAliases, makeCodeToTempTable, saveAsSnippet, toolStripSeparator9, pasteAsSelect, importFromClipboard, inRaw, inText, tsmiWordWrap });
            cmMain.Name = "cmMain";
            cmMain.Size = new Size(219, 418);
            // 
            // cutToolStripMenuItem
            // 
            cutToolStripMenuItem.Name = "cutToolStripMenuItem";
            cutToolStripMenuItem.Size = new Size(218, 22);
            cutToolStripMenuItem.Text = "Cut";
            cutToolStripMenuItem.Click += CutToolStripMenuItem_Click;
            // 
            // copyToolStripMenuItem
            // 
            copyToolStripMenuItem.Name = "copyToolStripMenuItem";
            copyToolStripMenuItem.Size = new Size(218, 22);
            copyToolStripMenuItem.Text = "Copy";
            copyToolStripMenuItem.Click += CopyToolStripMenuItem_Click;
            // 
            // copyRawToolStripMenuItem
            // 
            copyRawToolStripMenuItem.Name = "copyRawToolStripMenuItem";
            copyRawToolStripMenuItem.Size = new Size(218, 22);
            copyRawToolStripMenuItem.Text = "Copy without formatting";
            copyRawToolStripMenuItem.Click += CopyRawToolStripMenuItem_Click;
            // 
            // pasteToolStripMenuItem
            // 
            pasteToolStripMenuItem.Name = "pasteToolStripMenuItem";
            pasteToolStripMenuItem.Size = new Size(218, 22);
            pasteToolStripMenuItem.Text = "Paste";
            pasteToolStripMenuItem.Click += PasteToolStripMenuItem_Click;
            // 
            // toolStripSeparator3
            // 
            toolStripSeparator3.Name = "toolStripSeparator3";
            toolStripSeparator3.Size = new Size(215, 6);
            // 
            // zakomentujToolStrip
            // 
            zakomentujToolStrip.Name = "zakomentujToolStrip";
            zakomentujToolStrip.Size = new Size(218, 22);
            zakomentujToolStrip.Text = "Comment";
            zakomentujToolStrip.Click += CommentSelectedLinesToolStripMenuItem_Click;
            // 
            // odkomentujToolStrip
            // 
            odkomentujToolStrip.Name = "odkomentujToolStrip";
            odkomentujToolStrip.Size = new Size(218, 22);
            odkomentujToolStrip.Text = "Uncomment";
            odkomentujToolStrip.Click += UncommentSelectedLinesToolStripMenuItem_Click;
            // 
            // toolStripSeparator4
            // 
            toolStripSeparator4.Name = "toolStripSeparator4";
            toolStripSeparator4.Size = new Size(215, 6);
            // 
            // EksportytoolStripMenuItem11
            // 
            EksportytoolStripMenuItem11.DropDownItems.AddRange(new ToolStripItem[] { CSVtoolStripMenuItem12, XLSXStripMenuItem12 });
            EksportytoolStripMenuItem11.Name = "EksportytoolStripMenuItem11";
            EksportytoolStripMenuItem11.Size = new Size(218, 22);
            EksportytoolStripMenuItem11.Text = "Export results...";
            // 
            // CSVtoolStripMenuItem12
            // 
            CSVtoolStripMenuItem12.Name = "CSVtoolStripMenuItem12";
            CSVtoolStripMenuItem12.Size = new Size(146, 22);
            CSVtoolStripMenuItem12.Text = "Results to csv";
            CSVtoolStripMenuItem12.Click += ExportToCsvClick;
            // 
            // XLSXStripMenuItem12
            // 
            XLSXStripMenuItem12.Name = "XLSXStripMenuItem12";
            XLSXStripMenuItem12.Size = new Size(146, 22);
            XLSXStripMenuItem12.Text = "Results to xlsx";
            XLSXStripMenuItem12.Click += ExportToXlsxInlineClick;
            // 
            // addDollarSignMenuItem
            // 
            addDollarSignMenuItem.Name = "addDollarSignMenuItem";
            addDollarSignMenuItem.Size = new Size(218, 22);
            addDollarSignMenuItem.Text = "text -> $text";
            addDollarSignMenuItem.Click += AddDollarSign_Click;
            // 
            // formatSQL
            // 
            formatSQL.Name = "formatSQL";
            formatSQL.Size = new Size(218, 22);
            formatSQL.Text = "Format SQL";
            formatSQL.Click += FormatSQL_Click;
            // 
            // addAliases
            // 
            addAliases.Name = "addAliases";
            addAliases.Size = new Size(218, 22);
            addAliases.Text = "Try add aliases for selection";
            addAliases.Click += AddAliases_Click;
            // 
            // makeCodeToTempTable
            // 
            makeCodeToTempTable.Name = "makeCodeToTempTable";
            makeCodeToTempTable.Size = new Size(218, 22);
            makeCodeToTempTable.Text = "Selection to temp table";
            makeCodeToTempTable.Click += MakeCodeToTempTable_Click;
            // 
            // saveAsSnippet
            // 
            saveAsSnippet.Name = "saveAsSnippet";
            saveAsSnippet.Size = new Size(218, 22);
            saveAsSnippet.Text = "Save selection as snippet";
            saveAsSnippet.Click += SaveAsSnippet_Click;
            // 
            // toolStripSeparator9
            // 
            toolStripSeparator9.Name = "toolStripSeparator9";
            toolStripSeparator9.Size = new Size(215, 6);
            // 
            // pasteAsSelect
            // 
            pasteAsSelect.Name = "pasteAsSelect";
            pasteAsSelect.Size = new Size(218, 22);
            pasteAsSelect.Text = "Paste clip as Select/Union";
            pasteAsSelect.Click += pasteAsSelect_Click;
            // 
            // importFromClipboard
            // 
            importFromClipboard.Name = "importFromClipboard";
            importFromClipboard.Size = new Size(218, 22);
            importFromClipboard.Text = "Import from clipboard";
            importFromClipboard.Click += ImportFromClipboard_Click;
            // 
            // inRaw
            // 
            inRaw.Name = "inRaw";
            inRaw.Size = new Size(218, 22);
            inRaw.Text = "1 2-> (1,2)";
            inRaw.Click += PasteIn;
            // 
            // inText
            // 
            inText.Name = "inText";
            inText.Size = new Size(218, 22);
            inText.Text = "A B -> ('A','B')";
            inText.Click += PasteIn;
            // 
            // tsmiWordWrap
            // 
            tsmiWordWrap.Name = "tsmiWordWrap";
            tsmiWordWrap.Size = new Size(218, 22);
            tsmiWordWrap.Text = "Word wrap";
            tsmiWordWrap.Click += TsmiWordWrap_Click;
            // 
            // manySaveFileDialog
            // 
            manySaveFileDialog.DefaultExt = "sql";
            manySaveFileDialog.Filter = "Many SQL(*.manysql)|*.manysql";
            // 
            // manyOpenFileDialog
            // 
            manyOpenFileDialog.DefaultExt = "sql";
            manyOpenFileDialog.Filter = "Many SQL(*.manysql)|*.manysql";
            // 
            // cmGridContextMenuStrip1
            // 
            cmGridContextMenuStrip1.Items.AddRange(new ToolStripItem[] { copyWithHeadersStripMenuItem, copyWithOutHeadersStripMenuItem, clearFilters, singleRowStripMenuItem, pokazSQLMenuItem });
            cmGridContextMenuStrip1.Name = "exportContextMenuStrip1";
            cmGridContextMenuStrip1.Size = new Size(191, 114);
            // 
            // copyWithHeadersStripMenuItem
            // 
            copyWithHeadersStripMenuItem.Name = "copyWithHeadersStripMenuItem";
            copyWithHeadersStripMenuItem.Size = new Size(190, 22);
            copyWithHeadersStripMenuItem.Text = "Copy with headers";
            copyWithHeadersStripMenuItem.Click += CopyWithHeadersStripMenuItem_Click;
            // 
            // copyWithOutHeadersStripMenuItem
            // 
            copyWithOutHeadersStripMenuItem.Name = "copyWithOutHeadersStripMenuItem";
            copyWithOutHeadersStripMenuItem.Size = new Size(190, 22);
            copyWithOutHeadersStripMenuItem.Text = "Copy without headers";
            copyWithOutHeadersStripMenuItem.Click += copyWithOutHeadersStripMenuItem_Click;
            // 
            // clearFilters
            // 
            clearFilters.Name = "clearFilters";
            clearFilters.Size = new Size(190, 22);
            clearFilters.Text = "Clear Filters";
            clearFilters.Click += ClearFilters_Click;
            // 
            // singleRowStripMenuItem
            // 
            singleRowStripMenuItem.Name = "singleRowStripMenuItem";
            singleRowStripMenuItem.Size = new Size(190, 22);
            singleRowStripMenuItem.Text = "Row view";
            singleRowStripMenuItem.Click += SingleRow_Click;
            // 
            // pokazSQLMenuItem
            // 
            pokazSQLMenuItem.Name = "pokazSQLMenuItem";
            pokazSQLMenuItem.Size = new Size(190, 22);
            pokazSQLMenuItem.Text = "Show SQL";
            pokazSQLMenuItem.Click += pokazSQL_Click;
            // 
            // copyAsXlsxToClipboardMenuItem
            // 
            copyAsXlsxToClipboardMenuItem.Name = "copyAsXlsxToClipboardMenuItem";
            copyAsXlsxToClipboardMenuItem.Size = new Size(32, 19);
            // 
            // cmGridContextMenuStripRowView
            // 
            cmGridContextMenuStripRowView.Items.AddRange(new ToolStripItem[] { showDiff });
            cmGridContextMenuStripRowView.Name = "cmGridContextMenuStripRowView";
            cmGridContextMenuStripRowView.Size = new Size(169, 26);
            // 
            // showDiff
            // 
            showDiff.Name = "showDiff";
            showDiff.Size = new Size(168, 22);
            showDiff.Text = "Show diffferences";
            showDiff.Click += ShowDiff_Click;
            // 
            // saveFileCSV
            // 
            saveFileCSV.Filter = "csv Files(*.csv)|*.csv";
            // 
            // saveFileJson
            // 
            saveFileJson.Filter = "json Files(*.json)|*.json";
            // 
            // saveSQLLite
            // 
            saveSQLLite.Filter = "db Files(*.db)|*.db";
            // 
            // saveFileXlsx
            // 
            saveFileXlsx.Filter = "xlsx Files(*.xlsx)|*.xlsx";
            // 
            // openFileXlsx
            // 
            openFileXlsx.Filter = "(files)|*.xlsx;*.xlsb;*.csv;*.txt";
            // 
            // toolStripMenuItem11
            // 
            toolStripMenuItem11.Name = "toolStripMenuItem11";
            toolStripMenuItem11.Size = new Size(32, 19);
            // 
            // toolStripMenuItem12
            // 
            toolStripMenuItem12.Name = "toolStripMenuItem12";
            toolStripMenuItem12.Size = new Size(32, 19);
            // 
            // cmAllTables
            // 
            cmAllTables.Items.AddRange(new ToolStripItem[] { refreshTableListToolStripMenuItem, tcmDDLALLNz, tcmRecreateALL, showTablesSizes, showQueryHistory, showUserSessions, tcmChangeSorting, createNewTableToolStripMenuItem });
            cmAllTables.Name = "cmAllTables";
            cmAllTables.Size = new Size(238, 180);
            // 
            // refreshTableListToolStripMenuItem
            // 
            refreshTableListToolStripMenuItem.Name = "refreshTableListToolStripMenuItem";
            refreshTableListToolStripMenuItem.Size = new Size(237, 22);
            refreshTableListToolStripMenuItem.Text = "Refresh table list";
            refreshTableListToolStripMenuItem.Click += RefreshTableList;
            // 
            // tcmDDLALLNz
            // 
            tcmDDLALLNz.Name = "tcmDDLALLNz";
            tcmDDLALLNz.Size = new Size(237, 22);
            tcmDDLALLNz.Text = "DDL Tables";
            // tcmRecreateALL
            // 
            tcmRecreateALL.Name = "tcmRecreateALL";
            tcmRecreateALL.Size = new Size(237, 22);
            tcmRecreateALL.Text = "Recreate All Tables";
            // showTablesSizes
            // 
            showTablesSizes.Name = "showTablesSizes";
            showTablesSizes.Size = new Size(237, 22);
            showTablesSizes.Text = "Show tables size";
            // showQueryHistory
            // 
            showQueryHistory.Name = "showQueryHistory";
            showQueryHistory.Size = new Size(237, 22);
            showQueryHistory.Text = "Show query history";
            // showUserSessions
            // 
            showUserSessions.Name = "showUserSessions";
            showUserSessions.Size = new Size(237, 22);
            showUserSessions.Text = "Show user sessions";
            // tcmChangeSorting
            // 
            tcmChangeSorting.Name = "tcmChangeSorting";
            tcmChangeSorting.Size = new Size(237, 22);
            tcmChangeSorting.Text = "Refresh with another sort order";
            tcmChangeSorting.Click += TcmChangeSorting_Click;
            // 
            // createNewTableToolStripMenuItem
            // 
            createNewTableToolStripMenuItem.Name = "createNewTableToolStripMenuItem";
            createNewTableToolStripMenuItem.Size = new Size(237, 22);
            createNewTableToolStripMenuItem.Text = "Add new";
            // cmSynonyms
            // 
            cmSynonyms.Items.AddRange(new ToolStripItem[] { addNewSynonym });
            cmSynonyms.Name = "cmSynonyms";
            cmSynonyms.Size = new Size(122, 26);
            // 
            // addNewSynonym
            // 
            addNewSynonym.Name = "addNewSynonym";
            addNewSynonym.Size = new Size(121, 22);
            addNewSynonym.Text = "Add new";
            // cmAllProcsNetezza
            // 
            cmAllProcsNetezza.Items.AddRange(new ToolStripItem[] { menuItemDdlProcsNetezza, searchInProcs, netezzaProcExample, toolStripMenuItemAddProcedureNetezza });
            cmAllProcsNetezza.Name = "cmAllProcsNetezza";
            cmAllProcsNetezza.Size = new Size(185, 92);
            // 
            // menuItemDdlProcsNetezza
            // 
            menuItemDdlProcsNetezza.Name = "menuItemDdlProcsNetezza";
            menuItemDdlProcsNetezza.Size = new Size(184, 22);
            menuItemDdlProcsNetezza.Text = "DDL Procedures";
            // searchInProcs
            // 
            searchInProcs.Name = "searchInProcs";
            searchInProcs.Size = new Size(184, 22);
            searchInProcs.Text = "Search in procedures";
            // netezzaProcExample
            // 
            netezzaProcExample.Name = "netezzaProcExample";
            netezzaProcExample.Size = new Size(184, 22);
            netezzaProcExample.Text = "Procedure example";
            // toolStripMenuItemAddProcedureNetezza
            // 
            toolStripMenuItemAddProcedureNetezza.Name = "toolStripMenuItemAddProcedureNetezza";
            toolStripMenuItemAddProcedureNetezza.Size = new Size(184, 22);
            toolStripMenuItemAddProcedureNetezza.Text = "Add new";
            // tcmViewsSearchNetezza
            // 
            tcmViewsSearchNetezza.Name = "tcmViewsSearchNetezza";
            tcmViewsSearchNetezza.Size = new Size(154, 22);
            tcmViewsSearchNetezza.Text = "Search in views";
            // 
            // cmColumns
            // 
            cmColumns.Items.AddRange(new ToolStripItem[] { tcmAddColumn });
            cmColumns.Name = "cmColumns";
            cmColumns.Size = new Size(143, 26);
            // 
            // tcmAddColumn
            // 
            tcmAddColumn.Name = "tcmAddColumn";
            tcmAddColumn.Size = new Size(142, 22);
            tcmAddColumn.Text = "Add Column";
            // cmConstraints
            // 
            cmConstraints.Items.AddRange(new ToolStripItem[] { tcmAddConstraint });
            cmConstraints.Name = "cmConstraints";
            cmConstraints.Size = new Size(155, 26);
            // 
            // tcmAddConstraint
            // 
            tcmAddConstraint.Name = "tcmAddConstraint";
            tcmAddConstraint.Size = new Size(154, 22);
            tcmAddConstraint.Text = "Add Constraint";
            // cmIndexes
            // 
            cmIndexes.Items.AddRange(new ToolStripItem[] { tcmAddIndex });
            cmIndexes.Name = "cmIndexes";
            cmIndexes.Size = new Size(128, 26);
            // 
            // tcmAddIndex
            // 
            tcmAddIndex.Name = "tcmAddIndex";
            tcmAddIndex.Size = new Size(127, 22);
            tcmAddIndex.Text = "Add Index";
            // cmPartitions
            // 
            cmPartitions.Items.AddRange(new ToolStripItem[] { tcmAddPartitio });
            cmPartitions.Name = "cmPartitions";
            cmPartitions.Size = new Size(145, 26);
            // 
            // tcmAddPartitio
            // 
            tcmAddPartitio.Name = "tcmAddPartitio";
            tcmAddPartitio.Size = new Size(144, 22);
            tcmAddPartitio.Text = "Add Partition";
            // cmTriggers
            // 
            cmTriggers.Items.AddRange(new ToolStripItem[] { tcmAddTrigger });
            cmTriggers.Name = "cmTriggers";
            cmTriggers.Size = new Size(137, 26);
            // 
            // tcmAddTrigger
            // 
            tcmAddTrigger.Name = "tcmAddTrigger";
            tcmAddTrigger.Size = new Size(136, 22);
            tcmAddTrigger.Text = "Add Trigger";
            // cmAllViews
            // 
            cmAllViews.Items.AddRange(new ToolStripItem[] { tcmAllViews, tcmViewsSearchNetezza, addViewToolStripMenuItem });
            cmAllViews.Name = "cmAllViews";
            cmAllViews.Size = new Size(155, 70);
            // 
            // tcmAllViews
            // 
            tcmAllViews.Name = "tcmAllViews";
            tcmAllViews.Size = new Size(154, 22);
            tcmAllViews.Text = "DDL Views";
            // addViewToolStripMenuItem
            // 
            addViewToolStripMenuItem.Name = "addViewToolStripMenuItem";
            addViewToolStripMenuItem.Size = new Size(154, 22);
            addViewToolStripMenuItem.Text = "Add view";
            addViewToolStripMenuItem.Click += AddViewToolStripMenuItem_Click;
            // 
            // 
            // panel1
            // 
            panel1.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            panel1.Controls.Add(cursorPositionTextBox);
            panel1.Controls.Add(mainTextBox);
            panel1.Controls.Add(statusTextBox);
            panel1.Dock = DockStyle.Bottom;
            panel1.Name = "panel1";
            panel1.Size = new Size(1232, 23);
            panel1.TabIndex = 23;
            // 
            // maskedTextBox2
            // 
            cursorPositionTextBox.Anchor = AnchorStyles.Left;
            cursorPositionTextBox.Location = new Point(278, 0);
            cursorPositionTextBox.Margin = new Padding(2);
            cursorPositionTextBox.MinimumSize = new Size(100, 4);
            cursorPositionTextBox.Name = "maskedTextBox2";
            cursorPositionTextBox.ReadOnly = true;
            cursorPositionTextBox.Size = new Size(145, 23);
            cursorPositionTextBox.TabIndex = 16;
            // 
            // mainTextBox
            // 
            mainTextBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            mainTextBox.Location = new Point(427, 0);
            mainTextBox.Margin = new Padding(2);
            mainTextBox.MinimumSize = new Size(100, 4);
            mainTextBox.Name = "mainTextBox";
            mainTextBox.ReadOnly = true;
            mainTextBox.Size = new Size(437, 23);
            mainTextBox.TabIndex = 15;
            // 
            // statusTextBox
            // 
            statusTextBox.Anchor = AnchorStyles.Left;
            statusTextBox.Location = new Point(7, 0);
            statusTextBox.Margin = new Padding(2);
            statusTextBox.MinimumSize = new Size(100, 4);
            statusTextBox.Name = "statusTextBox";
            statusTextBox.ReadOnly = true;
            statusTextBox.Size = new Size(267, 23);
            statusTextBox.TabIndex = 11;

            // 
            // createNewSequenceToolStripMenuItem
            // 
            createNewSequenceToolStripMenuItem.Name = "createNewSequenceToolStripMenuItem";
            createNewSequenceToolStripMenuItem.Size = new Size(121, 22);
            createNewSequenceToolStripMenuItem.Text = "Add new";
            // contextMenuStripNetezzaSequences
            // 
            contextMenuStripNetezzaSequences.Items.AddRange(new ToolStripItem[] { createNewSequenceToolStripMenuItem });
            contextMenuStripNetezzaSequences.Name = "contextMenuStripNetezzaSequences";
            contextMenuStripNetezzaSequences.Size = new Size(122, 26);
            // 
            // contextMenuStripNetezzaUsersOrGroups
            // 
            contextMenuStripNetezzaUsersOrGroups.Items.AddRange(new ToolStripItem[] { tsmiAddNetezzaUser });
            contextMenuStripNetezzaUsersOrGroups.Name = "contextMenuStripNetezzaUsers";
            contextMenuStripNetezzaUsersOrGroups.Size = new Size(122, 26);
            // 
            // tsmiAddNetezzaUser
            // 
            tsmiAddNetezzaUser.Name = "tsmiAddNetezzaUser";
            tsmiAddNetezzaUser.Size = new Size(121, 22);
            tsmiAddNetezzaUser.Text = "Add new";
            // cmsDB2Server
            // 
            cmsDB2Server.Items.AddRange(new ToolStripItem[] { tsmiCreateServerDB2 });
            cmsDB2Server.Name = "cmsDB2Server";
            cmsDB2Server.Size = new Size(144, 26);
            // 
            // tsmiCreateServerDB2
            // 
            tsmiCreateServerDB2.Name = "tsmiCreateServerDB2";
            tsmiCreateServerDB2.Size = new Size(143, 22);
            tsmiCreateServerDB2.Text = "Create Server";
            // cmsSynonyms
            // 
            cmsSynonyms.Items.AddRange(new ToolStripItem[] { tsmiDDLSynonyms, tsmiValidateSynonyms });
            cmsSynonyms.Name = "cmsSynonyms";
            cmsSynonyms.Size = new Size(173, 48);
            // 
            // tsmiDDLSynonyms
            // 
            tsmiDDLSynonyms.Name = "tsmiDDLSynonyms";
            tsmiDDLSynonyms.Size = new Size(172, 22);
            tsmiDDLSynonyms.Text = "DDL Synonyms";
            // tsmiValidateSynonyms
            // 
            tsmiValidateSynonyms.Name = "tsmiValidateSynonyms";
            tsmiValidateSynonyms.Size = new Size(172, 22);
            tsmiValidateSynonyms.Text = "Validate synonyms";
            // BaseWindow
            // 
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(1232, 728);
            Controls.Add(panel1);
            Controls.Add(splitContainer1);
            Controls.Add(menuStrip1);
            ForeColor = SystemColors.ControlText;
            Icon = Resources.icon2ico;
            ImeMode = ImeMode.Hiragana;
            KeyPreview = true;
            MainMenuStrip = menuStrip1;
            Margin = new Padding(4);
            MinimumSize = new Size(600, 400);
            Name = "BaseWindow";
            Text = "JustyBaseLegacy";
            WindowState = FormWindowState.Maximized;
            FormClosing += BaseWindow_FormClosing;
            KeyDown += Form1_KeyDown;
            MouseDown += Form1_MouseDown;
            Move += BaseWindow_Move;
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            _leftTabs.ResumeLayout(false);
            databaseTabPage.ResumeLayout(false);
            databaseTabPage.PerformLayout();
            databaseContextMenuStrip.ResumeLayout(false);
            tabContextMenuStrip.ResumeLayout(false);
            cmResults.ResumeLayout(false);
            cmMain.ResumeLayout(false);
            cmGridContextMenuStrip1.ResumeLayout(false);
            cmGridContextMenuStripRowView.ResumeLayout(false);
            cmAllTables.ResumeLayout(false);
            cmSynonyms.ResumeLayout(false);
            cmAllProcsNetezza.ResumeLayout(false);
            cmColumns.ResumeLayout(false);
            cmConstraints.ResumeLayout(false);
            cmIndexes.ResumeLayout(false);
            cmPartitions.ResumeLayout(false);
            cmTriggers.ResumeLayout(false);
            cmAllViews.ResumeLayout(false);
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            contextMenuStripNetezzaSequences.ResumeLayout(false);
            contextMenuStripNetezzaUsersOrGroups.ResumeLayout(false);
            cmsDB2Server.ResumeLayout(false);
            cmsSynonyms.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }
        #endregion
        private MenuStrip menuStrip1;
        private SplitContainer splitContainer1;
        private OpenFileDialog ofdMain;
        private TabPage tabPage1;
        private TabControl _leftTabs;
        private TabPage databaseTabPage;
        private TabPage tabPageFiles;
        private SaveFileDialog saveFileDialogSQL;

        private ContextMenuStrip databaseContextMenuStrip;
        private ContextMenuStrip tabContextMenuStrip;
        private ContextMenuStrip cmResults;
        private TabPage tabPageLegend;
        private ContextMenuStrip cmMain;
        private SaveFileDialog manySaveFileDialog;
        private OpenFileDialog manyOpenFileDialog;
        private ContextMenuStrip cmGridContextMenuStrip1;
        private ContextMenuStrip cmGridContextMenuStripRowView;


        private SaveFileDialog saveFileCSV;
        private SaveFileDialog saveFileJson;
        private SaveFileDialog saveSQLLite;
        private SaveFileDialog saveFileXlsx;
        private OpenFileDialog openFileXlsx;
        private ImageList imageList1;
        
        private ToolStripMenuItem toolStripMenuItem11;
        private ToolStripMenuItem toolStripMenuItem12;

        private CustomToolStripSeparator toolStripMenuItem1;
        private CustomToolStripSeparator toolStripMenuItem2;
        private CustomToolStripSeparator toolStripMenuItem3;
        private CustomToolStripSeparator toolStripMenuItem4;
        private CustomToolStripSeparator toolStripMenuItem5;
        private CustomToolStripSeparator toolStripMenuItem6;
        private CustomToolStripSeparator toolStripMenuItem7;
        private CustomToolStripSeparator toolStripMenuItem8;
        private CustomToolStripSeparator toolStripSeparator1;
        private ToolStripMenuItem editToolStripMenuItem;
        private ToolStripMenuItem findToolStripMenuItem;
        private ToolStripMenuItem replaceToolStripMenuItem;
        private ToolStripMenuItem collapseAllregionToolStripMenuItem;
        private ToolStripMenuItem exapndAllregionToolStripMenuItem;
        private ToolStripMenuItem increaseIndentSiftTabToolStripMenuItem;
        private ToolStripMenuItem decreaseIndentTabToolStripMenuItem;
        private ToolStripMenuItem collapseSelectedBlockToolStripMenuItem;
        private ToolStripMenuItem goBackwardCtrlToolStripMenuItem;
        private ToolStripMenuItem goForwardCtrlShiftToolStripMenuItem;
        private ToolStripMenuItem autoIndentToolStripMenuItem;
        private ToolStripMenuItem goLeftBracketToolStripMenuItem;
        private ToolStripMenuItem goRightBracketToolStripMenuItem;
        private ToolStripMenuItem miPrint;
        private ToolStripMenuItem cSharpbuiltinHighlighterToolStripMenuItem;
        private ToolStripMenuItem setSelectedAsReadonlyToolStripMenuItem;
        private ToolStripMenuItem setSelectedAsWritableToolStripMenuItem;
        private ToolStripMenuItem changeHotkeysToolStripMenuItem;
        private ToolStripMenuItem toolStripMenuPlik;
        private ToolStripMenuItem fileOpenMenuItem;
        private ToolStripMenuItem fileOpenManyMenuItem;
        private ToolStripMenuItem fileSaveAsMenuItem;
        private ToolStripMenuItem fileExitMenuItem;
        private ToolStripMenuItem fileSaveMenuItem;




        




        
        private ToolStripMenuItem refreshTableListItem;
        private ToolStripMenuItem collapseDatabaseMenuItem;
        private ToolStripMenuItem closeAllTabsMenuItem;
        private ToolStripMenuItem closeOtherTabsMenuItem;
        private ToolStripMenuItem cmsSave;
        private ToolStripMenuItem cmsRenameTab;
        private ToolStripMenuItem cmsOpenInExplorer;
        private ToolStripMenuItem renameTab;
        private ToolStripMenuItem zamknijKarte;
        private ToolStripMenuItem zamknijWszytkieKarty;
        private ToolStripMenuItem zamknijReszte;
        private ToolStripMenuItem cutToolStripMenuItem;
        private ToolStripMenuItem copyToolStripMenuItem;
        private ToolStripMenuItem copyRawToolStripMenuItem;
        private ToolStripMenuItem pasteToolStripMenuItem;
        private ToolStripMenuItem databaseToolStripMenuItem;
        private ToolStripMenuItem historyToolStripMenuItem;
        private ToolStripMenuItem fileSaveManyMenuItem;
        private ToolStripMenuItem zakomentujToolStrip;
        private CustomToolStripSeparator toolStripSeparator3;
        private ToolStripMenuItem odkomentujToolStrip;
        private ToolStripMenuItem copyWithHeadersStripMenuItem;
        private ToolStripMenuItem copyWithOutHeadersStripMenuItem;
        private CustomToolStripSeparator toolStripSeparator4;
        private ToolStripMenuItem EksportytoolStripMenuItem11;
        private ToolStripMenuItem CSVtoolStripMenuItem12;
        private ToolStripMenuItem ImportToolStripMenuItem;
        private CustomToolStripSeparator toolStripSeparator6;
        private CustomToolStripSeparator toolStripSeparator5;
        private ToolStripMenuItem recentFilesMenu;
        private ToolStripMenuItem recentXlsx;
        private ToolStripMenuItem recentManyFilesMenu;
        private ToolStripMenuItem showDataFolderMenuItem;
        private ToolStripMenuItem clearDataFolderMenuItem;
        private ToolStripMenuItem clearFilters;
        private ToolStripMenuItem singleRowStripMenuItem;
        private ToolStripMenuItem pokazSQLMenuItem;
        private ToolStripMenuItem copyAsXlsxToClipboardMenuItem;
        private ToolStripMenuItem mapToolStripMenuItem;
        private ToolStripMenuItem settingsToolStripMenuItem;
        private ToolStripMenuItem XLSXStripMenuItem12;







        private ToolStripMenuItem XLSXtoolStripMenuItem;
        private ToolStripMenuItem optionsToolStripMenuItem;
        private ToolStripMenuItem addDollarSignMenuItem;
        private ToolStripMenuItem addAliases;
        private ToolStripMenuItem saveAsSnippet;
        private ToolStripMenuItem makeCodeToTempTable;
        private ToolStripMenuItem formatSQL;


        

        private ToolStripMenuItem showDiff;
        private CustomToolStripSeparator toolStripSeparator9;

        private ToolStripMenuItem pasteAsSelect;
        private ToolStripMenuItem inRaw;
        private ToolStripMenuItem inText;
        private ToolStripMenuItem importFromClipboard;
        private ImageList imageListFiles;
        private TabControl _tabControlMain;
        // databaseExplorerControl removed — use _mvvmDatabaseExplorerControl instead



        //private ToolStripMenuItem dashboardItem;
        private ContextMenuStrip cmAllTables;
        private ContextMenuStrip cmSynonyms;
        private ContextMenuStrip cmAllProcsNetezza;

        private ContextMenuStrip cmColumns;
        private ContextMenuStrip cmConstraints;
        private ContextMenuStrip cmIndexes;
        private ContextMenuStrip cmPartitions;
        private ContextMenuStrip cmTriggers;

        private ToolStripMenuItem searchInProcs;
        private ToolStripMenuItem netezzaProcExample;
        private ToolStripMenuItem tcmDDLALLNz;
        private ToolStripMenuItem addNewSynonym;


        private ToolStripMenuItem tcmAddColumn;
        private ToolStripMenuItem tcmAddConstraint;
        private ToolStripMenuItem tcmAddIndex;
        private ToolStripMenuItem tcmAddPartitio;
        private ToolStripMenuItem tcmAddTrigger;

        private ToolStripMenuItem showTablesSizes;
        private ToolStripMenuItem showQueryHistory;
        private ToolStripMenuItem showUserSessions;
        private ToolStripMenuItem tcmChangeSorting;
        private ContextMenuStrip cmAllViews;
        private ToolStripMenuItem tcmAllViews;
        private ToolStripMenuItem tcmViewsSearchNetezza;

        private ToolStripMenuItem tsmLicence;
        private Panel panel1;
        private TextBox cursorPositionTextBox;
        private TextBox mainTextBox;
        private TextBox statusTextBox;


        private ToolStripMenuItem toolStripMenuItemAddProcedureNetezza;


        private ToolStripMenuItem addViewToolStripMenuItem;
        private ToolStripMenuItem createNewTableToolStripMenuItem;
        private ToolStripMenuItem groomDatabaseToolStripMenuItem;
        private ToolStripMenuItem DbSizeToolStripMenuItem;

        private ToolStripMenuItem createNewSequenceToolStripMenuItem;
        private ContextMenuStrip contextMenuStripNetezzaSequences;
        private TabPage tabPageVariables;
        private ContextMenuStrip contextMenuStripNetezzaUsersOrGroups;
        private ToolStripMenuItem tsmiAddNetezzaUser;

        private ToolStripMenuItem refreshTableListToolStripMenuItem;        


        private ToolStripMenuItem verticalHorizontalToolStripMenuItem;
        private ToolStripMenuItem tsmiCreateServerDB2;

        private ContextMenuStrip cmsDB2Server;
        private ContextMenuStrip cmsSynonyms;
        private ToolStripMenuItem tsmiValidateSynonyms;
        private ToolStripMenuItem runToCursorToolStripMenuItem;
        private ToolStripMenuItem tsmiDDLSynonyms;
        private ToolStripMenuItem tsmiWordWrap;
        private ToolStripMenuItem menuItemDdlProcsNetezza;
        private ToolStripMenuItem tcmRecreateALL;
        private ToolStripMenuItem addNewConnectionToolStripMenuItem;
        private ToolStripMenuItem queryWatchToolStripMenuItem;
        private ToolStripMenuItem themeToolStripMenuItem;

    }
}

