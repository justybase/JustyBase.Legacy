using AppBase.Common;
using AppBase.Common.Enums;
using AppBase.Services;
using System.Globalization;
using System.Reflection;

namespace AppBase.Tests.Database;

public sealed class GeneralDbServicePrepareValueTests
{
    [Theory]
    [InlineData("", "", DatabaseColumnType.noinfo)]
    [InlineData(" null ", "", DatabaseColumnType.noinfo)]
    [InlineData("42", "42", DatabaseColumnType.integer)]
    [InlineData("0", "0", DatabaseColumnType.integer)]
    [InlineData("00123", "'00123'", DatabaseColumnType.nvarchar)]
    [InlineData("123456789", "123456789", DatabaseColumnType.integer)]
    [InlineData("12.5", "12.5", DatabaseColumnType.numeric)]
    [InlineData("12.5%", "0.125", DatabaseColumnType.numeric)]
    [InlineData("text", "'text'", DatabaseColumnType.nvarchar)]
    public void PrepareValue_ClassifiesCommonValues(
        string input,
        string expected,
        DatabaseColumnType expectedType)
    {
        using CultureScope _ = new("en-US");
        GeneralDbService service = CreateService();

        string result = service.PrepareValue(out DatabaseColumnType type, input);

        Assert.Equal(expected, result);
        Assert.Equal(expectedType, type);
    }

    [Fact]
    public void PrepareValue_UsesCurrentCultureForDecimalComma()
    {
        using CultureScope _ = new("pl-PL");
        GeneralDbService service = CreateService();

        string result = service.PrepareValue(out DatabaseColumnType type, "12,5");

        Assert.Equal("12.5", result);
        Assert.Equal(DatabaseColumnType.numeric, type);
    }

    [Fact]
    public void PrepareValue_CanEmitDateWithoutTimestampPrefix()
    {
        using CultureScope _ = new("en-US");
        GeneralDbService service = CreateService();

        string result = service.PrepareValue(
            out DatabaseColumnType type,
            "2026-07-16",
            typeAdn: false,
            forceTimestamp: false);

        Assert.Equal("'2026-07-16'", result);
        Assert.Equal(DatabaseColumnType.date, type);
    }

    [Fact]
    public void PrepareValue_PreservesWhitespaceWhenTrimmingIsDisabled()
    {
        GeneralDbService service = CreateService();

        string result = service.PrepareValue(out DatabaseColumnType type, " text ", doTrim: false);

        Assert.Equal("' text '", result);
        Assert.Equal(DatabaseColumnType.nvarchar, type);
    }

    private static GeneralDbService CreateService() => new(CreateProxy<ILogger>());

    private static T CreateProxy<T>() where T : class =>
        DispatchProxy.Create<T, NullDispatchProxy>();

    private class NullDispatchProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => null;
    }

    private sealed class CultureScope : IDisposable
    {
        private readonly CultureInfo _originalCulture = CultureInfo.CurrentCulture;
        private readonly CultureInfo _originalUiCulture = CultureInfo.CurrentUICulture;

        public CultureScope(string cultureName)
        {
            CultureInfo culture = CultureInfo.GetCultureInfo(cultureName);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
        }

        public void Dispose()
        {
            CultureInfo.CurrentCulture = _originalCulture;
            CultureInfo.CurrentUICulture = _originalUiCulture;
        }
    }
}
