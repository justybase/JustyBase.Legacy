using AppBase.Common;
using AppBase.Common.Configuration;
using AppBase.Common.Interfaces;
using AppBase.Data.Core.Core;
using AppBase.Data.Core.Enums;
using AppBase.Data.Core.Interfaces;
using JustData.Application.Schema;
using JustyBase.Ai.Ports;
using JustyBase.NetezzaSqlParser.Linter;
using System.ComponentModel;
using System.Data.Common;

namespace JustyBaseLegacy.UI.Ai;

/// <summary>Maps the host config onto the shared <see cref="ChatSettings"/> port.</summary>
public sealed class LegacyChatSettingsStore : IChatSettingsStore
{
    private readonly IApplicationSettingsContext _settings;

    public LegacyChatSettingsStore(IApplicationSettingsContext settings)
    {
        _settings = settings;
    }

    public ChatSettings Settings => Map(_settings.Config);

    public void Update(Action<ChatSettings> mutate)
    {
        var copy = Map(_settings.Config);
        mutate(copy);
        Apply(_settings.Config, copy);
        (_settings as IApplicationSettingsPersistence)?.SaveConfig();
    }

    private static ChatSettings Map(IApplicationConfig config)
    {
        return new ChatSettings
        {
            EnableAiChat = config.EnableAiChat,
            ChatSessions = config.ChatSessions,
            AiChatBackendId = config.AiChatBackendId,
            AiChatOpenAiCompatibleEndpoint = config.AiChatOpenAiCompatibleEndpoint,
            AiChatOpenAiCompatibleApiKey = config.AiChatOpenAiCompatibleApiKey,
            AiChatDefaultModel = config.AiChatDefaultModel,
            AiChatDefaultReasoningEffort = config.AiChatDefaultReasoningEffort,
            AiChatDefaultMode = config.AiChatDefaultMode,
            AiChatAutoConnect = config.AiChatAutoConnect,
            AiChatHistoryLimit = config.AiChatHistoryLimit,
            AiChatSystemPromptOverride = config.AiChatSystemPromptOverride,
            AiChatTemperature = config.AiChatTemperature,
            AiChatMaxTokens = config.AiChatMaxTokens,
            AiChatRequestTimeoutMs = config.AiChatRequestTimeoutMs,
            AiChatMaxRetries = config.AiChatMaxRetries,
            AiChatPreset = config.AiChatPreset,
            AiChatPresetIsCustom = config.AiChatPresetIsCustom,
            EnableEmbeddedChatAi = config.EnableEmbeddedChatAi,
            EmbeddedChatModelId = config.EmbeddedChatModelId,
            EmbeddedChatGpuLayers = config.EmbeddedChatGpuLayers,
            EmbeddedChatCtxSize = config.EmbeddedChatCtxSize,
            EmbeddedChatAcceptedLicenseModelIds = config.EmbeddedChatAcceptedLicenseModelIds,
            LlamaServerPreferVulkan = config.LlamaServerPreferVulkan
        };
    }

    private static void Apply(IApplicationConfig config, ChatSettings settings)
    {
        config.EnableAiChat = settings.EnableAiChat;
        config.ChatSessions = settings.ChatSessions;
        config.AiChatBackendId = settings.AiChatBackendId;
        config.AiChatOpenAiCompatibleEndpoint = settings.AiChatOpenAiCompatibleEndpoint;
        config.AiChatOpenAiCompatibleApiKey = settings.AiChatOpenAiCompatibleApiKey;
        config.AiChatDefaultModel = settings.AiChatDefaultModel;
        config.AiChatDefaultReasoningEffort = settings.AiChatDefaultReasoningEffort;
        config.AiChatDefaultMode = settings.AiChatDefaultMode;
        config.AiChatAutoConnect = settings.AiChatAutoConnect;
        config.AiChatHistoryLimit = settings.AiChatHistoryLimit;
        config.AiChatSystemPromptOverride = settings.AiChatSystemPromptOverride;
        config.AiChatTemperature = settings.AiChatTemperature;
        config.AiChatMaxTokens = settings.AiChatMaxTokens;
        config.AiChatRequestTimeoutMs = settings.AiChatRequestTimeoutMs;
        config.AiChatMaxRetries = settings.AiChatMaxRetries;
        config.AiChatPreset = settings.AiChatPreset;
        config.AiChatPresetIsCustom = settings.AiChatPresetIsCustom;
        config.EnableEmbeddedChatAi = settings.EnableEmbeddedChatAi;
        config.EmbeddedChatModelId = settings.EmbeddedChatModelId;
        config.EmbeddedChatGpuLayers = settings.EmbeddedChatGpuLayers;
        config.EmbeddedChatCtxSize = settings.EmbeddedChatCtxSize;
        config.EmbeddedChatAcceptedLicenseModelIds = settings.EmbeddedChatAcceptedLicenseModelIds;
        config.LlamaServerPreferVulkan = settings.LlamaServerPreferVulkan;
    }
}

