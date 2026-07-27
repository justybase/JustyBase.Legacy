using AppBase.Data.Core.Models;

namespace AppBase.Tests.Models;

public sealed class NetezzaAutocompleteStateTests
{
    [Fact]
    public void Reads_are_snapshots_and_updates_are_instance_local()
    {
        var first = new NetezzaAutocompleteState();
        var second = new NetezzaAutocompleteState();
        first.ReplaceSnippets(["SELECT"], ["sel"], ["@@one"]);

        IReadOnlyList<string> snapshot = first.Keywords;
        first.ReplaceSnippets(["UPDATE"], [], []);

        Assert.Equal(["SELECT"], snapshot);
        Assert.Equal(["UPDATE"], first.Keywords);
        Assert.Empty(second.Keywords);
    }
}
