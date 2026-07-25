using AppBase.Common;
using AppBase.Common.Enums;
using AppBase.Data;
using AppBase.Data.Core.Core;
using AppBase.Data.Core.Interfaces;
using AppBase.Data.Core.Models;
using JustData.Application.Schema;
using JustData.ViewModels.Explorer;
using JustyBase.NetezzaCatalogSql;
using JustyBase.NetezzaDdl;
using JustyBaseLegacy.UI.Schema;
using System.Data.Common;
using System.Text;
using System.Text.RegularExpressions;

namespace JustyBaseLegacy.UI;

public partial class BaseWindow
{
    private ContextMenuStrip? CreateMvvmSchemaContextMenu(ExplorerNodeViewModel node)
    {
        IReadOnlyList<SchemaContextMenuEntry> entries = SchemaContextMenuCatalog.GetEntries(node.Model);
        if (entries.Count == 0)
            return null;

        var menu = new ContextMenuStrip { Name = $"mvvmSchemaMenu_{node.Kind}" };
        AddSchemaMenuEntries(menu.Items, entries, node);
        return menu;
    }

    private void AddSchemaMenuEntries(
        ToolStripItemCollection target,
        IReadOnlyList<SchemaContextMenuEntry> entries,
        ExplorerNodeViewModel node)
    {
        foreach (SchemaContextMenuEntry entry in entries)
        {
            if (entry.Text == "-")
            {
                target.Add(new ToolStripSeparator());
                continue;
            }

            var item = new ToolStripMenuItem(entry.Text) { Enabled = entry.Enabled };
            target.Add(item);
            if (entry.Action == SchemaContextAction.UserScripts)
            {
                AddUserScriptMenu(item, node);
                continue;
            }
            if (entry.Children is { Count: > 0 })
            {
                AddSchemaMenuEntries(item.DropDownItems, entry.Children, node);
                continue;
            }
            if (entry.Action is SchemaContextAction action)
            {
                item.Name = $"schemaAction_{action}";
                item.Click += async (_, _) => await ExecuteMvvmSchemaActionSafelyAsync(node, action);
            }
        }
    }

    private void AddUserScriptMenu(ToolStripMenuItem parent, ExplorerNodeViewModel node)
    {
        var manage = new ToolStripMenuItem("Manage...");
        manage.Click += (_, _) =>
        {
            new ContexScripts(
                form => _colorTheme.ColorForm(form),
                _applicationSettingsContext.Config.ToolTipDelay,
                _applicationSettingsContext.Config.ContextScripts).ShowDialog(this);
        };
        parent.DropDownItems.Add(manage);
        parent.DropDownItems.Add(new ToolStripSeparator());

        int scriptIndex = GetContextScriptIndex(node.Model);
        foreach ((string name, List<string> parts) in _applicationSettingsContext.Config.ContextScripts)
        {
            if (scriptIndex < 0 || parts.Count < 4 || parts[3].Length <= scriptIndex || parts[3][scriptIndex] != 'Y')
                continue;
            var script = new ToolStripMenuItem(name);
            script.Click += (_, _) => ExecuteUserScript(node.Model, name, parts);
            parent.DropDownItems.Add(script);
        }
    }

    private static int GetContextScriptIndex(SchemaNode node)
    {
        if (Enum.TryParse(node.ProviderKind, true, out TypeInDatabase kind))
        {
            return kind switch
            {
                TypeInDatabase.table => 0,
                TypeInDatabase.view => 1,
                TypeInDatabase.procedure or TypeInDatabase.function or TypeInDatabase.thisAggregate => 2,
                TypeInDatabase.thisExternal => 3,
                TypeInDatabase.synonym => 4,
                _ => -1
            };
        }
        return -1;
    }

