using System.Data;
using System.Text.RegularExpressions;
using JustyBase.Core.Scripting;

namespace JustData.Application.Sql;

public sealed partial class SqlPreprocessingService : ISqlPreprocessingService
{
    private readonly Dictionary<string, string> _knownParameters = new(StringComparer.OrdinalIgnoreCase);
    private static readonly DataTable _expressionTable = new();
    private static readonly char[] _newLines = ['\n', '\r'];

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

        // Only canonicalize sleep markers here. Session/global vars keep Legacy
        // SQL_RESULT / DataTable.Compute evaluation below.
        // Skip matches inside quotes/comments so SQL literals stay intact.
        sql = LegacySleepOnlyRegex().Replace(sql, match =>
            IsInsideQuotedLiteral(sql, match.Index)
                ? match.Value
                : "@sleep:" + match.Groups[1].Value);

        // Step 1: Process __Let / __LetFor directives (synchronous)
        sql = ProcessLetDirectives(sql);

        // Step 1b: Resolve unknown $variable references via prompt
        sql = await ResolveUnknownVariablesAsync(sql, request, promptService, cancellationToken);

        // Step 2: Process __SessionVar__ / __GlobalVar__ directives
        sql = await ProcessVariableDefinitionsAsync(
            sql, request, sqlEvaluator, updatedSession, updatedGlobal, cancellationToken);

        // Step 3: Replace remaining session/global variables in SQL text
        sql = ReplaceVariables(sql, request.DocumentKey, request.KnownParameters);

        // Step 4: Check for export directive
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

        // Step 5: Apply known parameter values (longest key first)
        if (_knownParameters.Count > 0)
        {
            var ordered = _knownParameters
                .OrderByDescending(kvp => kvp.Key.Length)
                .ToArray();
            foreach (var kvp in ordered)
            {
                sql = sql.Replace(kvp.Key, kvp.Value, StringComparison.OrdinalIgnoreCase);
            }
        }

