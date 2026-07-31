using System.Text.RegularExpressions;
using JustyBase.Core.Scripting;

namespace JustData.Application.Sql;

public sealed partial class SqlPreprocessingService : ISqlPreprocessingService
{
    private readonly Dictionary<string, string> _knownParameters = new(StringComparer.OrdinalIgnoreCase);

    public SqlPreprocessingService()
    {
    }

    public SqlPreprocessingService(IReadOnlyDictionary<string, string> preloadedParameters)
    {
        if (preloadedParameters is not null)
        {
            foreach (var kvp in preloadedParameters)
                _knownParameters[kvp.Key] = kvp.Value;
        }
    }

    public async Task<PreprocessResult> PreprocessAsync(
        PreprocessRequest request,
        IVariablePromptService? promptService = null,
        Func<string, Task<object?>>? sqlEvaluator = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string sql = request.SqlText ?? string.Empty;
        var updatedSession = new Dictionary<string, string>(StringComparer.Ordinal);
        var updatedGlobal = new Dictionary<string, string>(StringComparer.Ordinal);

        sql = LegacySqlDirectiveProcessor.NormalizeSleepMarkers(sql);

        var letResult = LegacySqlDirectiveProcessor.ProcessLetDirectives(sql, _knownParameters);
        sql = letResult.Sql;
        foreach (var kvp in letResult.KnownParameters)
            _knownParameters[kvp.Key] = kvp.Value;

        sql = await ResolveUnknownVariablesAsync(sql, request, promptService, cancellationToken);

        ISessionVarEvaluator? evaluator = sqlEvaluator is null
            ? null
            : new FuncSessionVarEvaluator(sqlEvaluator);

        var sessionResult = await LegacySqlDirectiveProcessor.TryEvaluateSessionOrGlobalDefinitionAsync(
            sql,
            _knownParameters,
            request.KnownParameters,
            evaluator,
            cancellationToken);
        if (sessionResult is not null)
        {
            sql = sessionResult.SqlWithoutDefinition;
            if (sessionResult.IsSession)
                updatedSession[sessionResult.VariableName] = sessionResult.EvaluatedValue;
            else
                updatedGlobal[sessionResult.VariableName] = sessionResult.EvaluatedValue;
        }

        sql = LegacySqlDirectiveProcessor.ReplaceDollarVariables(sql, _knownParameters, request.KnownParameters);

        string? exportFilePath = null;
        string? exportDirective = null;
        Match exportMatch = XlsxExportRegex().Match(sql);
        if (exportMatch.Success)
        {
            exportFilePath = exportMatch.Groups["filepath"].Value;
            exportDirective = "xlsx";
            sql = XlsxExportRegex().Replace(sql, ";");
        }
        else
        {
            Match legacyExportMatch = LegacyExportRegex().Match(sql);
            if (legacyExportMatch.Success)
            {
                exportFilePath = legacyExportMatch.Groups["filePath"].Value.Trim();
                exportDirective = legacyExportMatch.Groups["format"].Value.ToLowerInvariant();
                sql = legacyExportMatch.Groups["sql"].Value.Trim();
            }
        }

        sql = LegacySqlDirectiveProcessor.ApplyKnownParameters(sql, _knownParameters);

        return new PreprocessResult(
            ProcessedSql: sql,
            ExportFilePath: exportFilePath,
            ExportOptionDirective: exportDirective,
            UpdatedKnownParameters: new Dictionary<string, string>(_knownParameters, StringComparer.OrdinalIgnoreCase),
            UpdatedSessionVariables: updatedSession,
            UpdatedGlobalVariables: updatedGlobal);
    }

    private async Task<string> ResolveUnknownVariablesAsync(
        string sql,
        PreprocessRequest request,
        IVariablePromptService? promptService,
        CancellationToken cancellationToken)
    {
        if (!request.AllowPrompts || promptService is null)
            return sql;

        var unresolved = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        MatchCollection variableMatches = VariableRefRegex().Matches(sql);

        foreach (Match varMatch in variableMatches.Cast<Match>())
        {
            string varName = '$' + varMatch.Groups["var"].Value;
            string varKey = varName.ToUpperInvariant();

            if (_knownParameters.ContainsKey(varKey) || request.KnownParameters.ContainsKey(varKey))
                continue;

            if (unresolved.ContainsKey(varKey))
                continue;

            if (LegacyScriptDialectAdapter.IsInsideQuotedLiteral(sql, varMatch.Index))
                continue;

            unresolved[varKey] = varName;
        }

        if (unresolved.Count > 0)
        {
            var promptValues = await promptService.PromptAsync(unresolved, cancellationToken);
            if (promptValues is not null)
            {
                foreach (var kvp in promptValues)
                    _knownParameters[kvp.Key.ToUpperInvariant()] = kvp.Value;
            }
        }

        return sql;
    }

    [GeneratedRegex(@"\$(?<var>[a-zA-Z]\w*)", RegexOptions.IgnoreCase)]
    private static partial Regex VariableRefRegex();

    [GeneratedRegex(@"__xlsx\s+""(?<filepath>[^""]+)""", RegexOptions.IgnoreCase)]
    private static partial Regex XlsxExportRegex();

    [GeneratedRegex(@"___exp(?<format>Csv|Xlsx)\s*:\s*(?<sql>.*?)\s+->\s*(?<filePath>[^;\r\n]+)\s*;?", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex LegacyExportRegex();

    private sealed class FuncSessionVarEvaluator(Func<string, Task<object?>> evaluate) : ISessionVarEvaluator
    {
        public async ValueTask<object?> EvaluateSqlAsync(string sql, CancellationToken cancellationToken = default)
            => await evaluate(sql).ConfigureAwait(false);
    }
}
