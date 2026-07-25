using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace AppBase.Services.Utilities;

public sealed record FileSearchOptions
{
    public required string Query { get; init; }
    public IReadOnlyList<string> ExtensionPatterns { get; init; } = Array.Empty<string>();
    public bool MatchWholeWord { get; init; }
    public bool MatchCase { get; init; }
    public bool UseRegex { get; init; }
    public int MaxFiles { get; init; } = 200;
    public int MaxMatchesPerFile { get; init; } = 50;
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(10);
}

public sealed record FileSearchMatch(int LineNumber, string LineText, int MatchIndex, int MatchLength);

public sealed record FileSearchFileResult(string Path, IReadOnlyList<FileSearchMatch> Matches, bool IsTruncated);

public sealed record FileSearchOutcome(
    IReadOnlyList<FileSearchFileResult> Files,
    bool WasCancelled,
    bool WasTruncated,
    int MatchCount);

/// <summary>Searches text files and returns line-level matches suitable for a grouped UI.</summary>
public sealed class FileSearchEngine : IFileSearchEngine
{
    public static readonly FileSearchEngine Default = new();

    public static readonly string[] DefaultExtensionPatterns =
    [
        ".sql", ".txt", ".dtsx", ".cs", ".py", ".ps1", ".vb", ".vbs",
        ".json", ".xml", ".html"
    ];

    public static IReadOnlyList<string> NormalizeExtensionPatterns(string? value)
        => Default.DoNormalizeExtensionPatterns(value);

    public static Task<FileSearchOutcome> SearchAsync(
        IEnumerable<string> paths,
        FileSearchOptions options,
        CancellationToken cancellationToken = default)
        => Default.DoSearchAsync(paths, options, cancellationToken);

    // --- Interface property ---
    public IReadOnlyList<string> GetDefaultExtensionPatterns() => DefaultExtensionPatterns;

    // --- Instance methods (DoXxx pattern) ---
    public IReadOnlyList<string> DoNormalizeExtensionPatterns(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return DefaultExtensionPatterns;

        return value
            .Split([',', ';', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(pattern => pattern.StartsWith("*.", StringComparison.Ordinal) ? pattern[1..] : pattern)
            .Select(pattern => pattern.StartsWith(".", StringComparison.Ordinal) ? pattern : "." + pattern)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public async Task<FileSearchOutcome> DoSearchAsync(
        IEnumerable<string> paths,
        FileSearchOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(paths);
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.Query))
            return new([], false, false, 0);

        Regex? regex = CreateMatcher(options);
        var filePaths = paths
            .Where(File.Exists)
            .Where(path => MatchesExtension(path, options.ExtensionPatterns))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        using var timeoutCts = new CancellationTokenSource(options.Timeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
        var token = linkedCts.Token;
        var results = new ConcurrentBag<FileSearchFileResult>();
        int matchCount = 0;

        try
        {
            await Task.Run(() =>
            {
                Parallel.ForEach(filePaths, new ParallelOptions
                {
                    CancellationToken = token,
                    MaxDegreeOfParallelism = Math.Max(1, Math.Min(Environment.ProcessorCount, 8))
                }, path =>
                {
                    if (results.Count >= options.MaxFiles)
                        return;

                    var matches = new List<FileSearchMatch>();
                    try
                    {
                        using var reader = new StreamReader(path, detectEncodingFromByteOrderMarks: true);
                        string? line;
                        int lineNumber = 0;
                        while ((line = reader.ReadLine()) is not null)
                        {
                            token.ThrowIfCancellationRequested();
                            lineNumber++;
                            foreach (var match in FindMatches(line, options, regex))
                            {
                                matches.Add(new FileSearchMatch(lineNumber, line, match.Index, match.Length));
                                matchCount++;
                                if (matches.Count >= options.MaxMatchesPerFile)
                                    break;
                            }

                            if (matches.Count >= options.MaxMatchesPerFile)
                                break;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (IOException)
                    {
                        return;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        return;
                    }

                    if (matches.Count > 0)
                        results.Add(new FileSearchFileResult(path, matches, matches.Count >= options.MaxMatchesPerFile));
                });
            }, token);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // A partial result is useful when a large tree exceeds the timeout.
        }

        var ordered = results
            .OrderBy(result => result.Path, StringComparer.OrdinalIgnoreCase)
            .Take(options.MaxFiles)
            .ToArray();

        return new(
            ordered,
            cancellationToken.IsCancellationRequested || timeoutCts.IsCancellationRequested,
            results.Count > options.MaxFiles || filePaths.Length > ordered.Length,
            matchCount);
    }

    private static Regex? CreateMatcher(FileSearchOptions options)
    {
        var regexOptions = RegexOptions.Compiled | RegexOptions.CultureInvariant;
        if (!options.MatchCase)
            regexOptions |= RegexOptions.IgnoreCase;

        if (options.UseRegex)
        {
            var pattern = options.MatchWholeWord ? $"\\b(?:{options.Query})\\b" : options.Query;
            return new Regex(pattern, regexOptions, TimeSpan.FromSeconds(3));
        }

        return options.MatchWholeWord
            ? new Regex($"\\b{Regex.Escape(options.Query)}\\b", regexOptions, TimeSpan.FromSeconds(3))
            : null;
    }

    private static IEnumerable<(int Index, int Length)> FindMatches(string line, FileSearchOptions options, Regex? regex)
    {
        if (regex is not null)
        {
            foreach (Match match in regex.Matches(line))
                yield return (match.Index, match.Length);
            yield break;
        }

        var comparison = options.MatchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        int offset = 0;
        while (offset < line.Length)
        {
            int index = line.IndexOf(options.Query, offset, comparison);
            if (index < 0)
                yield break;

            yield return (index, options.Query.Length);
            offset = index + Math.Max(1, options.Query.Length);
        }
    }

    private static bool MatchesExtension(string path, IReadOnlyList<string> patterns)
    {
        if (patterns.Count == 0)
            return true;

        string extension = Path.GetExtension(path);
        return patterns.Any(pattern => string.Equals(extension, pattern, StringComparison.OrdinalIgnoreCase));
    }

    // --- Explicit interface implementation ---
    IReadOnlyList<string> IFileSearchEngine.GetDefaultExtensionPatterns() => GetDefaultExtensionPatterns();
    IReadOnlyList<string> IFileSearchEngine.NormalizeExtensionPatterns(string? value) => DoNormalizeExtensionPatterns(value);
    Task<FileSearchOutcome> IFileSearchEngine.SearchAsync(IEnumerable<string> paths, FileSearchOptions options, CancellationToken cancellationToken)
        => DoSearchAsync(paths, options, cancellationToken);
}