        return new PreprocessResult(
            ProcessedSql: sql,
            ExportFilePath: exportFilePath,
            ExportOptionDirective: exportDirective,
            UpdatedKnownParameters: new Dictionary<string, string>(_knownParameters, StringComparer.OrdinalIgnoreCase),
            UpdatedSessionVariables: updatedSession,
            UpdatedGlobalVariables: updatedGlobal);
    }

    private string ProcessLetDirectives(string sql)
    {
        string trimmedSql = sql.TrimStart();
        if (!trimmedSql.StartsWith("__Let ", StringComparison.OrdinalIgnoreCase)
            && !trimmedSql.StartsWith("__LetFor ", StringComparison.OrdinalIgnoreCase))
            return sql;
        sql = trimmedSql;

        // __Let $var1=value1|$var2=value2
        if (sql.StartsWith("__Let ", StringComparison.OrdinalIgnoreCase))
        {
            int newlineIndex = sql.IndexOfAny(_newLines);
            string directive = newlineIndex > 0
                ? sql["__Let ".Length..newlineIndex]
                : sql["__Let ".Length..];
            string[] variables = directive.Split('|');
            sql = newlineIndex > 0 ? sql[newlineIndex..] : string.Empty;

            foreach (string variable in variables)
            {
                int equalsIndex = variable.IndexOf('=');
                if (equalsIndex > 0)
                {
                    string varName = variable[..equalsIndex].Trim();
                    string varValue = variable[(equalsIndex + 1)..].Trim();
                    if (!varName.StartsWith('$'))
                        varName = '$' + varName;
                    _knownParameters[varName.ToUpperInvariant()] = varValue;
                }
            }
        }
        // __LetFor $var|a|b|c
        else if (sql.Trim().StartsWith("__LetFor ", StringComparison.OrdinalIgnoreCase))
        {
            sql = sql.Trim();
            int newlineIndex = sql.IndexOfAny(_newLines);
            if (newlineIndex > 0)
            {
                string[] variables = sql["__LetFor ".Length..newlineIndex].Split('|');
                sql = sql[newlineIndex..];

                if (variables.Length >= 2)
                {
                    string varName = variables[0];
                    var sb = new System.Text.StringBuilder();
                    for (int i = 1; i < variables.Length; i++)
                    {
                        sb.Append(sql.Replace(varName, variables[i]));
                        sb.Append(';');
                    }
                    sql = sb.ToString();
                }
            }
        }

        return sql;
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

            if (IsInsideQuotedLiteral(sql, varMatch.Index))
                continue;

            unresolved[varKey] = varName;
        }

        if (unresolved.Count > 0)
        {
            var promptValues = await promptService.PromptAsync(unresolved, cancellationToken);
            if (promptValues is not null)
            {
                foreach (var kvp in promptValues)
                {
                    string upperKey = kvp.Key.ToUpperInvariant();
                    _knownParameters[upperKey] = kvp.Value;
                }
            }
        }

        return sql;
    }

    private static bool IsInsideQuotedLiteral(string text, int position)
    {
        if (position <= 0 || position >= text.Length)
            return false;

        bool inSingleQuote = false;
        bool inDoubleQuote = false;
        bool inLineComment = false;
        bool inBlockComment = false;

        for (int i = 0; i < position; i++)
        {
            char c = text[i];

            if (inLineComment)
            {
                if (c == '\n' || c == '\r')
                    inLineComment = false;
                continue;
            }

            if (inBlockComment)
            {
                if (c == '*' && i + 1 < position && text[i + 1] == '/')
                {
                    inBlockComment = false;
                    i++;
                }
                continue;
            }

            if (inSingleQuote)
            {
                if (c == '\'')
                {
                    if (i + 1 < position && text[i + 1] == '\'')
                        i++;
                    else
                        inSingleQuote = false;
                }
                continue;
            }

            if (inDoubleQuote)
            {
                if (c == '"')
                    inDoubleQuote = false;
                continue;
            }

            if (c == '-' && i + 1 < position && text[i + 1] == '-')
            {
                inLineComment = true;
                i++;
                continue;
            }

            if (c == '/' && i + 1 < position && text[i + 1] == '*')
            {
                inBlockComment = true;
                i++;
                continue;
            }

            if (c == '\'')
                inSingleQuote = true;
            else if (c == '"')
                inDoubleQuote = true;
        }

        return inSingleQuote || inDoubleQuote || inLineComment || inBlockComment;
    }

    private async Task<string> ProcessVariableDefinitionsAsync(
        string sql,
        PreprocessRequest request,
        Func<string, Task<object?>>? sqlEvaluator,
        Dictionary<string, string> updatedSession,
        Dictionary<string, string> updatedGlobal,
        CancellationToken cancellationToken)
    {
        var sessionDefRegex = SessionVarDefineRegex();
        var globalDefRegex = GlobalVarDefineRegex();

        Match m = sessionDefRegex.Match(sql);
        Match m2 = globalDefRegex.Match(sql);

        if (!m.Success && !m2.Success)
            return sql;

        Match activeMatch = m.Success ? m : m2;
        bool isSession = m.Success;

        string variableValue = activeMatch.Groups["sessionValue"].Value;
        string name = activeMatch.Groups["sessionVar"].Value;
        string val = ReplaceVariables(variableValue, request.DocumentKey, request.KnownParameters);

        object? evaluated = val;
        try
        {
            if (!val.StartsWith("SQL_", StringComparison.Ordinal))
            {
                evaluated = _expressionTable.Compute(val, "");
            }
            else if (sqlEvaluator is not null)
            {
                if (val.StartsWith("SQL_RESULT[", StringComparison.Ordinal) && val.EndsWith(']'))
                {
                    string innerSql = val["SQL_RESULT[".Length..^1];
                    evaluated = await sqlEvaluator(innerSql);
                }
                else if (val.StartsWith("SQL_RECORDS_AFFECTED[", StringComparison.Ordinal) && val.EndsWith(']'))
                {
                    string innerSql = val["SQL_RECORDS_AFFECTED[".Length..^1];
                    evaluated = await sqlEvaluator(innerSql);
                }
            }
        }
        catch
        {
            evaluated = val;
        }

        string stringValue = evaluated?.ToString() ?? string.Empty;

        if (isSession)
        {
            updatedSession[name] = stringValue;
        }
        else
        {
            updatedGlobal[name] = stringValue;
        }

        // Remove the definition line from the SQL
        sql = sessionDefRegex.Replace(globalDefRegex.Replace(sql, ""), "");

        return sql;
    }

    private string ReplaceVariables(string sql, string documentKey, IReadOnlyDictionary<string, string> knownParams)
    {
        if (string.IsNullOrEmpty(sql))
            return sql;

        // Replace only canonical parameter keys, including the '$' prefix.
        // Never replace a bare identifier such as 'value'.
        var parameters = new Dictionary<string, string>(_knownParameters, StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in knownParams)
            parameters[kvp.Key.StartsWith('$') ? kvp.Key : '$' + kvp.Key] = kvp.Value;

        if (parameters.Count > 0)
        {
            var ordered = parameters
                .OrderByDescending(kvp => kvp.Key.Length)
                .ToArray();
            foreach (var kvp in ordered)
            {
                if (sql.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
                    sql = sql.Replace(kvp.Key, kvp.Value, StringComparison.OrdinalIgnoreCase);
            }
        }

        return sql;
    }

    [GeneratedRegex(@"___sleep\s*[: ]\s*(\d+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex LegacySleepOnlyRegex();

    [GeneratedRegex(@"\$(?<var>[a-zA-Z]\w*)", RegexOptions.IgnoreCase)]
    private static partial Regex VariableRefRegex();

    [GeneratedRegex(@"__xlsx\s+""(?<filepath>[^""]+)""", RegexOptions.IgnoreCase)]
    private static partial Regex XlsxExportRegex();

    [GeneratedRegex(@"___exp(?<format>Csv|Xlsx)\s*:\s*(?<sql>.*?)\s+->\s*(?<filePath>[^;\r\n]+)\s*;?", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex LegacyExportRegex();

    [GeneratedRegex(@"^\s*__SessionVar__(?<sessionVar>\$\w+)\s*=\s*(?<sessionValue>.+)$", RegexOptions.Multiline)]
    private static partial Regex SessionVarDefineRegex();

    [GeneratedRegex(@"^\s*__GlobalVar__(?<sessionVar>\$\w+)\s*=\s*(?<sessionValue>.+)$", RegexOptions.Multiline)]
    private static partial Regex GlobalVarDefineRegex();
}