/// <summary>Adapter over WinForms <see cref="ISynchronizeInvoke"/> for the shared chat pipeline.</summary>
public sealed class WinFormsUiDispatcher : IUiDispatcher
{
    private readonly ISynchronizeInvoke _invoke;

    public WinFormsUiDispatcher(ISynchronizeInvoke invoke)
    {
        _invoke = invoke;
    }

    public bool CheckAccess() => !_invoke.InvokeRequired;

    public Task<T> InvokeAsync<T>(Func<T> func)
    {
        ArgumentNullException.ThrowIfNull(func);
        if (!_invoke.InvokeRequired)
        {
            return Task.FromResult(func());
        }

        return BeginInvokeOnUi(func);
    }

    public Task InvokeAsync(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (!_invoke.InvokeRequired)
        {
            action();
            return Task.CompletedTask;
        }

        return BeginInvokeOnUi(action);
    }

    private Task<T> BeginInvokeOnUi<T>(Func<T> func)
    {
        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            _invoke.BeginInvoke(new Action(() =>
            {
                try
                {
                    tcs.TrySetResult(func());
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            }), null);
        }
        catch (Exception ex)
        {
            tcs.TrySetException(ex);
        }

        return tcs.Task;
    }

    private Task BeginInvokeOnUi(Action action)
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            _invoke.BeginInvoke(new Action(() =>
            {
                try
                {
                    action();
                    tcs.TrySetResult();
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            }), null);
        }
        catch (Exception ex)
        {
            tcs.TrySetException(ex);
        }

        return tcs.Task;
    }
}

/// <summary>
/// Deferred <see cref="IUiDispatcher"/> over the main window. The chat pipeline is
/// constructed while <see cref="BaseWindow"/> is still being built (BaseWindow takes
/// the ChatViewModel as a constructor dependency), so the target is resolved lazily
/// on first use to break the DI cycle.
/// </summary>
public sealed class LazyWinFormsUiDispatcher : IUiDispatcher
{
    private readonly Func<ISynchronizeInvoke> _factory;
    private ISynchronizeInvoke? _invoke;

    public LazyWinFormsUiDispatcher(Func<ISynchronizeInvoke> factory)
    {
        _factory = factory;
    }

    private ISynchronizeInvoke InvokeTarget => _invoke ??= _factory();

    public bool CheckAccess() => !InvokeTarget.InvokeRequired;

    public Task<T> InvokeAsync<T>(Func<T> func)
    {
        ArgumentNullException.ThrowIfNull(func);
        var target = InvokeTarget;
        if (!target.InvokeRequired)
        {
            return Task.FromResult(func());
        }

        return BeginInvokeOnUi(target, func);
    }

    public Task InvokeAsync(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        var target = InvokeTarget;
        if (!target.InvokeRequired)
        {
            action();
            return Task.CompletedTask;
        }

        return BeginInvokeOnUi(target, action);
    }

    private static Task<T> BeginInvokeOnUi<T>(ISynchronizeInvoke target, Func<T> func)
    {
        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            target.BeginInvoke(new Action(() =>
            {
                try
                {
                    tcs.TrySetResult(func());
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            }), null);
        }
        catch (Exception ex)
        {
            tcs.TrySetException(ex);
        }

        return tcs.Task;
    }

