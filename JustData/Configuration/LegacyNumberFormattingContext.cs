using AppBase.Common.Interfaces;
using System.Globalization;

namespace JustyBaseLegacy.UI.Configuration;

/// <summary>Shared numeric formatting policy for grid rendering.</summary>
public sealed class LegacyNumberFormattingContext : INumberFormattingContext
{
    public NumberFormatInfo NumberWithDot { get; } = new();
}
