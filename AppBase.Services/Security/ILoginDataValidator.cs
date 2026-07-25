using AppBase.Common;

namespace AppBase.Services;

public interface ILoginDataValidator
{
    List<LoginData> Normalize(IEnumerable<LoginData> profiles);
    int ClampDefaultIndex(IReadOnlyList<LoginData> profiles, int defaultIndex);
}
