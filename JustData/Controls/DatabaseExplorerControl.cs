using AppBase.Common;
using AppBase.Common.Enums;
using AppBase.Common.Models;
using AppBase.Data;
using AppBase.Common.Interfaces;
using AppBase.Data.Completion;
using AppBase.Data.Core.Core;
using AppBase.Data.Core.Enums;
using AppBase.Data.Core.Interfaces;
using DatabaseDataGridView.WinForms;
using DatabaseDataGridView.WinForms.Coloring;
using JustDataAdditionalForms;
using JustyBase.Netezza.Ddl;
using JustyBase.NetezzaCatalogSql;
using JustyBase.NetezzaDdl;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace JustyBaseLegacy.UI.Controls
{
    public partial class DatabaseExplorerControl : UserControl
    {

        private readonly BaseWindow _baseWindow;
        private readonly IApplicationSettingsContext _applicationSettingsContext;
        private readonly IDatabaseRuntimeContext _databaseRuntimeContext;
        private readonly INetezzaCompletionContext _completionContext;
        private readonly IGeneralDbService _generalDbService;
        private readonly IUiHelperService _uiHelperService;
        private readonly IWindowManagementService _windowManagementService;
        private readonly INetezzaHelperService _netezzaHelperService;
        private readonly NetezzaSqlCompletionServices _netezzaSqlCompletionServices;
        private readonly IColorTheme _colorTheme;
        private TreeNode? _hoveredTreeNode;

        public DatabaseExplorerControl(
            BaseWindow baseWindow,
            IApplicationSettingsContext applicationSettingsContext,
            IDatabaseRuntimeContext databaseRuntimeContext,
            INetezzaCompletionContext completionContext,
            IGeneralDbService generalDbService,
            IUiHelperService uiHelperService,
            IWindowManagementService windowManagementService,
            INetezzaHelperService netezzaHelperService,
            NetezzaSqlCompletionServices netezzaSqlCompletionServices,
            IColorTheme colorTheme,
            NotifyIcon notifyIcon1)
        {
            _baseWindow = baseWindow;
            _applicationSettingsContext = applicationSettingsContext;
            _databaseRuntimeContext = databaseRuntimeContext;
            _completionContext = completionContext;
            _generalDbService = generalDbService;
            _uiHelperService = uiHelperService;
            _netezzaHelperService = netezzaHelperService;
            _netezzaSqlCompletionServices = netezzaSqlCompletionServices;
            _windowManagementService = windowManagementService;
            _colorTheme = colorTheme;
            _notifyIcon1 = notifyIcon1;
            InitializeComponent();

            // Let ColorTheme control the header colors. With visual styles enabled,
            // Windows paints these schema-search headers using the light system theme.
            dgvFastDbBrowser.EnableHeadersVisualStyles = false;

            databaseTreeView.DrawMode = TreeViewDrawMode.OwnerDrawText;
            databaseTreeView.FullRowSelect = true;
            databaseTreeView.HideSelection = false;
            databaseTreeView.DrawNode += DatabaseTreeView_DrawNode;
            databaseTreeView.MouseMove += DatabaseTreeView_MouseMove;
            databaseTreeView.MouseLeave += DatabaseTreeView_MouseLeave;

            cbWhatDb.FlatStyle = FlatStyle.Flat;
            cbSearchDb.FlatStyle = FlatStyle.Flat;

            tsmUserScriptExternal = new();
            addNewToolStripMenuItem = new();
            cmExternal = new();
            ddlExternal = new();
            ddlExternalNewWindow = new();
            cmProc = new();
            cmStripColumnNetezza = new();
            addColumnToolStripMenuItem = new();
            tsmiDropColumnNetezza = new();
            tsmiColNetEditComment = new();
            tsmUserScriptProcedure = new();
            cmStripSequence = new();
            ddlProc = new();
            selectFromSequence = new();
            ddlProcNzNewWindow = new();
            dropSequenceToolStripMenuItem = new();
            ddlClipSequence = new();
            tsmDropNetezzaTable = new();
            tsmaAdvancedTable = new();
            customToolStripSeparator6 = new CustomToolStripSeparator(_applicationSettingsContext.Config.UseSpecialColoring, Color.FromArgb(_applicationSettingsContext.Config.StripFore[0], _applicationSettingsContext.Config.StripFore[1], _applicationSettingsContext.Config.StripFore[2]), Color.FromArgb(_applicationSettingsContext.Config.StripBack[0], _applicationSettingsContext.Config.StripBack[1], _applicationSettingsContext.Config.StripBack[2]));
            imortDataToolStripMenuItem = new();
            exportDataToolStripMenuItem = new();
            selectClipboardDuplicatesTabeli = new();
            selectClipboardTabeli = new();
            selectTabeliNetezza = new();
            grantClipboardTabeli = new();
            groomTableNetezza = new();
            showDistribution = new();
            changeDistribution = new();
            selectViewDuplicates = new();
            selectView = new();
            selectDeletedRows = new();
            cmStripViewNetezza = new();
            recreateTable = new();
            addCommentToTable = new();
            addKeyCode = new();
            addUniqueCode = new();
            generateStatisticsToolStripMenuItem = new();
            emptyTableToolStripMenuItem = new();
            cmStripTableNetezza = new();
            tsmUserScriptTable = new();
            tsmUserScriptView = new();
            customToolStripSeparator7 = new CustomToolStripSeparator(_applicationSettingsContext.Config.UseSpecialColoring, Color.FromArgb(_applicationSettingsContext.Config.StripFore[0], _applicationSettingsContext.Config.StripFore[1], _applicationSettingsContext.Config.StripFore[2]), Color.FromArgb(_applicationSettingsContext.Config.StripBack[0], _applicationSettingsContext.Config.StripBack[1], _applicationSettingsContext.Config.StripBack[2]));
            ddlClipboardNetezzaTable = new();
            ddlNewQueryTabeli = new();
            customToolStripSeparator7 = new CustomToolStripSeparator(_applicationSettingsContext.Config.UseSpecialColoring, Color.FromArgb(_applicationSettingsContext.Config.StripFore[0], _applicationSettingsContext.Config.StripFore[1], _applicationSettingsContext.Config.StripFore[2]), Color.FromArgb(_applicationSettingsContext.Config.StripBack[0], _applicationSettingsContext.Config.StripBack[1], _applicationSettingsContext.Config.StripBack[2]));
            customToolStripSeparator8 = new CustomToolStripSeparator(_applicationSettingsContext.Config.UseSpecialColoring, Color.FromArgb(_applicationSettingsContext.Config.StripFore[0], _applicationSettingsContext.Config.StripFore[1], _applicationSettingsContext.Config.StripFore[2]), Color.FromArgb(_applicationSettingsContext.Config.StripBack[0], _applicationSettingsContext.Config.StripBack[1], _applicationSettingsContext.Config.StripBack[2]));
            customToolStripSeparator9 = new CustomToolStripSeparator(_applicationSettingsContext.Config.UseSpecialColoring, Color.FromArgb(_applicationSettingsContext.Config.StripFore[0], _applicationSettingsContext.Config.StripFore[1], _applicationSettingsContext.Config.StripFore[2]), Color.FromArgb(_applicationSettingsContext.Config.StripBack[0], _applicationSettingsContext.Config.StripBack[1], _applicationSettingsContext.Config.StripBack[2]));
            customToolStripSeparator10 = new CustomToolStripSeparator(_applicationSettingsContext.Config.UseSpecialColoring, Color.FromArgb(_applicationSettingsContext.Config.StripFore[0], _applicationSettingsContext.Config.StripFore[1], _applicationSettingsContext.Config.StripFore[2]), Color.FromArgb(_applicationSettingsContext.Config.StripBack[0], _applicationSettingsContext.Config.StripBack[1], _applicationSettingsContext.Config.StripBack[2]));
            customToolStripSeparator11 = new CustomToolStripSeparator(_applicationSettingsContext.Config.UseSpecialColoring, Color.FromArgb(_applicationSettingsContext.Config.StripFore[0], _applicationSettingsContext.Config.StripFore[1], _applicationSettingsContext.Config.StripFore[2]), Color.FromArgb(_applicationSettingsContext.Config.StripBack[0], _applicationSettingsContext.Config.StripBack[1], _applicationSettingsContext.Config.StripBack[2]));
            dDLToNewQueryWindowToolStripMenuItem1 = new ToolStripMenuItem();
            ddlView = new();
            dropViewToolStripMenuItem = new();
            cmStripTableGeneral = new();
            dDLToNewQueryWindowToolStripMenuItem = new();
            ddlGeneralView = new();
            ddlGeneralAliases = new();
            ddlGeneralSynonyms = new();
            ddlGeneralProcs = new();
            ddlSynonym = new();
            cmStripViewGeneral = new();
            cmAllTablesGeneral = new();
            cmStripAliasesGeneral = new();
            tcmDDLALLGeneral = new();
            cmStripProcs = new();
            selectTop100ToClipboardToolStripMenuItem = new();
            selectTop100ToNewQueryWindowToolStripMenuItem = new();
            selectDuplicatesToClipboardToolStripMenuItem = new();
            tsmUserScriptSynonyms = new();
            cmStripSynonymsGeneral = new();
            ddlGeneralTable = new();
            cmStripSynonym = new();

            tsmManageScriptsTable = new();
            tsmManageScriptsViews = new();
            tsmManageScriptsProcedures = new();
            tsmManageScriptsExternals = new();
            tsmManageScriptsSynonyms = new();
            customToolStripSeparator1 = new CustomToolStripSeparator(_applicationSettingsContext.Config.UseSpecialColoring, Color.FromArgb(_applicationSettingsContext.Config.StripFore[0], _applicationSettingsContext.Config.StripFore[1], _applicationSettingsContext.Config.StripFore[2]), Color.FromArgb(_applicationSettingsContext.Config.StripBack[0], _applicationSettingsContext.Config.StripBack[1], _applicationSettingsContext.Config.StripBack[2]));
            customToolStripSeparator2 = new CustomToolStripSeparator(_applicationSettingsContext.Config.UseSpecialColoring, Color.FromArgb(_applicationSettingsContext.Config.StripFore[0], _applicationSettingsContext.Config.StripFore[1], _applicationSettingsContext.Config.StripFore[2]), Color.FromArgb(_applicationSettingsContext.Config.StripBack[0], _applicationSettingsContext.Config.StripBack[1], _applicationSettingsContext.Config.StripBack[2]));
            customToolStripSeparator3 = new CustomToolStripSeparator(_applicationSettingsContext.Config.UseSpecialColoring, Color.FromArgb(_applicationSettingsContext.Config.StripFore[0], _applicationSettingsContext.Config.StripFore[1], _applicationSettingsContext.Config.StripFore[2]), Color.FromArgb(_applicationSettingsContext.Config.StripBack[0], _applicationSettingsContext.Config.StripBack[1], _applicationSettingsContext.Config.StripBack[2]));
            customToolStripSeparator4 = new CustomToolStripSeparator(_applicationSettingsContext.Config.UseSpecialColoring, Color.FromArgb(_applicationSettingsContext.Config.StripFore[0], _applicationSettingsContext.Config.StripFore[1], _applicationSettingsContext.Config.StripFore[2]), Color.FromArgb(_applicationSettingsContext.Config.StripBack[0], _applicationSettingsContext.Config.StripBack[1], _applicationSettingsContext.Config.StripBack[2]));
            customToolStripSeparator5 = new CustomToolStripSeparator(_applicationSettingsContext.Config.UseSpecialColoring, Color.FromArgb(_applicationSettingsContext.Config.StripFore[0], _applicationSettingsContext.Config.StripFore[1], _applicationSettingsContext.Config.StripFore[2]), Color.FromArgb(_applicationSettingsContext.Config.StripBack[0], _applicationSettingsContext.Config.StripBack[1], _applicationSettingsContext.Config.StripBack[2]));

            // 
            // cmExternal
            // 
            cmExternal.Items.AddRange(new ToolStripItem[] { tsmUserScriptExternal, ddlExternalNewWindow, ddlExternal, addNewToolStripMenuItem });
            cmExternal.Name = "cmExternal";
            cmExternal.Size = new Size(166, 92);
            // 
            // addNewToolStripMenuItem
            // 
            addNewToolStripMenuItem.Enabled = false;
            addNewToolStripMenuItem.Name = "addNewToolStripMenuItem";
            addNewToolStripMenuItem.Size = new Size(165, 22);
            addNewToolStripMenuItem.Text = "Add new";
            // 
            // ddlExternalNewWindow
            //
            ddlExternalNewWindow.Name = "ddlExternalNewWindow";
            ddlExternalNewWindow.Size = new Size(213, 22);
            ddlExternalNewWindow.Text = "DDL to new query window";
            ddlExternalNewWindow.Click += DdlNewQueryExternal_Click;
            //
            // ddlExternal
            //
            ddlExternal.Name = "ddlExternal";
            ddlExternal.Size = new Size(165, 22);
            ddlExternal.Text = "DDL to clipboard";
            ddlExternal.Click += DdlClipboardExternal_Click;

            // 
            // cmProc
            // 
            cmProc.Items.AddRange(new ToolStripItem[] { tsmUserScriptProcedure, ddlProc, ddlProcNzNewWindow });
            cmProc.Name = "cmProc";
            cmProc.Size = new Size(214, 70);
            // 
            // cmStripColumnNetezza
            // 
            cmStripColumnNetezza.Items.AddRange(new ToolStripItem[] { tsmiColNetEditComment, tsmiDropColumnNetezza, addColumnToolStripMenuItem });
            cmStripColumnNetezza.Name = "cmsColumnNetezza";
            cmStripColumnNetezza.Size = new Size(152, 70);
            // 
            // tsmUserScriptProcedure
            // 
            tsmUserScriptProcedure.Name = "tsmUserScriptProcedure";
            tsmUserScriptProcedure.Size = new Size(213, 22);
            tsmUserScriptProcedure.Text = "User Scripts";
            // 
            // tsmUserScriptExternal
            // 
            tsmUserScriptExternal.Name = "tsmUserScriptExternal";
            tsmUserScriptExternal.Size = new Size(165, 22);
            tsmUserScriptExternal.Text = "User Scripts";
            // 
            // tsmiColNetEditComment
            // 
            tsmiColNetEditComment.Name = "tsmiColNetEditComment";
            tsmiColNetEditComment.Size = new Size(151, 22);
            tsmiColNetEditComment.Text = "Edit Comment";
            tsmiColNetEditComment.Click += TsmiColNetEditComment_Click;
            // 
            // tsmiDropColumnNetezza
            // 
            tsmiDropColumnNetezza.Name = "tsmiDropColumnNetezza";
            tsmiDropColumnNetezza.Size = new Size(151, 22);
            tsmiDropColumnNetezza.Text = "Drop Column";
            tsmiDropColumnNetezza.Click += TsmiDropColumnNetezza_Click;
            // 
            // addColumnToolStripMenuItem
            // 
            addColumnToolStripMenuItem.Name = "addColumnToolStripMenuItem";
            addColumnToolStripMenuItem.Size = new Size(151, 22);
            addColumnToolStripMenuItem.Text = "Add Column";
            addColumnToolStripMenuItem.Click += TsmiDropColumnNetezza_Click;
            // 
            // ddlProc
            // 
            ddlProc.Name = "ddlProc";
            ddlProc.Size = new Size(213, 22);
            ddlProc.Text = "DDL to clipboard";
            ddlProc.Click += DdlClipboardProc_Click;
            // 
            // cmStripSequence
            // 
            cmStripSequence.Items.AddRange(new ToolStripItem[] { selectFromSequence, ddlClipSequence, dropSequenceToolStripMenuItem });
            cmStripSequence.Name = "cmStripSequence";
            cmStripSequence.Size = new Size(217, 70);
            // 
            // ddlProcNzNewWindow
            // 
            ddlProcNzNewWindow.Name = "ddlProcNzNewWindow";
            ddlProcNzNewWindow.Size = new Size(213, 22);
            ddlProcNzNewWindow.Text = "DDL to new query window";
            ddlProcNzNewWindow.Click += DdlProcNzNewWindow_Click;
            // 
            // selectFromSequence
            // 
            selectFromSequence.Name = "selectFromSequence";
            selectFromSequence.Size = new Size(216, 22);
            selectFromSequence.Text = "Select from sequence";
            selectFromSequence.Click += SelectFromSequence_Click;
            // 
            // dropSequenceToolStripMenuItem
            // 
            dropSequenceToolStripMenuItem.Name = "dropSequenceToolStripMenuItem";
            dropSequenceToolStripMenuItem.Size = new Size(216, 22);
            dropSequenceToolStripMenuItem.Text = "Drop sequence";
            dropSequenceToolStripMenuItem.Click += TsmDropNetezzaTable_Click;
            // 
            // ddlClipSequence
            // 
            ddlClipSequence.Name = "ddlClipSequence";
            ddlClipSequence.Size = new Size(216, 22);
            ddlClipSequence.Text = "DDL sequence to clipboard";
            ddlClipSequence.Click += DdlCipSequence_Click;
            // 
            // tsmDropNetezzaTable
            // 
            tsmDropNetezzaTable.Name = "tsmDropNetezzaTable";
            tsmDropNetezzaTable.Size = new Size(218, 22);
            tsmDropNetezzaTable.Text = "Drop Table";
            tsmDropNetezzaTable.Click += TsmDropNetezzaTable_Click;
            // 
            // tsmaAdvancedTable
            // 
            tsmaAdvancedTable.DropDownItems.AddRange(new ToolStripItem[] { groomTableNetezza, addCommentToTable,
                tsmDropNetezzaTable, generateStatisticsToolStripMenuItem, emptyTableToolStripMenuItem });
            tsmaAdvancedTable.Name = "tsmaAdvancedTable";
            tsmaAdvancedTable.Size = new Size(266, 22);
            tsmaAdvancedTable.Text = "Others";

            // 
            // groomTableNetezza
            // 
            groomTableNetezza.Name = "groomTableNetezza";
            groomTableNetezza.Size = new Size(218, 22);
            groomTableNetezza.Text = "Groom table";
            groomTableNetezza.Click += GroomTableNetezza_Click;

            // 
            // addCommentToTable
            // 
            addCommentToTable.Name = "addCommentToTable";
            addCommentToTable.Size = new Size(218, 22);
            addCommentToTable.Text = "Add comment to clipboard";
            addCommentToTable.Click += AddCommentToTable_Click;

            // 
            // generateStatisticsToolStripMenuItem
            // 
            generateStatisticsToolStripMenuItem.Name = "generateStatisticsToolStripMenuItem";
            generateStatisticsToolStripMenuItem.Size = new Size(218, 22);
            generateStatisticsToolStripMenuItem.Text = "Generate statistics";
            generateStatisticsToolStripMenuItem.Click += GenerateStatisticsToolStripMenuItem_Click;
            // 
            // cmStripTableNetezza
            // 
            cmStripTableNetezza.Items.AddRange(new ToolStripItem[] { tsmUserScriptTable,
                tsmaAdvancedTable, customToolStripSeparator7, ddlNewQueryTabeli, ddlClipboardNetezzaTable,
                customToolStripSeparator9, selectClipboardTabeli, selectTabeliNetezza, selectClipboardDuplicatesTabeli,
                selectDeletedRows, customToolStripSeparator8, grantClipboardTabeli, showDistribution, changeDistribution,
                recreateTable, customToolStripSeparator10, addKeyCode, addUniqueCode, customToolStripSeparator6,
                imortDataToolStripMenuItem, exportDataToolStripMenuItem });
            cmStripTableNetezza.Name = "cmStripTabeli";
            cmStripTableNetezza.Size = new Size(267, 386);
            // 
            // tsmUserScriptTable
            // 
            tsmUserScriptTable.Name = "tsmUserScriptTable";
            tsmUserScriptTable.Size = new Size(266, 22);
            tsmUserScriptTable.Text = "User Scripts";
            // 
            // customToolStripSeparator6
            // 
            customToolStripSeparator6.Name = "customToolStripSeparator6";
            customToolStripSeparator6.Size = new Size(263, 6);
            // 
            // imortDataToolStripMenuItem
            // 
            imortDataToolStripMenuItem.Name = "imortDataToolStripMenuItem";
            imortDataToolStripMenuItem.Size = new Size(266, 22);
            imortDataToolStripMenuItem.Text = "Import Data";
            imortDataToolStripMenuItem.Click += ImortDataToolStripMenuItem_Click;
            // 
            // exportDataToolStripMenuItem
            // 
            exportDataToolStripMenuItem.Name = "exportDataToolStripMenuItem";
            exportDataToolStripMenuItem.Size = new Size(266, 22);
            exportDataToolStripMenuItem.Text = "Export Data";
            exportDataToolStripMenuItem.Click += ExportDataToolStripMenuItem_Click;
            // 
            // selectClipboardTabeli
            // 
            selectClipboardTabeli.Name = "selectClipboardTabeli";
            selectClipboardTabeli.Size = new Size(266, 22);
            selectClipboardTabeli.Text = "Select Top 100 to clipboard";
            selectClipboardTabeli.Click += SelectTableNetezza_Click;
            // 
            // selectTabeliNetezza
            // 
            selectTabeliNetezza.Name = "selectTabeliNetezza";
            selectTabeliNetezza.Size = new Size(266, 22);
            selectTabeliNetezza.Text = "Select Top 100 to new query window";
            selectTabeliNetezza.Click += SelectTableNetezza_Click;
            // 
            // selectClipboardDuplicatesTabeli
            // 
            selectClipboardDuplicatesTabeli.Name = "selectClipboardDuplicatesTabeli";
            selectClipboardDuplicatesTabeli.Size = new Size(266, 22);
            selectClipboardDuplicatesTabeli.Text = "Select duplicates to clipboard";
            selectClipboardDuplicatesTabeli.Click += SelectClipboardDuplicates_Click;
            // 
            // grantClipboardTabeli
            // 
            grantClipboardTabeli.Name = "grantClipboardTabeli";
            grantClipboardTabeli.Size = new Size(266, 22);
            grantClipboardTabeli.Text = "Grant to clipboard";
            grantClipboardTabeli.Click += GrantClipboardTabeli_Click;
            // 
            // showDistribution
            // 
            showDistribution.Name = "showDistribution";
            showDistribution.Size = new Size(266, 22);
            showDistribution.Text = "Show distribution";
            showDistribution.Click += ShowDistribution_Click;
            // 
            // changeDistribution
            // 
            changeDistribution.Name = "changeDistribution";
            changeDistribution.Size = new Size(266, 22);
            changeDistribution.Text = "Change distribution";
            changeDistribution.Click += ChangeDistribution_Click;

            // 
            // selectView
            // 
            selectView.Name = "selectView";
            selectView.Size = new Size(232, 22);
            selectView.Text = "Select to Clipboard";
            selectView.Click += SelectTableNetezza_Click;
            // 
            // selectDeletedRows
            // 
            selectDeletedRows.Name = "selectDeletedRows";
            selectDeletedRows.Size = new Size(266, 22);
            selectDeletedRows.Text = "Select deleted rows";
            selectDeletedRows.Click += SelectDeletedRows_Click;
            // 
            // cmStripViewNetezza
            // 
            cmStripViewNetezza.Items.AddRange(new ToolStripItem[] { tsmUserScriptView, dDLToNewQueryWindowToolStripMenuItem1, ddlView,
                selectView, selectViewDuplicates, dropViewToolStripMenuItem });
            cmStripViewNetezza.Name = "cmWidoku";
            cmStripViewNetezza.Size = new Size(233, 136);
            // 
            // selectViewDuplicates
            // 
            selectViewDuplicates.Name = "selectViewDuplicates";
            selectViewDuplicates.Size = new Size(232, 22);
            selectViewDuplicates.Text = "Select Duplicates to Clipboard";
            selectViewDuplicates.Click += SelectClipboardDuplicates_Click;
            // 
            // recreateTable
            // 
            recreateTable.Name = "recreateTable";
            recreateTable.Size = new Size(266, 22);
            recreateTable.Text = "Recreate to new tab";
            recreateTable.Click += RecreateTable_Click;
            // 
            // addKeyCode
            // 
            addKeyCode.Name = "addKeyCode";
            addKeyCode.Size = new Size(266, 22);
            addKeyCode.Text = "Add key to clipboard";
            addKeyCode.Click += AddKeyCode_Click;
            // 
            // addUniqueCode
            // 
            addUniqueCode.Name = "addUniqueCode";
            addUniqueCode.Size = new Size(266, 22);
            addUniqueCode.Text = "Add unique constraint to clipboard";
            addUniqueCode.Click += AddKUnique_Click;
            // 
            // emptyTableToolStripMenuItem
            // 
            emptyTableToolStripMenuItem.Name = "emptyTableToolStripMenuItem";
            emptyTableToolStripMenuItem.Size = new Size(218, 22);
            emptyTableToolStripMenuItem.Text = "Empty table";
            emptyTableToolStripMenuItem.Click += EmptyTableToolStripMenuItem_Click;
            // 
            // tsmUserScriptView
            // 
            tsmUserScriptView.Name = "tsmUserScriptView";
            tsmUserScriptView.Size = new Size(232, 22);
            tsmUserScriptView.Text = "User Scripts";
            // 
            // customToolStripSeparator7
            // 
            customToolStripSeparator7.Name = "customToolStripSeparator7";
            customToolStripSeparator7.Size = new Size(263, 6);
            // 
            // ddlNewQueryTabeli
            // 
            ddlNewQueryTabeli.Name = "ddlNewQueryTabeli";
            ddlNewQueryTabeli.Size = new Size(266, 22);
            ddlNewQueryTabeli.Text = "DDL to new query window";
            ddlNewQueryTabeli.Click += DdlNewQueryTabeli_Click;
            // 
            // ddlClipboardNetezzaTable
            // 
            ddlClipboardNetezzaTable.Name = "ddlClipboardNetezzaTable";
            ddlClipboardNetezzaTable.Size = new Size(266, 22);
            ddlClipboardNetezzaTable.Text = "DDL to clipboard";
            ddlClipboardNetezzaTable.Click += DdlClipboardTable_Click;
            // 
            // dDLToNewQueryWindowToolStripMenuItem1
            // 
            dDLToNewQueryWindowToolStripMenuItem1.Name = "dDLToNewQueryWindowToolStripMenuItem1";
            dDLToNewQueryWindowToolStripMenuItem1.Size = new Size(232, 22);
            dDLToNewQueryWindowToolStripMenuItem1.Text = "DDL to new query window";
            dDLToNewQueryWindowToolStripMenuItem1.Click += DdlClipboardWidoku_Click;

            // 
            // ddlView
            // 
            ddlView.Name = "ddlView";
            ddlView.Size = new Size(232, 22);
            ddlView.Text = "DDL to clipboard";
            ddlView.Click += DdlClipboardWidoku_Click;
            // 
            // dropViewToolStripMenuItem
            // 
            dropViewToolStripMenuItem.Name = "dropViewToolStripMenuItem";
            dropViewToolStripMenuItem.Size = new Size(232, 22);
            dropViewToolStripMenuItem.Text = "Drop view";
            // 
            // ddlGeneralTable
            // 
            ddlGeneralTable.Name = "ddlGeneralTable";
            ddlGeneralTable.Size = new Size(266, 22);
            ddlGeneralTable.Text = "DDL to clipboard";
            ddlGeneralTable.Click += DdlClipboardTable_Click;
            // 
            // dDLToNewQueryWindowToolStripMenuItem
            // 
            dDLToNewQueryWindowToolStripMenuItem.Name = "dDLToNewQueryWindowToolStripMenuItem";
            dDLToNewQueryWindowToolStripMenuItem.Size = new Size(266, 22);
            dDLToNewQueryWindowToolStripMenuItem.Text = "DDL to new query window";
            dDLToNewQueryWindowToolStripMenuItem.Click += DDLToNewQueryWindowToolStripMenuItem_Click;

            // 
            // ddlGeneralView
            // 
            ddlGeneralView.Name = "ddlGeneralView";
            ddlGeneralView.Size = new Size(163, 22);
            ddlGeneralView.Text = "DDL to clipboard";
            ddlGeneralView.Click += DdlClipboardWidoku_Click;

            // 
            // cmStripTableGeneral
            // 
            cmStripTableGeneral.Items.AddRange(new ToolStripItem[] { dDLToNewQueryWindowToolStripMenuItem,
                ddlGeneralTable, customToolStripSeparator11, selectTop100ToClipboardToolStripMenuItem,
                selectTop100ToNewQueryWindowToolStripMenuItem, selectDuplicatesToClipboardToolStripMenuItem });
            cmStripTableGeneral.Name = "cmStripGeneralTable";
            cmStripTableGeneral.Size = new Size(267, 120);
            // 
            // ddlGeneralAliases
            // 
            ddlGeneralAliases.Name = "ddlGeneralAliases";
            ddlGeneralAliases.Size = new Size(163, 22);
            ddlGeneralAliases.Text = "DDL to clipboard";
            ddlGeneralAliases.Click += DdlGeneralAliases_Click;
            // 
            // ddlGeneralSynonyms
            // 
            ddlGeneralSynonyms.Name = "ddlGeneralSynonyms";
            ddlGeneralSynonyms.Size = new Size(163, 22);
            ddlGeneralSynonyms.Text = "DDL to clipboard";
            ddlGeneralSynonyms.Click += DdlGeneralSynonym_Click;
            // 
            // ddlSynonym
            // 
            ddlSynonym.Name = "ddlSynonym";
            ddlSynonym.Size = new Size(215, 22);
            ddlSynonym.Text = "DDL synonym to clipboard";
            ddlSynonym.Click += CmStripSynonym_Click;
            // 
            // ddlGeneralProcs
            // 
            ddlGeneralProcs.Name = "ddlGeneralProcs";
            ddlGeneralProcs.Size = new Size(163, 22);
            ddlGeneralProcs.Text = "DDL to clipboard";
            ddlGeneralProcs.Click += DdlGeneralProcs_Click;
            // 
            // cmStripSynonym
            // 
            cmStripSynonym.Items.AddRange(new ToolStripItem[] { tsmUserScriptSynonyms, ddlSynonym });
            cmStripSynonym.Name = "cmStripSynonym";
            cmStripSynonym.Size = new Size(216, 48);
            // 
            // cmStripViewGeneral
            // 
            cmStripViewGeneral.Items.AddRange(new ToolStripItem[] { ddlGeneralView });
            cmStripViewGeneral.Name = "cmWidoku";
            cmStripViewGeneral.Size = new Size(164, 26);
            // 
            // cmStripAliasesGeneral
            // 
            cmStripAliasesGeneral.Items.AddRange(new ToolStripItem[] { ddlGeneralAliases });
            cmStripAliasesGeneral.Name = "cmStripAliasesGeneral";
            cmStripAliasesGeneral.Size = new Size(164, 26);
            // 
            // cmStripSynonymsGeneral
            // 
            cmStripSynonymsGeneral.Items.AddRange(new ToolStripItem[] { ddlGeneralSynonyms });
            cmStripSynonymsGeneral.Name = "cmStripSynonymsGeneral";
            cmStripSynonymsGeneral.Size = new Size(164, 26);
            // 
            // cmStripProcs
            // 
            cmStripProcs.Items.AddRange(new ToolStripItem[] { ddlGeneralProcs });
            cmStripProcs.Name = "cmStripProcs";
            cmStripProcs.Size = new Size(164, 26);
            // 
            // cmAllTablesGeneral
            // 
            cmAllTablesGeneral.Items.AddRange(new ToolStripItem[] { tcmDDLALLGeneral });
            cmAllTablesGeneral.Name = "cmAllTablesGeneral";
            cmAllTablesGeneral.Size = new Size(133, 26);
            // 
            // tcmDDLALLGeneral
            // 
            tcmDDLALLGeneral.Name = "tcmDDLALLGeneral";
            tcmDDLALLGeneral.Size = new Size(132, 22);
            tcmDDLALLGeneral.Text = "DDL Tables";
            tcmDDLALLGeneral.Click += TcmDDLALLGeneral_Click;
            // 
            // selectTop100ToClipboardToolStripMenuItem
            // 
            selectTop100ToClipboardToolStripMenuItem.Name = "selectTop100ToClipboardToolStripMenuItem";
            selectTop100ToClipboardToolStripMenuItem.Size = new Size(266, 22);
            selectTop100ToClipboardToolStripMenuItem.Text = "Select Top 100 to clipboard";
            selectTop100ToClipboardToolStripMenuItem.Click += SelectTop100ToClipboardToolStripMenuItem_Click;
            // 
            // selectTop100ToNewQueryWindowToolStripMenuItem
            // 
            selectTop100ToNewQueryWindowToolStripMenuItem.Name = "selectTop100ToNewQueryWindowToolStripMenuItem";
            selectTop100ToNewQueryWindowToolStripMenuItem.Size = new Size(266, 22);
            selectTop100ToNewQueryWindowToolStripMenuItem.Text = "Select Top 100 to new query window";
            selectTop100ToNewQueryWindowToolStripMenuItem.Click += SelectTop100ToNewQueryWindowToolStripMenuItem_Click;
            // 
            // selectDuplicatesToClipboardToolStripMenuItem
            // 
            selectDuplicatesToClipboardToolStripMenuItem.Name = "selectDuplicatesToClipboardToolStripMenuItem";
            selectDuplicatesToClipboardToolStripMenuItem.Size = new Size(266, 22);
            selectDuplicatesToClipboardToolStripMenuItem.Text = "Select duplicates to clipboard";
            selectDuplicatesToClipboardToolStripMenuItem.Click += SelectDuplicatesToClipboardToolStripMenuItem_Click;

            // 
            // tsmUserScriptSynonyms
            // 
            tsmUserScriptSynonyms.Name = "tsmUserScriptSynonyms";
            tsmUserScriptSynonyms.Size = new Size(215, 22);
            tsmUserScriptSynonyms.Text = "User Scripts";
            // 
            // tsmManageScriptsTable
            // 
            tsmManageScriptsTable.Name = "tsmManageScriptsTable";
            tsmManageScriptsTable.Size = new Size(126, 22);
            tsmManageScriptsTable.Text = "Manage...";
            tsmManageScriptsTable.Click += TsmManageScripts_Click;
            // 
            // tsmManageScriptsViews
            // 
            tsmManageScriptsViews.Name = "tsmManageScriptsViews";
            tsmManageScriptsViews.Size = new Size(126, 22);
            tsmManageScriptsViews.Text = "Manage...";
            tsmManageScriptsViews.Click += TsmManageScripts_Click;
            // 
            // tsmManageScriptsProcedures
            // 
            tsmManageScriptsProcedures.Name = "tsmManageScriptsProcedures";
            tsmManageScriptsProcedures.Size = new Size(126, 22);
            tsmManageScriptsProcedures.Text = "Manage...";
            tsmManageScriptsProcedures.Click += TsmManageScripts_Click;
            // 
            // tsmManageScriptsExternals
            // 
            tsmManageScriptsExternals.Name = "tsmManageScriptsExternals";
            tsmManageScriptsExternals.Size = new Size(126, 22);
            tsmManageScriptsExternals.Text = "Manage...";
            tsmManageScriptsExternals.Click += TsmManageScripts_Click;
            // 
            // tsmManageScriptsSynonyms
            // 
            tsmManageScriptsSynonyms.Name = "tsmManageScriptsSynonyms";
            tsmManageScriptsSynonyms.Size = new Size(126, 22);
            tsmManageScriptsSynonyms.Text = "Manage...";
            tsmManageScriptsSynonyms.Click += TsmManageScripts_Click;



            // Wire up internal event handlers
            tbFastSchemaSearch.KeyDown += SchemaFastSearch_KeyDown;
            DataGridViewCellValueNeeded += DgvFastDbBrowser_CellValueNeeded;
            CbSearchDbSelectedIndexChanged += CbSearchDb_SelectedIndexChanged;
            TbFastSchemaSearchTextChanged += TbFastSchemaSearch_TextChanged;
            DataGridViewCellDoubleClick += DgvFastDbBrowser_CellDoubleClick;
            TreeViewNodeMouseClick += DatabaseTreeView_MouseClick;
            TreeViewNodeMouseDoubleClick += DatabaseTreeView_MouseDoubleClick;
            TreeViewKeyDown += DatabaseTreeView_KeyDown;
            TreeViewBeforeExpand += DatabaseTreeView_BeforeExpand;
            TreeViewAfterExpand += DatabaseTreeView_AfterExpand;
            databaseTreeView.ItemDrag += DatabaseTreeView_ItemDrag;
            ApplyDpiMetrics();
        }

        public void ApplyDpiMetrics()
        {
            int dpi = DeviceDpi;
            databaseTreeView.ItemHeight = Math.Max(DpiScale.Scale(26, dpi), (int)Math.Ceiling(databaseTreeView.Font.GetHeight()) + DpiScale.Scale(8, dpi));
            dgvFastDbBrowser.RowTemplate.Height = (int)Math.Ceiling(dgvFastDbBrowser.Font.GetHeight()) + DpiScale.Scale(8, dpi);
            dgvFastDbBrowser.ColumnHeadersHeight = dgvFastDbBrowser.RowTemplate.Height + DpiScale.Scale(4, dpi);

            int fieldHeight = Math.Max(
                DpiScale.Scale(26, dpi),
                (int)Math.Ceiling(cbWhatDb.Font.GetHeight()) + DpiScale.Scale(8, dpi));
            int fieldGap = DpiScale.Scale(3, dpi);
            int outerPadding = DpiScale.Scale(2, dpi);
            int controlsHeight = fieldHeight * 3 + fieldGap * 2 + outerPadding;

            panelControlsContainer.Height = controlsHeight;
            cbWhatDb.Height = fieldHeight;
            cbSearchDb.Height = fieldHeight;
            cbWhatDb.Location = new Point(0, 0);
            cbSearchDb.Location = new Point(0, fieldHeight + fieldGap);
            panelSchemaSearchContainer.Location = new Point(0, (fieldHeight + fieldGap) * 2);
            panelSchemaSearchContainer.Height = fieldHeight;
            panelSchemaSearchContainer.Padding = new Padding(outerPadding);
            tbFastSchemaSearch.Height = Math.Max(1, fieldHeight - outerPadding * 2);

            databaseTreeView.Invalidate();
        }

        protected override void OnDpiChangedAfterParent(EventArgs e)
        {
            base.OnDpiChangedAfterParent(e);
            ApplyDpiMetrics();
        }

        private void DatabaseTreeView_MouseMove(object? sender, MouseEventArgs e)
        {
            TreeNode? node = databaseTreeView.GetNodeAt(e.Location);
            if (ReferenceEquals(node, _hoveredTreeNode))
            {
                return;
            }

            InvalidateTreeNode(_hoveredTreeNode);
            _hoveredTreeNode = node;
            InvalidateTreeNode(_hoveredTreeNode);
        }

        private void DatabaseTreeView_MouseLeave(object? sender, EventArgs e)
        {
            if (_hoveredTreeNode is null)
            {
                return;
            }

            InvalidateTreeNode(_hoveredTreeNode);
            _hoveredTreeNode = null;
        }

        private void InvalidateTreeNode(TreeNode? node)
        {
            if (node is null || node.Bounds.IsEmpty)
            {
                return;
            }

            Rectangle bounds = node.Bounds;
            bounds.Inflate(DpiScale.Scale(6, DeviceDpi), DpiScale.Scale(2, DeviceDpi));
            databaseTreeView.Invalidate(bounds);
        }

        private void DatabaseTreeView_DrawNode(object? sender, DrawTreeNodeEventArgs e)
        {
            bool darkTheme = _colorTheme.IsDark(databaseTreeView.BackColor);
            bool selected = e.Node.IsSelected;
            bool hovered = ReferenceEquals(e.Node, _hoveredTreeNode);
            TreeNode node = e.Node;

            if (selected || hovered)
            {
                Rectangle selectionBounds = e.Bounds;
                int horizontalPadding = DpiScale.Scale(5, DeviceDpi);
                int verticalPadding = DpiScale.Scale(2, DeviceDpi);
                selectionBounds.Inflate(horizontalPadding, -verticalPadding);

                Color selectionBack = selected
                    ? darkTheme ? Color.FromArgb(55, 94, 132) : Color.FromArgb(224, 238, 250)
                    : darkTheme ? Color.FromArgb(48, 58, 72) : Color.FromArgb(244, 247, 251);
                Color selectionBorder = selected
                    ? Color.FromArgb(86, 156, 214)
                    : darkTheme ? Color.FromArgb(83, 101, 124) : Color.FromArgb(213, 221, 232);

                using var backBrush = new SolidBrush(selectionBack);
                using var borderPen = new Pen(selectionBorder, Math.Max(1f, DeviceDpi / 96f));
                e.Graphics.FillRectangle(backBrush, selectionBounds);
                e.Graphics.DrawRectangle(borderPen, selectionBounds);
            }

            Color textColor = node.ForeColor.IsEmpty ? databaseTreeView.ForeColor : node.ForeColor;
            if (selected)
            {
                textColor = darkTheme ? Color.White : Color.FromArgb(25, 67, 105);
            }

            Font textFont = node.NodeFont ?? databaseTreeView.Font;
            TextRenderer.DrawText(
                e.Graphics,
                node.Text,
                textFont,
                e.Bounds,
                textColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
            e.DrawDefault = false;
        }

        private CustomToolStripSeparator customToolStripSeparator1;
        private CustomToolStripSeparator customToolStripSeparator2;
        private CustomToolStripSeparator customToolStripSeparator3;
        private CustomToolStripSeparator customToolStripSeparator4;
        private CustomToolStripSeparator customToolStripSeparator5;

        private ToolStripMenuItem tsmManageScriptsTable;
        private ToolStripMenuItem tsmManageScriptsViews;
        private ToolStripMenuItem tsmManageScriptsProcedures;
        private ToolStripMenuItem tsmManageScriptsExternals;
        private ToolStripMenuItem tsmManageScriptsSynonyms;

        public ContextMenuStrip cmStripSynonymsGeneral;
        private ToolStripMenuItem tsmUserScriptSynonyms;
        private ToolStripMenuItem selectTop100ToClipboardToolStripMenuItem;
        private ToolStripMenuItem selectTop100ToNewQueryWindowToolStripMenuItem;
        private ToolStripMenuItem selectDuplicatesToClipboardToolStripMenuItem;
        private CustomToolStripSeparator customToolStripSeparator7;
        private CustomToolStripSeparator customToolStripSeparator8;
        private CustomToolStripSeparator customToolStripSeparator9;
        private CustomToolStripSeparator customToolStripSeparator10;
        private CustomToolStripSeparator customToolStripSeparator11;
        public ContextMenuStrip cmStripProcs;
        private ToolStripMenuItem tcmDDLALLGeneral;
        public ContextMenuStrip cmStripAliasesGeneral;
        public ContextMenuStrip cmAllTablesGeneral;
        public ContextMenuStrip cmStripViewGeneral;
        private ToolStripMenuItem ddlSynonym;
        private ToolStripMenuItem dDLToNewQueryWindowToolStripMenuItem;
        public ContextMenuStrip cmStripTableGeneral;
        private ToolStripMenuItem ddlGeneralTable;
        private ToolStripMenuItem dropViewToolStripMenuItem;
        private ToolStripMenuItem ddlView;
        private ToolStripMenuItem dDLToNewQueryWindowToolStripMenuItem1;
        private ToolStripMenuItem ddlClipboardNetezzaTable;
        private ToolStripMenuItem ddlNewQueryTabeli;
        private ToolStripMenuItem emptyTableToolStripMenuItem;
        private ToolStripMenuItem tsmUserScriptExternal;
        public ToolStripMenuItem addNewToolStripMenuItem;
        private ContextMenuStrip cmExternal;
        private ToolStripMenuItem ddlExternal;
        private ToolStripMenuItem ddlExternalNewWindow;
        private ContextMenuStrip cmProc;
        private ContextMenuStrip cmStripColumnNetezza;
        private ToolStripMenuItem tsmDropNetezzaTable;
        private ToolStripMenuItem addColumnToolStripMenuItem;
        private ToolStripMenuItem tsmiDropColumnNetezza;
        private ToolStripMenuItem tsmiColNetEditComment;
        private ToolStripMenuItem tsmUserScriptProcedure;
        private ContextMenuStrip cmStripSequence;
        private ToolStripMenuItem ddlProc;
        private ToolStripMenuItem selectFromSequence;
        private ToolStripMenuItem ddlProcNzNewWindow;
        private ToolStripMenuItem dropSequenceToolStripMenuItem;
        private ToolStripMenuItem ddlClipSequence;
        private ToolStripMenuItem tsmaAdvancedTable;
        private CustomToolStripSeparator customToolStripSeparator6;
        private ToolStripMenuItem imortDataToolStripMenuItem;
        private ToolStripMenuItem exportDataToolStripMenuItem;
        private ToolStripMenuItem selectClipboardDuplicatesTabeli;
        private ToolStripMenuItem selectClipboardTabeli;
        private ToolStripMenuItem selectTabeliNetezza;
        private ToolStripMenuItem grantClipboardTabeli;
        private ToolStripMenuItem groomTableNetezza;
        private ToolStripMenuItem showDistribution;
        private ToolStripMenuItem changeDistribution;
        private ToolStripMenuItem selectViewDuplicates;
        private ToolStripMenuItem selectView;
        private ToolStripMenuItem selectDeletedRows;
        private ContextMenuStrip cmStripViewNetezza;
        private ToolStripMenuItem recreateTable;
        private ToolStripMenuItem addCommentToTable;
        private ToolStripMenuItem addKeyCode;
        private ToolStripMenuItem addUniqueCode;
        private ToolStripMenuItem generateStatisticsToolStripMenuItem;
        private ContextMenuStrip cmStripTableNetezza;
        private ToolStripMenuItem tsmUserScriptTable;
        private ToolStripMenuItem tsmUserScriptView;

        private ToolStripMenuItem ddlGeneralView;
        private ToolStripMenuItem ddlGeneralAliases;
        private ToolStripMenuItem ddlGeneralSynonyms;
        private ToolStripMenuItem ddlGeneralProcs;
        private ContextMenuStrip cmStripSynonym;

        public TreeView DatabaseTreeView => databaseTreeView;
        public DataGridView DgvFastDbBrowser => dgvFastDbBrowser;

        public ComboBox CbWhatDb => cbWhatDb;
        public ComboBox CbSearchDb => cbSearchDb;
        public TextBox TbFastSchemaSearch => tbFastSchemaSearch;

        private readonly Dictionary<TypeInDatabase, string> _textTypeInDatabase = new();

        private DataTable _dtFastSearch;

        TreeNode GetRootNode(TreeNode node)
        {
            TreeNode root = node;
            while (root.Parent != null)
            {
                root = root.Parent;
            }
            return root;
        }
        private void DatabaseTreeView_ItemDrag(object sender, ItemDragEventArgs e)
        {
            if (e.Item is TreeNode node && node.Tag is DatabaseTag)
            {
                var txt = node.Text;
                if (node.Tag is DatabaseTag databaseTag && (databaseTag.KIND_ID == TypeInDatabase.table || databaseTag.KIND_ID == TypeInDatabase.view || databaseTag.KIND_ID == TypeInDatabase.thisExternal))
                {

                    var root = GetRootNode(node.Parent);
                    string connectionName = root.Text;
                    int objectID = databaseTag.OBJECT_ID;

                    string tableOwner = NetezzaHelpers.baseTableDictionary[connectionName][objectID].TABLE_OWNER;
                    string databaseName = _completionContext.DatabaseDictionary[connectionName][NetezzaHelpers.baseTableDictionary[connectionName][objectID].DATABASE_ID].DatabaseName;
                    var tableData = NetezzaHelpers.baseTableDictionary[connectionName][objectID];
                    txt = $"{databaseName}.{tableOwner}.{tableData.TABLE_NAME}";
                }
                DoDragDrop(new DataObject(DataFormats.Text, txt), DragDropEffects.Copy);
            }
        }

        private void TsmManageScripts_Click(object sender, EventArgs e)
        {
            new ContexScripts(o => _colorTheme.ColorForm(o),
                _applicationSettingsContext.Config.ToolTipDelay,
                _applicationSettingsContext.Config.ContextScripts).ShowDialog();
            ResetUserScriptMenu();
        }
        private string GetSelectTop100NonNetezza(TreeNode node)
        {
            var path = node.FullPath;
            var ind = path.IndexOf("\\");
            string connName = path.Substring(0, ind);
            if (!IGeneralDbService.GeneralDic.ContainsKey(connName))
                return "-1";

            string tabName = node.Text;
            string schema = node.Parent.Parent.Text;
            string db = node.Parent.Parent.Parent.Name + ".";
            if (IGeneralDbService.GeneralDic[connName].DatabaseType == DatabaseTypeEnum.DB2 ||
                IGeneralDbService.GeneralDic[connName].DatabaseType == DatabaseTypeEnum.Oracle)
            {
                db = "";
            }
            var cols = IGeneralDbService.GeneralDic[connName].GetColumns(db, schema, tabName);
            string limit = "LIMIT 100";
            if (IGeneralDbService.GeneralDic[connName].DatabaseType == DatabaseTypeEnum.Oracle)
            {
                limit = @"WHERE
    ROWNUM < 100";
            }

            string sql = @$"SELECT
    {String.Join(",\r\n    ", cols)}
FROM
    {db}{schema}.{tabName} T1
{limit};";
            return sql;
        }

        private async void TcmDDLALLGeneral_Click(object sender, EventArgs e)
        {
            Application.UseWaitCursor = true;
            var path = DatabaseTreeView.SelectedNode.FullPath;
            var ind = path.IndexOf("\\");
            string connName = path.Substring(0, ind);
            if (!IGeneralDbService.GeneralDic.ContainsKey(connName))
                return;
            string name = DatabaseTreeView.SelectedNode.Parent.Name;

            try
            {
                string txt = await Task.Run(() => IGeneralDbService.GeneralDic[connName].GetCreateAllTablesText(name));
                _baseWindow.AddMainTab(null, $"get create for {name}", txt);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Application.UseWaitCursor = false;
                _notifyIcon1.ShowBalloonTip(1000, "JustyBaseLegacy", "Copied to clipboard", ToolTipIcon.Info);
            }
        }

        private void SelectTop100ToClipboardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var node = DatabaseTreeView.SelectedNode;
            string sql = GetSelectTop100NonNetezza(node);
            if (sql == "-1" || sql is null)
            {
                return;
            }
            Clipboard.SetText(sql);
        }

        private void SelectTop100ToNewQueryWindowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var node = DatabaseTreeView.SelectedNode;
            string sql = GetSelectTop100NonNetezza(node);
            if (sql == "-1")
            {
                return;
            }

            _baseWindow.AddMainTab(null, $"{node.Text} - top 100", sql);
        }

        private void SelectDuplicatesToClipboardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var path = DatabaseTreeView.SelectedNode.FullPath;
            var ind = path.IndexOf("\\");
            string connName = path.Substring(0, ind);
            if (!IGeneralDbService.GeneralDic.ContainsKey(connName))
                return;

            string tabName = DatabaseTreeView.SelectedNode.Text;
            string schema = DatabaseTreeView.SelectedNode.Parent.Parent.Text;
            string db = DatabaseTreeView.SelectedNode.Parent.Parent.Parent.Name + ".";
            var cols = IGeneralDbService.GeneralDic[connName].GetColumns(db, schema, tabName);
            string limit = "LIMIT 500";
            if (IGeneralDbService.GeneralDic[connName].DatabaseType == DatabaseTypeEnum.DB2 ||
                IGeneralDbService.GeneralDic[connName].DatabaseType == DatabaseTypeEnum.Oracle)
            {
                db = "";
            }

            if (IGeneralDbService.GeneralDic[connName].DatabaseType == DatabaseTypeEnum.Oracle)
            {
                limit = @"WHERE
    ROWNUM < 500";
            }


            string sql = @$"SELECT
    {String.Join(",\r\n    ", cols)}{Environment.NewLine}    , COUNT(1)
FROM
    {db}{schema}.{tabName} T1
GROUP BY
    {String.Join(",\r\n    ", cols)} {Environment.NewLine}HAVING
    COUNT(1) > 1
{limit};";

            Clipboard.SetText(sql);

        }
        private string GetSynCodeById(int objectID, string connectionName)
        {
            string tableOwner = NetezzaHelpers.baseTableDictionary[connectionName][objectID].TABLE_OWNER;
            string databaseName = _completionContext.DatabaseDictionary[connectionName][NetezzaHelpers.baseTableDictionary[connectionName][objectID].DATABASE_ID].DatabaseName;
            var tableData = NetezzaHelpers.baseTableDictionary[connectionName][objectID];

            string res = _baseWindow.GetAddInfo(objectID, TypeInDatabase.synonym, connectionName);
            return $"CREATE SYNONYM {databaseName}.{tableOwner}.{tableData.TABLE_NAME} FOR {res};";
        }

        private (string, string) GetDeletedRowsCodeById(int objectID, string connectionName)
        {
            string tableOwner = NetezzaHelpers.baseTableDictionary[connectionName][objectID].TABLE_OWNER;
            string databaseName = _completionContext.DatabaseDictionary[connectionName][NetezzaHelpers.baseTableDictionary[connectionName][objectID].DATABASE_ID].DatabaseName;
            var tableData = NetezzaHelpers.baseTableDictionary[connectionName][objectID];

            string tabName = $"{databaseName}.{tableOwner}.{tableData.TABLE_NAME}";

            return (tabName, $"SET show_deleted_records = 1;\r\nselect t1.createxid, t1.deletexid, t1.* from {tabName} t1 WHERE deletexid != 0;\r\nSET show_deleted_records = 0;");
        }

        private async void DdlGeneralProcs_Click(object sender, EventArgs e)
        {
            var path = DatabaseTreeView.SelectedNode.FullPath;
            var ind = path.IndexOf("\\");
            string connName = path.Substring(0, ind);
            if (!IGeneralDbService.GeneralDic.ContainsKey(connName))
                return;
            string name = DatabaseTreeView.SelectedNode.Name;

            string result = await IGeneralDbService.GeneralDic[connName].GetCreateProcedureText(name);

            Clipboard.SetText(result ?? "");

        }
        private void CmStripSynonym_Click(object sender, EventArgs e)
        {
            var tag = (DatabaseTreeView.SelectedNode.Tag as DatabaseTag);
            if (tag != null)
            {
                int objectID = tag.OBJECT_ID;
                Clipboard.SetText(GetSynCodeById(objectID, DatabaseTreeView.SelectedNode.Parent.Parent.Parent.Text) ?? "");
            }
        }

        private async Task<string> GetViewCodeById(int objectID, string connectionName)
        {
            return await _netezzaHelperService.GetViewCodeById(_databaseRuntimeContext, objectID, connectionName);
        }

        private async void DdlGeneralAliases_Click(object sender, EventArgs e)
        {
            var path = DatabaseTreeView.SelectedNode.FullPath;
            var ind = path.IndexOf("\\");
            string connName = path.Substring(0, ind);
            if (!IGeneralDbService.GeneralDic.ContainsKey(connName))
                return;
            string name = DatabaseTreeView.SelectedNode.Name;

            IGeneralDb db2 = IGeneralDbService.GeneralDic[connName];
            string result = await db2.GetCreateAliasTextAsync(name);

            Clipboard.SetText(result ?? "");

        }
        private async void DdlGeneralSynonym_Click(object sender, EventArgs e)
        {
            var path = DatabaseTreeView.SelectedNode.FullPath;
            var ind = path.IndexOf("\\");
            string connName = path.Substring(0, ind);
            if (!IGeneralDbService.GeneralDic.ContainsKey(connName))
                return;
            string name = DatabaseTreeView.SelectedNode.Name;

            var db2 = IGeneralDbService.GeneralDic[connName];
            string result = await db2.GetCreateSynonymTextAsync(name);

            Clipboard.SetText(result ?? "");

        }
        private async void DdlNewQueryTabeli_Click(object sender, EventArgs e)
        {
            var tag = (DatabaseTreeView.SelectedNode.Tag as DatabaseTag);
            if (tag != null)
            {
                Application.UseWaitCursor = true;
                int objectID = tag.OBJECT_ID;
                string connectionName = DatabaseTreeView.SelectedNode.Parent.Parent.Parent.Text;
                var tableData = NetezzaHelpers.baseTableDictionary[connectionName][objectID];
                string tableName = tableData.TABLE_NAME;
            _baseWindow.AddMainTab(null, $"ddl for {tableName}", (await _netezzaHelperService.GetTableCodeById(null, _databaseRuntimeContext, connectionName, objectID)).Code);
                Application.UseWaitCursor = false;
            }
        }
        private void EmptyTableToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var tag = (DatabaseTreeView.SelectedNode.Tag as DatabaseTag);
            if (tag != null /*&& tag.KIND_ID == DatabaseObjectTypes.table*/)
            {
                string connectionName = DatabaseTreeView.SelectedNode.Parent.Parent.Parent.Text;
                string databaseName = _completionContext.DatabaseDictionary[connectionName][NetezzaHelpers.baseTableDictionary[connectionName][tag.OBJECT_ID].DATABASE_ID].DatabaseName;
                var tableData = NetezzaHelpers.baseTableDictionary[connectionName][tag.OBJECT_ID];

                string s = $"TRUNCATE TABLE {databaseName}..{tableData.TABLE_NAME};";
                s += $"\r\n--https://www.ibm.com/docs/en/netezza?topic=tables-truncate-table";
                _baseWindow.AddMainTab(null, $"empty for {tableData.TABLE_NAME}", s);
            }
        }

        private async void DDLToNewQueryWindowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.UseWaitCursor = true;
            var path = DatabaseTreeView.SelectedNode.FullPath;
            var ind = path.IndexOf("\\");
            string connName = path.Substring(0, ind);
            if (!IGeneralDbService.GeneralDic.ContainsKey(connName))
                return;
            string tableName = DatabaseTreeView.SelectedNode.Name;
            try
            {
                string txt = await Task.Run(() => IGeneralDbService.GeneralDic[connName].GetCreateTableText(tableName));
                Clipboard.SetText(txt ?? "");
                _baseWindow.AddMainTab(null, $"ddl for {tableName}", txt);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Application.UseWaitCursor = false;
                _notifyIcon1.ShowBalloonTip(1000, "JustyBaseLegacy", "Copied to clipboard", ToolTipIcon.Info);
            }
        }

        private async void DdlClipboardWidoku_Click(object sender, EventArgs e)
        {
            Application.UseWaitCursor = true;
            string result = "";
            string name = "";
            if (sender == ddlView || sender == dDLToNewQueryWindowToolStripMenuItem1)
            {
                int objectID = (DatabaseTreeView.SelectedNode.Tag as DatabaseTag).OBJECT_ID;
                name = DatabaseTreeView.SelectedNode.Text;
                try
                {
                    if (DatabaseTreeView.SelectedNode is null
                        || DatabaseTreeView.SelectedNode.Parent is null
                        || DatabaseTreeView.SelectedNode.Parent.Parent is null
                        || DatabaseTreeView.SelectedNode.Parent.Parent.Parent is null
                        || DatabaseTreeView.SelectedNode.Parent.Parent.Parent.Text is null
                        )
                    {
                        result = "PROBLEM";
                    }
                    else
                    {
                        result = await GetViewCodeById(objectID, DatabaseTreeView.SelectedNode.Parent.Parent.Parent.Text);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    result = "PROBLEM";
                }
            }
            else // sender = ddl general
            {
                var path = DatabaseTreeView.SelectedNode.FullPath;
                var ind = path.IndexOf("\\");
                string connName = path.Substring(0, ind);
                if (!IGeneralDbService.GeneralDic.ContainsKey(connName))
                    return;
                name = DatabaseTreeView.SelectedNode.Name;
                result = await Task.Run(() => IGeneralDbService.GeneralDic[connName].GetCreateViewText(name));
            }

            if (result is not null)
            {
                if (sender == dDLToNewQueryWindowToolStripMenuItem1)
                {
                    if (DatabaseTreeView.InvokeRequired)
                    {
                        DatabaseTreeView.Invoke(() =>
                        {
                            _baseWindow.AddMainTab(null, $"{name} - DDL", result);

                        });
                    }
                    else
                    {
                        _baseWindow.AddMainTab(null, $"{name} - DDL", result);
                    }
                }
                else
                {
                    if (DatabaseTreeView.InvokeRequired)
                    {
                        DatabaseTreeView.Invoke(() =>
                        {
                            Clipboard.SetText(result);

                        });
                    }
                    else
                    {
                        Clipboard.SetText(result ?? "");
                    }
                }
            }
            Application.UseWaitCursor = false;
            _notifyIcon1.ShowBalloonTip(1000, "JustyBaseLegacy", "Copied to clipboard", ToolTipIcon.Info);
        }

        private async void ChangeDistribution_Click(object sender, EventArgs e)
        {
            var selNodeTxt = DatabaseTreeView.SelectedNode.Text;
            var tag = (DatabaseTreeView.SelectedNode.Tag as DatabaseTag);
            if (tag != null)
            {
                int objectID = tag.OBJECT_ID;
                string connectionName = DatabaseTreeView.SelectedNode.Parent.Parent.Parent.Text;

                var res = await _netezzaHelperService.GetRecreateTableCodeById(_databaseRuntimeContext, connectionName, objectID);

                int firstColumnId = NetezzaHelpers.baseTableDictionary[connectionName][objectID].FIRST_COLUMN_ID;
                int columnCount = NetezzaHelpers.baseTableDictionary[connectionName][objectID].COLUMN_COUNT;

                var lst = new List<string>();
                for (int i = 0; i < columnCount; i++)
                {
                    int columnId = firstColumnId + i;
                    lst.Add(_completionContext.ColumnTablesDictionary[connectionName][columnId].COLUMN_NAME);
                }

                var cdTemp = new DbForms.DistForm(lst, res.Dystr.Select(a => a.Item2).ToList(), o => _colorTheme.ColorForm(o));

                if (cdTemp.ShowDialog() == DialogResult.OK)
                {
                    _baseWindow.AddMainTab(null, $"{selNodeTxt} - distribution", (await _netezzaHelperService.GetRecreateTableCodeById(_databaseRuntimeContext, connectionName, objectID, cdTemp.DistCols)).Code);
                }
            }
        }

        private async void ShowDistribution_Click(object sender, EventArgs e)
        {
            var tag = (DatabaseTreeView.SelectedNode.Tag as DatabaseTag);
            if (tag != null)
            {
                Application.UseWaitCursor = true;
                string connName = DatabaseTreeView.SelectedNode.Parent.Parent.Parent.Text;

                string tableOwner = NetezzaHelpers.baseTableDictionary[connName][tag.OBJECT_ID].TABLE_OWNER;
                string databaseName = _completionContext.DatabaseDictionary[connName][NetezzaHelpers.baseTableDictionary[connName][tag.OBJECT_ID].DATABASE_ID].DatabaseName;
                var tableData = NetezzaHelpers.baseTableDictionary[connName][tag.OBJECT_ID];

                string query1 = NetezzaSystemSql.GetDistributionWithDeletedRecords(databaseName, tableData.TABLE_NAME);
                string query2 = NetezzaSystemSql.GetTableStorageStatistics(tableData.TABLE_NAME);

                Int64 slicesNum = 0;
                double skew = 0.0;
                DateTime crtTime = default;
                long alocatedBytes = default;
                long usedBytes = default;
                long objId = default;

                Int64 Rows = 0;
                Int64 Max = 0;
                Int64 Min = Int64.MaxValue;

                Int64 Rows2 = 0;
                Int64 Max2 = 0;
                Int64 Min2 = Int64.MaxValue;

                Dictionary<int, (long count, long countWdeleted, string sliceName)> ForPlotDic = new Dictionary<int, (long count, long countWdeleted, string sliceName)>();

                await Task.Run(() =>
                {
                    using DbConnection dbConnection = IGeneralDbService.GeneralDic[connName].GetConnection(databaseName);
                    dbConnection.Open();

                    try
                    {
                        using (DbCommand slicesNums = dbConnection.CreateCommand())
                        {
                            slicesNums.CommandText = NetezzaSystemSql.DataSliceCount;
                            slicesNums.CommandTimeout = _applicationSettingsContext.Config.CommandDistTimeout;
                            slicesNum = (Int64)slicesNums.ExecuteScalar();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, ex.Message);
                        return;
                    }

                    try
                    {
                        using (DbCommand dataOnClices = dbConnection.CreateCommand())
                        {
                            dataOnClices.CommandText = query1;
                            dataOnClices.CommandTimeout = _applicationSettingsContext.Config.CommandDistTimeout;
                            using DbDataReader rdr = dataOnClices.ExecuteReader();
                            int i = 0;
                            while (rdr.Read())
                            {
                                Int64 countAll = rdr.GetInt64(1);
                                Int64 countDeleted = rdr.GetInt64(2);

                                Int64 countX = countAll - countDeleted;
                                ForPlotDic[i++] = (countX, countAll, $"{rdr.GetValue(0)}");

                                Rows2 += countAll;
                                if (Max2 < countAll)
                                    Max2 = countAll;
                                if (Min2 > countAll)
                                    Min2 = countAll;

                                Rows += countX;
                                if (Max < countX)
                                    Max = countX;
                                if (Min > countX)
                                    Min = countX;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, ex.Message);
                        return;
                    }

                    try
                    {
                        using (DbCommand addInfo = dbConnection.CreateCommand())
                        {
                            addInfo.CommandText = query2;
                            using DbDataReader rdr2 = addInfo.ExecuteReader();
                            while (rdr2.Read())
                            {
                                objId = rdr2.GetInt64(0);

                                object temp = rdr2.GetValue(1);
                                if (temp == DBNull.Value || temp is null)
                                    skew = 0.0;
                                else
                                    skew = (double)temp;

                                temp = rdr2.GetValue(2);
                                if (temp != DBNull.Value)
                                    crtTime = (DateTime)temp;

                                temp = rdr2.GetValue(3);
                                if (temp != DBNull.Value && temp is not null)
                                    alocatedBytes = (long)temp;

                                temp = rdr2.GetValue(4);
                                if (temp != DBNull.Value && temp is not null)
                                    usedBytes = (long)temp;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, ex.Message);
                    }
                });

                Application.UseWaitCursor = false;

                var d = new NetezzaDistribution($"{databaseName}..{tableData.TABLE_NAME}", _colorTheme);
                d.Skew = skew;
                d.Slices = slicesNum;
                d.Rows = Rows;
                d.Max = Max;
                d.Min = Min;
                d.RowsWDeleted = Rows2;
                d.MaxWDeleted = Max2;
                d.MinWDeleted = Min2;
                d.crtTime = crtTime;
                d.AlocatedBytes = alocatedBytes;
                d.UsedBytes = usedBytes;
                d.ObjId = objId;

                d.ForPlotDic = ForPlotDic;
                //d.testData();
                d.Init2();
                d.Show();
            }
        }

        private async void DdlClipboardTable_Click(object sender, EventArgs e)
        {
            if (sender == ddlClipboardNetezzaTable)
            {
                var tag = (DatabaseTreeView.SelectedNode.Tag as DatabaseTag);
                if (tag != null)
                {
                    int objectID = tag.OBJECT_ID;
                    string connectionName = DatabaseTreeView.SelectedNode.Parent.Parent.Parent.Text;
                    Application.UseWaitCursor = true;
            Clipboard.SetText((await _netezzaHelperService.GetTableCodeById(null, _databaseRuntimeContext, connectionName, objectID)).Code ?? "");
                    Application.UseWaitCursor = false;
                    _notifyIcon1.ShowBalloonTip(1000, "JustyBaseLegacy", "Copied to clipboard", ToolTipIcon.Info);
                }
            }
            else if (sender == ddlGeneralTable)
            {
                Application.UseWaitCursor = true;
                var path = DatabaseTreeView.SelectedNode.FullPath;
                var ind = path.IndexOf("\\");
                string connName = path.Substring(0, ind);
                if (!IGeneralDbService.GeneralDic.ContainsKey(connName))
                    return;
                string name = DatabaseTreeView.SelectedNode.Name;

                try
                {
                    string txt = await Task.Run(() => IGeneralDbService.GeneralDic[connName].GetCreateTableText(name));
                    Clipboard.SetText(txt ?? "");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    Application.UseWaitCursor = false;
                    _notifyIcon1.ShowBalloonTip(1000, "JustyBaseLegacy", "Copied to clipboard", ToolTipIcon.Info);
                }

            }
        }

        private void ExportDataToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                string txt = DatabaseTreeView.SelectedNode.Text;
                string dbName = DatabaseTreeView.SelectedNode.Parent.Parent.Text;
                var d = new DbForms.ExportTableDataNetezza(dbName, txt, o => _colorTheme.ColorForm(o));
                if (d.ShowDialog() == DialogResult.OK)
                {
                    _baseWindow.AddMainTab(null, $"external - txt", d.GetCode);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message);
            }

        }
        private string GetCommentTableCodeById(int objectID)
        {
            string connectionName = DatabaseTreeView.SelectedNode.Parent.Parent.Parent.Text;
            string tableOwner = NetezzaHelpers.baseTableDictionary[connectionName][objectID].TABLE_OWNER;
            string databaseName = _completionContext.DatabaseDictionary[connectionName][NetezzaHelpers.baseTableDictionary[connectionName][objectID].DATABASE_ID].DatabaseName;
            var tableData = NetezzaHelpers.baseTableDictionary[connectionName][objectID];

            return $"COMMENT ON TABLE {databaseName}.{tableOwner}.{tableData.TABLE_NAME} IS 'some comment';";
        }

        private string GetKeyCodeById(int objectID)
        {
            string connectionName = DatabaseTreeView.SelectedNode.Parent.Parent.Parent.Text;
            string tableOwner = NetezzaHelpers.baseTableDictionary[connectionName][objectID].TABLE_OWNER;
            string databaseName = _completionContext.DatabaseDictionary[connectionName][NetezzaHelpers.baseTableDictionary[connectionName][objectID].DATABASE_ID].DatabaseName;
            var tableData = NetezzaHelpers.baseTableDictionary[connectionName][objectID];

            return $"ALTER TABLE {databaseName}.{tableOwner}.{tableData.TABLE_NAME} ADD CONSTRAINT PK_{tableData.TABLE_NAME} PRIMARY KEY (COL1,COL2);";
        }

        private string GetUiqueCodeById(int objectID)
        {
            string connectionName = DatabaseTreeView.SelectedNode.Parent.Parent.Parent.Text;
            string tableOwner = NetezzaHelpers.baseTableDictionary[connectionName][objectID].TABLE_OWNER;
            string databaseName = _completionContext.DatabaseDictionary[connectionName][NetezzaHelpers.baseTableDictionary[connectionName][objectID].DATABASE_ID].DatabaseName;
            var tableData = NetezzaHelpers.baseTableDictionary[connectionName][objectID];

            return $"ALTER TABLE {databaseName}.{tableOwner}.{tableData.TABLE_NAME} ADD CONSTRAINT UK_{tableData.TABLE_NAME} UNIQUE (COL1,COL2);";
        }

        private void AddCommentToTable_Click(object sender, EventArgs e)
        {
            var tag = (DatabaseTreeView.SelectedNode.Tag as DatabaseTag);
            if (tag != null)
            {
                int objectID = tag.OBJECT_ID;
                Clipboard.SetText(GetCommentTableCodeById(objectID) ?? "");
            }
        }
        private void AddKUnique_Click(object sender, EventArgs e)
        {
            var tag = (DatabaseTreeView.SelectedNode.Tag as DatabaseTag);
            if (tag != null)
            {
                int objectID = tag.OBJECT_ID;
                Clipboard.SetText(GetUiqueCodeById(objectID) ?? "");
            }
        }

        private void AddKeyCode_Click(object sender, EventArgs e)
        {
            var tag = (DatabaseTreeView.SelectedNode.Tag as DatabaseTag);
            if (tag != null)
            {
                int objectID = tag.OBJECT_ID;
                Clipboard.SetText(GetKeyCodeById(objectID) ?? "");
            }
        }

        private async void SchemaFastSearch_KeyDown(object sender, KeyEventArgs e)
        {
            string driverName = _generalDbService.DriverName(_completionContext.SelectedConnectionName);
            if (e.KeyCode == Keys.Return)
            {
                await DoSearch();
            }
        }

        private async void RecreateTable_Click(object sender, EventArgs e)
        {
            var selNodeTxt = DatabaseTreeView.SelectedNode.Text;
            var tag = (DatabaseTreeView.SelectedNode.Tag as DatabaseTag);
            if (tag != null)
            {
                int objectID = tag.OBJECT_ID;
                string connectionName = DatabaseTreeView.SelectedNode.Parent.Parent.Parent.Text;

                _baseWindow.AddMainTab(null, $"{selNodeTxt} - recreate", (await _netezzaHelperService.GetRecreateTableCodeById(_databaseRuntimeContext, connectionName, objectID)).Code);
            }
        }
        private void TsmiColNetEditComment_Click(object sender, EventArgs e)
        {
            TreeNode node = DatabaseTreeView.SelectedNode;
            if (node.Tag is not null && node.Tag is DatabaseTag tg)
            {
                string connectionName = node?.Parent?.Parent?.Parent?.Parent?.Parent?.Text;
                if (string.IsNullOrWhiteSpace(connectionName))
                {
                    MessageBox.Show(this, "Could not update the column comment.", "Edit column comment", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                int columnID = tg.OBJECT_ID;

                string colName = _completionContext.ColumnTablesDictionary[connectionName][columnID].COLUMN_NAME;
                int tableId = _completionContext.ColumnTablesDictionary[connectionName][columnID].TABLE_ID;
                string tableName = NetezzaHelpers.baseTableDictionary[connectionName][tableId].TABLE_NAME;

                string tableOwner = NetezzaHelpers.baseTableDictionary[connectionName][tableId].TABLE_OWNER;
                string dbName = _completionContext.DatabaseDictionary[connectionName][NetezzaHelpers.baseTableDictionary[connectionName][tableId].DATABASE_ID].DatabaseName;
                string actualDesc = _completionContext.ColumnTablesDictionary[connectionName][columnID].COLUMN_DESCRIPTION ?? "";


                var d1 = new DbForms.ColumnEditNetezzaForm(actualDesc, o => _colorTheme.ColorForm(o));

                if (d1.ShowDialog() == DialogResult.OK)
                {
                    string finalDesc = d1.finalDesc;
                    string editColumnSql = $"COMMENT ON COLUMN {dbName}.{tableOwner}.{tableName}.{colName} IS '{finalDesc.Replace("'", "''")}';";
                    node.ToolTipText = finalDesc;

                    try
                    {
                        using (DbConnection conn = IGeneralDbService.GeneralDic[connectionName].GetConnection())
                        {
                            conn.Open();
                            using (DbCommand cmd = conn.CreateCommand())
                            {
                                cmd.CommandText = editColumnSql;
                                cmd.CommandTimeout = 5;
                                cmd.ExecuteNonQuery();
                            }
                        }

                        var columnData = _completionContext.ColumnTablesDictionary[connectionName][columnID];
                        _completionContext.ColumnTablesDictionary[connectionName][columnID] = new NetezzaColumnInfoRow()
                        {
                            COLUMN_NUMBER = columnData.COLUMN_NUMBER,
                            TABLE_ID = columnData.TABLE_ID,
                            DATABASE_ID = columnData.COLUMN_NUMBER,
                            COLUMN_NAME = columnData.COLUMN_NAME,
                            COLUMN_DESCRIPTION = finalDesc,
                            DATA_TYPE = columnData.DATA_TYPE,
                            IS_NULLABLE = columnData.IS_NULLABLE,
                            DISTSEQNO = columnData.DISTSEQNO,
                            ORGSEQNO = columnData.ORGSEQNO,
                            COLDEFAULT = columnData.COLDEFAULT
                        };
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, ex.Message);
                        throw;
                    }
                }
            }
        }

        private void GenerateStatisticsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var tag = (DatabaseTreeView.SelectedNode.Tag as DatabaseTag);
            if (tag != null /*&& tag.KIND_ID == DatabaseObjectTypes.table*/)
            {
                string connectionName = DatabaseTreeView.SelectedNode.Parent.Parent.Parent.Text;
                string databaseName = _completionContext.DatabaseDictionary[connectionName][NetezzaHelpers.baseTableDictionary[connectionName][tag.OBJECT_ID].DATABASE_ID].DatabaseName;
                var tableData = NetezzaHelpers.baseTableDictionary[connectionName][tag.OBJECT_ID];

                string s = $"GENERATE EXPRESS STATISTICS ON {databaseName}..{tableData.TABLE_NAME};";
                s += $"\r\n--https://www.ibm.com/docs/en/netezza?topic=reference-generate-express-statistics";
                _baseWindow.AddMainTab(null, $"stats for {tableData.TABLE_NAME}", s);
            }
        }

        private void SelectFromSequence_Click(object sender, EventArgs e)
        {
            string nme = DatabaseTreeView.SelectedNode.Text;
            _baseWindow.AddMainTab(null, $"SELECT FROM {nme}", $"SELECT NEXT VALUE FOR {nme};");
        }

        private void SelectDeletedRows_Click(object sender, EventArgs e)
        {
            var tag = (DatabaseTreeView.SelectedNode.Tag as DatabaseTag);
            if (tag != null)
            {
                int objectID = tag.OBJECT_ID;
                var res = GetDeletedRowsCodeById(objectID, DatabaseTreeView.SelectedNode.Parent.Parent.Parent.Text);
                _baseWindow.AddMainTab(null, res.Item1, res.Item2);
            }
        }

        private void SelectClipboardDuplicates_Click(object sender, EventArgs e)
        {
            var tag = (DatabaseTreeView.SelectedNode.Tag as DatabaseTag);
            if (tag != null /*&& tag.KIND_ID == DatabaseObjectTypes.table*/)
            {
                string connectionName = DatabaseTreeView.SelectedNode.Parent.Parent.Parent.Text;
                string tableOwner = NetezzaHelpers.baseTableDictionary[connectionName][tag.OBJECT_ID].TABLE_OWNER;
                string databaseName = _completionContext.DatabaseDictionary[connectionName][NetezzaHelpers.baseTableDictionary[connectionName][tag.OBJECT_ID].DATABASE_ID].DatabaseName;
                var tableData = NetezzaHelpers.baseTableDictionary[connectionName][tag.OBJECT_ID];

                StringBuilder sb = new StringBuilder();

                int firstColumnId = NetezzaHelpers.baseTableDictionary[connectionName][tag.OBJECT_ID].FIRST_COLUMN_ID;
                int columnCount = NetezzaHelpers.baseTableDictionary[connectionName][tag.OBJECT_ID].COLUMN_COUNT;
                for (int i = 0; i < columnCount; i++)
                {
                    int columnId = firstColumnId + i;
                    if (i != 0)
                    {
                        sb.Append("\n    , ");
                    }
                    else
                    {
                        sb.Append("\n    ");

                    }
                    sb.Append($"T1.{_completionContext.ColumnTablesDictionary[connectionName][columnId].COLUMN_NAME}");
                }
                Clipboard.SetText(@$"SELECT {sb.ToString()}{Environment.NewLine}    , COUNT(1) {Environment.NewLine}FROM {Environment.NewLine}   {databaseName}.{tableOwner}.{tableData.TABLE_NAME} T1{Environment.NewLine}GROUP BY {sb.ToString()} {Environment.NewLine}HAVING {Environment.NewLine}    COUNT(1) > 1 {Environment.NewLine}LIMIT 500;
                " ?? "");
            }

        }

        private void GroomTableNetezza_Click(object sender, EventArgs e)
        {
            if (DatabaseTreeView.SelectedNode.Tag is not null && DatabaseTreeView.SelectedNode.Tag is DatabaseTag tg)
            {
                string connectionName = DatabaseTreeView.SelectedNode?.Parent?.Parent?.Parent?.Text;
                if (string.IsNullOrWhiteSpace(connectionName))
                {
                    MessageBox.Show(this, "Could not run GROOM.", "GROOM", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                int objectID = tg.OBJECT_ID;
                var tableData = NetezzaHelpers.baseTableDictionary[connectionName][objectID];
                string tableName = tableData.TABLE_NAME;
                string dbName = _completionContext.DatabaseDictionary[connectionName][NetezzaHelpers.baseTableDictionary[connectionName][objectID].DATABASE_ID].DatabaseName;

                var d1 = new GroomForm($"{dbName}..{tableName}", o => _colorTheme.ColorForm(o));
                var d = d1.ShowDialog();
                if (d == DialogResult.OK)
                {
                    _baseWindow.AddMainTab(null, $"groom of {dbName}..{tableName}", "--PLEASE VERIFY THIS SQL\r\n" + d1.ResultSql + "\r\n");
                }
            }
        }

        private void GrantClipboardTabeli_Click(object sender, EventArgs e)
        {
            var tag = (DatabaseTreeView.SelectedNode.Tag as DatabaseTag);
            if (tag != null /*&& tag.KIND_ID == DatabaseObjectTypes.table*/)
            {
                string connectionName = DatabaseTreeView.SelectedNode.Parent.Parent.Parent.Text;
                string databaseName = _completionContext.DatabaseDictionary[connectionName][NetezzaHelpers.baseTableDictionary[connectionName][tag.OBJECT_ID].DATABASE_ID].DatabaseName;
                var tableData = NetezzaHelpers.baseTableDictionary[connectionName][tag.OBJECT_ID];
                string qualifiedName = $"{databaseName}..{tableData.TABLE_NAME}";
                string s = NetezzaDdlTemplates.GetGrantSelectSql(qualifiedName);
                Clipboard.SetText(s ?? "");
            }
        }

        private void SelectTableNetezza_Click(object sender, EventArgs e)
        {
            var tag = (DatabaseTreeView.SelectedNode.Tag as DatabaseTag);
            if (tag != null /*&& tag.KIND_ID == DatabaseObjectTypes.table*/)
            {
                string connectionName = DatabaseTreeView.SelectedNode.Parent.Parent.Parent.Text;
                string tableOwner = NetezzaHelpers.baseTableDictionary[connectionName][tag.OBJECT_ID].TABLE_OWNER;
                //string objectType = BaseWindow.tableMetadata[tag.tableId].objectType;
                string databaseName = _completionContext.DatabaseDictionary[connectionName][NetezzaHelpers.baseTableDictionary[connectionName][tag.OBJECT_ID].DATABASE_ID].DatabaseName;
                var tableData = NetezzaHelpers.baseTableDictionary[connectionName][tag.OBJECT_ID];

                StringBuilder sb = new StringBuilder();

                sb.Append("SELECT");

                int firstColumnId = NetezzaHelpers.baseTableDictionary[connectionName][tag.OBJECT_ID].FIRST_COLUMN_ID;
                int columnCount = NetezzaHelpers.baseTableDictionary[connectionName][tag.OBJECT_ID].COLUMN_COUNT;
                for (int i = 0; i < columnCount; i++)
                {
                    int columnId = firstColumnId + i;
                    if (i != 0)
                    {
                        sb.Append("\n    , ");
                    }
                    else
                    {
                        sb.Append("\n    ");

                    }
                    sb.Append($"T1.{_completionContext.ColumnTablesDictionary[connectionName][columnId].COLUMN_NAME} --{_completionContext.ColumnTablesDictionary[connectionName][columnId].DATA_TYPE} ");
                }
                sb.AppendLine();
                sb.AppendLine($"FROM\n    {databaseName}.{tableOwner}.{tableData.TABLE_NAME} T1\nLIMIT 100;");

                if (sender == selectClipboardTabeli || sender == selectView)
                {
                    Clipboard.SetText(sb.ToString() ?? "");
                }
                else if (sender == selectTabeliNetezza)
                {
                    _baseWindow.AddMainTab(null, "databaseName - select", sb.ToString());
                }
            }
        }

        private void ImortDataToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                string txt = DatabaseTreeView.SelectedNode.Text;
                string dbName = DatabaseTreeView.SelectedNode.Parent.Parent.Text;
                var d = new DbForms.ImportTableDataNetezza(dbName, txt, o => _colorTheme.ColorForm(o), _applicationSettingsContext.ConfigDirectory);
                if (d.ShowDialog() == DialogResult.OK)
                {
                    _baseWindow.AddMainTab(null, $"external - {txt}", d.GetCode);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message);
            }
        }

        private void DdlCipSequence_Click(object sender, EventArgs e)
        {
            var tag = (DatabaseTreeView.SelectedNode.Tag as DatabaseTag);
            if (tag != null)
            {
                int objectID = tag.OBJECT_ID;
                string db;
                if (DatabaseTreeView.SelectedNode.Parent.Parent.Parent != null)
                {
                    db = DatabaseTreeView.SelectedNode.Parent.Parent.Parent.Text;
                }
                else
                {
                    MessageBox.Show(this, "Could not clip the sequence.", "Clip sequence", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    db = _completionContext.SelectedDatabase;
                }

                Clipboard.SetText(_baseWindow.GetSeqCodeById(objectID, db) ?? "");
            }
        }

        private async void DdlProcNzNewWindow_Click(object sender, EventArgs e)
        {
            Application.UseWaitCursor = true;
            int objectID = (DatabaseTreeView.SelectedNode.Tag as DatabaseTag).OBJECT_ID;
            string connection = DatabaseTreeView.SelectedNode.Parent.Parent.Parent.Text;
            string objectName = NetezzaHelpers.baseTableDictionary[connection][objectID].TABLE_NAME;

            try
            {
                string result = await GetProcCodeById(objectID, connection);
                if (result != null)
                {
                    if (DatabaseTreeView.InvokeRequired)
                    {
                        DatabaseTreeView.Invoke(() =>
                        {
                            _baseWindow.AddMainTab(null, objectName, result);
                        });
                    }
                    else
                    {
                        _baseWindow.AddMainTab(null, objectName, result);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (DatabaseTreeView.InvokeRequired)
                {
                    DatabaseTreeView.Invoke(() =>
                    {
                        Application.UseWaitCursor = false;
                    });
                }
                else
                {
                    Application.UseWaitCursor = false;
                }
            }
        }

        private async Task<string> GetProcCodeById(int objectID, string connectionName)
        {
            string databaseName = _completionContext.DatabaseDictionary[connectionName][NetezzaHelpers.baseTableDictionary[connectionName][objectID].DATABASE_ID].DatabaseName;

            int l1 = default;
            string procSign = default;
            string procReturns = default;
            bool execAsOwner = default;
            object desc = default;
            string procSource = "";

            await Task.Run(() =>
            {
                using DbConnection connection = IGeneralDbService.GeneralDic[connectionName].GetConnection(databaseName);
                connection.Open();
                using var cmd1 = connection.CreateCommand();
                cmd1.CommandText = NetezzaSystemSql.GetProcedureByObjectId(objectID);
                using DbDataReader rdr = cmd1.ExecuteReader();
                while (rdr.Read())
                {
                    l1 = (int)rdr.GetValue(0);
                    procSign = (string)rdr.GetValue(1);
                    procReturns = (string)rdr.GetValue(2);
                    execAsOwner = (bool)rdr.GetValue(3);
                    desc = rdr.GetValue(4);
                    procSource = rdr.GetString(5);
                }
                procReturns = NetezzaHelpers.NzProcReturnFix(procReturns);

            });

            string schema = NetezzaHelpers.baseTableDictionary[connectionName][objectID].TABLE_OWNER;
            var input = NetezzaDdlInputFactory.BuildProcedureFromSignature(
                databaseName,
                schema,
                procSign,
                procReturns,
                procSource,
                execAsOwner,
                desc == DBNull.Value ? null : desc?.ToString());
            return new NetezzaDdlTextBuilder().BuildCreateProcedure(input);

        }

        private async void DdlClipboardProc_Click(object sender, EventArgs e)
        {
            Application.UseWaitCursor = true;
            int objectID = (DatabaseTreeView.SelectedNode.Tag as DatabaseTag).OBJECT_ID;
            try
            {
                string result = await GetProcCodeById(objectID, DatabaseTreeView.SelectedNode.Parent.Parent.Parent.Text);
                if (result != null)
                {
                    if (DatabaseTreeView.InvokeRequired)
                    {
                        DatabaseTreeView.Invoke(() =>
                        {
                            Clipboard.SetText(result ?? "");
                        });
                    }
                    else
                    {
                        Clipboard.SetText(result ?? "");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (DatabaseTreeView.InvokeRequired)
                {
                    DatabaseTreeView.Invoke(() =>
                    {
                        Application.UseWaitCursor = false;
                    });
                }
                else
                {
                    Application.UseWaitCursor = false;
                }
            }

        }
        private void TsmDropNetezzaTable_Click(object sender, EventArgs e)
        {

            string smallWord = "table";
            string bigWord = "TABLE";
            if (sender == dropSequenceToolStripMenuItem)
            {
                smallWord = "sequence";
                bigWord = "SEQUENCE";
            }


            TreeNode node = DatabaseTreeView.SelectedNode;
            if (node.Tag is not null && node.Tag is DatabaseTag tg)
            {
                string connectionName = DatabaseTreeView.SelectedNode?.Parent?.Parent?.Parent?.Text;
                if (string.IsNullOrWhiteSpace(connectionName))
                {
                    MessageBox.Show(this, $"Could not drop the {bigWord.ToLower()}.", "Drop object", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                int objectID = tg.OBJECT_ID;
                var tableData = NetezzaHelpers.baseTableDictionary[connectionName][objectID];
                string tableName = tableData.TABLE_NAME;
                string dbName = _completionContext.DatabaseDictionary[connectionName][NetezzaHelpers.baseTableDictionary[connectionName][objectID].DATABASE_ID].DatabaseName;

                var d = MessageBox.Show(this, $"Drop {smallWord} {tableName} (this action cannot be undone)?", $"Warning - {tableName}", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);

                if (d == DialogResult.Yes)
                {
                    string sql = _baseWindow.GetDropTableSequenceCodeById(objectID, bigWord);

                    try
                    {
                        using (DbConnection conn = IGeneralDbService.GeneralDic[connectionName].GetConnection())
                        {
                            conn.Open();
                            using (DbCommand cmd = conn.CreateCommand())
                            {
                                cmd.CommandText = sql;
                                cmd.CommandTimeout = 5;
                                cmd.ExecuteNonQuery();
                            }
                        }
                        var parentNode = node.Parent;
                        parentNode.Nodes.Remove(node);
                        _completionContext.DatabaseSchemaLookup[connectionName][dbName].Remove(tableName);
                        _netezzaSqlCompletionServices.InvalidateSchema();
                        _netezzaSqlCompletionServices.EnsureSchemaForConnection(_completionContext, connectionName);
                        DynamicCollectionForNettezaHelpers.ResetCache();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, ex.Message);
                    }
                }
            }
        }

        private void TsmiDropColumnNetezza_Click(object sender, EventArgs e)
        {
            TreeNode node = DatabaseTreeView.SelectedNode;
            if (node.Tag is not null && node.Tag is DatabaseTag tg)
            {
                string connectionName = node?.Parent?.Parent?.Parent?.Parent?.Parent?.Text;
                if (string.IsNullOrWhiteSpace(connectionName))
                {
                    MessageBox.Show(this, "The operation failed.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                int columnID = tg.OBJECT_ID;

                string colName = _completionContext.ColumnTablesDictionary[connectionName][columnID].COLUMN_NAME;
                int tableId = _completionContext.ColumnTablesDictionary[connectionName][columnID].TABLE_ID;
                string tableName = NetezzaHelpers.baseTableDictionary[connectionName][tableId].TABLE_NAME;

                string tableOwner = NetezzaHelpers.baseTableDictionary[connectionName][tableId].TABLE_OWNER;
                string dbName = _completionContext.DatabaseDictionary[connectionName][NetezzaHelpers.baseTableDictionary[connectionName][tableId].DATABASE_ID].DatabaseName;
                //string actualDesc = _baseWindowHelpers.columnDescriptionById[_baseWindowHelpers.tableColumns[connectionName][columnID].descriptionId] ?? "";


                DialogResult d = DialogResult.Yes;
                if (sender == tsmiDropColumnNetezza)
                {
                    string txt1 = "Drop column - restrict mode (this action cannot be undone)?" +
"\nschema refresh may be needed for properly working autocomplete and some other functions";
                    d = MessageBox.Show(this, txt1, "Warning", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
                }
                else
                {
                }
                string newColNameFull = "";
                string newColName = "";

                if (d == DialogResult.Yes)
                {
                    string sql = "";
                    if (sender == tsmiDropColumnNetezza)
                    {
                        sql = $"ALTER TABLE {dbName}.{tableOwner}.{tableName} DROP COLUMN {colName} RESTRICT;";
                    }
                    else if (sender == addColumnToolStripMenuItem)
                    {
                        var tmpForm = new AddColumnForm(o => _colorTheme.ColorForm(o));
                        var res = tmpForm.ShowDialog();
                        if (res == DialogResult.Cancel)
                            return;

                        newColNameFull = tmpForm.ChosedColumn;
                        newColName = tmpForm.ChosedColumnName;

                        sql = $"ALTER TABLE {dbName}.{tableOwner}.{tableName} ADD COLUMN {newColNameFull};";
                    }
                    try
                    {
                        using (DbConnection conn = IGeneralDbService.GeneralDic[connectionName].GetConnection())
                        {
                            conn.Open();
                            using (DbCommand cmd = conn.CreateCommand())
                            {
                                cmd.CommandText = sql;
                                cmd.CommandTimeout = 5;
                                cmd.ExecuteNonQuery();
                            }
                        }
                        TreeNode parentNode = node.Parent;
                        if (sender == tsmiDropColumnNetezza)
                        {
                            parentNode.Nodes.Remove(node);
                        }
                        else if (sender == addColumnToolStripMenuItem)
                        {
                            parentNode.Nodes.Add(newColName, newColNameFull);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, ex.Message);
                    }
                }
            }
        }

        private void CbSearchDb_SelectedIndexChanged(object sender, EventArgs e)
        {
            TbFastSchemaSearch_TextChanged(this, null);
        }

        private void TbFastSchemaSearch_TextChanged(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(_completionContext.SelectedConnectionName))
            {
                string driverName = _generalDbService.DriverName(_completionContext.SelectedConnectionName);
                if (driverName == "NetezzaSQL")
                {
                    if (_searchTimer is null)
                    {
                        SearchTimerInitialize();
                    }
                    _searchTimer.Stop();
                    _searchTimer.Start();
                }
            }
        }
        private void DgvFastDbBrowser_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                e.Value = _dtFastSearch.Rows[e.RowIndex][e.ColumnIndex];
            }
        }

        private async Task DoSearch(bool disable = true)
        {
            string driverName = _generalDbService.DriverName(_completionContext.SelectedConnectionName);
            TbFastSchemaSearch.ReadOnly = true;
            if (disable)
            {
                _baseWindow.SchemaRefreshOptionEnable(false);
            }

            string txtToSearch = TbFastSchemaSearch.Text;

            if (_dtFastSearch == null)
            {
                DgvFastDbBrowser.Columns.Clear();
                _dtFastSearch = new DataTable();

                var col = _dtFastSearch.Columns.Add("Type");
                col.AllowDBNull = true;
                col = _dtFastSearch.Columns.Add("Name");
                col.AllowDBNull = true;
                col = _dtFastSearch.Columns.Add("Db");
                col.AllowDBNull = true;
                col = _dtFastSearch.Columns.Add("Desc");
                col.AllowDBNull = true;
                col = _dtFastSearch.Columns.Add("Schema");
                col.AllowDBNull = true;
                DgvFastDbBrowser.RowCount = 0;
                DgvFastDbBrowser.ColumnCount = 5;
                DgvFastDbBrowser.Columns[0].HeaderText = "Type";
                DgvFastDbBrowser.Columns[1].HeaderText = "Name";
                DgvFastDbBrowser.Columns[2].HeaderText = "Db";
                DgvFastDbBrowser.Columns[3].HeaderText = "Desc";
                DgvFastDbBrowser.Columns[4].HeaderText = "Schema";

                _colorTheme.ColorDataGridView(DgvFastDbBrowser);
                _uiHelperService.DoubleBufDateGridView(DgvFastDbBrowser);
            }
            else
            {
                DgvFastDbBrowser.RowCount = 0;
                _dtFastSearch.Clear();
            }
            if (String.IsNullOrEmpty(txtToSearch))
            {
                //splitContainer2.SplitterDistance = splitContainer2.Height;
                splitContainerBase.Panel2Collapsed = true;
                splitContainerBase.Panel2.Hide();
                _baseWindow.SchemaRefreshOptionEnable(true);
                if (disable)
                {
                    _baseWindow.SchemaRefreshOptionEnable(true);
                }

                TbFastSchemaSearch.ReadOnly = false;

                return;
            }

            bool fastMode = driverName == "NetezzaSQL" && _applicationSettingsContext.Config.RefreshMode == 1;
            if (!fastMode)
            {
                splitContainerBase.Panel2Collapsed = false;
                splitContainerBase.Panel2.Show();
            }

            List<string> dbs = new List<string>();
            for (int i = 1; i < CbWhatDb.Items.Count; i++)
            {
                dbs.Add(CbWhatDb.Items[i].ToString());
            }

            if (driverName == "NetezzaSQL")
            {
                string whatDb = CbWhatDb.Text;
                if (txtToSearch.Contains('\''))
                {
                    txtToSearch = txtToSearch.Replace("'", "''");
                }

                if (CbSearchDb.Text == "table/view/etc. or column" || CbSearchDb.Text == "table/view/etc.")
                {
                    try
                    {
                        if (IGeneralDbService.GeneralDic.TryGetValue(_completionContext.SelectedConnectionName, out var db) && db is INetezza netezza)
                        {
                            DgvFastDbBrowser.RowCount = 0;
                            if ((CbSearchDb.Text == "table/view/etc." || CbSearchDb.Text == "table/view/etc. or column") &&
NetezzaHelpers.baseTableDictionary.TryGetValue(_completionContext.SelectedConnectionName, out var res))
                            {
                                foreach (var item in res)
                                {
                                    int id = item.Key;
                                    var vall = item.Value;
                                    var columnCnt = vall.COLUMN_COUNT;
                                    var l2 = vall.FIRST_COLUMN_ID;

                                    bool columnOk = false;
                                    if (CbSearchDb.Text != "table/view/etc. or column")
                                    {
                                        columnOk = true;
                                    }
                                    if (!columnOk && columnCnt > 0 && _completionContext.ColumnTablesDictionary is not null
                                        && _completionContext.ColumnTablesDictionary.TryGetValue(_completionContext.SelectedConnectionName, out var tempAr))
                                    {
                                        for (int j = 0; j < columnCnt; j++)
                                        {
                                            var tmp = tempAr[l2 + j];
                                            var colName = tmp.COLUMN_NAME;
                                            var descTxt = tmp.COLUMN_DESCRIPTION;
                                            if (colName.Contains(txtToSearch, StringComparison.OrdinalIgnoreCase)
                                                || descTxt?.Contains(txtToSearch, StringComparison.OrdinalIgnoreCase) == true
                                                )
                                            {
                                                columnOk = true;
                                                break;
                                            }
                                        }
                                    }

                                    string currDb = netezza.DatabaseIdToName[vall.DATABASE_ID];
                                    if (
                                        (whatDb == "all" || currDb == whatDb)
                                        &&
                                        (columnOk && CbSearchDb.Text == "table/view/etc. or column"
                                        || vall.TABLE_NAME.Contains(txtToSearch, StringComparison.OrdinalIgnoreCase)
                                            || vall.TABLE_DESC?.Contains(txtToSearch, StringComparison.OrdinalIgnoreCase) == true
                                            )
                                        )
                                    {
                                        string roadzajTxt = "";
                                        if (!_textTypeInDatabase.TryGetValue(vall.TABLE_KIND, out var outTxt))
                                        {
                                            _textTypeInDatabase[vall.TABLE_KIND] = vall.TABLE_KIND.ToString().ToUpper();
                                        }
                                        roadzajTxt = _textTypeInDatabase[vall.TABLE_KIND];

                                        if (vall.TABLE_KIND == TypeInDatabase.thisExternal)
                                        {
                                            roadzajTxt = "EXTERNAL TABLE";
                                        }

                                        _dtFastSearch.Rows.Add(new string[] {
                                                roadzajTxt,
                                                vall.TABLE_NAME,
                                                currDb,
                                                vall.TABLE_DESC,
                                                vall.TABLE_OWNER
                                            });
                                        if (_dtFastSearch.Rows.Count > 1_000)
                                        {
                                            break;
                                        }
                                    }
                                }
                            }
                            DgvFastDbBrowser.RowCount = _dtFastSearch.Rows.Count;
                        }
                    }
                    catch (Exception ex)
                    {
                        this.Invoke(() =>
                        {
                            MessageBox.Show(this, ex.Message);
                        });
                    }
                    finally
                    {
                        _baseWindow.SchemaRefreshOptionEnable(true);
                    }
                }
                else if (CbSearchDb.Text == "sources" && IGeneralDbService.GeneralDic.TryGetValue(_completionContext.SelectedConnectionName, out var db5)
                    && db5 is INetezza netezza5)
                {
                    if (netezza5.ProcCache is null)
                    {
                        var t1 = netezza5.LoadSourceTextCache();
                        MessageBox.Show(this, "Please wait for the current operation to finish.", "Please wait", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        await t1;
                    }

                    DgvFastDbBrowser.RowCount = 0;
                    foreach (var item2 in netezza5.ViewCache)
                    {
                        Parallel.ForEach(item2.Value, item =>
                        {
                            if (item.name.Contains(txtToSearch, StringComparison.OrdinalIgnoreCase)
                                || item.DESCRIPTION?.Contains(txtToSearch, StringComparison.OrdinalIgnoreCase) == true
                                || item.DEFINITION?.Contains(txtToSearch, StringComparison.OrdinalIgnoreCase) == true
                                )
                            {
                                Monitor.Enter(this);
                                _dtFastSearch.Rows.Add(new string[] {
                                            "VIEW",
                                            item.name,
                                            item.database,
                                            item.DESCRIPTION,
                                            item2.Key
                                        });
                                Monitor.Exit(this);
                            }
                        });
                    }
                    foreach (var item2 in netezza5.ProcCache)
                    {
                        Parallel.ForEach(item2.Value, item =>
                        {
                            if (item.name.Contains(txtToSearch, StringComparison.OrdinalIgnoreCase)
                                || item.DESCRIPTION?.Contains(txtToSearch, StringComparison.OrdinalIgnoreCase) == true
                                || item.DEFINITION?.Contains(txtToSearch, StringComparison.OrdinalIgnoreCase) == true
                                )
                            {
                                Monitor.Enter(this);
                                _dtFastSearch.Rows.Add(new string[]
                                {
                                                "PROCEDURE",
                                                item.name,
                                                item.database,
                                                item.DESCRIPTION,
                                                item2.Key
                                });
                                Monitor.Exit(this);
                            }
                        });
                    }
                    foreach (var item2 in netezza5.ExternalCache)
                    {
                        foreach (var item in item2.Value)
                        {
                            if (item.name.Contains(txtToSearch, StringComparison.OrdinalIgnoreCase)
    || item.DESCRIPTION?.Contains(txtToSearch, StringComparison.OrdinalIgnoreCase) == true
    || item.extobjname?.Contains(txtToSearch, StringComparison.OrdinalIgnoreCase) == true
    )
                            {
                                _dtFastSearch.Rows.Add(new string[]
                                {
                                                "EXTERNAL TABLE",
                                                item.name,
                                                item.database,
                                                item.DESCRIPTION,
                                                item2.Key
                                });
                            }
                        }
                    }
                    foreach (var item2 in netezza5.SynonymCache)
                    {
                        foreach (var item in item2.Value)
                        {
                            if (item.name.Contains(txtToSearch, StringComparison.OrdinalIgnoreCase)
    || item.DESCRIPTION?.Contains(txtToSearch, StringComparison.OrdinalIgnoreCase) == true
    || item.refobjname?.Contains(txtToSearch, StringComparison.OrdinalIgnoreCase) == true
    )
                            {
                                _dtFastSearch.Rows.Add(new string[]
                                {
                                                "SYNONYM",
                                                item.name,
                                                item.database,
                                                item.DESCRIPTION,
                                                item2.Key
                                });
                            }
                        }
                    }

                    DgvFastDbBrowser.RowCount = _dtFastSearch.Rows.Count;
                }
            }
            else
            {
                try
                {
                    DgvFastDbBrowser.RowCount = 0;
                    //generalDic[SelectedConnectionName].getCreateAllTablesText
                    string connectionName = _completionContext.SelectedConnectionName;
                    string db = _completionContext.SelectedDatabase;
                    string searchType = CbSearchDb.Text;

                    if (IGeneralDbService.GeneralDic.ContainsKey(connectionName))
                    {
                        if (searchType == "table/view/etc.")
                        {
                            foreach (string owner in IGeneralDbService.GeneralDic[connectionName].objectInSchema.Keys)
                            {
                                foreach (var (tempName, tempType) in IGeneralDbService.GeneralDic[connectionName].objectInSchema[owner])
                                {
                                    if (tempName.Contains(txtToSearch, StringComparison.OrdinalIgnoreCase))
                                    {
                                        string type2 = "";
                                        if (tempType == TypeInDatabase.table)
                                        {
                                            type2 = "Tables";
                                        }
                                        else if (tempType == TypeInDatabase.view)
                                        {
                                            type2 = "Views";
                                        }
                                        else if (tempType == TypeInDatabase.procedure)
                                        {
                                            type2 = "Procedures";
                                        }
                                        else if (tempType == TypeInDatabase.synonym)
                                        {
                                            type2 = "Synonyms";
                                        }
                                        else if (tempType == TypeInDatabase.db2alias)
                                        {
                                            type2 = "Aliases";
                                        }
                                        else
                                        {
                                            continue;
                                        }
                                        _dtFastSearch.Clear();
                                        if (_dtFastSearch.Columns.Count == 0)
                                        {
                                            var col = _dtFastSearch.Columns.Add("Type");
                                            col.AllowDBNull = true;
                                            col = _dtFastSearch.Columns.Add("Name");
                                            col.AllowDBNull = true;
                                            col = _dtFastSearch.Columns.Add("Db");
                                            col.AllowDBNull = true;
                                            col = _dtFastSearch.Columns.Add("Desc");
                                            col.AllowDBNull = true;
                                            col = _dtFastSearch.Columns.Add("Schema");
                                            col.AllowDBNull = true;
                                            _dtFastSearch.Constraints.Clear();
                                        }

                                        _dtFastSearch.Rows.Add(new string[] { type2, tempName, db, "", owner });
                                        DgvFastDbBrowser.Columns[0].Width = 80;
                                        DgvFastDbBrowser.Columns[1].Width = 100;
                                        DgvFastDbBrowser.Columns[2].Width = 60;
                                        DgvFastDbBrowser.Columns[3].Width = 50;
                                        DgvFastDbBrowser.Columns[4].Width = 90;

                                    }
                                }
                            }
                        }
                        else if (searchType == "table/view/etc. or column")
                        {
                            this.Invoke(() =>
                            {
                                MessageBox.Show(this, "This feature is not implemented yet.", "Not implemented", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            });
                        }
                        else if (searchType == "sources")
                        {
                            string sql = IGeneralDbService.GeneralDic[connectionName].SearchInProcedureSource(txtToSearch);

                            using (var conn = IGeneralDbService.GeneralDic[connectionName].GetConnection())
                            {
                                conn.Open();
                                using (var cmd = conn.CreateCommand())
                                {
                                    cmd.CommandText = sql;
                                    cmd.CommandTimeout = 180;
                                    var rdr = cmd.ExecuteReader();

                                    _dtFastSearch.Load(rdr);
                                    DgvFastDbBrowser.Columns[0].Width = 80;
                                    DgvFastDbBrowser.Columns[1].Width = 100;
                                    DgvFastDbBrowser.Columns[2].Width = 60;
                                    DgvFastDbBrowser.Columns[3].Width = 50;
                                    DgvFastDbBrowser.Columns[4].Width = 90;
                                }
                            }
                        }
                    }
                    DgvFastDbBrowser.RowCount = _dtFastSearch.Rows.Count;
                }
                catch (Exception ex)
                {
                    this.Invoke(() => MessageBox.Show(this, ex.Message));
                }
                finally
                {
                    _baseWindow.SchemaRefreshOptionEnable(true);
                }
            }

            if (disable)
            {
                _baseWindow.SchemaRefreshOptionEnable(true);
            }

            if (fastMode)
            {
                splitContainerBase.Panel2Collapsed = false;
                splitContainerBase.Panel2.Show();
            }

            TbFastSchemaSearch.ReadOnly = false;
            TbFastSchemaSearch.Focus();
        }

        private System.Windows.Forms.Timer _searchTimer;
        private void SearchTimerInitialize()
        {
            _searchTimer = new System.Windows.Forms.Timer();
            _searchTimer.Interval = 100;
            _searchTimer.Tick += async (_, _) =>
            {
                _searchTimer.Stop();
                await DoSearch();
            };
        }

        private void DgvFastDbBrowser_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex != -1 && e.ColumnIndex != -1)
            {
                string objectType = DgvFastDbBrowser[0, e.RowIndex].Value.ToString();
                string objectName = DgvFastDbBrowser[1, e.RowIndex].Value.ToString();
                string objectDb = DgvFastDbBrowser[2, e.RowIndex].Value.ToString();

                if (objectType == "TABLE")
                {
                    ExpandBaseToTable(objectDb, objectName, "Tables", _completionContext.SelectedConnectionName);
                }
                else if (objectType == "VIEW")
                {
                    ExpandBaseToTable(objectDb, objectName, "Views", _completionContext.SelectedConnectionName);
                }
                else if (objectType == "EXTERNAL TABLE")
                {
                    ExpandBaseToTable(objectDb, objectName, "External Tables", _completionContext.SelectedConnectionName);
                }
                else if (objectType == "SYNONYM")
                {
                    ExpandBaseToTable(objectDb, objectName, "Synonyms", _completionContext.SelectedConnectionName);
                }
                else if (objectType == "PROCEDURE")
                {
                    ExpandBaseToTable(objectDb, objectName, "Procedures", _completionContext.SelectedConnectionName);
                }
                else if (objectType == "FUNCTION")
                {
                    ExpandBaseToTable(objectDb, objectName, "Functions", _completionContext.SelectedConnectionName);
                }
                else // not netezza ?
                {
                    string owner = DgvFastDbBrowser[4, e.RowIndex].Value.ToString();
                    if (
                        IGeneralDbService.GeneralDic.TryGetValue(_completionContext.SelectedConnectionName, out var thisGeneralDic) &&
                        thisGeneralDic.objectInSchema.TryGetValue(owner, out var thisOwner) && thisOwner.ContainsKey(objectName)
                        )
                    {
                        _baseWindow.GoToObjectNotNetezza($"{owner}.{objectName}", objectType);
                    }
                }
            }
        }

        public void ExpandBaseToTable(string db, string table, string tableOrView, string connectionName)
        {
            try
            {
                _baseWindow.SelectDatabaseTab();
                connectionName ??= _baseWindow.SelectedConnectionName;

                TreeNode nd = DatabaseTreeView.Nodes[connectionName].Nodes[db];
                if (nd == null)
                    return;
                if (!nd.IsExpanded)
                {
                    nd.Expand();
                }
                if (!nd.Nodes[tableOrView].IsExpanded)
                {
                    nd.Nodes[tableOrView].Expand();
                }
                DatabaseTreeView.SelectedNode = nd.Nodes[tableOrView].Nodes[table];
                DatabaseTreeView.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Expand database error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void DdlClipboardExternal_Click(object sender, EventArgs e)
        {
            Application.UseWaitCursor = true;
            string connName = "";
            DatabaseTreeView.Invoke(() =>
            {
                Clipboard.SetText("try in a moment");
                if (DatabaseTreeView.SelectedNode.Level < 3)
                {
                    Application.UseWaitCursor = false;
                    return;
                }
                connName = DatabaseTreeView.SelectedNode.Parent.Parent.Parent.Text;
            });

            int OBJECT_ID = (DatabaseTreeView.SelectedNode.Tag as DatabaseTag).OBJECT_ID;
            string externalSQL = await _netezzaHelperService.GetExternaTableCode(_databaseRuntimeContext, OBJECT_ID, connName);

            if (externalSQL.EndsWith("_problem"))
            {
                externalSQL = await _netezzaHelperService.GetExternaTableCode(_databaseRuntimeContext, OBJECT_ID, connName, force: true);
            }

            if (externalSQL != null)
            {
                this.Invoke(() =>
                {
                    Clipboard.SetText(externalSQL);
                });
            }
            Application.UseWaitCursor = false;
            _notifyIcon1.ShowBalloonTip(1000, "JustyBaseLegacy", "Copied to clipboard", ToolTipIcon.Info);

        }

        private async void DdlNewQueryExternal_Click(object sender, EventArgs e)
        {
            Application.UseWaitCursor = true;
            try
            {
                if (DatabaseTreeView.SelectedNode?.Tag is not DatabaseTag tag || DatabaseTreeView.SelectedNode.Level < 3)
                    return;

                string connName = DatabaseTreeView.SelectedNode.Parent.Parent.Parent.Text;
                string objectName = DatabaseTreeView.SelectedNode.Text;
                string externalSQL = await _netezzaHelperService.GetExternaTableCode(_databaseRuntimeContext, tag.OBJECT_ID, connName);
                if (externalSQL.EndsWith("_problem"))
                    externalSQL = await _netezzaHelperService.GetExternaTableCode(_databaseRuntimeContext, tag.OBJECT_ID, connName, force: true);

                if (externalSQL is null)
                    return;

                void OpenTab() => _baseWindow.AddMainTab(null, $"ddl for {objectName}", externalSQL);
                if (DatabaseTreeView.InvokeRequired)
                    DatabaseTreeView.Invoke(OpenTab);
                else
                    OpenTab();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Application.UseWaitCursor = false;
            }
        }

        private readonly NotifyIcon _notifyIcon1;

        private async void DatabaseTreeView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F5)
            {
                e.Handled = true;
                await _baseWindow.RefreshAllNetezzaTablesAsync();
            }
        }

        private void DatabaseTreeView_MouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                DatabaseTreeView.SelectedNode = e.Node;
            }
        }

        private async void DatabaseTreeView_MouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (_baseWindow.CurrentTB != null)
            {
                if (e.Node.Tag != null && e.Node.Tag is DatabaseTag tdb && tdb.KIND_ID == TypeInDatabase.serverInfo)
                {
                    string path = e.Node.FullPath;
                    int indx = path.IndexOf('\\');
                    string nme = path[0..indx];
                    try
                    {
                        _baseWindow.AddMainTab(null, $"{e.Node.Text} - server info", e.Node.Name);
                        _baseWindow.CurrentTB.SelectAll();
                        _baseWindow.SelectedConnectionName = nme;
                        await _baseWindow.RunNzSQL(_baseWindow.KeepConnectionOpen);
                    }
                    catch (Exception ex)
                    {
                        this.Invoke(() => MessageBox.Show(this, "Error", ex.Message, MessageBoxButtons.OK, MessageBoxIcon.Error));
                    }
                }
                else
                {
                    if (e.Node.Tag is not null && e.Node.Tag is DatabaseTag tag && tag.KIND_ID == TypeInDatabase.table
                        && e.Node.Parent is not null && e.Node.Parent.Parent is TreeNode node
                        )
                    {
                        _baseWindow.CurrentTB.InsertText($"{node.Text}..{e.Node.Name}");
                        _baseWindow.CurrentTB.Focus();
                    }
                    else
                    {
                        _baseWindow.CurrentTB.InsertText(e.Node.Name);
                    }
                    _baseWindow.CurrentTB.Focus();
                }
            }
        }

        private int position = 0;
        private bool _ignoreRefreshing = false;
        private ContextMenuStrip emptyContextMenuStrip = new ContextMenuStrip();
        private readonly Dictionary<TreeNode, (string ImageKey, string SelectedImageKey, int ImageIndex, int SelectedImageIndex)> _loadingNodeImages = new();

        private void ShowLoadingVisual(TreeNode node)
        {
            if (!_loadingNodeImages.ContainsKey(node))
            {
                _loadingNodeImages[node] = (node.ImageKey, node.SelectedImageKey, node.ImageIndex, node.SelectedImageIndex);
            }

            ImageList? imageList = node.TreeView?.ImageList;
            if (imageList is not null && imageList.Images.IndexOfKey("hourglass.png") < 0)
            {
                imageList.Images.Add("hourglass.png", JustData.Properties.Resources.Hourglass);
            }

            int loadingIndex = imageList?.Images.IndexOfKey("hourglass.png") ?? -1;
            if (loadingIndex >= 0)
            {
                node.ImageKey = "hourglass.png";
                node.SelectedImageKey = "hourglass.png";
                node.ImageIndex = loadingIndex;
                node.SelectedImageIndex = loadingIndex;
            }

            node.TreeView?.Invalidate();
        }

        private void RestoreLoadingVisual(TreeNode node)
        {
            if (!_loadingNodeImages.Remove(node, out var original))
            {
                return;
            }

            node.ImageKey = original.ImageKey;
            node.SelectedImageKey = original.SelectedImageKey;
            node.ImageIndex = original.ImageIndex;
            node.SelectedImageIndex = original.SelectedImageIndex;
            node.TreeView?.Invalidate();
        }

        public void ClearLoadingVisuals()
        {
            foreach (var node in _loadingNodeImages.Keys.ToArray())
            {
                RestoreLoadingVisual(node);
            }
        }

        private async void DatabaseTreeView_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            if (!_ignoreRefreshing && !_completionContext.SchemaRefreshed && e.Node.Level >= 1)
            {
                ShowLoadingVisual(e.Node);
                e.Cancel = true;
                return;
            }


            TreeNode node = e.Node;
            int ind = -1;
            try
            {
                ind = node.FullPath.IndexOf("\\");
            }
            catch (Exception ex)
            {
                this.Invoke(() => MessageBox.Show(this, ex.Message));

                e.Cancel = true;
                return;
            }

            string connectionName;
            if (e.Node.Level == 0)
            {
                connectionName = e.Node.Text;
            }
            else if (ind != -1)
            {
                connectionName = node.FullPath.Substring(0, ind);
            }
            else
            {
                this.Invoke(() => MessageBox.Show(this, "Could not expand the tree node.", "Explorer", MessageBoxButtons.OK, MessageBoxIcon.Warning));
                return;
            }

            if (_generalDbService.LoginDataDic.ContainsKey(connectionName) && _generalDbService.DriverName(connectionName) != "NetezzaSQL")
            {
                if (e.Node.Level >= 2 && e.Node.Text == "Columns" && (e.Node.Parent.Parent.Text == "Tables" || e.Node.Parent.Parent.Text == "Views" || e.Node.Parent.Parent.Text == "Synonyms") && e.Node.Nodes.Count == 1 && e.Node.Nodes[0].Name == "fool")
                {
                    var columns = IGeneralDbService.GeneralDic[connectionName].GetColumnsEx(e.Node.Parent.Parent.Parent.Parent.Name, e.Node.Parent.Parent.Parent.Name, e.Node.Parent.Text);
                    e.Node.Nodes.Clear();
                    TreeNode temp1 = null;
                    for (int i = 0; i < columns.Item1.Length; i++)
                    {
                        temp1 = e.Node.Nodes.Add(columns.Item1[i], $"{columns.Item1[i]} - {columns.Item2[i]}");
                        temp1.ContextMenuStrip = emptyContextMenuStrip;
                        if (columns.Item3[i] != -1)
                        {
                            temp1.ImageIndex = 4;
                            temp1.SelectedImageIndex = 4;
                        }
                        else
                        {
                            temp1.ImageIndex = 36;
                            temp1.SelectedImageIndex = 36;
                        }
                    }
                }
                else if (e.Node.Level >= 2 && e.Node.Text == "Indexes" && (e.Node.Parent.Parent.Text == "Tables") && e.Node.Nodes.Count == 1 && e.Node.Nodes[0].Name == "fool")
                {
                    var columns = IGeneralDbService.GeneralDic[connectionName].GetIndexes(e.Node.Parent.Parent.Parent.Parent.Name, e.Node.Parent.Parent.Parent.Name, e.Node.Parent.Text);
                    e.Node.Nodes.Clear();
                    TreeNode temp1 = null;
                    for (int i = 0; i < columns.Length; i++)
                    {
                        temp1 = e.Node.Nodes.Add(columns[i], columns[i]);
                        temp1.ImageIndex = e.Node.ImageIndex;
                        temp1.SelectedImageIndex = e.Node.SelectedImageIndex;
                    }
                }
                else if (e.Node.Level >= 2 && e.Node.Text == "Partitions" && (e.Node.Parent.Parent.Text == "Tables") && e.Node.Nodes.Count == 1 && e.Node.Nodes[0].Name == "fool")
                {
                    var columns = IGeneralDbService.GeneralDic[connectionName].GetPartitions(e.Node.Parent.Parent.Parent.Parent.Name, e.Node.Parent.Parent.Parent.Name, e.Node.Parent.Text);
                    e.Node.Nodes.Clear();
                    TreeNode temp1 = null;
                    for (int i = 0; i < columns.Length; i++)
                    {
                        temp1 = e.Node.Nodes.Add(columns[i], columns[i]);
                        temp1.ImageIndex = e.Node.ImageIndex;
                        temp1.SelectedImageIndex = e.Node.SelectedImageIndex;
                    }
                }
                else if (e.Node.Level >= 2 && e.Node.Text == "Constraints" && (e.Node.Parent.Parent.Text == "Tables") && e.Node.Nodes.Count == 1 && e.Node.Nodes[0].Name == "fool")
                {
                    var columns = IGeneralDbService.GeneralDic[connectionName].GetConstraints(e.Node.Parent.Parent.Parent.Parent.Name, e.Node.Parent.Parent.Parent.Name, e.Node.Parent.Text);
                    e.Node.Nodes.Clear();
                    TreeNode temp1 = null;
                    for (int i = 0; i < columns.Length; i++)
                    {
                        temp1 = e.Node.Nodes.Add(columns[i], columns[i]);
                        temp1.ImageIndex = e.Node.ImageIndex;
                        temp1.SelectedImageIndex = e.Node.SelectedImageIndex;
                    }
                }
                else if (e.Node.Level >= 2 && e.Node.Text == "Triggers" && (e.Node.Parent.Parent.Text == "Tables") && e.Node.Nodes.Count == 1 && e.Node.Nodes[0].Name == "fool")
                {
                    var columns = IGeneralDbService.GeneralDic[connectionName].GetTriggers(e.Node.Parent.Parent.Parent.Parent.Name, e.Node.Parent.Parent.Parent.Name, e.Node.Parent.Text);
                    e.Node.Nodes.Clear();
                    TreeNode temp1 = null;
                    for (int i = 0; i < columns.Length; i++)
                    {
                        temp1 = e.Node.Nodes.Add(columns[i], columns[i]);
                        temp1.ImageIndex = e.Node.ImageIndex;
                        temp1.SelectedImageIndex = e.Node.SelectedImageIndex;
                    }
                }
                else if (e.Node.Level >= 2 && (e.Node.Parent.Text == "Tables" || e.Node.Parent.Text == "Views" || e.Node.Parent.Text == "Synonyms") && e.Node.Nodes.Count == 1 && e.Node.Nodes[0].Name == "fool") // table + loading marker
                {
                    var columns = IGeneralDbService.GeneralDic[connectionName].GetColumnsEx(e.Node.Parent.Parent.Parent.Name, e.Node.Parent.Parent.Name, e.Node.Text);
                    e.Node.Nodes.Clear();
                    TreeNode temp1 = null;
                    for (int i = 0; i < columns.Item1.Length; i++)
                    {
                        temp1 = e.Node.Nodes.Add(columns.Item1[i], $"{columns.Item1[i]} - {columns.Item2[i]}");
                        if (columns.Item3[i] != -1)
                        {
                            temp1.ImageIndex = 4;
                            temp1.SelectedImageIndex = 4;
                        }
                        else
                        {
                            temp1.ImageIndex = 36;
                            temp1.SelectedImageIndex = 36;
                        }
                    }
                }
                else if (e.Node.Level >= 2 && (e.Node.Parent.Text == "Aliases" || e.Node.Parent.Text == "Synonyms") && _generalDbService.DriverName(connectionName) == "DB2" && e.Node.Nodes[0].Name == "fool")
                {
                    var db2 = IGeneralDbService.GeneralDic[connectionName];

                    string name;
                    string desc;
                    (name, desc) = e.Node.Parent.Text switch
                    {
                        "Aliases" => await db2.GetAliasDataAsync(e.Node.Parent.Parent.Name, e.Node.Text),
                        "Synonyms" => await db2.GetSynonymDataAsync(e.Node.Parent.Parent.Name, e.Node.Text),
                        _ => ("problem", "problem"),
                    };
                    e.Node.Nodes.Clear();
                    var n2 = e.Node.Nodes.Add(name, name);
                    n2.ToolTipText = desc;
                    n2.ImageIndex = e.Node.ImageIndex;
                    n2.SelectedImageIndex = e.Node.SelectedImageIndex;
                }
                else if (e.Node.Level == 4 && e.Node.Text == "Server Objects" && _generalDbService.DriverName(connectionName) == "DB2" && e.Node.Nodes[0].Name == "fool")
                {
                    var db2 = IGeneralDbService.GeneralDic[connectionName];
                    e.Node.Nodes.Clear();

                    var ls = await db2.GetLinkedServerTablesAsync(e.Node.Parent.Text);
                    for (int i = 0; i < ls.Length; i++)
                    {
                        var n = e.Node.Nodes.Add(ls[i], ls[i]);
                        n.ContextMenuStrip = emptyContextMenuStrip;
                    }
                }
                return;
            }

            int idObj = (node.Tag as DatabaseTag).OBJECT_ID;
            TypeInDatabase typeId = (node.Tag as DatabaseTag).KIND_ID;
            string placeholderName = node.Nodes[0].Name as string;

            if (typeId == TypeInDatabase.dbase && _applicationSettingsContext.Config.RefreshMode != 1 && IGeneralDbService.GeneralDic.TryGetValue(connectionName, out var generalDb) && generalDb is INetezza netezza)
            {
                string dbName = e.Node.Text;
                if (!netezza.AttachedDbsToSchema.ContainsKey(dbName))
                {
                    if (!netezza.IsDbInProgress(dbName))
                    {
                        ShowLoadingVisual(e.Node);
                        _baseWindow.SchemaRefreshOptionEnable(false);
                        try
                        {
                            await _baseWindow.AddOneDbToNetezzaSchemaTree(connectionName, netezza, dbName);
                        }
                        catch (Exception ex)
                        {
                            this.Invoke(() => MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error));

                            e.Cancel = true;
                            e.Node.Collapse();
                        }
                        finally
                        {
                            RestoreLoadingVisual(e.Node);
                            _baseWindow.SchemaRefreshOptionEnable(true);
                        }
                    }
                    else
                    {

                    }
                }
            }

            if (e.Node.Tag == null)
            {
                this.Invoke(() => MessageBox.Show(this, "This feature is not implemented yet.", "Not implemented", MessageBoxButtons.OK, MessageBoxIcon.Information));
                e.Cancel = true;
                return;
            }

            if (LvlDB(typeId) && placeholderName == "fool")
            {
                node.Nodes[0].Remove();
                int n = _databaseRuntimeContext.BaseTableConnections[connectionName][idObj].Count;
                int limit = 100;
                position = AddBasicObjects(0, _databaseRuntimeContext.BaseTableConnections[connectionName][idObj].Count, node, idObj, typeId, limit, connectionName);
                if (n > limit && position < _databaseRuntimeContext.BaseTableConnections[connectionName][idObj].Count)
                {
                    _doContinueOnExpand = true;
                }
            }
            else if ((typeId == TypeInDatabase.view || typeId == TypeInDatabase.thisExternal) && placeholderName == "fool")
            {
                node.Nodes[0].Remove();

                int firstColumnId = NetezzaHelpers.baseTableDictionary[connectionName][idObj].FIRST_COLUMN_ID;
                int columnCount = NetezzaHelpers.baseTableDictionary[connectionName][idObj].COLUMN_COUNT;

                for (int i = 0; i < columnCount; i++)
                {
                    int columnId = firstColumnId + i;
                    var n1 = node.Nodes.Add(_completionContext.ColumnTablesDictionary[connectionName][columnId].COLUMN_NAME, $"{_completionContext.ColumnTablesDictionary[connectionName][columnId].COLUMN_NAME} - {_completionContext.ColumnTablesDictionary[connectionName][columnId].DATA_TYPE}");
                    n1.ToolTipText = _completionContext.ColumnTablesDictionary[connectionName][columnId].COLUMN_DESCRIPTION;
                    n1.Tag = new DatabaseTag() { KIND_ID = TypeInDatabase.column, OBJECT_ID = columnId };
                    n1.ImageIndex = 36;
                    n1.SelectedImageIndex = 36;
                }

            }
            else if (typeId == TypeInDatabase.table && placeholderName == "fool")
            {
                node.Nodes[0].Remove();

                var n1 = node.Nodes.Add("Columns", "Columns");
                n1.ContextMenuStrip = emptyContextMenuStrip;
                n1.Tag = new DatabaseTag() { KIND_ID = TypeInDatabase.columnInTables, OBJECT_ID = idObj };
                n1.ImageIndex = 11;
                n1.SelectedImageIndex = 11;
                n1.Nodes.Add("fool", "Loading…");

                var n2 = node.Nodes.Add("Distributed On", "Distributed On");
                n2.ContextMenuStrip = emptyContextMenuStrip;
                n2.Tag = new DatabaseTag() { KIND_ID = TypeInDatabase.distributionColumns, OBJECT_ID = idObj };
                n2.Nodes.Add("fool", "Loading…");
                n2.ImageIndex = 14;
                n2.SelectedImageIndex = 14;

                var n3 = node.Nodes.Add("References", "References");
                n3.ContextMenuStrip = emptyContextMenuStrip;
                n3.Tag = new DatabaseTag() { KIND_ID = TypeInDatabase.references, OBJECT_ID = idObj };
                n3.Nodes.Add("fool", "Loading…");
                n3.ImageIndex = 13;
                n3.SelectedImageIndex = 13;
            }
            else if (typeId == TypeInDatabase.columnInTables && placeholderName == "fool")
            {
                node.Nodes[0].Remove();

                int firstColumnId = NetezzaHelpers.baseTableDictionary[connectionName][idObj].FIRST_COLUMN_ID;
                int columnCount = NetezzaHelpers.baseTableDictionary[connectionName][idObj].COLUMN_COUNT;

                List<string> c = new List<string>();
                if (IGeneralDbService.GeneralDic[connectionName] is not INetezza nz)
                {
                    return;
                }
                if (nz.keysInTables.TryGetValue(idObj, out var ks))
                {
                    c = ks?.Where(arg => arg.keyType == 'p').Select(arg => arg.columnName).ToList();
                }

                for (int i = 0; i < columnCount; i++)
                {
                    int columnId = firstColumnId + i;
                    if (_completionContext.ColumnTablesDictionary is not null &&
                        _completionContext.ColumnTablesDictionary.TryGetValue(connectionName, out var ccTmp) &&
                        ccTmp.Count >= columnId + 1)
                    {
                        var cc = ccTmp[columnId];

                        var n1 = node.Nodes.Add(cc.COLUMN_NAME, $"{cc.COLUMN_NAME} - {cc.DATA_TYPE}{(cc.IS_NULLABLE ? " NOT NULL" : "")} ");
                        n1.ToolTipText = cc.COLUMN_DESCRIPTION;
                        n1.Tag = new DatabaseTag() { KIND_ID = TypeInDatabase.column, OBJECT_ID = columnId };
                        n1.ContextMenuStrip = cmStripColumnNetezza;

                        if (c.Contains(cc.COLUMN_NAME))
                        {
                            n1.ImageIndex = 4;
                            n1.SelectedImageIndex = 4;
                        }
                        else
                        {
                            n1.ImageIndex = 36;
                            n1.SelectedImageIndex = 36;
                        }

                        //COMMENT ON COLUMN JUST_DATA.ADMIN.DIMDATE2.FISCALYEAR IS 'KOMENARZ2!';

                    }
                }
            }
            else if (typeId == TypeInDatabase.distributionColumns && placeholderName == "fool")
            {
                node.Nodes[0].Remove();

                List<(Byte, string, int)> dystr = new List<(Byte, string, int)>();
                int firstColumnId = NetezzaHelpers.baseTableDictionary[connectionName][idObj].FIRST_COLUMN_ID;
                int columnCount = NetezzaHelpers.baseTableDictionary[connectionName][idObj].COLUMN_COUNT;

                for (int i = 0; i < columnCount; i++)
                {
                    int columnId = firstColumnId + i;
                    if (_completionContext.ColumnTablesDictionary[connectionName].Count >= columnId + 1 && _completionContext.ColumnTablesDictionary[connectionName][columnId].DISTSEQNO is not null)
                    {
                        dystr.Add(((byte)_completionContext.ColumnTablesDictionary[connectionName][columnId].DISTSEQNO, _completionContext.ColumnTablesDictionary[connectionName][columnId].COLUMN_NAME, columnId));
                    }
                }
                dystr.Sort((arg1, arg2) => (arg1.Item1 - arg2.Item1));

                foreach (var item in dystr)
                {
                    var n1 = node.Nodes.Add(item.Item2, item.Item2);
                    n1.ToolTipText = $"{item.Item2} - dist column";
                    n1.Tag = new DatabaseTag() { KIND_ID = TypeInDatabase.thisDistributionCollumn, OBJECT_ID = item.Item3 };
                    n1.ImageIndex = 36;
                    n1.SelectedImageIndex = 36;
                }
            }
            else if (typeId == TypeInDatabase.references && placeholderName == "fool")
            {
                node.Nodes[0].Remove();

                if (IGeneralDbService.GeneralDic[connectionName] is not INetezza nz)
                {
                    return;
                }

                if (!nz.keysInTables.TryGetValue(idObj, out var tableKeys))
                {
                    return;
                }
                else
                {
                    var primaryKeys = tableKeys.Where(arg => arg.keyType == 'p');
                    var kluczeGlowneLista = primaryKeys.Select(arg => arg.keyName).Distinct();

                    var uniqueKeys = tableKeys.Where(arg => arg.keyType == 'u');
                    var kluczeUnikalneLista = uniqueKeys.Select(arg => arg.keyName).Distinct();

                    var foreignKeys = tableKeys.Where(arg => arg.keyType == 'f');
                    var kluczeObceLista = foreignKeys.Select(arg => arg.keyName).Distinct();


                    foreach (string key in kluczeGlowneLista)
                    {
                        TreeNode n1 = node.Nodes.Add(key, key);
                        n1.Tag = new DatabaseTag() { KIND_ID = TypeInDatabase.thisReference, OBJECT_ID = -1 };
                        var columnList = primaryKeys.Where(arg => arg.keyName == key).Select(arg => arg.columnName);
                        n1.ImageIndex = node.ImageIndex;
                        n1.SelectedImageIndex = node.ImageIndex;
                        foreach (string item in columnList)
                        {
                            var n2 = n1.Nodes.Add(item);
                            n2.ImageIndex = n1.ImageIndex;
                            n2.SelectedImageIndex = n1.SelectedImageIndex;
                        }
                    }
                    foreach (string key in kluczeUnikalneLista)
                    {
                        var n1 = node.Nodes.Add(key, key);
                        n1.Tag = new DatabaseTag() { KIND_ID = TypeInDatabase.thisReference, OBJECT_ID = -1 };
                        n1.ImageIndex = node.ImageIndex;
                        n1.SelectedImageIndex = node.ImageIndex;
                        var columnList = uniqueKeys.Where(arg => arg.keyName == key).Select(arg => arg.columnName);
                        foreach (string item in columnList)
                        {
                            var n2 = n1.Nodes.Add(item);
                            n2.ImageIndex = n1.ImageIndex;
                            n2.SelectedImageIndex = n1.SelectedImageIndex;
                        }
                    }
                    foreach (string key in kluczeObceLista)
                    {
                        var n1 = node.Nodes.Add(key, key);
                        n1.Tag = new DatabaseTag() { KIND_ID = TypeInDatabase.thisReference, OBJECT_ID = -1 };
                        n1.ImageIndex = node.ImageIndex;
                        n1.SelectedImageIndex = node.SelectedImageIndex;

                        var columnList = foreignKeys.Where(arg => arg.keyName == key).Select(arg => arg.columnName).ToList();
                        var referencedColumns = foreignKeys.Where(arg => arg.keyName == key).Select(arg => arg.refColumnName).ToList();

                        var referencedDatabase = foreignKeys.Where(arg => arg.keyName == key).Select(arg => _completionContext.DatabaseDictionary[connectionName][NetezzaHelpers.baseTableDictionary[connectionName][(int)arg.refTableId].DATABASE_ID].DatabaseName).First();
                        var referencedTable = foreignKeys.Where(arg => arg.keyName == key).Select(arg => NetezzaHelpers.baseTableDictionary[connectionName][(int)arg.refTableId].TABLE_NAME).First();
                        for (int i = 0; i < columnList.Count; i++)
                        {
                            var n2 = n1.Nodes.Add($"{columnList[i]} - {referencedDatabase}..{referencedTable}.{referencedColumns[i]}");
                            n2.ImageIndex = n1.ImageIndex;
                            n2.SelectedImageIndex = n1.SelectedImageIndex;
                        }
                    }
                }
            }
            else if ((typeId == TypeInDatabase.synonym || typeId == TypeInDatabase.sequence || typeId == TypeInDatabase.function || typeId == TypeInDatabase.thisAggregate) && placeholderName == "fool")
            {
                node.Nodes[0].Remove();

                string refObject = _baseWindow.GetAddInfo(idObj, typeId, e.Node.Parent.Parent.Parent.Text);
                TreeNode n1 = node.Nodes.Add(refObject, refObject);
                n1.Tag = typeId;
                if (typeId == TypeInDatabase.synonym)
                {
                    n1.ImageIndex = 20;
                    n1.SelectedImageIndex = 20;
                }
                else
                {
                    n1.ImageIndex = node.ImageIndex;
                    n1.SelectedImageIndex = node.SelectedImageIndex;
                }
            }
            else if (typeId == TypeInDatabase.baseFluides && node.Nodes.Count == 1 && placeholderName == "fool")
            {
                string dbName = _completionContext.DatabaseDictionary[connectionName][idObj].DatabaseName;
                try
                {
                    var res = ((INetezza)IGeneralDbService.GeneralDic[connectionName]).GetFulides(dbName, idObj);
                    List<string> owner = res.owner;
                    List<string> name = res.name;
                    List<string> desc = res.desc;
                    List<int> id = res.id;
                    node.Nodes[0].Remove();

                    for (int i = 0; i < owner.Count; i++)
                    {
                        if (!owner[i].IsGoodName())
                        {
                            owner[i] = $"\"{owner[i]}\"";
                        }

                        var n1 = node.Nodes.Add($"{owner[i]}.{name[i]}", $"{owner[i]}.{name[i]}");
                        n1.Tag = new DatabaseTag()
                        {
                            OBJECT_ID = id[i],
                            KIND_ID = TypeInDatabase.fluid
                        };
                        n1.ImageIndex = node.ImageIndex;
                        n1.SelectedImageIndex = node.SelectedImageIndex;

                        string fluidTxt = $"SELECT * FROM TABLE WITH FINAL ({dbName}..{name[i]}  ('', '', 'SELECT *  FROM SOME_TABLE'))";
                        var n2 = n1.Nodes.Add(fluidTxt, fluidTxt);
                        n2.ToolTipText = "example of usage";
                        n2.ImageIndex = n1.ImageIndex;
                        n2.SelectedImageIndex = n1.SelectedImageIndex;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message);
                    return;
                }
            }
        }

        private int AddBasicObjects(int start, int end, TreeNode node, int idObj, TypeInDatabase databaseTypes, int limit, string connectionName)
        {
            int i;
            int itemCount = 0;
            for (i = start; i < end; i++)
            {
                int tableId = _databaseRuntimeContext.BaseTableConnections[connectionName][idObj][i];
                TypeInDatabase type = NetezzaHelpers.baseTableDictionary[connectionName][tableId].TABLE_KIND;

                if (MatchTypes(databaseTypes, type))
                {
                    TreeNode n1;
                    if (_applicationSettingsContext.Config.DontShowOwner)
                    {
                        n1 = node.Nodes.Add(NetezzaHelpers.baseTableDictionary[connectionName][tableId].TABLE_NAME, $"{NetezzaHelpers.baseTableDictionary[connectionName][tableId].TABLE_NAME}");
                    }
                    else
                    {
                        n1 = node.Nodes.Add(NetezzaHelpers.baseTableDictionary[connectionName][tableId].TABLE_NAME, $"{NetezzaHelpers.baseTableDictionary[connectionName][tableId].TABLE_OWNER}.{NetezzaHelpers.baseTableDictionary[connectionName][tableId].TABLE_NAME}");
                    }

                    n1.ToolTipText = NetezzaHelpers.baseTableDictionary[connectionName][tableId].TABLE_DESC;
                    n1.Tag = new DatabaseTag() { KIND_ID = type, OBJECT_ID = tableId };
                    if (type == TypeInDatabase.table)
                    {
                        n1.ContextMenuStrip = cmStripTableNetezza;
                        n1.ImageIndex = 8;
                        n1.SelectedImageIndex = 8;
                    }
                    else if (type == TypeInDatabase.view)
                    {
                        n1.ContextMenuStrip = cmStripViewNetezza;
                        n1.ImageIndex = 9;
                        n1.SelectedImageIndex = 9;
                    }
                    else if (type == TypeInDatabase.thisExternal)
                    {
                        n1.ContextMenuStrip = cmExternal;
                        n1.ImageIndex = 10;
                        n1.SelectedImageIndex = 10;
                    }
                    else if (type == TypeInDatabase.synonym)
                    {
                        n1.ContextMenuStrip = cmStripSynonym;
                        n1.ImageIndex = 18;
                        n1.SelectedImageIndex = 18;
                    }
                    else if (type == TypeInDatabase.function)
                    {
                        n1.ImageIndex = 19;
                        n1.SelectedImageIndex = 19;
                    }
                    else if (type == TypeInDatabase.procedure)
                    {
                        n1.ContextMenuStrip = cmProc;
                        n1.ImageIndex = 15;
                        n1.SelectedImageIndex = 15;
                    }
                    else if (type == TypeInDatabase.sequence)
                    {
                        n1.ContextMenuStrip = cmStripSequence;
                        n1.ImageIndex = node.ImageIndex;
                        n1.SelectedImageIndex = node.SelectedImageIndex;
                    }
                    else
                    {
                        n1.ImageIndex = node.ImageIndex;
                        n1.SelectedImageIndex = node.SelectedImageIndex;
                    }

                    if (type != TypeInDatabase.procedure)
                    {
                        n1.Nodes.Add("fool", "Loading…");
                    }


                    if (++itemCount >= limit)
                    {
                        return i;
                    }
                }
            }
            return i;
        }

        private static bool MatchTypes(TypeInDatabase databaseTypes, TypeInDatabase type)
        {
            if (
                (databaseTypes == TypeInDatabase.baseTables && type == TypeInDatabase.table)
                || (databaseTypes == TypeInDatabase.baseViews && type == TypeInDatabase.view)
                || (databaseTypes == TypeInDatabase.baseFunctions && type == TypeInDatabase.function)
                || (databaseTypes == TypeInDatabase.baseSequence && type == TypeInDatabase.sequence)
                || (databaseTypes == TypeInDatabase.baseSynonyms && type == TypeInDatabase.synonym)
                || (databaseTypes == TypeInDatabase.baseProcedures && type == TypeInDatabase.procedure)
                || (databaseTypes == TypeInDatabase.baseExternals && type == TypeInDatabase.thisExternal)
                || (databaseTypes == TypeInDatabase.baseAggregates && type == TypeInDatabase.thisAggregate)
                )
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private static bool LvlDB(TypeInDatabase typeId)
        {
            if (typeId == TypeInDatabase.baseTables || typeId == TypeInDatabase.baseViews || typeId == TypeInDatabase.baseProcedures || typeId == TypeInDatabase.baseFunctions || typeId == TypeInDatabase.baseSequence || typeId == TypeInDatabase.baseSynonyms || typeId == TypeInDatabase.baseExternals
                || typeId == TypeInDatabase.baseAggregates)
            {
                return true;
            }

            return false;
        }

        public Stack<string> GetChainNames()
        {
            var TopNode = DatabaseTreeView.TopNode;
            Stack<string> namesChainStck = new Stack<string>();
            if (TopNode is not null)
            {
                namesChainStck.Push(TopNode.Name);
                while (TopNode.Parent != null)
                {
                    TopNode = TopNode.Parent;
                    namesChainStck.Push(TopNode.Name);
                }
            }

            return namesChainStck;
        }

        public void SwapTreeViewNodesOnDb(string connectionName, string databaseName, TreeView databaseTreeViewPomocnicze)
        {
            _baseWindow.ExtendDatabasesList(_completionContext.DatabaseDictionary[connectionName].Values.Select(arg => arg.DatabaseName).ToArray());

            this.CbWhatDb.Items.Clear();
            this.CbWhatDb.Items.Add("all");
            this.CbWhatDb.Items.AddRange(_completionContext.DatabaseDictionary[connectionName].Values.Select(arg => arg.DatabaseName).ToArray());
            this.CbWhatDb.SelectedIndex = 0;
            DatabaseTreeView.BeginUpdate();

            Stack<string> namesChainStck = GetChainNames();

            DatabaseTreeView.ShowNodeToolTips = true;
            if (DatabaseTreeView.Nodes.ContainsKey(connectionName))
            {
                var nd = DatabaseTreeView.Nodes[connectionName].Nodes[databaseName];
                foreach (TreeNode nd2 in nd.Nodes)
                {
                    nd2.Nodes.Clear();

                    TreeNode nodeToCloneBase = databaseTreeViewPomocnicze.Nodes[connectionName].Nodes[databaseName].Nodes[nd2.Name];

                    foreach (TreeNode nodeToClone in nodeToCloneBase.Nodes)
                    {
                        nd2.Nodes.Add((TreeNode)nodeToClone.Clone());
                        if (nd2.IsExpanded == true)
                        {
                            nd2.Collapse();
                            _ignoreRefreshing = true;
                            nd2.Expand();
                            _ignoreRefreshing = false;
                        }
                    }
                }
                RestoreChainDatabaseTreeView(namesChainStck);
            }
            DatabaseTreeView.EndUpdate();
        }

        private readonly Lock _syncRestoreChain = new Lock();
        public void RestoreChainDatabaseTreeView(Stack<string> namesChainStck)
        {
            lock (_syncRestoreChain)
            {
                if (namesChainStck.Count > 0)
                {
                    TreeNode tn = DatabaseTreeView.Nodes[namesChainStck.Pop()];
                    while (namesChainStck.Count > 0)
                    {
                        if (tn == null)
                        {
                            break;
                        }
                        tn = tn.Nodes[namesChainStck.Pop()];
                    }
                    if (tn != null)
                    {
                        DatabaseTreeView.TopNode = tn;
                    }
                }
            }
        }

        public void ExpandLastKnownFull(TreeView treeView, List<(TreeNode, string FullPath, List<string> names)> expandedItems, int lvlLimit = -1)
        {
            lock (_syncRestoreChain)
            {
                foreach (var item in expandedItems)
                {
                    var tn = treeView.Nodes.Find(item.Item1.Name, true);
                    foreach (TreeNode item1 in tn)
                    {
                        if (lvlLimit != -1 && item1.Level > lvlLimit)
                        {
                            continue;
                        }
                        if (item1.FullPath == item.FullPath)
                        {
                            item1.Expand();
                        }

                    }
                }
            }
        }


        private static bool _doContinueOnExpand = false;

        private void DatabaseTreeView_AfterExpand(object sender, TreeViewEventArgs e)
        {
            if (!_doContinueOnExpand)
            {
                return;
            }
            _doContinueOnExpand = false;

            TreeNode node = e.Node;

            int ind = node.FullPath.IndexOf("\\");
            string connectionName;
            if (e.Node.Level == 0)
            {
                connectionName = e.Node.Text;
            }
            else if (ind != -1)
            {
                connectionName = node.FullPath.Substring(0, ind);
            }
            else
            {
                MessageBox.Show(this, "Could not expand the tree node.", "Explorer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }


            int idObj = (node.Tag as DatabaseTag).OBJECT_ID;
            TypeInDatabase typeId = (node.Tag as DatabaseTag).KIND_ID;
            if (LvlDB(typeId))
            {
                DatabaseTreeView.BeginUpdate();
                AddBasicObjects(position + 1, _databaseRuntimeContext.BaseTableConnections[connectionName][idObj].Count, node, idObj, typeId, _databaseRuntimeContext.BaseTableConnections[connectionName][idObj].Count, connectionName);
                position = 0;
                DatabaseTreeView.EndUpdate();
            }
        }

        private void TsmScript1_Click(object sender, EventArgs e)
        {
            string pre = _applicationSettingsContext.Config.ContextScripts[sender.ToString()][0];
            string main = _applicationSettingsContext.Config.ContextScripts[sender.ToString()][1];

            var tg = DatabaseTreeView.SelectedNode.Tag as DatabaseTag;
            int id = -1;
            if (tg != null)
            {
                id = tg.OBJECT_ID;
            }

            if (id != -1)
            {
                string connectionName = DatabaseTreeView.SelectedNode.Parent.Parent.Parent.Text;
                string tableOwner = NetezzaHelpers.baseTableDictionary[connectionName][id].TABLE_OWNER;
                string databaseName = _completionContext.DatabaseDictionary[connectionName][NetezzaHelpers.baseTableDictionary[connectionName][id].DATABASE_ID].DatabaseName;
                //var tableData = Netezza.tableMetadata[connectionName][id];

                if (main.Contains(@"$name"))
                {
                    main = main.Replace(@"$name", DatabaseTreeView.SelectedNode.Name);
                }
                if (main.Contains(@"$db"))
                {
                    main = main.Replace(@"$db", databaseName);
                }
                if (main.Contains(@"$schema"))
                {
                    main = main.Replace(@"$schema", tableOwner);
                }

                if (Regex.IsMatch(main, @"\$signature\b", RegexOptions.IgnoreCase) && tg.KIND_ID == TypeInDatabase.procedure)
                {
                    main = main.Replace("$signature", $"show procedure {DatabaseTreeView.SelectedNode.Name};");
                }
                else if (Regex.IsMatch(main, @"\$signature\b", RegexOptions.IgnoreCase))
                {
                    main = Regex.Replace(main, @"\signature\b", "");
                }

                if (tg.KIND_ID == TypeInDatabase.table || tg.KIND_ID == TypeInDatabase.view || tg.KIND_ID == TypeInDatabase.thisExternal)
                {
                    int firstColumnId = NetezzaHelpers.baseTableDictionary[connectionName][id].FIRST_COLUMN_ID;
                    int columnCount = NetezzaHelpers.baseTableDictionary[connectionName][id].COLUMN_COUNT;
                    List<string> ls = new List<string>();
                    for (int i = 0; i < columnCount; i++)
                    {
                        int columnId = firstColumnId + i;

                        ls.Add(_completionContext.ColumnTablesDictionary[connectionName][columnId].COLUMN_NAME);
                    }
                    main = main.Replace("$columns", String.Join(", ", ls));
                }
                else if (tg.KIND_ID == TypeInDatabase.procedure)
                {

                }
            }

            string post = _applicationSettingsContext.Config.ContextScripts[sender.ToString()][2];
            _baseWindow.AddMainTab(null, sender.ToString(), pre + Environment.NewLine + main + Environment.NewLine + post + Environment.NewLine);
        }

        public void ResetUserScriptMenu()
        {
            tsmUserScriptTable.DropDownItems.Clear();
            tsmUserScriptView.DropDownItems.Clear();
            tsmUserScriptProcedure.DropDownItems.Clear();
            tsmUserScriptExternal.DropDownItems.Clear();
            tsmUserScriptSynonyms.DropDownItems.Clear();

            tsmUserScriptTable.DropDownItems.Add(this.tsmManageScriptsTable);
            tsmUserScriptView.DropDownItems.Add(this.tsmManageScriptsViews);
            tsmUserScriptProcedure.DropDownItems.Add(this.tsmManageScriptsProcedures);
            tsmUserScriptExternal.DropDownItems.Add(this.tsmManageScriptsExternals);
            tsmUserScriptSynonyms.DropDownItems.Add(this.tsmManageScriptsSynonyms);

            tsmUserScriptTable.DropDownItems.Add(this.customToolStripSeparator1);
            tsmUserScriptView.DropDownItems.Add(this.customToolStripSeparator2);
            tsmUserScriptProcedure.DropDownItems.Add(this.customToolStripSeparator3);
            tsmUserScriptExternal.DropDownItems.Add(this.customToolStripSeparator4);
            tsmUserScriptSynonyms.DropDownItems.Add(this.customToolStripSeparator5);


            foreach (var item in _applicationSettingsContext.Config.ContextScripts)
            {
                if (item.Value[3][0] == 'Y')
                {
                    ToolStripMenuItem toolStripMenu = new ToolStripMenuItem(item.Key);
                    toolStripMenu.Name = item.Key;
                    toolStripMenu.ForeColor = tsmManageScriptsTable.ForeColor;
                    toolStripMenu.BackColor = tsmManageScriptsTable.BackColor;
                    tsmUserScriptTable.DropDownItems.Add(toolStripMenu);
                    toolStripMenu.Click += TsmScript1_Click;
                }
                if (item.Value[3][1] == 'Y') // view
                {
                    ToolStripMenuItem toolStripMenu = new ToolStripMenuItem(item.Key);
                    toolStripMenu.Name = item.Key;
                    toolStripMenu.ForeColor = tsmManageScriptsViews.ForeColor;
                    toolStripMenu.BackColor = tsmManageScriptsViews.BackColor;
                    tsmUserScriptView.DropDownItems.Add(toolStripMenu);
                    toolStripMenu.Click += TsmScript1_Click;
                }
                if (item.Value[3][2] == 'Y') // proc
                {
                    ToolStripMenuItem toolStripMenu = new ToolStripMenuItem(item.Key);
                    toolStripMenu.Name = item.Key;
                    toolStripMenu.ForeColor = tsmManageScriptsProcedures.ForeColor;
                    toolStripMenu.BackColor = tsmManageScriptsProcedures.BackColor;
                    tsmUserScriptProcedure.DropDownItems.Add(toolStripMenu);
                    toolStripMenu.Click += TsmScript1_Click;
                }
                if (item.Value[3][3] == 'Y') // Ext
                {
                    ToolStripMenuItem toolStripMenu = new ToolStripMenuItem(item.Key);
                    toolStripMenu.Name = item.Key;
                    toolStripMenu.ForeColor = tsmManageScriptsExternals.ForeColor;
                    toolStripMenu.BackColor = tsmManageScriptsExternals.BackColor;
                    tsmUserScriptExternal.DropDownItems.Add(toolStripMenu);
                    toolStripMenu.Click += TsmScript1_Click;
                }
                if (item.Value[3][4] == 'Y') // synonym
                {
                    ToolStripMenuItem toolStripMenu = new ToolStripMenuItem(item.Key);
                    toolStripMenu.Name = item.Key;
                    toolStripMenu.ForeColor = tsmManageScriptsSynonyms.ForeColor;
                    toolStripMenu.BackColor = tsmManageScriptsSynonyms.BackColor;
                    tsmUserScriptSynonyms.DropDownItems.Add(toolStripMenu);
                    toolStripMenu.Click += TsmScript1_Click;
                }

            }
        }


        // Property to set the ImageList for the TreeView
        public ImageList? TreeViewImageList
        {
            get => databaseTreeView.ImageList;
            set => databaseTreeView.ImageList = value;
        }

        // Property to set the ContextMenuStrip for the TreeView
        public ContextMenuStrip? TreeViewContextMenuStrip
        {
            get => databaseTreeView.ContextMenuStrip;
            set => databaseTreeView.ContextMenuStrip = value;
        }

        // Public events that expose the internal control events
        public event TreeViewCancelEventHandler? TreeViewBeforeExpand
        {
            add => databaseTreeView.BeforeExpand += value;
            remove => databaseTreeView.BeforeExpand -= value;
        }

        public event TreeViewEventHandler? TreeViewAfterExpand
        {
            add => databaseTreeView.AfterExpand += value;
            remove => databaseTreeView.AfterExpand -= value;
        }

        public event TreeNodeMouseClickEventHandler? TreeViewNodeMouseClick
        {
            add => databaseTreeView.NodeMouseClick += value;
            remove => databaseTreeView.NodeMouseClick -= value;
        }

        public event TreeNodeMouseClickEventHandler? TreeViewNodeMouseDoubleClick
        {
            add => databaseTreeView.NodeMouseDoubleClick += value;
            remove => databaseTreeView.NodeMouseDoubleClick -= value;
        }

        public event KeyEventHandler? TreeViewKeyDown
        {
            add => databaseTreeView.KeyDown += value;
            remove => databaseTreeView.KeyDown -= value;
        }

        public event DataGridViewCellEventHandler? DataGridViewCellDoubleClick
        {
            add => dgvFastDbBrowser.CellDoubleClick += value;
            remove => dgvFastDbBrowser.CellDoubleClick -= value;
        }

        public event DataGridViewCellValueEventHandler? DataGridViewCellValueNeeded
        {
            add => dgvFastDbBrowser.CellValueNeeded += value;
            remove => dgvFastDbBrowser.CellValueNeeded -= value;
        }

        // Events for the new controls
        public event EventHandler? CbWhatDbSelectedIndexChanged
        {
            add => cbWhatDb.SelectedIndexChanged += value;
            remove => cbWhatDb.SelectedIndexChanged -= value;
        }

        public event EventHandler? CbSearchDbSelectedIndexChanged
        {
            add => cbSearchDb.SelectedIndexChanged += value;
            remove => cbSearchDb.SelectedIndexChanged -= value;
        }

        public event EventHandler? TbFastSchemaSearchTextChanged
        {
            add => tbFastSchemaSearch.TextChanged += value;
            remove => tbFastSchemaSearch.TextChanged -= value;
        }

        public event KeyEventHandler? TbFastSchemaSearchKeyDown
        {
            add => tbFastSchemaSearch.KeyDown += value;
            remove => tbFastSchemaSearch.KeyDown -= value;
        }
    }
}
