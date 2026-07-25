namespace AppBase.Services;

public interface ICredentialStore
{
    CredentialStoreReadResult Read(string encryptedPath);
    void Write(string encryptedPath, string content);
}

public sealed record CredentialStoreReadResult(string Content, bool IsLegacyFormat);

public sealed class CredentialStore : ICredentialStore
{
    public CredentialStoreReadResult Read(string encryptedPath)
    {
        var encryption = new HandleEncryption();
        string content = encryption.DecryptFileToString(encryptedPath);
        return new CredentialStoreReadResult(content, !encryption.IsCurrentFormat(encryptedPath));
    }

    public void Write(string encryptedPath, string content)
    {
        new HandleEncryption().EncryptFromString(content, encryptedPath);
    }
}
