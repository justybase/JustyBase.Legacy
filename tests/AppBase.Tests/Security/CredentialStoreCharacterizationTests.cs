using AppBase.Services;

namespace AppBase.Tests.Security;

public sealed class CredentialStoreCharacterizationTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "JustData-CredentialStoreTests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void Encrypted_credentials_round_trip_without_writing_plaintext()
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "credentials.json.enc");
        const string credentials = "[{\"Name\":\"legacy\",\"Password\":\"secret\"}]";
        var store = new CredentialStore();

        store.Write(path, credentials);
        var result = store.Read(path);

        Assert.Equal(credentials, result.Content);
        Assert.False(File.ReadAllText(path).Contains("secret", StringComparison.Ordinal));
    }

    [Fact]
    public void Invalid_encrypted_credentials_throw_so_the_login_host_can_create_a_corrupt_backup()
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "credentials.json.enc");
        File.WriteAllText(path, "not encrypted credentials");

        Assert.ThrowsAny<Exception>(() => new CredentialStore().Read(path));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
    }
}
