namespace AppBase.Common.Models;

public class ExcelExportException : Exception
{
    public ExcelExportException()
    {
    }

    public ExcelExportException(string? message) : base(message)
    {
    }

    public ExcelExportException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
