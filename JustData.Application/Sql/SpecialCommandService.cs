using System.Text.RegularExpressions;

namespace JustData.Application.Sql;

public sealed partial class SpecialCommandService : ISpecialCommandService
{
    public Task<SpecialCommandResult> TryHandleAsync(
        string sql,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sql) || sql.Length >= 120)
            return Task.FromResult(new SpecialCommandResult(null, WasHandled: false));

        string trimmed = sql.Trim();

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

        m = EchoRegex().Match(trimmed);
        if (m.Success)
        {
            return Task.FromResult(new SpecialCommandResult(
                $"SELECT '{m.Groups["msg"].Value.Replace("'", "''")}'", WasHandled: true));
        }

        m = EchoFileRegex().Match(trimmed);
        if (m.Success)
        {
            string message = m.Groups["msg"].Value;
            string filePath = m.Groups["filePath"].Value;
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

        return Task.FromResult(new SpecialCommandResult(null, WasHandled: false));
    }

    [GeneratedRegex(@"^__create_directory\s+""(?<directory>.+)""__$")]
    private static partial Regex CreateDirectoryRegex();

    [GeneratedRegex(@"^__delete_directory\s+""(?<directory>.+)""__$")]
    private static partial Regex DeleteDirectoryRegex();

    [GeneratedRegex(@"^___sleep\s+(?<nums>\d+)$", RegexOptions.IgnoreCase)]
    private static partial Regex SleepRegex();

    [GeneratedRegex(@"^___max_rows\s+(?<nums>\d+)$", RegexOptions.IgnoreCase)]
    private static partial Regex MaxRowsRegex();

    [GeneratedRegex(@"^___echo\s+""(?<msg>[^""]*)""$", RegexOptions.IgnoreCase)]
    private static partial Regex EchoRegex();

    [GeneratedRegex(@"^___echo_file\s+""(?<msg>[^""]*)""\s+""(?<filePath>[^""]*)""$", RegexOptions.IgnoreCase)]
    private static partial Regex EchoFileRegex();
}