    private void ExecuteUserScript(SchemaNode node, string scriptName, IReadOnlyList<string> parts)
    {
        if (parts.Count < 3 || !TryGetNetezzaObject(node, out _, out NetezzaTableInfo? table, out string databaseName))
            return;
        string main = parts[1]
            .Replace("$name", table.TABLE_NAME, StringComparison.Ordinal)
            .Replace("$db", databaseName, StringComparison.Ordinal)
            .Replace("$schema", table.TABLE_OWNER, StringComparison.Ordinal);
        if (main.Contains("$columns", StringComparison.Ordinal))
            main = main.Replace("$columns", string.Join(", ", GetNetezzaColumns(node)), StringComparison.Ordinal);
        if (Regex.IsMatch(main, @"\$signature\b", RegexOptions.IgnoreCase))
        {
            main = table.TABLE_KIND == TypeInDatabase.procedure
                ? Regex.Replace(main, @"\$signature\b", $"show procedure {table.TABLE_NAME};", RegexOptions.IgnoreCase)
                : Regex.Replace(main, @"\$signature\b", string.Empty, RegexOptions.IgnoreCase);
        }
        AddMainTab(null, scriptName, $"{parts[0]}{Environment.NewLine}{main}{Environment.NewLine}{parts[2]}{Environment.NewLine}");
    }

    private async Task ExecuteMvvmSchemaActionSafelyAsync(ExplorerNodeViewModel node, SchemaContextAction action)
    {
        try
        {
            Application.UseWaitCursor = true;
            await ExecuteMvvmSchemaActionAsync(node, action);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Schema explorer", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            Application.UseWaitCursor = false;
        }
    }

    private async Task ExecuteMvvmSchemaActionAsync(ExplorerNodeViewModel node, SchemaContextAction action)
    {
        if (action == SchemaContextAction.Refresh)
        {
            await RefreshMvvmExplorerAsync();
            return;
        }
        if (action == SchemaContextAction.CollapseAll)
        {
            _mvvmDatabaseExplorerControl?.CollapseAllNodes();
            return;
        }
        if (action == SchemaContextAction.DdlAll)
        {
            await OpenAllTableDdlAsync(node);
            return;
        }

        if (action is SchemaContextAction.DdlClipboard or SchemaContextAction.DdlNew)
        {
            string ddl = await GetSchemaDdlTextAsync(node, SchemaDdlKind.Create);
            if (action == SchemaContextAction.DdlClipboard)
                SetClipboardText(ddl);
            else
                AddMainTab(null, $"ddl for {node.Name}", ddl);
            return;
        }
        if (action is SchemaContextAction.SelectClipboard or SchemaContextAction.SelectNew)
        {
            string select = await GetSchemaDdlTextAsync(node, SchemaDdlKind.SelectTop);
            if (action == SchemaContextAction.SelectClipboard)
                SetClipboardText(select);
            else
                AddMainTab(null, $"select from {node.Name}", select);
            return;
        }

        // Actions that do not require a Netezza catalog object (Connection/Database/Schema level)
        if (action is SchemaContextAction.CreateTable or SchemaContextAction.CreateSequence
            or SchemaContextAction.CreateProcedure or SchemaContextAction.AddSynonym
            or SchemaContextAction.CreateUser
            or SchemaContextAction.ShowTableSizes or SchemaContextAction.ShowQueryHistory
            or SchemaContextAction.ShowUserSessions)
        {
            await ExecuteNonNetezzaActionAsync(node, action);
            return;
        }

        if (node.Kind == SchemaNodeKind.Column)
        {
            await ExecuteColumnActionAsync(node.Model, action);
            return;
        }
        if (!TryGetNetezzaObject(node.Model, out int objectId, out NetezzaTableInfo? table, out string databaseName))
            throw new InvalidOperationException("The selected Netezza object is no longer present in the loaded catalog.");

        string qualifiedName = $"{databaseName}.{table.TABLE_OWNER}.{table.TABLE_NAME}";
        switch (action)
        {
            case SchemaContextAction.SelectDuplicates:
                SetClipboardText(BuildDuplicatesSql(node.Model, qualifiedName));
                break;
            case SchemaContextAction.SelectDeletedRows:
                AddMainTab(null, qualifiedName,
                    $"SET show_deleted_records = 1;\r\nselect t1.createxid, t1.deletexid, t1.* from {qualifiedName} t1 WHERE deletexid != 0;\r\nSET show_deleted_records = 0;");
                break;
            case SchemaContextAction.GrantClipboard:
                SetClipboardText(NetezzaDdlTemplates.GetGrantSelectSql($"{databaseName}..{table.TABLE_NAME}"));
                break;
            case SchemaContextAction.CommentClipboard:
                SetClipboardText($"COMMENT ON TABLE {qualifiedName} IS 'some comment';");
                break;
            case SchemaContextAction.AddKey:
                SetClipboardText($"ALTER TABLE {qualifiedName} ADD CONSTRAINT PK_{table.TABLE_NAME} PRIMARY KEY (COL1,COL2);");
                break;
            case SchemaContextAction.AddUnique:
                SetClipboardText($"ALTER TABLE {qualifiedName} ADD CONSTRAINT UK_{table.TABLE_NAME} UNIQUE (COL1,COL2);");
                break;
            case SchemaContextAction.GenerateStatistics:
                AddMainTab(null, $"stats for {table.TABLE_NAME}",
                    $"GENERATE EXPRESS STATISTICS ON {databaseName}..{table.TABLE_NAME};\r\n--https://www.ibm.com/docs/en/netezza?topic=reference-generate-express-statistics");
                break;
            case SchemaContextAction.EmptyTable:
                AddMainTab(null, $"empty for {table.TABLE_NAME}",
                    $"TRUNCATE TABLE {databaseName}..{table.TABLE_NAME};\r\n--https://www.ibm.com/docs/en/netezza?topic=tables-truncate-table");
                break;
            case SchemaContextAction.Recreate:
                AddMainTab(null, $"{table.TABLE_NAME} - recreate",
                    (await _netezzaHelperService.GetRecreateTableCodeById(_databaseRuntimeContext, node.Path.Connection, objectId)).Code);
                break;
            case SchemaContextAction.Groom:
                OpenGroomForm(databaseName, table.TABLE_NAME);
                break;
            case SchemaContextAction.ChangeDistribution:
                await OpenChangeDistributionFormAsync(node.Model, objectId, table);
                break;
            case SchemaContextAction.ShowDistribution:
                await ShowMvvmDistributionAsync(node.Model, table, databaseName);
                break;
            case SchemaContextAction.ImportData:
                OpenImportForm(databaseName, table.TABLE_NAME);
                break;
            case SchemaContextAction.ExportData:
                OpenExportForm(databaseName, table.TABLE_NAME);
                break;
            case SchemaContextAction.SelectSequence:
                AddMainTab(null, $"SELECT FROM {table.TABLE_NAME}", $"SELECT NEXT VALUE FOR {table.TABLE_NAME};");
                break;
            case SchemaContextAction.Drop:
                await DropNetezzaObjectAsync(node.Model, table, databaseName);
                break;
        }
    }

