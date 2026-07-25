namespace JustData.Application.Login;

public sealed class ApplicationSession : IApplicationSession
{
    private IReadOnlyList<ConnectionProfile> _profiles = [];
    public LoginSelection? CurrentLogin { get; private set; }
    public IReadOnlyList<ConnectionProfile> Profiles => _profiles;
    public void SetLogin(LoginSelection selection, IReadOnlyList<ConnectionProfile> profiles)
    {
        CurrentLogin = selection ?? throw new ArgumentNullException(nameof(selection));
        _profiles = profiles.Select(profile => profile.Clone()).ToArray();
    }
}
