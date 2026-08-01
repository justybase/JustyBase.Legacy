using AppBase.Common.Configuration;
using AppBase.Common.Interfaces;
using JustData.Application.Git;
using JustyBase.Ai.Fim.Abstractions;
using JustyBase.Ai.Fim.Benchmark;
using JustyBase.Ai.Fim.Download;
using JustyBase.Ai.Fim.LlamaSharp;
using JustyBase.Ai.Fim.Prompting;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

namespace JustyBaseLegacy.UI.Fim;

public static class FimServiceCollectionExtensions
{
    public static IServiceCollection AddEmbeddedFimCompletion(this IServiceCollection collection)
    {
        collection.AddSingleton<IFimModelCatalog, FimModelCatalog>();
        collection.AddSingleton<IFimModelStore>(sp =>
        {
            var catalog = sp.GetRequiredService<IFimModelCatalog>();
            var config = sp.GetRequiredService<IApplicationSettingsContext>().Config;
            return new HuggingFaceFimModelStore(catalog, () => config.EmbeddedFimModelId);
        });
        collection.AddSingleton<IFimPromptBuilder>(sp =>
        {
            var catalog = sp.GetRequiredService<IFimModelCatalog>();
            var config = sp.GetRequiredService<IApplicationSettingsContext>().Config;
            return new CatalogFimPromptBuilder(catalog, () => config.EmbeddedFimModelId);
        });
        collection.AddSingleton(sp =>
        {
            var store = sp.GetRequiredService<IFimModelStore>();
            var config = sp.GetRequiredService<IApplicationSettingsContext>().Config;
            LlamaSharpModelHost.ConfigureNativeBackend(config.EmbeddedFimPreferVulkan);
            return new LlamaSharpModelHost(
                store,
                getGpuLayerCount: () => ResolveGpuLayers(config));
        });
        collection.AddSingleton<LlamaSharpCompletionProvider>(sp =>
        {
            var host = sp.GetRequiredService<LlamaSharpModelHost>();
            var builder = sp.GetRequiredService<IFimPromptBuilder>();
            return new LlamaSharpCompletionProvider(host, builder);
        });
        collection.AddSingleton<ICompletionProvider>(sp => sp.GetRequiredService<LlamaSharpCompletionProvider>());
        collection.AddSingleton(sp =>
        {
            var provider = sp.GetRequiredService<ICompletionProvider>();
            var config = sp.GetRequiredService<IApplicationSettingsContext>().Config;
            return new FimInlineCompletionBridge(
                provider,
                () => config.EnableEmbeddedFimAi,
                () => new FimPromptBudget(
                    config.EmbeddedFimMaxPromptTokens,
                    config.EmbeddedFimPrefixPercentage,
                    config.EmbeddedFimSuffixPercentage,
                    config.EmbeddedFimMaxTokens));
        });
        collection.AddSingleton<IFimModelBootstrapService, FimModelBootstrapService>();
        collection.AddSingleton<IGitCommitMessageAiService, EmbeddedFimGitCommitMessageAiService>();
        collection.AddSingleton<FimEditorHost>();
        return collection;
    }

    private static int ResolveGpuLayers(IApplicationConfig config)
    {
        if (!config.EmbeddedFimPreferVulkan)
            return 0;

        var layers = config.EmbeddedFimGpuLayers;
        if (layers < 0)
            return 99;

        return Math.Clamp(layers, 0, 999);
    }
}

public interface IFimModelBootstrapService
{
    string ModelsDirectory { get; }
    bool IsSelectedModelPresent { get; }
    string SelectedModelLocalPath { get; }
    string SelectedModelDiskStatus { get; }
    string EnsureModelsDirectory();
    Task EnsureReadyAsync(IProgress<FimModelProgress>? progress = null, CancellationToken cancellationToken = default);
    Task DeleteSelectedModelAsync(CancellationToken cancellationToken = default);
    Task ReloadModelAsync(CancellationToken cancellationToken = default);
    Task<FimBenchmarkComparisonReport> RunSpeedBenchmarkAsync(
        int maxTokens,
        int maxPromptTokens,
        double prefixPercentage,
        double suffixPercentage,
        int debounceMs,
        int configuredGpuLayers,
        IProgress<FimModelProgress>? progress = null,
        CancellationToken cancellationToken = default);
}