    private static void SetClipboardText(string? text)
    {
        if (!string.IsNullOrEmpty(text))
            Clipboard.SetText(text);
    }

    private bool TryGetNetezzaObject(
        SchemaNode node,
        out int objectId,
        out NetezzaTableInfo? table,
        out string databaseName)
    {
        objectId = node.LegacyObjectId ?? -1;
        table = null;
        databaseName = node.Path.Database ?? string.Empty;
        if (objectId < 0
            || !NetezzaHelpers.baseTableDictionary.TryGetValue(node.Path.Connection, out var tables)
            || !tables.TryGetValue(objectId, out table))
            return false;
        if (_completionContext.DatabaseDictionary.TryGetValue(node.Path.Connection, out var databases)
            && databases.TryGetValue(table.DATABASE_ID, out var database))
            databaseName = database.DatabaseName;
        return !string.IsNullOrWhiteSpace(databaseName);
    }

    private IReadOnlyList<string> GetNetezzaColumns(SchemaNode node)
    {
        if (!TryGetNetezzaObject(node, out int objectId, out NetezzaTableInfo? table, out _)
            || !_completionContext.ColumnTablesDictionary.TryGetValue(node.Path.Connection, out var columns))
            return [];
        return Enumerable.Range(table.FIRST_COLUMN_ID, table.COLUMN_COUNT)
            .Where(index => index >= 0 && index < columns.Count && columns[index].TABLE_ID == objectId)
            .Select(index => columns[index].COLUMN_NAME)
            .ToArray();
    }

