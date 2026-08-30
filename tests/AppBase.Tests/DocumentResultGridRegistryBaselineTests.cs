using DatabaseDataGridView.WinForms;
using JustData.Application.Editor;
using JustData.Application.Sql;
using JustyBaseLegacy.UI.ImportExport;

namespace AppBase.Tests;

public sealed class DocumentResultGridRegistryBaselineTests
{
    private static CustomDataGridView CreateUninitializedGrid()
    {
#pragma warning disable SYSLIB0050
        return (CustomDataGridView)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(CustomDataGridView));
#pragma warning restore SYSLIB0050
    }

    [Fact]
    public void Register_TryGet_GetDocument_RemoveDocument_flow()
    {
        var registry = new DocumentResultGridRegistry();
        var doc1 = EditorDocumentId.New();
        var doc2 = EditorDocumentId.New();
        var key1 = new ResultSetKey(doc1, "rs1");
        var key2 = new ResultSetKey(doc1, "rs2");
        var key3 = new ResultSetKey(doc2, "rs1");

        var g1 = CreateUninitializedGrid();
        var g2 = CreateUninitializedGrid();
        var g3 = CreateUninitializedGrid();

        registry.Register(key1, g1);
        registry.Register(key2, g2);
        registry.Register(key3, g3);

        Assert.True(registry.TryGet(key1, out var got1));
        Assert.Same(g1, got1);
        Assert.Equal(2, registry.GetDocument(doc1).Count);
        Assert.Single(registry.GetDocument(doc2));

        registry.RemoveResult(key1);
        Assert.False(registry.TryGet(key1, out _));
        Assert.Single(registry.GetDocument(doc1));

        registry.RemoveDocument(doc1);
        Assert.Empty(registry.GetDocument(doc1));
        Assert.Single(registry.GetDocument(doc2));

        Assert.True(registry.TryFind(doc2, g3, out var foundKey));
        Assert.Equal(key3, foundKey);
    }

    [Fact]
    public void Register_invalid_key_throws()
    {
        var registry = new DocumentResultGridRegistry();
        var g = CreateUninitializedGrid();
        Assert.Throws<ArgumentException>(() => registry.Register(new ResultSetKey(default, ""), g));
    }
}
