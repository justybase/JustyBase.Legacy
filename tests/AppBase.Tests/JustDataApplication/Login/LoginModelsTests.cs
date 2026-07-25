using JustData.Application.Login;

namespace AppBase.Tests.JustDataApplication.Login;

public sealed class ConnectionProfileTests
{
    [Fact]
    public void ConnectionProfile_defaults_to_empty()
    {
        var profile = new ConnectionProfile();
        Assert.Equal("", profile.Name);
        Assert.Equal("", profile.Driver);
        Assert.Equal("", profile.Server);
        Assert.Equal("", profile.UserName);
        Assert.Equal("", profile.Password);
        Assert.Equal("", profile.Database);
    }

    [Fact]
    public void ConnectionProfile_Clone_creates_independent_copy()
    {
        var original = new ConnectionProfile
        {
            Name = "dev", Driver = "Netezza", Server = "localhost",
            UserName = "admin", Password = "secret", Database = "mydb"
        };

        var clone = original.Clone();

        Assert.Equal(original.Name, clone.Name);
        Assert.Equal(original.Driver, clone.Driver);
        Assert.Equal(original.Server, clone.Server);
        Assert.Equal(original.UserName, clone.UserName);
        Assert.Equal(original.Password, clone.Password);
        Assert.Equal(original.Database, clone.Database);
        Assert.NotSame(original, clone);
    }

    [Fact]
    public void ConnectionProfile_Clone_modifying_copy_does_not_affect_original()
    {
        var original = new ConnectionProfile { Name = "original" };
        var clone = original.Clone();
        clone.Name = "modified";

        Assert.Equal("original", original.Name);
        Assert.Equal("modified", clone.Name);
    }

    [Fact]
    public void ConnectionProfile_ToString_redacts_password()
    {
        var profile = new ConnectionProfile { Name = "dev", Password = "supersecret" };
        var str = profile.ToString();
        Assert.Contains("[REDACTED]", str);
        Assert.DoesNotContain("supersecret", str);
    }

    [Fact]
    public void ConnectionProfile_ToString_includes_key_fields()
    {
        var profile = new ConnectionProfile { Name = "dev", Driver = "Netezza", Server = "host" };
        var str = profile.ToString();
        Assert.Contains("Name = dev", str);
        Assert.Contains("Driver = Netezza", str);
        Assert.Contains("Server = host", str);
    }
}

public sealed class LoginSelectionTests
{
    [Fact]
    public void LoginSelection_constructor_throws_on_null_profile()
    {
        Assert.Throws<ArgumentNullException>(() => new LoginSelection(null!, false));
    }

    [Fact]
    public void LoginSelection_stores_profile()
    {
        var profile = new ConnectionProfile { Name = "dev" };
        var selection = new LoginSelection(profile, true);

        Assert.Same(profile, selection.Profile);
        Assert.True(selection.FastLogin);
    }

    [Fact]
    public void LoginSelection_fast_login_false()
    {
        var profile = new ConnectionProfile();
        var selection = new LoginSelection(profile, false);
        Assert.False(selection.FastLogin);
    }

    [Fact]
    public void LoginSelection_ToString_redacts_password()
    {
        var profile = new ConnectionProfile { Name = "dev", Password = "secret" };
        var selection = new LoginSelection(profile, true);
        var str = selection.ToString();
        Assert.Contains("[REDACTED]", str);
        Assert.DoesNotContain("secret", str);
    }
}

public sealed class ApplicationSessionTests
{
    [Fact]
    public void ApplicationSession_default_state()
    {
        var session = new ApplicationSession();
        Assert.Null(session.CurrentLogin);
        Assert.Empty(session.Profiles);
    }

    [Fact]
    public void SetLogin_throws_on_null_selection()
    {
        var session = new ApplicationSession();
        Assert.Throws<ArgumentNullException>(() =>
            session.SetLogin(null!, []));
    }

    [Fact]
    public void SetLogin_stores_selection()
    {
        var session = new ApplicationSession();
        var profile = new ConnectionProfile { Name = "dev" };
        var selection = new LoginSelection(profile, true);

        session.SetLogin(selection, [profile]);

        Assert.Same(selection, session.CurrentLogin);
    }

    [Fact]
    public void SetLogin_clones_profiles()
    {
        var session = new ApplicationSession();
        var profile = new ConnectionProfile { Name = "dev" };
        var selection = new LoginSelection(profile, false);

        session.SetLogin(selection, [profile]);

        var storedProfile = session.Profiles[0];
        Assert.NotSame(profile, storedProfile);
        Assert.Equal("dev", storedProfile.Name);
    }

    [Fact]
    public void SetLogin_modifying_original_does_not_affect_stored()
    {
        var session = new ApplicationSession();
        var profile = new ConnectionProfile { Name = "dev" };
        var selection = new LoginSelection(profile, false);

        session.SetLogin(selection, [profile]);
        profile.Name = "modified";

        Assert.Equal("dev", session.Profiles[0].Name);
    }

    [Fact]
    public void SetLogin_multiple_profiles()
    {
        var session = new ApplicationSession();
        var profiles = new[]
        {
            new ConnectionProfile { Name = "dev" },
            new ConnectionProfile { Name = "prod" }
        };
        var selection = new LoginSelection(profiles[0], false);

        session.SetLogin(selection, profiles);

        Assert.Equal(2, session.Profiles.Count);
        Assert.Equal("dev", session.Profiles[0].Name);
        Assert.Equal("prod", session.Profiles[1].Name);
    }
}