    private string BuildDuplicatesSql(SchemaNode node, string qualifiedName)
    {
        IReadOnlyList<string> columns = GetNetezzaColumns(node);
        string projection = columns.Count == 0 ? "*" : string.Join("\r\n    , ", columns.Select(column => $"T1.{column}"));
        return $"SELECT\r\n    {projection}\r\n    , COUNT(1)\r\nFROM\r\n    {qualifiedName} T1\r\nGROUP BY\r\n    {projection}\r\nHAVING\r\n    COUNT(1) > 1\r\nLIMIT 500;";
    }

    private async Task OpenAllTableDdlAsync(ExplorerNodeViewModel node)
    {
        await _databaseExplorerViewModel.ExpandAsync(node);
        var ddl = new StringBuilder();
        foreach (ExplorerNodeViewModel table in node.Children.Where(child => child.Kind == SchemaNodeKind.Table))
        {
            ddl.AppendLine(await GetSchemaDdlTextAsync(table, SchemaDdlKind.Create));
            ddl.AppendLine();
        }
        AddMainTab(null, $"ddl {node.Path.Database}.{node.Name} tables", ddl.ToString());
    }

    private void OpenGroomForm(string databaseName, string tableName)
    {
        var form = new GroomForm($"{databaseName}..{tableName}", item => _colorTheme.ColorForm(item));
        if (form.ShowDialog(this) == DialogResult.OK)
            AddMainTab(null, $"groom of {databaseName}..{tableName}", "--PLEASE VERIFY THIS SQL\r\n" + form.ResultSql + "\r\n");
    }

    private Task<string> GetSchemaDdlTextAsync(ExplorerNodeViewModel node, SchemaDdlKind kind) =>
        _ddlService.GetDdlAsync(new SchemaDdlRequest(node.Model, kind));

    private void OpenImportForm(string databaseName, string tableName)
    {
        var form = new DbForms.ImportTableDataNetezza(databaseName, tableName, item => _colorTheme.ColorForm(item), _applicationSettingsContext.ConfigDirectory);
        if (form.ShowDialog(this) == DialogResult.OK)
            AddMainTab(null, $"external - {tableName}", form.GetCode);
    }

    private void OpenExportForm(string databaseName, string tableName)
    {
        var form = new DbForms.ExportTableDataNetezza(databaseName, tableName, item => _colorTheme.ColorForm(item));
        if (form.ShowDialog(this) == DialogResult.OK)
            AddMainTab(null, "external - txt", form.GetCode);
    }

