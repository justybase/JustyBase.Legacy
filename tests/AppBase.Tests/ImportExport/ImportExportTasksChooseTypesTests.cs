using AppBase.Common.Configuration;
using AppBase.Common.Interfaces;
using AppBase.Services;
using JustyBase.ImportExport.Import;
using NSubstitute;

namespace AppBase.Tests.ImportExport;

public sealed class ImportExportTasksChooseTypesTests
{
    private readonly ImportExportTasks _sut;

    public ImportExportTasksChooseTypesTests()
    {
        var config = Substitute.For<IApplicationConfig>();
        config.DefaultNvarcharLength.Returns(255);
        var settings = Substitute.For<IApplicationSettingsContext>();
        settings.Config.Returns(config);
        _sut = new ImportExportTasks(settings);
    }

    private static void AssertType(ImportExportTasks sut, string header, string expected, Action<ImportTypeAnalyzer>? feed = null)
    {
        var headers = new[] { header };
        var analyzer = new ImportTypeAnalyzer(1);
        feed?.Invoke(analyzer);
        sut.ChooseTypes(analyzer, headers);
        Assert.Equal(expected, headers[0]);
    }

    [Theory]
    [InlineData("COL_#TEXT", "COL_#TEXT NVARCHAR(255)")]
    [InlineData("COL_#NUMERIC", "COL_#NUMERIC NUMERIC(20,6)")]
    [InlineData("COL_#INTEGER", "COL_#INTEGER BIGINT")]
    [InlineData("COL_#DATE", "COL_#DATE DATE")]
    [InlineData("COL_#TIMESTAMP", "COL_#TIMESTAMP TIMESTAMP")]
    public void ChooseTypes_honors_explicit_header_suffixes(string header, string expected)
    {
        AssertType(_sut, header, expected);
    }

    [Fact]
    public void ChooseTypes_defaults_empty_column_to_nvarchar()
    {
        AssertType(_sut, "NAME", "NAME NVARCHAR(255)");
    }

    [Fact]
    public void ChooseTypes_prefers_text_when_column_contains_labels()
    {
        AssertType(_sut, "MIXED", "MIXED NVARCHAR(20)", a =>
        {
            a.AddValue(0, "abc");
            a.AddValue(0, "12");
            a.AddValue(0, "1.5");
        });
    }

    [Fact]
    public void ChooseTypes_treats_date_timestamp_mix_as_text()
    {
        AssertType(_sut, "MIXED", "MIXED NVARCHAR(24)", a =>
        {
            a.AddValue(0, "2024-01-15");
            a.AddValue(0, "2024-01-16 10:30:00");
        });
    }

    [Fact]
    public void ChooseTypes_emits_numeric_for_decimal_column()
    {
        AssertType(_sut, "AMOUNT", "AMOUNT NUMERIC(16,4)", a =>
        {
            a.AddValue(0, "10.1234");
            a.AddValue(0, "20.5678");
        });
    }

    [Fact]
    public void ChooseTypes_promotes_integer_and_decimal_mix_to_numeric()
    {
        AssertType(_sut, "AMOUNT", "AMOUNT NUMERIC(16,2)", a =>
        {
            a.AddValue(0, "7");
            a.AddValue(0, "10.5");
            a.AddValue(0, "100.25");
        });
    }

    [Fact]
    public void ChooseTypes_treats_numeric_timestamp_mix_as_text()
    {
        AssertType(_sut, "CONFUSING", "CONFUSING NVARCHAR(24)", a =>
        {
            a.AddValue(0, "10.5");
            a.AddValue(0, "2024-01-15 10:30:00");
        });
    }

    [Fact]
    public void ChooseTypes_treats_timestamp_integer_mix_as_text()
    {
        AssertType(_sut, "CONFUSING", "CONFUSING NVARCHAR(20)", a =>
        {
            a.AddValue(0, "2024-01-15 10:30:00");
            a.AddValue(0, "12345");
        });
    }

    [Theory]
    [InlineData(ImportColumnKind.Integer, "ID BIGINT")]
    [InlineData(ImportColumnKind.Date, "ID DATE")]
    [InlineData(ImportColumnKind.TimeStamp, "ID TIMESTAMP")]
    [InlineData(ImportColumnKind.Boolean, "ID NVARCHAR(20)")]
    public void ChooseTypes_homogeneous_column_maps_kind(ImportColumnKind kind, string expected)
    {
        AssertType(_sut, "ID", expected, a => a.AddCell(0, kind));
    }
}