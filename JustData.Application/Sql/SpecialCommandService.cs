using System.Text.RegularExpressions;
using JustyBase.Core.Scripting;

namespace JustData.Application.Sql;

public sealed partial class SpecialCommandService : ISpecialCommandService
{
    public Task<SpecialCommandResult> TryHandleAsync(
        string sql,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sql) || sql.Length >= 120)
            return Task.FromResult(new SpecialCommandResult(null, WasHandled: false));

        // Canonicalize Legacy sleep/session markers toward Avalonia dialect where applicable.
        string trimmed = LegacyScriptDialectAdapter.Normalize(sql).Trim();

        Match m;

        m = CreateDirectoryRegex().Match(trimmed);
        if (m.Success)
        {
            try
            {
                Directory.CreateDirectory(m.Groups["directory"].Value);
                return Task.FromResult(new SpecialCommandResult(
                    $"SELECT 'created {m.Groups["directory"]}'", WasHandled: true));
            }
            catch (Exception ex)
            {
                return Task.FromResult(new SpecialCommandResult(
                    $"SELECT '{ex.Message}'", WasHandled: true));
            }
        }

        m = DeleteDirectoryRegex().Match(trimmed);
        if (m.Success)
        {
            try
            {
                Directory.Delete(m.Groups["directory"].Value);
                return Task.FromResult(new SpecialCommandResult(
                    $"SELECT 'deleted {m.Groups["directory"]}'", WasHandled: true));
            }
            catch (Exception ex)
            {
                return Task.FromResult(new SpecialCommandResult(
                    $"SELECT '{ex.Message}'", WasHandled: true));
            }
        }

        m = SleepRegex().Match(trimmed);
        if (m.Success)
        {
            int ms = int.Parse(m.Groups["nums"].Value);
            return Task.FromResult(new SpecialCommandResult(
                null, WasHandled: true, SleepMilliseconds: ms));
        }

        m = MaxRowsRegex().Match(trimmed);
        if (m.Success)
        {
            return Task.FromResult(new SpecialCommandResult(
                null, WasHandled: true, MaxRows: int.Parse(m.Groups["nums"].Value)));
        }

        m = EchoQuotedRegex().Match(trimmed);
        if (m.Success)
        {
            return Task.FromResult(new SpecialCommandResult(
                $"SELECT '{m.Groups["msg"].Value.Replace("'", "''")}'", WasHandled: true));
        }

        m = EchoFileQuotedRegex().Match(trimmed);
        if (m.Success)
        {
            return EchoToFile(m.Groups["msg"].Value, m.Groups["filePath"].Value);
        }

        m = EchoFileLegacyRegex().Match(trimmed);
        if (m.Success)
        {
            return EchoToFile(m.Groups["msg"].Value, m.Groups["filePath"].Value);
        }

        m = EchoUnquotedRegex().Match(trimmed);
        if (m.Success)
        {
            return Task.FromResult(new SpecialCommandResult(
                $"SELECT '{m.Groups["msg"].Value.Replace("'", "''")}'", WasHandled: true));
        }

        return Task.FromResult(new SpecialCommandResult(null, WasHandled: false));
    }

    private static Task<SpecialCommandResult> EchoToFile(string message, string filePath)
    {
        if (!string.IsNullOrWhiteSpace(filePath))
        {
            try
            {
                File.AppendAllText(filePath, message + Environment.NewLine);
            }
            catch { }
        }
        return Task.FromResult(new SpecialCommandResult(
            $"SELECT 'echoed to {filePath}'", WasHandled: true));
    }

    [GeneratedRegex(@"^__create_directory\s+""(?<directory>.+)""__$")]
    private static partial Regex CreateDirectoryRegex();

    [GeneratedRegex(@"^__delete_directory\s+""(?<directory>.+)""__$")]
    private static partial Regex DeleteDirectoryRegex();

    // Accepts both Legacy ___sleep N and Avalonia @sleep:N after Normalize.
    [GeneratedRegex(@"^(?:___sleep\s+|@sleep\s*:)\s*(?<nums>\d+)\s*;?$", RegexOptions.IgnoreCase)]
    private static partial Regex SleepRegex();

    // Legacy snippets use ___maxRows; Avalonia-style uses ___max_rows.
    [GeneratedRegex(@"^___max_?rows\s+(?<nums>\d+)\s*;?$", RegexOptions.IgnoreCase)]
    private static partial Regex MaxRowsRegex();

    [GeneratedRegex(@"^___echo\s+""(?<msg>[^""]*)""\s*;?$", RegexOptions.IgnoreCase)]
    private static partial Regex EchoQuotedRegex();

    [GeneratedRegex(@"^___echo\s+(?<msg>.+?)\s*;?$", RegexOptions.IgnoreCase)]
    private static partial Regex EchoUnquotedRegex();

    [GeneratedRegex(@"^___echo_file\s+""(?<msg>[^""]*)""\s+""(?<filePath>[^""]*)""\s*;?$", RegexOptions.IgnoreCase)]
    private static partial Regex EchoFileQuotedRegex();

    // Legacy snippet form: ___echoFile filepath:message
    [GeneratedRegex(@"^___echoFile\s+(?<filePath>.{2}.*?):(?<msg>.+?)\s*;?$", RegexOptions.IgnoreCase)]
    private static partial Regex EchoFileLegacyRegex();
}
