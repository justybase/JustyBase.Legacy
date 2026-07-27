using AppBase.Data.Core.Models;

namespace AppBase.Tests.Models;

public sealed class AutocompleteSuggestionStoreTests
{
    [Fact]
    public void Default_initializes_all_lists_as_empty()
    {
        var store = new AutocompleteSuggestionStore();

        Assert.Empty(store.OneWord);
        Assert.Empty(store.OneWordAdditions);
        Assert.Empty(store.TwoWords);
        Assert.Empty(store.TwoWordsAdditions);
        Assert.Empty(store.TreeWords);
        Assert.Empty(store.ActualColumnList);
    }

    [Fact]
    public void OneWord_can_add_items()
    {
        var store = new AutocompleteSuggestionStore();
        store.OneWord.Add("SELECT");
        store.OneWord.Add("FROM");

        Assert.Equal(2, store.OneWord.Count);
        Assert.Equal("SELECT", store.OneWord[0]);
    }

    [Fact]
    public void OneWord_can_be_replaced()
    {
        var store = new AutocompleteSuggestionStore();
        store.OneWord = ["a", "b", "c"];

        Assert.Equal(3, store.OneWord.Count);
        Assert.Equal("a", store.OneWord[0]);
    }

    [Fact]
    public void TwoWords_can_add_items()
    {
        var store = new AutocompleteSuggestionStore();
        store.TwoWords.Add("schema.table");

        Assert.Single(store.TwoWords);
    }

    [Fact]
    public void TreeWords_can_add_items()
    {
        var store = new AutocompleteSuggestionStore();
        store.TreeWords.Add("db.schema.table");

        Assert.Single(store.TreeWords);
    }

    [Fact]
    public void ActualColumnList_can_be_replaced()
    {
        var store = new AutocompleteSuggestionStore();
        var columns = new List<string> { "id", "name" };
        store.ActualColumnList = columns;

        Assert.Same(columns, store.ActualColumnList);
    }

    [Fact]
    public void Multiple_stores_are_independent()
    {
        var store1 = new AutocompleteSuggestionStore();
        var store2 = new AutocompleteSuggestionStore();

        store1.OneWord.Add("from store1");

        Assert.Single(store1.OneWord);
        Assert.Empty(store2.OneWord);
    }

}
