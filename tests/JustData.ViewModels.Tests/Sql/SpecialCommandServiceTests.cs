using JustData.Application.Sql;

namespace JustData.ViewModels.Tests.Sql;

public sealed class SpecialCommandServiceTests
{
    private readonly SpecialCommandService _service = new();

    [Theory]
    [InlineData("", false)]
    [InlineData("SELECT 1", false)]
    public async Task Non_special_sql_is_not_handled(string sql, bool expected)
    {
        SpecialCommandResult result = await _service.TryHandleAsync(sql);

        Assert.Equal(expected, result.WasHandled);
        Assert.Null(result.ReplacementSql);
    }

    [Fact]
    public async Task Sleep_and_max_rows_commands_return_control_metadata()
    {
        SpecialCommandResult sleep = await _service.TryHandleAsync("___SLEEP 250");
        SpecialCommandResult maxRows = await _service.TryHandleAsync("___max_rows 50");

        Assert.True(sleep.WasHandled);
        Assert.Equal(250, sleep.SleepMilliseconds);
        Assert.Null(sleep.ReplacementSql);
        Assert.True(maxRows.WasHandled);
        Assert.Equal(50, maxRows.MaxRows);
    }

    [Fact]
    public async Task Echo_escapes_sql_literals()
    {
        SpecialCommandResult result = await _service.TryHandleAsync("___echo \"Bob's query\"");

        Assert.True(result.WasHandled);
        Assert.Equal("SELECT 'Bob''s query'", result.ReplacementSql);
    }

    [Fact]
    public async Task Echo_file_writes_message_and_returns_replacement()
    {
        string path = Path.Combine(Path.GetTempPath(), $"justdata-echo-{Guid.NewGuid():N}.txt");
        try
        {
            SpecialCommandResult result = await _service.TryHandleAsync(
                $"___echo_file \"hello\" \"{path}\"");

            Assert.True(result.WasHandled);
            Assert.Equal($"SELECT 'echoed to {path}'", result.ReplacementSql);
            Assert.Equal($"hello{Environment.NewLine}", await File.ReadAllTextAsync(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task Create_and_delete_directory_commands_change_the_file_system()
    {
        string path = Path.Combine(Path.GetTempPath(), $"justdata-special-{Guid.NewGuid():N}");
        try
        {
            SpecialCommandResult create = await _service.TryHandleAsync(
                $"__create_directory \"{path}\"__");

            Assert.True(create.WasHandled);
            Assert.True(Directory.Exists(path));

            SpecialCommandResult delete = await _service.TryHandleAsync(
                $"__delete_directory \"{path}\"__");

            Assert.True(delete.WasHandled);
            Assert.False(Directory.Exists(path));
        }
        finally
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
    }
}
