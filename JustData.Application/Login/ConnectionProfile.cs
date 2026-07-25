namespace JustData.Application.Login;

public sealed class ConnectionProfile
{
    public string Name { get; set; } = string.Empty;
    public string Driver { get; set; } = string.Empty;
    public string Server { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Database { get; set; } = string.Empty;

    public ConnectionProfile Clone() => new()
    {
        Name = Name, Driver = Driver, Server = Server, UserName = UserName, Password = Password, Database = Database
    };

    public override string ToString() => $"ConnectionProfile {{ Name = {Name}, Driver = {Driver}, Server = {Server}, UserName = {UserName}, Password = [REDACTED], Database = {Database} }}";
}
