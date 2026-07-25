using JustData.Application;
using JustData.Application.ImportExport;

namespace JustData.ViewModels.ImportExport;

public sealed class ImportExportViewModelFactory
{
    private readonly IImportUseCase _importUseCase;
    private readonly IResultExportUseCase _resultExportUseCase;
    private readonly IUiDispatcher? _uiDispatcher;

    public ImportExportViewModelFactory(
        IImportUseCase importUseCase,
        IResultExportUseCase resultExportUseCase,
        IUiDispatcher? uiDispatcher = null)
    {
        _importUseCase = importUseCase;
        _resultExportUseCase = resultExportUseCase;
        _uiDispatcher = uiDispatcher;
    }

    public ImportExportViewModel Create() =>
        new(_importUseCase, _resultExportUseCase, _uiDispatcher);
}
