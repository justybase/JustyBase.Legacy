using System.Globalization;

namespace AppBase.Common.Interfaces;

public interface INumberFormattingContext
{
    NumberFormatInfo NumberWithDot { get; }
}
