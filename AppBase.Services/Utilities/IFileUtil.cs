using System.Diagnostics;

namespace AppBase.Services;

public interface IFileUtil
{
    List<Process> WhoIsLocking(string path);
}
