using Shouldly;
using NatsManager.Infrastructure.Auth;

namespace NatsManager.Infrastructure.Tests.Auth;

public sealed class PasswordHasherTests
{
    [Fact]
    public void Hash_ShouldReturnVersionedFormat()
    {
        var result = PasswordHasher.Hash("password123");

        var parts = result.Split('.');
        parts.Length.ShouldBe(4);
        parts[0].ShouldBe("pbkdf2-sha512");
        parts[1].ShouldBe("100000");
        parts[2].ShouldNotBeNullOrEmpty();
        parts[3].ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void Hash_ShouldProduceDifferentHashesForSamePassword()
    {
        var hash1 = PasswordHasher.Hash("password123");
        var hash2 = PasswordHasher.Hash("password123");

        hash1.ShouldNotBe(hash2);
    }

    [Fact]
    public void Verify_WithCorrectPassword_ShouldReturnTrue()
    {
        var hash = PasswordHasher.Hash("password123");

        PasswordHasher.Verify("password123", hash).ShouldBeTrue();
    }

    [Fact]
    public void Verify_WithWrongPassword_ShouldReturnFalse()
    {
        var hash = PasswordHasher.Hash("password123");

        PasswordHasher.Verify("wrongpassword", hash).ShouldBeFalse();
    }

    [Fact]
    public void Verify_WithInvalidHashFormat_ShouldReturnFalse()
    {
        PasswordHasher.Verify("password123", "invalidhash").ShouldBeFalse();
    }

    [Fact]
    public void Verify_WithMalformedBase64_ShouldReturnFalseNotThrow()
    {
        // 4-part shape but base64 segments are not decodable.
        PasswordHasher.Verify("password123", "pbkdf2-sha512.100000.!!!.???").ShouldBeFalse();
    }

    [Fact]
    public void Verify_WithDecodableButWrongSizedSaltAndHash_ShouldReturnFalse()
    {
        // Both segments are valid base64 but decode to sizes that do not match the hasher's
        // expected salt (16) / hash (32) sizes. Such degenerate values must be rejected before
        // being fed to PBKDF2 rather than treated as a valid (unverifiable) hash.
        var tinySalt = Convert.ToBase64String(new byte[1]);
        var tinyHash = Convert.ToBase64String(new byte[1]);

        PasswordHasher.Verify("password123", $"pbkdf2-sha512.100000.{tinySalt}.{tinyHash}").ShouldBeFalse();
        PasswordHasher.Verify("password123", $"{tinySalt}.{tinyHash}").ShouldBeFalse();
    }

    [Fact]
    public void Verify_WithEmptyDecodedSaltAndHash_ShouldReturnFalse()
    {
        var empty = Convert.ToBase64String([]);

        PasswordHasher.Verify("password123", $"pbkdf2-sha512.100000.{empty}.{empty}").ShouldBeFalse();
    }

    [Fact]
    public void Verify_WithLegacyTwoPartHash_ShouldStillVerify()
    {
        // Legacy format: {saltBase64}.{hashBase64} with 100k PBKDF2-SHA512.
        var salt = System.Security.Cryptography.RandomNumberGenerator.GetBytes(16);
        var hash = System.Security.Cryptography.Rfc2898DeriveBytes.Pbkdf2(
            "password123", salt, 100_000, System.Security.Cryptography.HashAlgorithmName.SHA512, 32);
        var legacy = $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";

        PasswordHasher.Verify("password123", legacy).ShouldBeTrue();
        PasswordHasher.Verify("wrong", legacy).ShouldBeFalse();
    }

    [Fact]
    public void NeedsRehash_ShouldBeTrueForLegacyAndFalseForCurrent()
    {
        var current = PasswordHasher.Hash("password123");
        PasswordHasher.NeedsRehash(current).ShouldBeFalse();

        var salt = System.Security.Cryptography.RandomNumberGenerator.GetBytes(16);
        var hash = System.Security.Cryptography.Rfc2898DeriveBytes.Pbkdf2(
            "password123", salt, 100_000, System.Security.Cryptography.HashAlgorithmName.SHA512, 32);
        var legacy = $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";

        PasswordHasher.NeedsRehash(legacy).ShouldBeTrue();
        PasswordHasher.NeedsRehash("garbage").ShouldBeTrue();
    }

    [Fact]
    public void Hash_WithNullPassword_ShouldThrow()
    {
        var act = () => PasswordHasher.Hash(null!);
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Hash_WithEmptyPassword_ShouldThrow()
    {
        var act = () => PasswordHasher.Hash("");
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void IPasswordHasher_ShouldDelegateCorrectly()
    {
        var hasher = new PasswordHasher();
        var iface = (NatsManager.Application.Modules.Auth.Ports.IPasswordHasher)hasher;

        var hash = iface.Hash("test");
        iface.Verify("test", hash).ShouldBeTrue();
        iface.Verify("wrong", hash).ShouldBeFalse();
    }
}
