using AppBase.Data.Completion;
using FastColoredTextBoxNS;
using JustyBase.NetezzaSqlParser.Completion;

namespace AppBase.Tests.Sql;

public sealed class CompletionItemAppearanceTests
{
    [Theory]
    [InlineData(CompletionKind.Table, CompletionIconKind.Table)]
    [InlineData(CompletionKind.View, CompletionIconKind.View)]
    [InlineData(CompletionKind.Column, CompletionIconKind.Column)]
    [InlineData(CompletionKind.Database, CompletionIconKind.Database)]
    [InlineData(CompletionKind.Schema, CompletionIconKind.Schema)]
    [InlineData(CompletionKind.Function, CompletionIconKind.Function)]
    [InlineData(CompletionKind.Cte, CompletionIconKind.Cte)]
    [InlineData(CompletionKind.Alias, CompletionIconKind.Alias)]
    [InlineData(CompletionKind.Keyword, CompletionIconKind.Keyword)]
    [InlineData(CompletionKind.Snippet, CompletionIconKind.Snippet)]
    [InlineData(CompletionKind.DataType, CompletionIconKind.DataType)]
    [InlineData(CompletionKind.Variable, CompletionIconKind.Variable)]
    public void ToIconKind_maps_known_kinds(CompletionKind kind, CompletionIconKind expected)
    {
        Assert.Equal(expected, CompletionItemAppearance.ToIconKind(kind));
    }

    [Fact]
    public void ToIconKind_falls_back_to_reference_for_unknown_value()
    {
        Assert.Equal(CompletionIconKind.Reference, CompletionItemAppearance.ToIconKind((CompletionKind)999));
    }

    [Fact]
    public void Apply_sets_image_index_tag_and_texts()
    {
        var item = new AutocompleteItem("EMPLOYEES");

        var result = CompletionItemAppearance.Apply(
            item,
            CompletionIconKind.Table,
            detail: "Table",
            description: "Employee roster");

        Assert.Same(item, result);
        Assert.Equal((int)CompletionIconKind.Table, item.ImageIndex);
        Assert.Equal(CompletionIconKind.Table, item.Tag);
        Assert.Equal("Table", item.DetailText);
        Assert.Equal("Employee roster", item.DescriptionText);
    }

    [Fact]
    public void ApplyKind_uses_kind_name_as_default_detail()
    {
        var item = new AutocompleteItem("id");

        CompletionItemAppearance.ApplyKind(item, CompletionKind.Column);

        Assert.Equal((int)CompletionIconKind.Column, item.ImageIndex);
        Assert.Equal("Column", item.DetailText);
    }

    [Fact]
    public void ApplyKind_preserves_explicit_detail()
    {
        var item = new AutocompleteItem("id");

        CompletionItemAppearance.ApplyKind(item, CompletionKind.Column, detail: "INTEGER", description: "pk");

        Assert.Equal("INTEGER", item.DetailText);
        Assert.Equal("pk", item.DescriptionText);
    }
}
