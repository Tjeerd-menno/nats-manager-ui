using System.Security.Cryptography;
using FluentAssertions;
using NatsManager.Infrastructure.Auth;

namespace NatsManager.Infrastructure.Tests.Auth;

public sealed class CredentialEncryptionServiceTests
{
    private readonly CredentialEncryptionService _service;

    public CredentialEncryptionServiceTests()
    {
        var key = RandomNumberGenerator.GetBytes(32);
        _service = new CredentialEncryptionService(key);
    }

    [Fact]
    public void Encrypt_ShouldReturnBase64String()
    {
        var cipher = _service.Encrypt("secret-password");

        cipher.Should().NotBeNullOrEmpty();
        var act = () => Convert.FromBase64String(cipher);
        act.Should().NotThrow();
    }

    [Fact]
    public void Decrypt_ShouldReturnOriginalText()
    {
        var original = "my-secret-nkey";
        var cipher = _service.Encrypt(original);

        var decrypted = _service.Decrypt(cipher);

        decrypted.Should().Be(original);
    }

    [Fact]
    public void Encrypt_SamePlaintext_ShouldProduceDifferentCiphers()
    {
        var cipher1 = _service.Encrypt("password");
        var cipher2 = _service.Encrypt("password");

        cipher1.Should().NotBe(cipher2);
    }

    [Fact]
    public void Constructor_WithWrongKeySize_ShouldThrow()
    {
        var act = () => new CredentialEncryptionService(new byte[16]);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Encrypt_WithNullInput_ShouldThrow()
    {
        var act = () => _service.Encrypt(null!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Decrypt_WithNullInput_ShouldThrow()
    {
        var act = () => _service.Decrypt(null!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Decrypt_WithWrongKey_ShouldThrowOrReturnDifferentText()
    {
        var cipher = _service.Encrypt("secret");

        var otherKey = RandomNumberGenerator.GetBytes(32);
        var otherService = new CredentialEncryptionService(otherKey);

        var act = () => otherService.Decrypt(cipher);
        act.Should().Throw<CryptographicException>();
    }
}
