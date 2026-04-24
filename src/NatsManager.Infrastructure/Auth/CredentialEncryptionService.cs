using System.Security.Cryptography;
using System.Text;
using NatsManager.Application.Modules.Environments.Ports;

namespace NatsManager.Infrastructure.Auth;

/// <summary>
/// Authenticated-encryption service for sensitive credential material.
/// Uses AES-256-GCM which provides confidentiality, integrity, and authenticity
/// in a single primitive (no padding-oracle risk, unlike raw AES-CBC).
/// Ciphertext layout (before base64): [12-byte nonce][16-byte auth tag][ciphertext].
/// </summary>
public sealed class CredentialEncryptionService : ICredentialEncryptionService
{
    private const int NonceSize = 12; // AES-GCM standard nonce size
    private const int TagSize = 16;   // AES-GCM standard authentication tag size
    private const int KeySize = 32;   // AES-256

    private readonly byte[] _key;

    public CredentialEncryptionService(byte[] encryptionKey)
    {
        ArgumentNullException.ThrowIfNull(encryptionKey);
        if (encryptionKey.Length != KeySize)
        {
            throw new ArgumentException($"Encryption key must be {KeySize * 8} bits ({KeySize} bytes).", nameof(encryptionKey));
        }

        // Copy to avoid aliasing with caller-owned buffer.
        _key = (byte[])encryptionKey.Clone();
    }

    public string Encrypt(string plainText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plainText);

        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var cipherBytes = new byte[plainBytes.Length];
        var tag = new byte[TagSize];

        using (var aes = new AesGcm(_key, TagSize))
        {
            aes.Encrypt(nonce, plainBytes, cipherBytes, tag);
        }

        var result = new byte[NonceSize + TagSize + cipherBytes.Length];
        Buffer.BlockCopy(nonce, 0, result, 0, NonceSize);
        Buffer.BlockCopy(tag, 0, result, NonceSize, TagSize);
        Buffer.BlockCopy(cipherBytes, 0, result, NonceSize + TagSize, cipherBytes.Length);

        return Convert.ToBase64String(result);
    }

    public string Decrypt(string cipherText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cipherText);

        var fullCipher = Convert.FromBase64String(cipherText);
        if (fullCipher.Length < NonceSize + TagSize)
        {
            throw new CryptographicException("Ciphertext is malformed or truncated.");
        }

        var nonce = new byte[NonceSize];
        var tag = new byte[TagSize];
        var cipher = new byte[fullCipher.Length - NonceSize - TagSize];

        Buffer.BlockCopy(fullCipher, 0, nonce, 0, NonceSize);
        Buffer.BlockCopy(fullCipher, NonceSize, tag, 0, TagSize);
        Buffer.BlockCopy(fullCipher, NonceSize + TagSize, cipher, 0, cipher.Length);

        var plainBytes = new byte[cipher.Length];
        using (var aes = new AesGcm(_key, TagSize))
        {
            // Throws AuthenticationTagMismatchException (derives from CryptographicException)
            // if the ciphertext was tampered with or a wrong key is used.
            aes.Decrypt(nonce, cipher, tag, plainBytes);
        }

        return Encoding.UTF8.GetString(plainBytes);
    }
}
