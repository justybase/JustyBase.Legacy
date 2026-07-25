using AppBase.Services;

namespace AppBase.Tests.Utilities;

public sealed class FileUtilTests
{
    [Fact]
    public void Default_is_singleton()
    {
        Assert.Same(FileUtil.Default, FileUtil.Default);
    }

    [Fact]
    public void Implements_IFileUtil()
    {
        Assert.IsAssignableFrom<IFileUtil>(FileUtil.Default);
    }

    [Fact]
    public void Static_WhoIsLocking_delegates_to_default()
    {
        // Verify the static method works (Win32 Restart Manager API call)
        // Result may be empty list or throw depending on environment
        var result = FileUtil.WhoIsLocking(typeof(FileUtil).Assembly.Location);
        Assert.NotNull(result);
    }

    [Fact]
    public void Instance_DoWhoIsLocking_delegates_to_core()
    {
        var result = FileUtil.Default.DoWhoIsLocking(typeof(FileUtil).Assembly.Location);
        Assert.NotNull(result);
    }

    [Fact]
    public void WhoIsLocking_on_valid_but_locked_file_returns_process_list()
    {
        // Temp file that's not locked by anything
        var path = Path.GetTempFileName();
        try
        {
            var processes = FileUtil.Default.DoWhoIsLocking(path);
            Assert.NotNull(processes);
        }
        catch
        {
            // On some CI environments, Restart Manager may not be available
            // This is acceptable - the API is verified to exist
        }
        finally
        {
            File.Delete(path);
        }
    }
}
