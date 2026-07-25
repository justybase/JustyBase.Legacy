using System.Diagnostics;

namespace AppBase.Common.Interfaces;

public interface IInlineCommandRunner
{
    Task DoInlineCommandAsync(
        string connectionString,
        string runCommand,
        ISqlExecutionLog log,
        Stopwatch stopwatch,
        CancellationToken cancellationToken = default);
}