    private async Task OpenChangeDistributionFormAsync(SchemaNode node, int objectId, NetezzaTableInfo table)
    {
        var recreate = await _netezzaHelperService.GetRecreateTableCodeById(_databaseRuntimeContext, node.Path.Connection, objectId);
        var form = new DbForms.DistForm(GetNetezzaColumns(node).ToList(), recreate.Dystr.Select(value => value.Item2).ToList(), item => _colorTheme.ColorForm(item));
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            AddMainTab(null, $"{table.TABLE_NAME} - distribution",
                (await _netezzaHelperService.GetRecreateTableCodeById(_databaseRuntimeContext, node.Path.Connection, objectId, form.DistCols)).Code);
        }
    }

    private async Task DropNetezzaObjectAsync(SchemaNode node, NetezzaTableInfo table, string databaseName)
    {
        string objectType = table.TABLE_KIND switch
        {
            TypeInDatabase.view => "VIEW",
            TypeInDatabase.sequence => "SEQUENCE",
            _ => "TABLE"
        };
        DialogResult confirmation = MessageBox.Show(this,
            $"Drop {objectType.ToLowerInvariant()} {table.TABLE_NAME} (this action cannot be undone)?",
            $"Warning - {table.TABLE_NAME}", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
        if (confirmation != DialogResult.Yes)
            return;

        string sql = $"DROP {objectType} {databaseName}.{table.TABLE_OWNER}.{table.TABLE_NAME};";
        if (!IGeneralDbService.ConnectionSessions.TryGetValue(node.Path.Connection, out IGeneralDb? database))
            throw new InvalidOperationException($"Connection '{node.Path.Connection}' is not initialized.");
        await Task.Run(() =>
        {
            using DbConnection connection = database.GetConnection(databaseName);
            connection.Open();
            using DbCommand command = connection.CreateCommand();
            command.CommandText = sql;
            command.CommandTimeout = 5;
            command.ExecuteNonQuery();
        });
        await RefreshMvvmExplorerAsync();
    }

    private async Task ExecuteColumnActionAsync(SchemaNode node, SchemaContextAction action)
    {
        if (node.LegacyObjectId is not int columnId
            || !_completionContext.ColumnTablesDictionary.TryGetValue(node.Path.Connection, out var columns)
            || columnId < 0 || columnId >= columns.Count)
            throw new InvalidOperationException("The selected Netezza column is no longer present in the loaded catalog.");
        NetezzaColumnInfoRow column = columns[columnId];
        if (!NetezzaHelpers.baseTableDictionary.TryGetValue(node.Path.Connection, out var tables)
            || !tables.TryGetValue(column.TABLE_ID, out NetezzaTableInfo? table)
            || !_completionContext.DatabaseDictionary[node.Path.Connection].TryGetValue(table.DATABASE_ID, out var database))
            throw new InvalidOperationException("The selected Netezza table is no longer present in the loaded catalog.");
        string qualifiedColumn = $"{database.DatabaseName}.{table.TABLE_OWNER}.{table.TABLE_NAME}.{column.COLUMN_NAME}";
        string qualifiedTable = $"{database.DatabaseName}.{table.TABLE_OWNER}.{table.TABLE_NAME}";

        if (action == SchemaContextAction.AddColumn)
        {
            AddMainTab(null, "Add column code", $"ALTER TABLE {qualifiedTable} ADD COLUMN new_column VARCHAR(100);");
            return;
        }
        if (action == SchemaContextAction.EditColumnComment)
        {
            var form = new DbForms.ColumnEditNetezzaForm(column.COLUMN_DESCRIPTION ?? string.Empty, item => _colorTheme.ColorForm(item));
            if (form.ShowDialog(this) != DialogResult.OK)
                return;
            await ExecuteNetezzaNonQueryAsync(node.Path.Connection, database.DatabaseName,
                $"COMMENT ON COLUMN {qualifiedColumn} IS '{form.finalDesc.Replace("'", "''")}';");
            columns[columnId] = column with { COLUMN_DESCRIPTION = form.finalDesc };
            return;
        }
        if (action == SchemaContextAction.DropColumn)
        {
            DialogResult confirmation = MessageBox.Show(this,
                "Drop column - restrict mode (this action cannot be undone)?\nschema refresh may be needed for properly working autocomplete and some other functions",
                "Warning", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
            if (confirmation == DialogResult.Yes)
            {
                await ExecuteNetezzaNonQueryAsync(node.Path.Connection, database.DatabaseName,
                    $"ALTER TABLE {qualifiedTable} DROP COLUMN {column.COLUMN_NAME} RESTRICT;");
                await RefreshMvvmExplorerAsync();
            }
        }
    }

    private async Task ExecuteNonNetezzaActionAsync(ExplorerNodeViewModel node, SchemaContextAction action)
    {
        switch (action)
        {
            case SchemaContextAction.CreateTable:
                OpenCreateNewTableDialog(node);
                break;
            case SchemaContextAction.CreateSequence:
                OpenCreateNewSequenceDialog();
                break;
            case SchemaContextAction.CreateProcedure:
                OpenCreateNewProcedureDialog();
                break;
            case SchemaContextAction.AddSynonym:
                AddMainTab(null, "new synonym", "CREATE SYNONYM <synonym> FOR <name>");
                break;
            case SchemaContextAction.CreateUser:
                OpenCreateUserDialog(node);
                break;
            case SchemaContextAction.ShowTableSizes:
                await ShowTableSizesReportAsync(node);
                break;
            case SchemaContextAction.ShowQueryHistory:
                await ShowQueryHistoryReportAsync(node);
                break;
            case SchemaContextAction.ShowUserSessions:
                await ShowUserSessionsReportAsync(node);
                break;
        }
    }

    private void OpenCreateNewTableDialog(ExplorerNodeViewModel node)
    {
        var tp = new TabPagePicture
        {
            CloseImage = _normalXimage,
            Name = "NO FAST COLORED",
            Text = "Create New Table"
        };
        var cnt = new DbForms.AddNewTableControl(
            node.Path.Schema ?? string.Empty,
            o => _colorTheme.ColorForm(o),
            f => _uiHelperService.DoubleBufDateGridView(f),
            (a, b, c) => AddMainTab(a, b, c));
        cnt.Dock = DockStyle.Fill;
        tp.Controls.Add(cnt);
        _tabControlMain.TabPages.Add(tp);
        _tabManager.SelectTab(tp);
    }

    private void OpenCreateNewSequenceDialog()
    {
        var dialog = new DbForms.CreateSequenceNz(o => _colorTheme.ColorForm(o));
        if (dialog.ShowDialog(this) == DialogResult.OK)
            AddMainTab(null, $"sequence - {dialog.SeqName}", dialog.SqlCode);
    }

    private void OpenCreateNewProcedureDialog()
    {
        var dialog = new DbForms.NewProcedureNetezza(this, o => _colorTheme.ColorForm(o), f => _uiHelperService.DoubleBufDateGridView(f));
        if (dialog.ShowDialog(this) == DialogResult.OK)
            AddMainTab(null, $"procedure - {dialog.ProcName}", dialog.ProcCode);
    }

    private void OpenCreateUserDialog(ExplorerNodeViewModel node)
    {
        string connectionName = node.Path.Connection;
        if (string.IsNullOrWhiteSpace(connectionName) || !IGeneralDbService.GeneralDic.TryGetValue(connectionName, out var generalDb))
            return;
        var groups = generalDb is INetezza netezza ? netezza.GroupsList() : [];
        var dialog = new DbForms.NetezzaCreateUser(connectionName, o => _colorTheme.ColorForm(o), groups);
        if (dialog.ShowDialog(this) == DialogResult.OK)
            AddMainTab(null, "Create new user", dialog.Sql);
    }

    private async Task ShowTableSizesReportAsync(ExplorerNodeViewModel node)
    {
        string databaseName = node.Path.Database ?? string.Empty;
        if (string.IsNullOrWhiteSpace(databaseName) && node.Kind == SchemaNodeKind.Connection)
        {
            // Find first database from children
            var dbChild = node.Children.FirstOrDefault(c => c.Kind == SchemaNodeKind.Database);
            databaseName = dbChild?.Name ?? string.Empty;
        }
        if (string.IsNullOrWhiteSpace(databaseName))
            return;
        string sql = NetezzaSystemSql.GetTableSizesReport(databaseName);
        await OpenSqlInNewTab(sql, $"sizes for {databaseName}");
    }

    private async Task ShowQueryHistoryReportAsync(ExplorerNodeViewModel node)
    {
        string databaseName = node.Path.Database ?? string.Empty;
        if (string.IsNullOrWhiteSpace(databaseName) && node.Kind == SchemaNodeKind.Connection)
        {
            var dbChild = node.Children.FirstOrDefault(c => c.Kind == SchemaNodeKind.Database);
            databaseName = dbChild?.Name ?? string.Empty;
        }
        if (string.IsNullOrWhiteSpace(databaseName))
            return;
        string sql = NetezzaSystemSql.GetQueryHistory(databaseName);
        await OpenSqlInNewTab(sql, $"query history for {databaseName}");
    }

    private async Task ShowUserSessionsReportAsync(ExplorerNodeViewModel node)
    {
        string databaseName = node.Path.Database ?? string.Empty;
        if (string.IsNullOrWhiteSpace(databaseName) && node.Kind == SchemaNodeKind.Connection)
        {
            var dbChild = node.Children.FirstOrDefault(c => c.Kind == SchemaNodeKind.Database);
            databaseName = dbChild?.Name ?? string.Empty;
        }
        if (string.IsNullOrWhiteSpace(databaseName))
            return;
        string sql = NetezzaSystemSql.GetUserSessions(databaseName);
        await OpenSqlInNewTab(sql, $"user sessions for {databaseName}");
    }

    private async Task OpenSqlInNewTab(string sql, string title)
    {
        var tab = AddMainTab(null, title, sql);
        if (tab is not null)
        {
            tab.SelectAll();
            await RunNzSQL(CurrentUpper?.KeepConnectionOpen ?? false);
        }
    }

    private static async Task ExecuteNetezzaNonQueryAsync(string connectionName, string databaseName, string sql)
    {
        if (!IGeneralDbService.ConnectionSessions.TryGetValue(connectionName, out IGeneralDb? database))
            throw new InvalidOperationException($"Connection '{connectionName}' is not initialized.");
        await Task.Run(() =>
        {
            using DbConnection connection = database.GetConnection(databaseName);
            connection.Open();
            using DbCommand command = connection.CreateCommand();
            command.CommandText = sql;
            command.CommandTimeout = 5;
            command.ExecuteNonQuery();
        });
    }

    private Task RefreshMvvmExplorerAsync() => _mvvmDatabaseExplorerControl is not null
        ? _mvvmDatabaseExplorerControl.RefreshAsync()
        : _databaseExplorerViewModel.RefreshAsync();

    private async Task ShowMvvmDistributionAsync(SchemaNode node, NetezzaTableInfo table, string databaseName)
    {
        if (!IGeneralDbService.ConnectionSessions.TryGetValue(node.Path.Connection, out IGeneralDb? database))
            throw new InvalidOperationException($"Connection '{node.Path.Connection}' is not initialized.");

        long slices = 0;
        long rows = 0;
        long maximum = 0;
        long minimum = long.MaxValue;
        long rowsWithDeleted = 0;
        long maximumWithDeleted = 0;
        long minimumWithDeleted = long.MaxValue;
        double skew = 0;
        DateTime createTime = default;
        long allocatedBytes = 0;
        long usedBytes = 0;
        long storageObjectId = 0;
        var plot = new Dictionary<int, (long count, long countWdeleted, string sliceName)>();

        await Task.Run(() =>
        {
            using DbConnection connection = database.GetConnection(databaseName);
            connection.Open();
            using (DbCommand sliceCount = connection.CreateCommand())
            {
                sliceCount.CommandText = NetezzaSystemSql.DataSliceCount;
                sliceCount.CommandTimeout = _applicationSettingsContext.Config.CommandDistTimeout;
                slices = Convert.ToInt64(sliceCount.ExecuteScalar());
            }
            using (DbCommand distribution = connection.CreateCommand())
            {
                distribution.CommandText = NetezzaSystemSql.GetDistributionWithDeletedRecords(databaseName, table.TABLE_NAME);
                distribution.CommandTimeout = _applicationSettingsContext.Config.CommandDistTimeout;
                using DbDataReader reader = distribution.ExecuteReader();
                int index = 0;
                while (reader.Read())
                {
                    long all = reader.GetInt64(1);
                    long deleted = reader.GetInt64(2);
                    long current = all - deleted;
                    plot[index++] = (current, all, reader.GetValue(0)?.ToString() ?? string.Empty);
                    rows += current;
                    maximum = Math.Max(maximum, current);
                    minimum = Math.Min(minimum, current);
                    rowsWithDeleted += all;
                    maximumWithDeleted = Math.Max(maximumWithDeleted, all);
                    minimumWithDeleted = Math.Min(minimumWithDeleted, all);
                }
            }
            using (DbCommand storage = connection.CreateCommand())
            {
                storage.CommandText = NetezzaSystemSql.GetTableStorageStatistics(table.TABLE_NAME);
                using DbDataReader reader = storage.ExecuteReader();
                if (reader.Read())
                {
                    storageObjectId = reader.GetInt64(0);
                    if (reader.GetValue(1) is double valueSkew) skew = valueSkew;
                    if (reader.GetValue(2) is DateTime valueTime) createTime = valueTime;
                    if (reader.GetValue(3) is long valueAllocated) allocatedBytes = valueAllocated;
                    if (reader.GetValue(4) is long valueUsed) usedBytes = valueUsed;
                }
            }
        });

        var form = new NetezzaDistribution($"{databaseName}..{table.TABLE_NAME}", _colorTheme)
        {
            Skew = skew,
            Slices = slices,
            Rows = rows,
            Max = maximum,
            Min = minimum == long.MaxValue ? 0 : minimum,
            RowsWDeleted = rowsWithDeleted,
            MaxWDeleted = maximumWithDeleted,
            MinWDeleted = minimumWithDeleted == long.MaxValue ? 0 : minimumWithDeleted,
            crtTime = createTime,
            AlocatedBytes = allocatedBytes,
            UsedBytes = usedBytes,
            ObjId = storageObjectId,
            ForPlotDic = plot
        };
        form.Init2();
        form.Show(this);
    }
}
