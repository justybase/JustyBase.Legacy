using AppBase.Common;

namespace AppBase.Services;

public sealed class LoginDataValidator : ILoginDataValidator
{
    public static readonly LoginDataValidator Default = new();

    public static List<LoginData> Normalize(IEnumerable<LoginData> profiles)
        => Default.DoNormalize(profiles);

    public static int ClampDefaultIndex(IReadOnlyList<LoginData> profiles, int defaultIndex)
        => Default.DoClampDefaultIndex(profiles, defaultIndex);

    // --- Instance methods (DoXxx pattern) ---
    public List<LoginData> DoNormalize(IEnumerable<LoginData> profiles)
    {
        var normalized = profiles?
            .Where(profile => profile is not null)
            .ToList() ?? [];

        for (int i = 0; i < normalized.Count; i++)
        {
            LoginData profile = normalized[i];
            profile.Name = string.IsNullOrWhiteSpace(profile.Name) ? $"Connection {i + 1}" : profile.Name;
            profile.Driver ??= string.Empty;
            profile.Server ??= string.Empty;
            profile.UserName ??= string.Empty;
            profile.Password ??= string.Empty;
            profile.Database ??= string.Empty;
        }

        if (normalized.Count > 0)
        {
            normalized[0].DefaultIndex = DoClampDefaultIndex(normalized, normalized[0].DefaultIndex);
        }

        return normalized;
    }

    public int DoClampDefaultIndex(IReadOnlyList<LoginData> profiles, int defaultIndex)
    {
        if (profiles is null || profiles.Count == 0)
        {
            return 0;
        }

        return Math.Clamp(defaultIndex, 0, profiles.Count - 1);
    }

    // --- Explicit interface implementation ---
    List<LoginData> ILoginDataValidator.Normalize(IEnumerable<LoginData> profiles) => DoNormalize(profiles);
    int ILoginDataValidator.ClampDefaultIndex(IReadOnlyList<LoginData> profiles, int defaultIndex) => DoClampDefaultIndex(profiles, defaultIndex);
}
