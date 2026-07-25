using AppBase.Common;
using AppBase.Services;

namespace AppBase.Tests.Security;

public sealed class LoginDataValidatorExtendedTests
{
    [Fact]
    public void Normalize_filters_out_null_profiles()
    {
        var profiles = new List<LoginData?>
        {
            new LoginData { Name = "valid" },
            null,
            new LoginData { Name = "also valid" },
        };

        var result = LoginDataValidator.Normalize(profiles!);

        Assert.Equal(2, result.Count);
        Assert.All(result, p => Assert.NotNull(p));
    }

    [Fact]
    public void Normalize_names_empty_get_default_names()
    {
        var profiles = new List<LoginData>
        {
            new LoginData(),
            new LoginData { Name = "   " },
            new LoginData { Name = "" },
        };

        var result = LoginDataValidator.Normalize(profiles);

        Assert.Equal("Connection 1", result[0].Name);
        Assert.Equal("Connection 2", result[1].Name);
        Assert.Equal("Connection 3", result[2].Name);
    }

    [Fact]
    public void Normalize_fills_null_fields_with_empty_string()
    {
        var profiles = new List<LoginData>
        {
            new LoginData { Name = "test" },
        };

        var result = LoginDataValidator.Normalize(profiles);

        Assert.Equal(string.Empty, result[0].Driver);
        Assert.Equal(string.Empty, result[0].Server);
        Assert.Equal(string.Empty, result[0].UserName);
        Assert.Equal(string.Empty, result[0].Password);
        Assert.Equal(string.Empty, result[0].Database);
    }

    [Fact]
    public void Normalize_clamps_default_index()
    {
        var profiles = new List<LoginData>
        {
            new LoginData { Name = "one", DefaultIndex = 100 },
        };

        var result = LoginDataValidator.Normalize(profiles);

        Assert.Equal(0, result[0].DefaultIndex);
    }

    [Fact]
    public void Normalize_empty_list_returns_empty()
    {
        var result = LoginDataValidator.Normalize([]);

        Assert.Empty(result);
    }

    [Fact]
    public void Normalize_null_list_returns_empty()
    {
        var result = LoginDataValidator.Normalize(null!);

        Assert.Empty(result);
    }

    [Fact]
    public void ClampDefaultIndex_returns_zero_for_empty_list()
    {
        Assert.Equal(0, LoginDataValidator.ClampDefaultIndex([], 5));
    }

    [Fact]
    public void ClampDefaultIndex_returns_zero_for_null_list()
    {
        Assert.Equal(0, LoginDataValidator.ClampDefaultIndex(null!, 5));
    }

    [Fact]
    public void ClampDefaultIndex_clamps_negative_to_zero()
    {
        var profiles = new List<LoginData> { new LoginData { Name = "a" } };
        Assert.Equal(0, LoginDataValidator.ClampDefaultIndex(profiles, -5));
    }

    [Fact]
    public void ClampDefaultIndex_clamps_above_max()
    {
        var profiles = new List<LoginData>
        {
            new LoginData { Name = "a" },
            new LoginData { Name = "b" },
        };
        Assert.Equal(1, LoginDataValidator.ClampDefaultIndex(profiles, 100));
    }

    [Fact]
    public void ClampDefaultIndex_valid_index_unchanged()
    {
        var profiles = new List<LoginData>
        {
            new LoginData { Name = "a" },
            new LoginData { Name = "b" },
            new LoginData { Name = "c" },
        };
        Assert.Equal(1, LoginDataValidator.ClampDefaultIndex(profiles, 1));
    }
}