    private static Task BeginInvokeOnUi(ISynchronizeInvoke target, Action action)
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            target.BeginInvoke(new Action(() =>
            {
                try
                {
                    action();
                    tcs.TrySetResult();
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            }), null);
        }
        catch (Exception ex)
        {
            tcs.TrySetException(ex);
        }

        return tcs.Task;
    }
}

/// <summary>Adapter over the host logger for the shared chat pipeline.</summary>
public sealed class LegacyChatLogger : ISimpleLogger
{
    private readonly ILogger _logger;

    public LegacyChatLogger(ILogger logger)
    {
        _logger = logger;
    }

    public void TrackError(Exception ex, bool isCrash) => _logger.LogError(ex.Message, ex);
}

/// <summary>Adapter over the host application environment for the shared chat pipeline.</summary>
public sealed class LegacyChatEnvironment : IChatEnvironment
{
    private readonly IApplicationSettingsContext _settings;

    public LegacyChatEnvironment(IApplicationSettingsContext settings)
    {
        _settings = settings;
    }

    public string ConfigDirectory => _settings.ConfigDirectory;
}

/// <summary>
/// Host adapter over the active SQL editor lint issues for the "get_diagnostics" chat tool.
/// <see cref="SetActiveEditorIssuesProvider"/> is wired by the main window.
/// </summary>
public sealed class LegacySqlDiagnosticsProvider : ISqlDiagnosticsProvider
{
    private Func<IReadOnlyList<LintIssue>?>? _activeEditorIssuesProvider;

    public void SetActiveEditorIssuesProvider(Func<IReadOnlyList<LintIssue>?> provider)
        => _activeEditorIssuesProvider = provider;

    public IReadOnlyList<ChatDiagnosticItem> Items
    {
        get
        {
            var issues = _activeEditorIssuesProvider?.Invoke();
            if (issues is null || issues.Count == 0)
            {
                return [];
            }

            return issues
                .Select(issue => new ChatDiagnosticItem(
                    issue.RuleId,
                    issue.Message,
                    SeverityToString(issue.Severity),
                    issue.StartLine,
                    issue.StartColumn))
                .ToList();
        }
    }

    private static string SeverityToString(LintSeverity severity) => severity switch
    {
        LintSeverity.Error => "Error",
        LintSeverity.Warning => "Warning",
        LintSeverity.Information => "Info",
        LintSeverity.Hint => "Hint",
        _ => "Unknown"
    };
}

/// <summary>
/// Resolves the shared chat database port over the host schema repository and
/// provider sessions.
/// </summary>
public sealed class LegacyChatDatabaseAccessProvider : IChatDatabaseAccessProvider
{
    private readonly ISchemaRepository _schemaRepository;
    private readonly ISchemaDdlService _ddlService;
    private readonly IGeneralDbService _generalDbService;
    private readonly IDatabaseRuntimeContext _databaseRuntimeContext;
    private readonly ILogger _logger;
    private readonly IImportExportTasks _importExportTasks;

    public LegacyChatDatabaseAccessProvider(
        ISchemaRepository schemaRepository,
        ISchemaDdlService ddlService,
        IGeneralDbService generalDbService,
        IDatabaseRuntimeContext databaseRuntimeContext,
        ILogger logger,
        IImportExportTasks importExportTasks)
    {
        _schemaRepository = schemaRepository;
        _ddlService = ddlService;
        _generalDbService = generalDbService;
        _databaseRuntimeContext = databaseRuntimeContext;
        _logger = logger;
        _importExportTasks = importExportTasks;
    }

    public IChatDatabaseAccess? GetDatabaseAccess(string connectionName)
    {
        if (string.IsNullOrWhiteSpace(connectionName))
        {
            return null;
        }

        return new LegacyChatDatabaseAccess(
            connectionName,
            _schemaRepository,
            _ddlService,
            _generalDbService,
            _databaseRuntimeContext,
            _logger,
            _importExportTasks);
    }
}

