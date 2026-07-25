using System.Reflection;
using JustyBaseLegacy.UI;

namespace JustData.Login.Tests;

public sealed class LoginFormLifetimeTests
{
    [Fact]
    public void LoginForm_DoesNotKeepAGlobalDialogInstance()
    {
        FieldInfo[] staticFields = typeof(LoginForm).GetFields(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

        Assert.DoesNotContain(staticFields, field => field.FieldType == typeof(LoginForm));
        Assert.DoesNotContain(staticFields, field => field.Name.Contains("Global", StringComparison.OrdinalIgnoreCase));
    }
}
