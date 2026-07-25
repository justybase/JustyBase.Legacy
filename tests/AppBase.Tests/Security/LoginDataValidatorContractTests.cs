using AppBase.Common;
using AppBase.Services;

namespace AppBase.Tests.Security;

public sealed class LoginDataValidatorContractTests
{
    private readonly ILoginDataValidator _sut = new LoginDataValidator();

    [Fact]
    public void Implements_ILoginDataValidator()
    {
        Assert.IsAssignableFrom<ILoginDataValidator>(_sut);
    }

    [Fact]
    public void Default_is_singleton()
    {
        Assert.Same(LoginDataValidator.Default, LoginDataValidator.Default);
    }

    [Fact]
    public void Default_implements_interface()
    {
        Assert.IsAssignableFrom<ILoginDataValidator>(LoginDataValidator.Default);
    }

    [Fact]
    public void Static_methods_delegate_to_default()
    {
        var profiles = new List<LoginData> { new() { Name = "Test" } };
        var staticResult = LoginDataValidator.Normalize(profiles);
        var instanceResult = LoginDataValidator.Default.DoNormalize(profiles);
        Assert.Equal(staticResult.Count, instanceResult.Count);
        Assert.Equal(staticResult[0].Name, instanceResult[0].Name);
    }

    [Fact]
    public void Interface_methods_delegate_correctly()
    {
        var profiles = new List<LoginData> { new() { Name = "Test" } };
        var interfaceResult = _sut.Normalize(profiles);
        var instanceResult = LoginDataValidator.Default.DoNormalize(profiles);
        Assert.Equal(interfaceResult.Count, instanceResult.Count);
        Assert.Equal(interfaceResult[0].Name, instanceResult[0].Name);
    }

    // ── Normalize edge cases ──

    [Fact]
    public void Normalize_null_list_returns_empty()
    {
        var result = _sut.Normalize(null!);
        Assert.Empty(result);
    }

    [Fact]
    public void Normalize_empty_list_returns_empty()
    {
        var result = _sut.Normalize([]);
        Assert.Empty(result);
    }

    [Fact]
    public void Normalize_fills_null_fields_with_empty_string()
    {
        var profiles = new List<LoginData>
        {
            new()
            {
                Name = "Test",
                Driver = null!,
                Server = null!,
                UserName = null!,
                Password = null!,
                Database = null!
            }
        };
        var result = _sut.Normalize(profiles);
        Assert.Single(result);
        Assert.Equal("Test", result[0].Name);
        Assert.Equal(string.Empty, result[0].Driver);
        Assert.Equal(string.Empty, result[0].Server);
        Assert.Equal(string.Empty, result[0].UserName);
        Assert.Equal(string.Empty, result[0].Password);
        Assert.Equal(string.Empty, result[0].Database);
    }

    [Fact]
    public void Normalize_names_empty_get_default_names()
    {
        var profiles = new List<LoginData>
        {
            new() { Name = "" },
            new() { Name = null! },
            new() { Name = "   " }
        };
        var result = _sut.Normalize(profiles);
        Assert.Equal(3, result.Count);
        Assert.Equal("Connection 1", result[0].Name);
        Assert.Equal("Connection 2", result[1].Name);
        Assert.Equal("Connection 3", result[2].Name);
    }

    [Fact]
    public void Normalize_clamps_default_index()
    {
        var profiles = new List<LoginData>
        {
            new() { Name = "First", DefaultIndex = 999 },
            new() { Name = "Second" }
        };
        var result = _sut.Normalize(profiles);
        // DefaultIndex on profiles[0] should be clamped to profiles.Count - 1 = 1
        Assert.Equal(1, result[0].DefaultIndex);
    }

    [Fact]
    public void Normalize_filters_out_null_profiles()
    {
        var profiles = new List<LoginData>
        {
            new() { Name = "Valid" },
            null!,
            new() { Name = "Also Valid" }
        };
        var result = _sut.Normalize(profiles);
        Assert.Equal(2, result.Count);
    }

    // ── ClampDefaultIndex edge cases ──

    [Fact]
    public void ClampDefaultIndex_returns_zero_for_null_list()
    {
        var result = _sut.ClampDefaultIndex(null!, 5);
        Assert.Equal(0, result);
    }

    [Fact]
    public void ClampDefaultIndex_returns_zero_for_empty_list()
    {
        var result = _sut.ClampDefaultIndex([], 5);
        Assert.Equal(0, result);
    }

    [Fact]
    public void ClampDefaultIndex_valid_index_unchanged()
    {
        var profiles = new List<LoginData> { new(), new(), new() };
        var result = _sut.ClampDefaultIndex(profiles, 1);
        Assert.Equal(1, result);
    }

    [Fact]
    public void ClampDefaultIndex_clamps_negative_to_zero()
    {
        var profiles = new List<LoginData> { new(), new() };
        var result = _sut.ClampDefaultIndex(profiles, -5);
        Assert.Equal(0, result);
    }

    [Fact]
    public void ClampDefaultIndex_clamps_above_max()
    {
        var profiles = new List<LoginData> { new(), new() };
        var result = _sut.ClampDefaultIndex(profiles, 10);
        Assert.Equal(1, result); // profiles.Count - 1 = 1
    }

    [Fact]
    public void ClampDefaultIndex_zero_index_for_single_item()
    {
        var profiles = new List<LoginData> { new() };
        var result = _sut.ClampDefaultIndex(profiles, 0);
        Assert.Equal(0, result);
    }

    [Fact]
    public void Normalize_through_interface()
    {
        var profiles = new List<LoginData> { new() { Name = "Test" } };
        var result = _sut.Normalize(profiles);
        Assert.Single(result);
        Assert.Equal("Test", result[0].Name);
    }

    [Fact]
    public void ClampDefaultIndex_through_interface()
    {
        var profiles = new List<LoginData> { new(), new(), new() };
        var result = _sut.ClampDefaultIndex(profiles, 5);
        Assert.Equal(2, result);
    }
}