/// <summary>Adapter over the host <see cref="ISchemaRepository"/>/<see cref="IGeneralDb"/> for the chat tools.</summary>
public sealed class LegacyChatDatabaseAccess : IChatDatabaseAccess
{
    private readonly string _connectionName;
    private readonly ISchemaRepository _schemaRepository;
    private readonly ISchemaDdlService _ddlService;
    private readonly IGeneralDbService _generalDbService;
    private readonly IDatabaseRuntimeContext _databaseRuntimeContext;
    private readonly ILogger _logger;
    private readonly IImportExportTasks _importExportTasks;

    public LegacyChatDatabaseAccess(
        string connectionName,
        ISchemaRepository schemaRepository,
        ISchemaDdlService ddlService,
        IGeneralDbService generalDbService,
        IDatabaseRuntimeContext databaseRuntimeContext,
        ILogger logger,
        IImportExportTasks importExportTasks)
    {
        _connectionName = connectionName;
        _schemaRepository = schemaRepository;
        _ddlService = ddlService;
        _generalDbService = generalDbService;
        _databaseRuntimeContext = databaseRuntimeContext;
        _logger = logger;
        _importExportTasks = importExportTasks;
    }

    public string Database => _generalDbService.DBname(_connectionName);

    public IReadOnlyList<string> GetSchemas(string databaseName, string schemaPattern)
    {
        var databaseNode = FindDatabaseNode(databaseName);
        if (databaseNode is null)
        {
            return [];
        }

        var children = _schemaRepository.GetChildrenAsync(databaseNode).GetAwaiter().GetResult();
        return children
            .Where(node => node.Kind == SchemaNodeKind.Schema)
            .Select(node => node.Name)
            .Where(name => string.IsNullOrEmpty(schemaPattern)
                || name.Contains(schemaPattern, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public IReadOnlyList<ChatDatabaseObject> GetDbObjects(
        string databaseName,
        string schemaName,
        string objectPattern,
        ChatObjectType type)
    {
        var schemaNode = FindSchemaNode(databaseName, schemaName);
        if (schemaNode is null)
        {
            return [];
        }

        var children = _schemaRepository.GetChildrenAsync(schemaNode).GetAwaiter().GetResult();
        return children
            .Where(node => MapKind(node.Kind) == type)
            .Where(node => string.IsNullOrEmpty(objectPattern)
                || node.Name.Contains(objectPattern, StringComparison.OrdinalIgnoreCase)
                || (node.Description is not null
                    && node.Description.Contains(objectPattern, StringComparison.OrdinalIgnoreCase)))
            .Select(node => new ChatDatabaseObject(node.Name, node.Description))
            .ToList();
    }

    public IReadOnlyList<ChatDatabaseColumn> GetColumns(
        string databaseName,
        string schemaName,
        string objectName,
        string columnPattern)
    {
        var objectNode = FindObjectNode(databaseName, schemaName, objectName);
        if (objectNode is null)
        {
            return [];
        }

        var children = _schemaRepository.GetChildrenAsync(objectNode).GetAwaiter().GetResult();
        return children
            .Where(node => node.Kind == SchemaNodeKind.Column)
            .Select(node => new ChatDatabaseColumn(node.Name, node.Description ?? string.Empty))
            .ToList();
    }

    public Task<string?> GetCreateTableTextAsync(string database, string schema, string table)
        => GetDdlAsync(database, schema, table, SchemaNodeKind.Table);

    public Task<string?> GetCreateViewTextAsync(string database, string schema, string view)
        => GetDdlAsync(database, schema, view, SchemaNodeKind.View);

    public Task<string?> GetCreateProcedureTextAsync(string database, string schema, string procedure)
        => GetDdlAsync(database, schema, procedure, SchemaNodeKind.Procedure);

    // External tables are not classified under a stable SchemaNodeKind across providers
    // (some list them as Table, others under a dedicated kind). Match by name only so the
    // external-table DDL lookup never silently misses a differently-classified node.
    public Task<string?> GetCreateExternalTextAsync(string database, string schema, string externalTable)
        => GetDdlAsync(database, schema, externalTable, kind: null);

    public Task<string?> GetCreateSynonymTextAsync(string database, string schema, string synonym)
        => GetDdlAsync(database, schema, synonym, SchemaNodeKind.Synonym);

    public Task<string?> GetCreateIndexTextAsync(string database, string schema, string index)
        => GetDdlAsync(database, schema, index, SchemaNodeKind.Index);

    public Task<string?> GetCreatePartitionTextAsync(string database, string schema, string partition)
        => GetDdlAsync(database, schema, partition, SchemaNodeKind.Partition);

    public string GetCheckDistributeText(string database, string schema, string table)
        => string.Empty;

    public async Task<int> ExecuteNonQueryAsync(
        string sql,
        string databaseName,
        CancellationToken cancellationToken = default)
    {
        var gdb = _generalDbService.GetGeneralDb(
            _databaseRuntimeContext,
            _logger,
            _importExportTasks,
            _connectionName,
            out _);
        if (gdb is null)
        {
            throw new InvalidOperationException($"Connection '{_connectionName}' is unavailable.");
        }

        await using DbConnection connection = gdb.GetConnection(
            string.IsNullOrWhiteSpace(databaseName) ? null : databaseName,
            usePool: false);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = sql;
        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public IReadOnlyList<string>? TryGetDistributionColumns(string database, string schema, string table)
        => null;

    public IReadOnlyList<string>? TryGetOrganizeColumns(string database, string schema, string table)
        => null;

    private Task<string?> GetDdlAsync(string database, string schema, string objectName, SchemaNodeKind? kind)
    {
        var node = FindObjectNode(database, schema, objectName, kind);
        if (node is null)
        {
            return Task.FromResult<string?>(null);
        }

        return _ddlService.GetDdlAsync(new SchemaDdlRequest(node, SchemaDdlKind.Create));
    }

    private SchemaNode? FindDatabaseNode(string databaseName)
    {
        var roots = _schemaRepository.GetRootsAsync(_connectionName).GetAwaiter().GetResult();
        var connectionNode = roots.FirstOrDefault(node =>
            node.Kind == SchemaNodeKind.Connection
            && node.Name.Equals(_connectionName, StringComparison.OrdinalIgnoreCase));
        if (connectionNode is null)
        {
            return null;
        }

        var databases = _schemaRepository.GetChildrenAsync(connectionNode).GetAwaiter().GetResult();
        return databases.FirstOrDefault(node =>
            node.Kind == SchemaNodeKind.Database
            && node.Name.Equals(databaseName, StringComparison.OrdinalIgnoreCase));
    }

    private SchemaNode? FindSchemaNode(string databaseName, string schemaName)
    {
        var databaseNode = FindDatabaseNode(databaseName);
        if (databaseNode is null)
        {
            return null;
        }

        var schemas = _schemaRepository.GetChildrenAsync(databaseNode).GetAwaiter().GetResult();
        return schemas.FirstOrDefault(node =>
            node.Kind == SchemaNodeKind.Schema
            && node.Name.Equals(schemaName, StringComparison.OrdinalIgnoreCase));
    }

    private SchemaNode? FindObjectNode(
        string databaseName,
        string schemaName,
        string objectName,
        SchemaNodeKind? kindHint = null)
    {
        var schemaNode = FindSchemaNode(databaseName, schemaName);
        if (schemaNode is null)
        {
            return null;
        }

        var objects = _schemaRepository.GetChildrenAsync(schemaNode).GetAwaiter().GetResult();
        return objects.FirstOrDefault(node =>
            node.Name.Equals(objectName, StringComparison.OrdinalIgnoreCase)
            && (kindHint is null || node.Kind == kindHint || node.Kind == SchemaNodeKind.Unknown));
    }

    private static ChatObjectType MapKind(SchemaNodeKind kind) => kind switch
    {
        SchemaNodeKind.Table => ChatObjectType.Table,
        SchemaNodeKind.View => ChatObjectType.View,
        SchemaNodeKind.Procedure => ChatObjectType.Procedure,
        SchemaNodeKind.Function => ChatObjectType.Function,
        SchemaNodeKind.Synonym or SchemaNodeKind.Alias => ChatObjectType.Synonym,
        SchemaNodeKind.Index => ChatObjectType.Index,
        SchemaNodeKind.Partition => ChatObjectType.Partition,
        SchemaNodeKind.Nickname => ChatObjectType.ExternalTable,
        _ => ChatObjectType.Other
    };
}
