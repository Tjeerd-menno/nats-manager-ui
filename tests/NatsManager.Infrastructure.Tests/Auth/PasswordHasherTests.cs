using Shouldly;
using NatsManager.Infrastructure.Auth;

namespace NatsManager.Infrastructure.Tests.Auth;

public sealed class PasswordHasherTests
{
    [Fact]
    public void Hash_ShouldReturnSaltDotHash()
    {
        var result = PasswordHasher.Hash("password123");

        result.ShouldContain(".");
        var parts = result.Split('.');
        parts.Count().ShouldBe(2);
        parts[0].ShouldNotBeNullOrEmpty();
        parts[1].ShouldNotBeNullOrEmpty();
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
