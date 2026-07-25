using JustData.Application.Editor;
using System.Text.Json;

namespace JustyBaseLegacy.UI.Editor;

/// <summary>WinForms-compatible `.manysql` and `.manysql.enc` JSON wire-format adapter.</summary>
public sealed class WinFormsManySqlBundleService : IManySqlBundleService
{
    public async Task<ManySqlBundle> LoadAsync(string path, CancellationToken cancellationToken = default)
    {
        string json = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
        var wire = JsonSerializer.Deserialize(json, AppJsonSerializerContext.Default.ManySqlWireModel)
            ?? throw new InvalidDataException("The Many SQL bundle is empty.");

        return new ManySqlBundle(
            wire.SqlPaths ?? [],
            (wire.SqlContentList ?? [])
                .Where(items => items is { Count: >= 2 })
                .Select(items => new ManySqlContent(items[0], items[1]))
                .ToArray(),
            wire.TabsOrder ?? [],
            wire.SelectedTabNum);
    }

    public async Task SaveAsync(
        string path,
        ManySqlBundle bundle,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bundle);
        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var wire = new ManySqlWireModel
        {
            SqlPaths = bundle.SqlPaths.ToList(),
            SqlContentList = bundle.SqlContentList
                .Select(content => new List<string> { content.Title, content.Text })
                .ToList(),
            TabsOrder = bundle.TabsOrder.ToList(),
            SelectedTabNum = bundle.SelectedTabNum
        };

        string json = JsonSerializer.Serialize(wire, AppJsonSerializerContext.Default.ManySqlWireModel);
        await File.WriteAllTextAsync(path, json, cancellationToken).ConfigureAwait(false);
    }
}
