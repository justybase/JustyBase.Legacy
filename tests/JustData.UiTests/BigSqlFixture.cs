using System.Text;

namespace JustData.UiTests;

/// <summary>
/// Resolves the real BIG.SQL fixture (production file shipped with the test output)
/// and falls back to the generated ~245 KB fixture when the file is not present.
/// </summary>
internal static class BigSqlFixture
{
    public const int TargetBytes = 245_000;
    public const int TargetLines = 9_186;

    /// <summary>
    /// Prefers the checked-in production fixture (<c>Fixtures/BIG.SQL</c> in the test output);
    /// falls back to the generated content so the typing-perf scenario always has a big document.
    /// </summary>
    public static string ResolveBigSqlPath()
    {
        string fixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "BIG.SQL");
        if (File.Exists(fixture) && new FileInfo(fixture).Length >= 100_000)
        {
            return fixture;
        }

        return CreateOrReuse();
    }

    public static string CreateOrReuse(string? directory = null)
    {
        directory ??= Path.Combine(Path.GetTempPath(), "JustyBaseLegacy.TypingPerf");
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "BIG.SQL");
        if (File.Exists(path))
        {
            long length = new FileInfo(path).Length;
            if (length >= TargetBytes - 5_000 && length <= TargetBytes + 40_000)
                return path;
        }

        File.WriteAllText(path, BuildContent(), Encoding.UTF8);
        return path;
    }

    public static string BuildContent()
    {
        var sb = new StringBuilder(TargetBytes + 8_192);
        sb.AppendLine("-- JUSTYBASE typing perf fixture (generated)");
        sb.AppendLine("-- Approximate size of production BIG.SQL for FCTB lag reproduction.");
        sb.AppendLine();

        int line = 0;
        while (sb.Length < TargetBytes || line < TargetLines)
        {
            line++;
            int stmt = line % 17;
            switch (stmt)
            {
                case 0:
                    sb.AppendLine($"CREATE TABLE TMP_PERF_{line:D5} (ID BIGINT, CODE VARCHAR(64), AMT NUMERIC(18,2), TS TIMESTAMP);");
                    break;
                case 1:
                    sb.AppendLine($"INSERT INTO TMP_PERF_{line:D5} SELECT ID, CODE, AMT, CURRENT_TIMESTAMP FROM JUST_DATA..DIMDATE WHERE ID = {line};");
                    break;
                case 2:
                    sb.AppendLine($"-- comment block {line}: " + new string('x', 80));
                    break;
                case 3:
                    sb.AppendLine("SELECT");
                    sb.AppendLine("    D.DATE_KEY,");
                    sb.AppendLine("    D.CALENDAR_YEAR,");
                    sb.AppendLine($"    D.CALENDAR_MONTH + {line % 12} AS M");
                    sb.AppendLine("FROM JUST_DATA..DIMDATE D");
                    sb.AppendLine($"WHERE D.DATE_KEY >= {20000101 + (line % 5000)}");
                    sb.AppendLine($"  AND D.DATE_KEY <  {20100101 + (line % 5000)};");
                    line += 5;
                    break;
                default:
                    sb.AppendLine(
                        $"UPDATE ANALYTICS.FACT_{line % 40:D2} SET FLAG = {line % 2} WHERE KEY = {line} /* tag={line:D6} */;");
                    break;
            }

            if (line > TargetLines + 2_000)
                break;
        }

        return sb.ToString();
    }
}