public sealed class FimModelBootstrapService : IFimModelBootstrapService
{
    private readonly LlamaSharpCompletionProvider _provider;
    private readonly IFimModelStore _store;
    private readonly LlamaSharpModelHost _host;
    private int _busy;

    public FimModelBootstrapService(
        LlamaSharpCompletionProvider provider,
        IFimModelStore store,
        LlamaSharpModelHost host)
    {
        _provider = provider;
        _store = store;
        _host = host;
    }

    public string ModelsDirectory => _store.ModelsDirectory;
    public bool IsSelectedModelPresent => _store.IsModelPresent;
    public string SelectedModelLocalPath => _store.LocalModelPath;
    public string EnsureModelsDirectory() => _store.EnsureModelsDirectory();

    public string SelectedModelDiskStatus
    {
        get
        {
            var model = _store.CurrentModel;
            if (!_store.IsModelPresent)
                return $"{model.DisplayName}: not downloaded.";

            try
            {
                var mb = new FileInfo(_store.LocalModelPath).Length / (1024d * 1024d);
                var layers = _host.IsLoaded ? _host.LoadedGpuLayerCount : _host.EffectiveGpuLayerCount;
                return $"{model.DisplayName}: on disk ({mb:0.#} MB), gpu_layers={layers}.";
            }
#pragma warning disable CA1031
            catch
#pragma warning restore CA1031
            {
                return $"{model.DisplayName}: on disk.";
            }
        }
    }

    public async Task EnsureReadyAsync(IProgress<FimModelProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0)
            throw new InvalidOperationException("A FIM model download/load is already in progress.");

        try
        {
            await EnsureReadyCoreAsync(progress, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            Interlocked.Exchange(ref _busy, 0);
        }
    }

    public async Task DeleteSelectedModelAsync(CancellationToken cancellationToken = default)
    {
        if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0)
            throw new InvalidOperationException("A FIM model download/load is already in progress.");

        try
        {
            await _host.UnloadAsync(cancellationToken).ConfigureAwait(false);
            if (!_store.TryDeleteCurrentModel())
                throw new InvalidOperationException("No local model file to delete for the selected model.");
        }
        finally
        {
            Interlocked.Exchange(ref _busy, 0);
        }
    }

    public async Task ReloadModelAsync(CancellationToken cancellationToken = default)
    {
        if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0)
            throw new InvalidOperationException("A FIM model download/load is already in progress.");

        try
        {
            await _host.UnloadAsync(cancellationToken).ConfigureAwait(false);
            if (_store.IsModelPresent)
                await EnsureReadyCoreAsync(progress: null, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            Interlocked.Exchange(ref _busy, 0);
        }
    }

    public async Task<FimBenchmarkComparisonReport> RunSpeedBenchmarkAsync(
        int maxTokens,
        int maxPromptTokens,
        double prefixPercentage,
        double suffixPercentage,
        int debounceMs,
        int configuredGpuLayers,
        IProgress<FimModelProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (!_store.IsModelPresent)
            throw new InvalidOperationException("Download / prepare the selected model before running the speed test.");

        if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0)
            throw new InvalidOperationException("A FIM model download/load is already in progress.");

        try
        {
            return await FimSpeedBenchmark.RunComparisonAsync(
                _provider,
                _host,
                _store.CurrentModel.DisplayName,
                maxPromptTokens,
                prefixPercentage,
                suffixPercentage,
                maxTokens,
                debounceMs,
                configuredGpuLayers,
                progress,
                cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            Interlocked.Exchange(ref _busy, 0);
        }
    }

    private async Task EnsureReadyCoreAsync(IProgress<FimModelProgress>? progress, CancellationToken cancellationToken)
    {
        _store.EnsureModelsDirectory();
        var model = _store.CurrentModel;
        IProgress<FimModelProgress> combined = new Progress<FimModelProgress>(p =>
        {
            Debug.WriteLine($"[FIM:{model.Id}] {p.Message}");
            progress?.Report(p);
        });

        await _provider.EnsureReadyAsync(combined, cancellationToken).ConfigureAwait(false);
    }
}
