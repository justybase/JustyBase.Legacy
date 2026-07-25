using AppBase.Common;
using AppBase.Services;

namespace AppBase.Tests.Security;

public sealed class LoginDataValidatorTests
{
    [Fact]
    public void Normalize_ClampsInvalidDefaultIndexAndFillsMissingValues()
    {
        var profiles = LoginDataValidator.Normalize(
        [
            new LoginData { DefaultIndex = 99 },
            new LoginData { Name = "second" }
        ]);

        Assert.Equal(2, profiles.Count);
        Assert.Equal(1, profiles[0].DefaultIndex);
        Assert.Equal("Connection 1", profiles[0].Name);
        Assert.NotNull(profiles[0].Driver);
    }

    [Fact]
    public void Normalize_DropsNullProfilesAndHandlesEmptyInput()
    {
        var profiles = LoginDataValidator.Normalize(new LoginData[] { null! });

        Assert.Empty(profiles);
        Assert.Equal(0, LoginDataValidator.ClampDefaultIndex(profiles, -1));
    }
}
