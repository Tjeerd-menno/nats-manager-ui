using FluentAssertions;
using NatsManager.Infrastructure.Auth;

namespace NatsManager.Infrastructure.Tests.Auth;

public sealed class PasswordHasherTests
{
    [Fact]
    public void Hash_ShouldReturnSaltDotHash()
    {
        var result = PasswordHasher.Hash("password123");

        result.Should().Contain(".");
        var parts = result.Split('.');
        parts.Should().HaveCount(2);
        parts[0].Should().NotBeNullOrEmpty();
        parts[1].Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Hash_ShouldProduceDifferentHashesForSamePassword()
    {
        var hash1 = PasswordHasher.Hash("password123");
        var hash2 = PasswordHasher.Hash("password123");

        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void Verify_WithCorrectPassword_ShouldReturnTrue()
    {
        var hash = PasswordHasher.Hash("password123");

        PasswordHasher.Verify("password123", hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_WithWrongPassword_ShouldReturnFalse()
    {
        var hash = PasswordHasher.Hash("password123");

        PasswordHasher.Verify("wrongpassword", hash).Should().BeFalse();
    }

    [Fact]
    public void Verify_WithInvalidHashFormat_ShouldReturnFalse()
    {
        PasswordHasher.Verify("password123", "invalidhash").Should().BeFalse();
    }

    [Fact]
    public void Hash_WithNullPassword_ShouldThrow()
    {
        var act = () => PasswordHasher.Hash(null!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Hash_WithEmptyPassword_ShouldThrow()
    {
        var act = () => PasswordHasher.Hash("");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void IPasswordHasher_ShouldDelegateCorrectly()
    {
        var hasher = new PasswordHasher();
        var iface = (NatsManager.Application.Modules.Auth.Ports.IPasswordHasher)hasher;

        var hash = iface.Hash("test");
        iface.Verify("test", hash).Should().BeTrue();
        iface.Verify("wrong", hash).Should().BeFalse();
    }
}
