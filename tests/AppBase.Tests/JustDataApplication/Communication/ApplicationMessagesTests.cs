using JustData.Application.Communication;

namespace AppBase.Tests.JustDataApplication.Communication;

public sealed class ApplicationMessagesTests
{
    [Fact]
    public void SettingsSavedMessage_can_be_created()
    {
        var msg = new SettingsSavedMessage();
        Assert.NotNull(msg);
    }

    [Fact]
    public void ActiveConnectionChangedMessage_stores_connection_name()
    {
        var msg = new ActiveConnectionChangedMessage("my_conn");
        Assert.Equal("my_conn", msg.ConnectionName);
    }

    [Fact]
    public void ActiveConnectionChangedMessage_supports_null_name()
    {
        var msg = new ActiveConnectionChangedMessage(null!);
        Assert.Null(msg.ConnectionName);
    }

    [Fact]
    public void SchemaRefreshedMessage_stores_connection_name()
    {
        var msg = new SchemaRefreshedMessage("my_conn");
        Assert.Equal("my_conn", msg.ConnectionName);
    }

    [Fact]
    public void Messages_with_same_values_are_equal()
    {
        var msg1 = new ActiveConnectionChangedMessage("conn");
        var msg2 = new ActiveConnectionChangedMessage("conn");
        Assert.Equal(msg1, msg2);
    }

    [Fact]
    public void Messages_with_different_values_are_not_equal()
    {
        var msg1 = new ActiveConnectionChangedMessage("conn1");
        var msg2 = new ActiveConnectionChangedMessage("conn2");
        Assert.NotEqual(msg1, msg2);
    }
}
