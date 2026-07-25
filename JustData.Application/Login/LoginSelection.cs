namespace JustData.Application.Login;

public sealed class LoginSelection(ConnectionProfile profile, bool fastLogin)
{
    public ConnectionProfile Profile { get; } = profile ?? throw new ArgumentNullException(nameof(profile));
    public bool FastLogin { get; } = fastLogin;
    public override string ToString() => $"LoginSelection {{ Connection = {Profile.Name}, FastLogin = {FastLogin}, Password = [REDACTED] }}";
}
