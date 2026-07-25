using JustData.Application.Editor;
using JustyBaseLegacy.UI.Editor;

namespace JustData.Preferences.Tests;

public sealed class EditorFileAdapterTests
{
    [Fact]
    public async Task Many_sql_adapter_preserves_the_legacy_wire_shape_and_order()
    {
        string path = Path.Combine(Path.GetTempPath(), $"manysql-{Guid.NewGuid():N}.manysql.enc");
        try
        {
            var service = new WinFormsManySqlBundleService();
            var expected = new ManySqlBundle(
                ["C:\\work\\one.sql"],
                [new ManySqlContent("scratch", "SELECT 2")],
                ["C:\\work\\one.sql", "scratch"],
                1);

            await service.SaveAsync(path, expected);
            string json = await File.ReadAllTextAsync(path);
            Assert.Contains("\"SqlPaths\"", json);
            Assert.Contains("\"SqlContentList\"", json);
            Assert.Contains("\"TabsOrder\"", json);
            Assert.Contains("\"SelectedTabNum\"", json);

            ManySqlBundle actual = await service.LoadAsync(path);
            Assert.Equal(expected.SqlPaths, actual.SqlPaths);
            Assert.Equal(expected.TabsOrder, actual.TabsOrder);
            Assert.Equal(expected.SelectedTabNum, actual.SelectedTabNum);
            Assert.Equal(expected.SqlContentList, actual.SqlContentList);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public async Task Editor_file_service_supports_utf8_without_a_bom()
    {
        string path = Path.Combine(Path.GetTempPath(), $"editor-{Guid.NewGuid():N}.sql");
        try
        {
            var service = new WinFormsEditorFileService();
            await service.WriteAsync(path, "żółw", useUtf8WithoutBom: true);

            byte[] bytes = await File.ReadAllBytesAsync(path);
            Assert.False(bytes.Take(3).SequenceEqual(new byte[] { 0xEF, 0xBB, 0xBF }));
            Assert.Equal("żółw", await service.ReadAsync(path));
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
