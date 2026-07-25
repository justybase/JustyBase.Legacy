using AppBase.Services;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AppBase.Tests.Security;

[SupportedOSPlatform("windows")]
public sealed class HandleEncryptionTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly HandleEncryption _sut;

    public HandleEncryptionTests()
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("HandleEncryption requires Windows CSP.");
        _tempDirectory = Path.Combine(Path.GetTempPath(), "JustyBaseLegacy.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
        _sut = new HandleEncryption();
    }

    [Fact]
    public void EncryptFromString_And_DecryptFileToString_RoundTrip_PreservesJsonContent()
    {
        var payload = new[]
        {
            new { Name = "test-connection", Server = "127.0.0.1", User = "admin" }
        };
        string json = JsonSerializer.Serialize(payload);
        string encryptedPath = Path.Combine(_tempDirectory, "credentials.json.enc");

        _sut.EncryptFromString(json, encryptedPath);
        string decrypted = _sut.DecryptFileToString(encryptedPath);

        Assert.Equal(json, decrypted);
        Assert.True(_sut.IsCurrentFormat(encryptedPath));
    }

    [Fact]
    public void EncryptFromString_CreatesEncryptedFile()
    {
        string encryptedPath = Path.Combine(_tempDirectory, "credentials.json.enc");
        const string content = "[{\"Name\":\"demo\"}]";

        _sut.EncryptFromString(content, encryptedPath);

        Assert.True(File.Exists(encryptedPath));
        Assert.True(new FileInfo(encryptedPath).Length > 0);
        Assert.NotEqual(content, File.ReadAllText(encryptedPath));
    }

    [Fact]
    public void DecryptFileToString_RejectsTamperedAuthenticatedFile()
    {
        string encryptedPath = Path.Combine(_tempDirectory, "credentials.json.enc");
        _sut.EncryptFromString("[{\"Name\":\"demo\"}]", encryptedPath);
        byte[] bytes = File.ReadAllBytes(encryptedPath);
        bytes[^1] ^= 0x01;
        File.WriteAllBytes(encryptedPath, bytes);

        Assert.ThrowsAny<CryptographicException>(() => _sut.DecryptFileToString(encryptedPath));
    }

    [Fact]
    public void DecryptFileToString_RejectsEmptyFile()
    {
        string encryptedPath = Path.Combine(_tempDirectory, "empty.enc");
        File.WriteAllBytes(encryptedPath, []);

        Assert.Throws<CryptographicException>(() => _sut.DecryptFileToString(encryptedPath));
    }

    [Fact]
    public void DecryptFileToString_RejectsTruncatedCurrentFile()
    {
        string encryptedPath = Path.Combine(_tempDirectory, "truncated.enc");
        _sut.EncryptFromString("secret", encryptedPath);
        byte[] bytes = File.ReadAllBytes(encryptedPath);
        File.WriteAllBytes(encryptedPath, bytes[..10]);

        Assert.Throws<EndOfStreamException>(() => _sut.DecryptFileToString(encryptedPath));
    }

    [Fact]
    public void DecryptFileToString_RejectsReplacedHeader()
    {
        string encryptedPath = Path.Combine(_tempDirectory, "header.enc");
        _sut.EncryptFromString("secret", encryptedPath);
        byte[] bytes = File.ReadAllBytes(encryptedPath);
        bytes[0] ^= 0x20;
        File.WriteAllBytes(encryptedPath, bytes);

        Assert.ThrowsAny<CryptographicException>(() => _sut.DecryptFileToString(encryptedPath));
    }

    [Fact]
    public void EncryptFromString_AtomicallyOverwritesExistingFile()
    {
        string encryptedPath = Path.Combine(_tempDirectory, "overwrite.enc");
        _sut.EncryptFromString("first", encryptedPath);

        _sut.EncryptFromString("second", encryptedPath);

        Assert.Equal("second", _sut.DecryptFileToString(encryptedPath));
        Assert.Empty(Directory.EnumerateFiles(_tempDirectory, ".*.tmp"));
    }

    [Fact]
    public void DecryptFileToString_ReadsLegacyRsaAesFile()
    {
        const string content = "[{\"Name\":\"legacy\"}]";
        string encryptedPath = Path.Combine(_tempDirectory, "legacy.json.enc");

        WriteLegacyFile(encryptedPath, content);

        Assert.Equal(content, _sut.DecryptFileToString(encryptedPath));
        Assert.False(_sut.IsCurrentFormat(encryptedPath));
    }

    [Fact]
    public void CredentialStore_ReadsLegacyAndWriteMigratesToCurrentFormat()
    {
        const string content = "[{\"Name\":\"legacy-migration\"}]";
        string encryptedPath = Path.Combine(_tempDirectory, "migration.json.enc");
        WriteLegacyFile(encryptedPath, content);
        CredentialStore store = new();

        CredentialStoreReadResult legacy = store.Read(encryptedPath);

        Assert.Equal(content, legacy.Content);
        Assert.True(legacy.IsLegacyFormat);

        store.Write(encryptedPath, legacy.Content);
        CredentialStoreReadResult current = store.Read(encryptedPath);

        Assert.Equal(content, current.Content);
        Assert.False(current.IsLegacyFormat);
        Assert.True(_sut.IsCurrentFormat(encryptedPath));
    }

    private static void WriteLegacyFile(string path, string content)
    {
        using var rsa = new RSACryptoServiceProvider(new CspParameters { KeyContainerName = "Key01" });
        using Aes aes = Aes.Create();
        byte[] encryptedKey = rsa.Encrypt(aes.Key, false);
        byte[] plaintext = Encoding.UTF8.GetBytes(content);

        using FileStream file = new(path, FileMode.Create, FileAccess.Write, FileShare.None);
        file.Write(BitConverter.GetBytes(encryptedKey.Length));
        file.Write(BitConverter.GetBytes(aes.IV.Length));
        file.Write(encryptedKey);
        file.Write(aes.IV);
        using CryptoStream crypto = new(file, aes.CreateEncryptor(), CryptoStreamMode.Write);
        crypto.Write(plaintext);
        crypto.FlushFinalBlock();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
