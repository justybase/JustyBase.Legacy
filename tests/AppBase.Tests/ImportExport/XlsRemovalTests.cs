using AppBase.Common.Configuration;
using AppBase.Common.Interfaces;
using AppBase.Services;

namespace AppBase.Tests.ImportExport;

public sealed class XlsRemovalTests
{
    [Theory]
    [InlineData("legacy.xls")]
    [InlineData("legacy.XLS")]
    public void Importing_xls_is_rejected_with_a_migration_message(string fileName)
    {
        var tasks = new ImportExportTasks(new TestSettingsContext());

        var exception = Assert.Throws<NotSupportedException>(
            () => tasks.ReadFileAndMakeDataSet(fileName, skipRows: 0));

        Assert.Equal(
            "The .xls format is no longer supported. Use .xlsx or .xlsb instead.",
            exception.Message);
    }

    private sealed class TestSettingsContext : IApplicationSettingsContext
    {
        public IApplicationConfig Config => null!;
        public string ConfigDirectory => string.Empty;
        public string ConfigMainFile => string.Empty;
        public bool DoSaveConfig => false;
    }
}
