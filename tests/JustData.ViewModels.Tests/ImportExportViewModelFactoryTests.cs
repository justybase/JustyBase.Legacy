using JustData.Application.ImportExport;
using JustData.ViewModels.ImportExport;

namespace JustData.ViewModels.Tests;

public sealed class ImportExportViewModelFactoryTests
{
    [Fact]
    public void Create_returns_view_model_with_both_use_cases()
    {
        var import = new FakeImportUseCase();
        var export = new FakeExportUseCase();
        var factory = new ImportExportViewModelFactory(import, export);

        using var vm = factory.Create();

        Assert.NotNull(vm);
        // Commands require a request to be set before they become executable
        Assert.False(vm.ImportCommand.CanExecute(null));
        Assert.False(vm.ExportCommand.CanExecute(null));

        // After setting a request, commands become executable
        vm.CurrentImportRequest = new ImportRequest(null, "file.csv", ImportFormat.Csv);
        Assert.True(vm.ImportCommand.CanExecute(null));

        vm.CurrentExportRequest = new ExportRequest(JustData.Application.Editor.EditorDocumentId.New(), "out.csv", ExportFormat.Csv);
        Assert.True(vm.ExportCommand.CanExecute(null));
    }

    [Fact]
    public void Create_without_export_returns_view_model_with_import_only()
    {
        var import = new FakeImportUseCase();
        var factory = new ImportExportViewModelFactory(import, null!);

        using var vm = factory.Create();
        Assert.NotNull(vm);
        Assert.False(vm.ExportCommand.CanExecute(null));
    }

    [Fact]
    public void Create_without_import_returns_view_model_with_export_only()
    {
        var export = new FakeExportUseCase();
        var factory = new ImportExportViewModelFactory(null!, export);

        using var vm = factory.Create();
        Assert.NotNull(vm);
        Assert.False(vm.ImportCommand.CanExecute(null));
    }

    [Fact]
    public void Create_returns_new_instance_each_time()
    {
        var import = new FakeImportUseCase();
        var export = new FakeExportUseCase();
        var factory = new ImportExportViewModelFactory(import, export);

        using var vm1 = factory.Create();
        using var vm2 = factory.Create();

        Assert.NotSame(vm1, vm2);
    }

    private sealed class FakeImportUseCase : IImportUseCase
    {
        public Task<ImportPreview> PreviewAsync(ImportRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new ImportPreview(request.SourcePath, request.Format, [], [], 0));

        public async IAsyncEnumerable<ImportProgress> ImportAsync(
            ImportRequest request,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield return new ImportProgress("completed", IsCompleted: true,
                Result: new ImportResult(0, 0, 0, []));
        }
    }

    private sealed class FakeExportUseCase : IResultExportUseCase
    {
        public async IAsyncEnumerable<ExportProgress> ExportAsync(
            ExportRequest request,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield return new ExportProgress("completed", 0, IsCompleted: true);
        }
    }
}
