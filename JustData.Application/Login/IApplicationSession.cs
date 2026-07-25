namespace JustData.Application.Login;

public interface IApplicationSession
{
    LoginSelection? CurrentLogin { get; }
    IReadOnlyList<ConnectionProfile> Profiles { get; }
    void SetLogin(LoginSelection selection, IReadOnlyList<ConnectionProfile> profiles);
}
