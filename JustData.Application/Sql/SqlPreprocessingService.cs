using System.Data;
using System.Text;
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

    /// <summary>
    /// Returns an offset-preserving SQL view for authoring services. Legacy
    /// ampersand declarations are application directives rather than SQL
    /// statements, so they are blanked out before the SQL parser sees them.
    /// Newlines are retained so diagnostics continue to point at the original
    /// document locations.
    /// </summary>
    public static string RemoveAmpersandDeclarationsForAuthoring(string sql)
    {
        if (string.IsNullOrEmpty(sql))
            return sql;

        IReadOnlyList<AmpersandDeclaration> declarations = FindAmpersandDeclarations(sql);
        if (declarations.Count == 0)
            return sql;

        var result = new StringBuilder(sql);
        foreach (AmpersandDeclaration declaration in declarations)
        {
            for (int index = declaration.Start; index < declaration.End; index++)
            {
                if (result[index] is not '\r' and not '\n')
                    result[index] = ' ';
            }
        }

        return result.ToString();
    }

    public async Task<PreprocessResult> PreprocessAsync(
        PreprocessRequest request,
        IVariablePromptService? promptService = null,
        Func<string, Task<object?>>? sqlEvaluator = null,
        CancellationToken cancellationToken = default,
        Func<SqlPreprocessCommand, Task<object?>>? sqlCommandEvaluator = null)
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

        // &... variables are the legacy session/global-variable dialect. Keep
        // them separate from the $... parameter path so that values such as
        // '2026' can be inserted correctly inside an SQL string literal.
        var knownVariables = CreateKnownVariables(_knownParameters, request.KnownParameters);
        sql = await ProcessAmpersandDeclarationsAsync(
            sql,
            knownVariables,
            sqlEvaluator,
            sqlCommandEvaluator,
            updatedGlobal,
            cancellationToken);

        Dictionary<string, string> requestDollarParameters = CreateDollarParameters(request.KnownParameters);

        ISessionVarEvaluator? evaluator = sqlEvaluator is null
            ? null
            : new FuncSessionVarEvaluator(sqlEvaluator);

        var sessionResult = await TryEvaluateLegacySqlDefinitionAsync(
            sql,
            knownVariables,
            requestDollarParameters,
            sqlEvaluator,
            sqlCommandEvaluator,
            cancellationToken);
        if (sessionResult is null)
        {
            sessionResult = await LegacySqlDirectiveProcessor.TryEvaluateSessionOrGlobalDefinitionAsync(
                sql,
                _knownParameters,
                requestDollarParameters,
                evaluator,
                cancellationToken);
        }
        if (sessionResult is not null)
        {
            sql = sessionResult.SqlWithoutDefinition;
            if (sessionResult.IsSession)
                updatedSession[sessionResult.VariableName] = sessionResult.EvaluatedValue;
            else
                updatedGlobal[sessionResult.VariableName] = sessionResult.EvaluatedValue;
        }

        sql = LegacySqlDirectiveProcessor.ReplaceDollarVariables(
            sql,
            _knownParameters,
            requestDollarParameters);
        sql = ReplaceAmpersandVariables(sql, GetAmpersandVariables(knownVariables));

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

    private static async Task<string> ProcessAmpersandDeclarationsAsync(
        string sql,
        Dictionary<string, string> knownVariables,
        Func<string, Task<object?>>? sqlEvaluator,
        Func<SqlPreprocessCommand, Task<object?>>? sqlCommandEvaluator,
        Dictionary<string, string> updatedGlobal,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<AmpersandDeclaration> declarations = FindAmpersandDeclarations(sql);
        if (declarations.Count == 0)
            return sql;

        var result = new StringBuilder(sql.Length);
        int copiedThrough = 0;

        foreach (AmpersandDeclaration declaration in declarations)
        {
            result.Append(sql, copiedThrough, declaration.Start - copiedThrough);

            cancellationToken.ThrowIfCancellationRequested();

            string variableName = declaration.Name;
            string expression = declaration.Expression;
            string variableValue;

            if (TryExtractSqlCommand(
                    expression,
                    out SqlPreprocessCommandKind commandKind,
                    out string innerSql))
            {
                string expandedInnerSql = ReplaceScriptVariables(innerSql, knownVariables);
                object? evaluated = await EvaluateSqlCommandAsync(
                    expandedInnerSql,
                    commandKind,
                    sqlEvaluator,
                    sqlCommandEvaluator,
                    cancellationToken).ConfigureAwait(false);
                variableValue = FormatSqlResultValue(evaluated);
            }
            else
            {
                string expandedExpression = ReplaceScriptVariables(expression, knownVariables);
                variableValue = EvaluateLegacyExpression(expandedExpression);
            }

            knownVariables[variableName] = variableValue;
            updatedGlobal[variableName] = variableValue;
            copiedThrough = declaration.End;
        }

        result.Append(sql, copiedThrough, sql.Length - copiedThrough);
        return result.ToString();
    }

    private async Task<SessionVarDefinitionResult?> TryEvaluateLegacySqlDefinitionAsync(
        string sql,
        IReadOnlyDictionary<string, string> knownVariables,
        IReadOnlyDictionary<string, string> requestDollarParameters,
        Func<string, Task<object?>>? sqlEvaluator,
        Func<SqlPreprocessCommand, Task<object?>>? sqlCommandEvaluator,
        CancellationToken cancellationToken)
    {
        Match sessionMatch = LegacySessionVarDefinitionRegex().Match(sql);
        Match globalMatch = LegacyGlobalVarDefinitionRegex().Match(sql);
        if (!sessionMatch.Success && !globalMatch.Success)
            return null;

        Match activeMatch = sessionMatch.Success ? sessionMatch : globalMatch;
        string expression = activeMatch.Groups["value"].Value.Trim();
        expression = LegacySqlDirectiveProcessor.ReplaceDollarVariables(
            expression,
            _knownParameters,
            requestDollarParameters);
        expression = ReplaceAmpersandVariables(expression, GetAmpersandVariables(knownVariables));

        if (!TryExtractSqlCommand(
                expression,
                out SqlPreprocessCommandKind commandKind,
                out string innerSql))
        {
            return null;
        }

        object? evaluated = await EvaluateSqlCommandAsync(
            innerSql,
            commandKind,
            sqlEvaluator,
            sqlCommandEvaluator,
            cancellationToken).ConfigureAwait(false);

        string withoutDefinition = LegacySessionVarDefinitionRegex().Replace(
            LegacyGlobalVarDefinitionRegex().Replace(sql, string.Empty),
            string.Empty);
        return new SessionVarDefinitionResult(
            withoutDefinition,
            activeMatch.Groups["name"].Value,
            FormatSqlResultValue(evaluated),
            sessionMatch.Success);
    }

    private static async Task<object?> EvaluateSqlCommandAsync(
        string sql,
        SqlPreprocessCommandKind commandKind,
        Func<string, Task<object?>>? sqlEvaluator,
        Func<SqlPreprocessCommand, Task<object?>>? sqlCommandEvaluator,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (sqlCommandEvaluator is not null)
        {
            return await sqlCommandEvaluator(
                new SqlPreprocessCommand(sql, commandKind, cancellationToken)).ConfigureAwait(false);
        }

        if (sqlEvaluator is not null)
            return await sqlEvaluator(sql).ConfigureAwait(false);

        throw new InvalidOperationException(
            $"{commandKind} SQL preprocessing command requires an active database connection.");
    }

    private static string EvaluateLegacyExpression(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return string.Empty;

        string candidate = expression.Trim();
        if (IsSqlStringLiteral(candidate))
            return candidate;

        try
        {
            using var table = new DataTable();
            object? evaluated = table.Compute(candidate, string.Empty);
            return evaluated?.ToString() ?? string.Empty;
        }
        catch
        {
            // Preserve the historical best-effort behaviour for non-SQL
            // declarations. SQL_RESULT failures are intentionally not caught.
            return expression;
        }
    }

    private static bool TryExtractSqlCommand(
        string expression,
        out SqlPreprocessCommandKind commandKind,
        out string innerSql)
    {
        string candidate = expression.Trim();
        (string Prefix, SqlPreprocessCommandKind Kind)[] prefixes =
        [
            ("SQL_RESULT[", SqlPreprocessCommandKind.Scalar),
            ("SQL_RECORDS_AFFECTED[", SqlPreprocessCommandKind.NonQuery)
        ];

        foreach ((string prefix, SqlPreprocessCommandKind kind) in prefixes)
        {
            if (candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                && candidate.EndsWith(']')
                && candidate.Length >= prefix.Length + 1)
            {
                commandKind = kind;
                innerSql = candidate[prefix.Length..^1].Trim();
                return true;
            }
        }

        commandKind = default;
        innerSql = string.Empty;
        return false;
    }

    private static string FormatSqlResultValue(object? value) =>
        value is null or DBNull ? string.Empty : value.ToString() ?? string.Empty;

    private static Dictionary<string, string> CreateKnownVariables(
        IReadOnlyDictionary<string, string> knownParameters,
        IReadOnlyDictionary<string, string> requestParameters)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in knownParameters)
            result[pair.Key] = pair.Value;

        foreach (var pair in requestParameters)
        {
            string key = pair.Key.Trim();
            if (key.Length == 0)
                continue;

            if (!key.StartsWith('$') && !key.StartsWith('&'))
                key = '$' + key;
            result[key] = pair.Value;
        }

        return result;
    }

    private static Dictionary<string, string> CreateDollarParameters(
        IReadOnlyDictionary<string, string> parameters)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in parameters)
        {
            string key = pair.Key.Trim();
            if (key.StartsWith('&'))
                continue;
            if (!key.StartsWith('$'))
                key = '$' + key;
            result[key] = pair.Value;
        }

        return result;
    }

    private static Dictionary<string, string> GetAmpersandVariables(
        IReadOnlyDictionary<string, string> variables)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in variables)
        {
            if (pair.Key.StartsWith('&'))
                result[pair.Key] = pair.Value;
        }

        return result;
    }

    private static string ReplaceScriptVariables(
        string sql,
        IReadOnlyDictionary<string, string> variables)
    {
        if (string.IsNullOrEmpty(sql) || variables.Count == 0)
            return sql;

        sql = LegacySqlDirectiveProcessor.ReplaceDollarVariables(
            sql,
            CreateDollarParameters(variables));
        return ReplaceAmpersandVariables(sql, GetAmpersandVariables(variables));
    }

    private static string ReplaceAmpersandVariables(
        string sql,
        IReadOnlyDictionary<string, string> variables)
    {
        if (string.IsNullOrEmpty(sql) || variables.Count == 0)
            return sql;

        var result = new StringBuilder(sql.Length);
        bool inSingleQuote = false;
        bool inDoubleQuote = false;
        bool inLineComment = false;
        bool inBlockComment = false;

        for (int index = 0; index < sql.Length; index++)
        {
            char current = sql[index];

            if (inLineComment)
            {
                result.Append(current);
                if (current is '\r' or '\n')
                    inLineComment = false;
                continue;
            }

            if (inBlockComment)
            {
                result.Append(current);
                if (current == '*' && index + 1 < sql.Length && sql[index + 1] == '/')
                {
                    result.Append('/');
                    index++;
                    inBlockComment = false;
                }
                continue;
            }

            if (inSingleQuote)
            {
                if (current == '\'' && index + 1 < sql.Length && sql[index + 1] == '\'')
                {
                    result.Append("''");
                    index++;
                    continue;
                }

                if (current == '\'')
                    inSingleQuote = false;

                if (current == '&' && TryReadVariable(sql, index, out string quotedKey, out int quotedEnd)
                    && variables.TryGetValue(quotedKey, out string? quotedValue))
                {
                    result.Append(EscapeSqlLiteralContent(quotedValue));
                    index = quotedEnd - 1;
                    continue;
                }

                result.Append(current);
                continue;
            }

            if (inDoubleQuote)
            {
                result.Append(current);
                if (current == '"')
                    inDoubleQuote = false;
                continue;
            }

            if (current == '-' && index + 1 < sql.Length && sql[index + 1] == '-')
            {
                result.Append("--");
                index++;
                inLineComment = true;
                continue;
            }

            if (current == '/' && index + 1 < sql.Length && sql[index + 1] == '*')
            {
                result.Append("/*");
                index++;
                inBlockComment = true;
                continue;
            }

            if (current == '\'')
            {
                inSingleQuote = true;
                result.Append(current);
                continue;
            }

            if (current == '"')
            {
                inDoubleQuote = true;
                result.Append(current);
                continue;
            }

            if (current == '&' && TryReadVariable(sql, index, out string key, out int end)
                && variables.TryGetValue(key, out string? value))
            {
                result.Append(value);
                index = end - 1;
                continue;
            }

            result.Append(current);
        }

        return result.ToString();
    }

    private static bool TryReadVariable(
        string sql,
        int start,
        out string key,
        out int end)
    {
        key = string.Empty;
        end = start + 1;
        if (start + 1 >= sql.Length || sql[start] != '&')
            return false;

        char first = sql[start + 1];
        if (!(char.IsLetter(first) || first == '_'))
            return false;

        end = start + 2;
        while (end < sql.Length
            && (char.IsLetterOrDigit(sql[end]) || sql[end] == '_'))
        {
            end++;
        }

        key = sql[start..end];
        return true;
    }

    private static string EscapeSqlLiteralContent(string value)
    {
        string content = value;
        if (content.Length >= 2 && content[0] == '\'' && content[^1] == '\'')
            content = content[1..^1].Replace("''", "'", StringComparison.Ordinal);
        return content.Replace("'", "''", StringComparison.Ordinal);
    }

    private static bool IsSqlStringLiteral(string value)
    {
        if (value.Length < 2 || value[0] != '\'' || value[^1] != '\'')
            return false;

        for (int index = 1; index < value.Length - 1; index++)
        {
            if (value[index] != '\'')
                continue;

            if (index + 1 < value.Length - 1 && value[index + 1] == '\'')
            {
                index++;
                continue;
            }

            return false;
        }

        return true;
    }

    private static IReadOnlyList<AmpersandDeclaration> FindAmpersandDeclarations(string sql)
    {
        var declarations = new List<AmpersandDeclaration>();

        for (int index = 0; index < sql.Length; index++)
        {
            char current = sql[index];
            if (current == '\'')
            {
                index = SkipSingleQuotedLiteral(sql, index) - 1;
                continue;
            }

            if (current == '"')
            {
                index = SkipDoubleQuotedLiteral(sql, index) - 1;
                continue;
            }

            if (current == '-' && index + 1 < sql.Length && sql[index + 1] == '-')
            {
                index = SkipLineComment(sql, index) - 1;
                continue;
            }

            if (current == '/' && index + 1 < sql.Length && sql[index + 1] == '*')
            {
                index = SkipBlockComment(sql, index) - 1;
                continue;
            }

            if (!IsDeclareKeywordAt(sql, index) || !IsDeclarationBoundary(sql, index))
                continue;

            if (!TryReadAmpersandDeclaration(sql, index, out AmpersandDeclaration declaration))
                continue;

            declarations.Add(declaration);
            index = declaration.End - 1;
        }

        return declarations;
    }

    private static bool IsDeclareKeywordAt(string sql, int index) =>
        index + 7 <= sql.Length
        && sql.AsSpan(index, 7).Equals("declare".AsSpan(), StringComparison.OrdinalIgnoreCase)
        && (index + 7 == sql.Length || char.IsWhiteSpace(sql[index + 7]));

    private static bool IsDeclarationBoundary(string sql, int index)
    {
        if (index == 0)
            return true;

        int previous = index - 1;
        while (previous >= 0 && sql[previous] is ' ' or '\t')
            previous--;

        return previous < 0 || sql[previous] is ';' or '\r' or '\n';
    }

    private static bool TryReadAmpersandDeclaration(
        string sql,
        int start,
        out AmpersandDeclaration declaration)
    {
        int cursor = SkipWhitespace(sql, start + 7);
        if (cursor >= sql.Length || sql[cursor] != '&')
        {
            declaration = default;
            return false;
        }

        int nameStart = cursor++;
        if (cursor >= sql.Length || !(char.IsLetter(sql[cursor]) || sql[cursor] == '_'))
        {
            declaration = default;
            return false;
        }

        cursor++;
        while (cursor < sql.Length && (char.IsLetterOrDigit(sql[cursor]) || sql[cursor] == '_'))
            cursor++;

        string name = sql[nameStart..cursor];
        cursor = SkipWhitespace(sql, cursor);
        if (cursor >= sql.Length || sql[cursor] != '=')
        {
            declaration = default;
            return false;
        }

        cursor = SkipWhitespace(sql, cursor + 1);
        int expressionStart = cursor;
        int expressionEnd = cursor;
        int declarationEnd = cursor;

        while (cursor < sql.Length)
        {
            char current = sql[cursor];
            if (current == '\'')
            {
                cursor = SkipSingleQuotedLiteral(sql, cursor);
                continue;
            }

            if (current == '"')
            {
                cursor = SkipDoubleQuotedLiteral(sql, cursor);
                continue;
            }

            if (current == '-' && cursor + 1 < sql.Length && sql[cursor + 1] == '-')
            {
                expressionEnd = TrimEnd(sql, expressionStart, cursor);
                declarationEnd = cursor;
                break;
            }

            if (current == '/' && cursor + 1 < sql.Length && sql[cursor + 1] == '*')
            {
                cursor = SkipBlockComment(sql, cursor);
                continue;
            }

            if (current == ';')
            {
                expressionEnd = TrimEnd(sql, expressionStart, cursor);
                declarationEnd = cursor + 1;
                break;
            }

            if (current is '\r' or '\n')
            {
                expressionEnd = TrimEnd(sql, expressionStart, cursor);
                declarationEnd = cursor;
                break;
            }

            cursor++;
        }

        if (cursor >= sql.Length)
        {
            expressionEnd = TrimEnd(sql, expressionStart, sql.Length);
            declarationEnd = sql.Length;
        }

        if (expressionEnd <= expressionStart)
        {
            declaration = default;
            return false;
        }

        declaration = new AmpersandDeclaration(
            Start: start,
            End: declarationEnd,
            Name: name,
            Expression: sql[expressionStart..expressionEnd]);
        return true;
    }

    private static int SkipWhitespace(string sql, int index)
    {
        while (index < sql.Length && char.IsWhiteSpace(sql[index]))
            index++;
        return index;
    }

    private static int TrimEnd(string sql, int start, int end)
    {
        while (end > start && char.IsWhiteSpace(sql[end - 1]))
            end--;
        return end;
    }

    private static int SkipSingleQuotedLiteral(string sql, int start)
    {
        for (int index = start + 1; index < sql.Length; index++)
        {
            if (sql[index] != '\'')
                continue;
            if (index + 1 < sql.Length && sql[index + 1] == '\'')
            {
                index++;
                continue;
            }
            return index + 1;
        }

        return sql.Length;
    }

    private static int SkipDoubleQuotedLiteral(string sql, int start)
    {
        for (int index = start + 1; index < sql.Length; index++)
        {
            if (sql[index] != '"')
                continue;
            if (index + 1 < sql.Length && sql[index + 1] == '"')
            {
                index++;
                continue;
            }
            return index + 1;
        }

        return sql.Length;
    }

    private static int SkipLineComment(string sql, int start)
    {
        int newline = sql.IndexOfAny(['\r', '\n'], start + 2);
        return newline < 0 ? sql.Length : newline;
    }

    private static int SkipBlockComment(string sql, int start)
    {
        int end = sql.IndexOf("*/", start + 2, StringComparison.Ordinal);
        return end < 0 ? sql.Length : end + 2;
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
            if (promptValues is null)
                throw new OperationCanceledException(cancellationToken);

            foreach (var kvp in promptValues)
                _knownParameters[kvp.Key.ToUpperInvariant()] = kvp.Value;
        }

        return sql;
    }

    [GeneratedRegex(@"\$(?<var>[a-zA-Z]\w*)", RegexOptions.IgnoreCase)]
    private static partial Regex VariableRefRegex();

    [GeneratedRegex(
        @"^[ \t]*__SessionVar__(?<name>\$\w+)\s*=\s*(?<value>.+?)[ \t]*;?[ \t]*(?=\r?$)",
        RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex LegacySessionVarDefinitionRegex();

    [GeneratedRegex(
        @"^[ \t]*__GlobalVar__(?<name>\$\w+)\s*=\s*(?<value>.+?)[ \t]*;?[ \t]*(?=\r?$)",
        RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex LegacyGlobalVarDefinitionRegex();

    [GeneratedRegex(@"__xlsx\s+""(?<filepath>[^""]+)""", RegexOptions.IgnoreCase)]
    private static partial Regex XlsxExportRegex();

    [GeneratedRegex(@"___exp(?<format>Csv|Xlsx)\s*:\s*(?<sql>.*?)\s+->\s*(?<filePath>[^;\r\n]+)\s*;?", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex LegacyExportRegex();

    private sealed class FuncSessionVarEvaluator(Func<string, Task<object?>> evaluate) : ISessionVarEvaluator
    {
        public async ValueTask<object?> EvaluateSqlAsync(string sql, CancellationToken cancellationToken = default)
            => await evaluate(sql).ConfigureAwait(false);
    }

    private readonly record struct AmpersandDeclaration(
        int Start,
        int End,
        string Name,
        string Expression);
}
