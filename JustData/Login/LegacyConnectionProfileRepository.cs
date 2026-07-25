using System.Text.Json;
using AppBase.Common;
using AppBase.Services;
using JustData.Application.Login;

namespace JustyBaseLegacy.UI.Login;

internal sealed class LegacyConnectionProfileRepository(ICredentialStore credentialStore, string credentialPath, ILoginDataValidator loginDataValidator) : IConnectionProfileRepository
{
    private readonly ILoginDataValidator _loginDataValidator = loginDataValidator ?? throw new ArgumentNullException(nameof(loginDataValidator));
    private readonly string _encryptedPath = credentialPath + ".enc";

    public Task<ConnectionProfilesLoadResult> LoadAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!File.Exists(_encryptedPath)) return Task.FromResult(new ConnectionProfilesLoadResult([], 0, false));
        try
        {
            var content = credentialStore.Read(_encryptedPath).Content;
            var legacy = string.IsNullOrWhiteSpace(content) ? [] : JsonSerializer.Deserialize(content, MyJsonContextLoginData.Default.ListLoginData) ?? [];
            legacy = _loginDataValidator.Normalize(legacy);
            var profiles = legacy.Select(Map).ToArray();
            var defaultIndex = profiles.Length == 0 ? 0 : _loginDataValidator.ClampDefaultIndex(legacy, legacy[0].DefaultIndex);
            return Task.FromResult(new ConnectionProfilesLoadResult(profiles, defaultIndex, false));
        }
        catch (Exception)
        {
            BackupCorruptFile();
            return Task.FromResult(new ConnectionProfilesLoadResult([], 0, true));
        }
    }

    public Task SaveAsync(IReadOnlyList<ConnectionProfile> profiles, int defaultIndex, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var legacy = profiles.Select(Map).ToList();
        if (legacy.Count > 0) legacy[0].DefaultIndex = Math.Clamp(defaultIndex, 0, legacy.Count - 1);
        credentialStore.Write(_encryptedPath, JsonSerializer.Serialize(legacy, MyJsonContextLoginData.Default.ListLoginData));
        return Task.CompletedTask;
    }

    private void BackupCorruptFile()
    {
        try { File.Move(_encryptedPath, $"{_encryptedPath}.corrupt-{DateTime.UtcNow:yyyyMMddHHmmssfff}.bak", false); }
        catch (Exception) { }
    }

    internal static ConnectionProfile Map(LoginData source) => new() { Name = source.Name, Driver = source.Driver, Server = source.Server, UserName = source.UserName, Password = source.Password, Database = source.Database };
    internal static LoginData Map(ConnectionProfile source) => new() { Name = source.Name, Driver = source.Driver, Server = source.Server, UserName = source.UserName, Password = source.Password, Database = source.Database };
}
