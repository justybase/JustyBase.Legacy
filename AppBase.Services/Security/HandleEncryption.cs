using System.Security.Cryptography;
using System.Text;

namespace AppBase.Services;

/// <summary>
/// Encrypts the local credentials file.
///
/// Version 1 uses the existing per-user RSA key container to wrap an AES key and
/// AES-GCM for authenticated encryption. The legacy RSA/AES-CBC format is still
/// accepted on read so existing installations can migrate on their next save.
/// </summary>
public sealed class HandleEncryption
{
    private static readonly byte[] CurrentMagic = "JBCG"u8.ToArray();
    private const byte CurrentVersion = 1;
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private const int MaxKeySize = 4096;
    private const int MaxPayloadSize = 64 * 1024 * 1024;
    private const string KeyName = "Key01";

    private readonly RSACryptoServiceProvider _rsa;

    // Kept for source compatibility with the original form code.
    public string Path { get; set; } = string.Empty;

    public HandleEncryption()
    {
        _rsa = new RSACryptoServiceProvider(new CspParameters
        {
            KeyContainerName = KeyName
        });
    }

    public void EncryptFromString(string content, string outPath)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(outPath);

        byte[] plaintext = Encoding.UTF8.GetBytes(content);
        byte[] aesKey = RandomNumberGenerator.GetBytes(32);
        byte[] nonce = RandomNumberGenerator.GetBytes(NonceSize);
        byte[] tag = new byte[TagSize];
        byte[] ciphertext = new byte[plaintext.Length];
        byte[] encryptedKey = _rsa.Encrypt(aesKey, false);

        using (var aes = new AesGcm(aesKey, TagSize))
        {
            aes.Encrypt(nonce, plaintext, ciphertext, tag, BuildAssociatedData());
        }

        WriteAtomically(outPath, stream =>
        {
            stream.Write(CurrentMagic);
            stream.WriteByte(CurrentVersion);
            stream.Write(BitConverter.GetBytes(encryptedKey.Length));
            stream.Write(encryptedKey);
            stream.Write(nonce);
            stream.Write(tag);
            stream.Write(ciphertext);
            stream.Flush(true);
        });

        CryptographicOperations.ZeroMemory(aesKey);
        CryptographicOperations.ZeroMemory(plaintext);
    }

    public string DecryptFileToString(string inFile)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inFile);

        return IsCurrentFormat(inFile)
            ? DecryptCurrent(inFile)
            : DecryptLegacy(inFile);
    }

    public bool IsCurrentFormat(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return false;
        }

        using FileStream stream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (stream.Length < CurrentMagic.Length + 1)
        {
            return false;
        }

        Span<byte> header = stackalloc byte[CurrentMagic.Length + 1];
        stream.ReadExactly(header);
        return header[..CurrentMagic.Length].SequenceEqual(CurrentMagic)
            && header[CurrentMagic.Length] == CurrentVersion;
    }

    private string DecryptCurrent(string inFile)
    {
        using FileStream stream = new(inFile, FileMode.Open, FileAccess.Read, FileShare.Read);
        byte[] magic = new byte[CurrentMagic.Length];
        stream.ReadExactly(magic);
        int version = stream.ReadByte();
        if (!magic.AsSpan().SequenceEqual(CurrentMagic) || version != CurrentVersion)
        {
            throw new CryptographicException("Unsupported credentials format.");
        }

        Span<byte> lengthBytes = stackalloc byte[sizeof(int)];
        stream.ReadExactly(lengthBytes);
        int encryptedKeyLength = BitConverter.ToInt32(lengthBytes);
        if (encryptedKeyLength <= 0 || encryptedKeyLength > MaxKeySize)
        {
            throw new CryptographicException("Invalid credentials key length.");
        }

        byte[] encryptedKey = new byte[encryptedKeyLength];
        byte[] nonce = new byte[NonceSize];
        byte[] tag = new byte[TagSize];
        stream.ReadExactly(encryptedKey);
        stream.ReadExactly(nonce);
        stream.ReadExactly(tag);

        long payloadLength = stream.Length - stream.Position;
        if (payloadLength < 0 || payloadLength > MaxPayloadSize)
        {
            throw new CryptographicException("Invalid credentials payload length.");
        }

        byte[] ciphertext = new byte[checked((int)payloadLength)];
        stream.ReadExactly(ciphertext);
        byte[] aesKey = _rsa.Decrypt(encryptedKey, false);
        byte[] plaintext = new byte[ciphertext.Length];

        try
        {
            using var aes = new AesGcm(aesKey, TagSize);
            aes.Decrypt(nonce, ciphertext, tag, plaintext, BuildAssociatedData());
            return Encoding.UTF8.GetString(plaintext);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(aesKey);
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }

    private string DecryptLegacy(string inFile)
    {
        using FileStream stream = new(inFile, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (stream.Length < 8)
        {
            throw new CryptographicException("Invalid legacy credentials file.");
        }

        Span<byte> lengthBytes = stackalloc byte[sizeof(int)];
        stream.ReadExactly(lengthBytes);
        int encryptedKeyLength = BitConverter.ToInt32(lengthBytes);
        stream.ReadExactly(lengthBytes);
        int ivLength = BitConverter.ToInt32(lengthBytes);

        long cipherLength = stream.Length - 8L - encryptedKeyLength - ivLength;
        if (encryptedKeyLength <= 0 || encryptedKeyLength > MaxKeySize
            || ivLength <= 0 || ivLength > 32
            || cipherLength <= 0 || cipherLength > MaxPayloadSize)
        {
            throw new CryptographicException("Invalid legacy credentials header.");
        }

        byte[] encryptedKey = new byte[encryptedKeyLength];
        byte[] iv = new byte[ivLength];
        stream.ReadExactly(encryptedKey);
        stream.ReadExactly(iv);
        byte[] decryptedKey = _rsa.Decrypt(encryptedKey, false);

        try
        {
            using var aes = Aes.Create();
            using var decryptor = new CryptoStream(stream, aes.CreateDecryptor(decryptedKey, iv), CryptoStreamMode.Read);
            using var output = new MemoryStream();
            decryptor.CopyTo(output);
            return Encoding.UTF8.GetString(output.ToArray());
        }
        finally
        {
            CryptographicOperations.ZeroMemory(decryptedKey);
        }
    }

    private static byte[] BuildAssociatedData()
    {
        return [CurrentMagic[0], CurrentMagic[1], CurrentMagic[2], CurrentMagic[3], CurrentVersion];
    }

    private static void WriteAtomically(string destination, Action<FileStream> write)
    {
        string fullDestination = System.IO.Path.GetFullPath(destination);
        string directory = System.IO.Path.GetDirectoryName(fullDestination)
            ?? throw new IOException("Credentials directory is invalid.");
        Directory.CreateDirectory(directory);
        string temporary = System.IO.Path.Combine(directory, $".{System.IO.Path.GetFileName(fullDestination)}.{Guid.NewGuid():N}.tmp");

        try
        {
            using (FileStream stream = new(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                write(stream);
            }

            File.Move(temporary, fullDestination, true);
        }
        finally
        {
            if (File.Exists(temporary))
            {
                File.Delete(temporary);
            }
        }
    }
}
