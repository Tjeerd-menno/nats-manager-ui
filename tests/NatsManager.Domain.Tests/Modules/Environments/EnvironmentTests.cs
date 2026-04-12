using FluentAssertions;
using NatsManager.Domain.Modules.Common;
using Environment = NatsManager.Domain.Modules.Environments.Environment;

namespace NatsManager.Domain.Tests.Modules.Environments;

public sealed class EnvironmentTests
{
    [Fact]
    public void Create_WithValidParameters_ShouldCreateEnvironment()
    {
        var env = Environment.Create("Test Env", "nats://localhost:4222");

        env.Id.Should().NotBeEmpty();
        env.Name.Should().Be("Test Env");
        env.ServerUrl.Should().Be("nats://localhost:4222");
        env.Description.Should().BeEmpty();
        env.CredentialType.Should().Be(CredentialType.None);
        env.CredentialReference.Should().BeEmpty();
        env.IsEnabled.Should().BeTrue();
        env.IsProduction.Should().BeFalse();
        env.ConnectionStatus.Should().Be(ConnectionStatus.Unknown);
        env.LastSuccessfulContact.Should().BeNull();
        env.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
        env.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Create_WithAllParameters_ShouldSetAllProperties()
    {
        var env = Environment.Create(
            "Production",
            "nats://prod:4222",
            description: "Prod env",
            credentialType: CredentialType.Token,
            credentialReference: "encrypted-token",
            isProduction: true);

        env.Name.Should().Be("Production");
        env.Description.Should().Be("Prod env");
        env.ServerUrl.Should().Be("nats://prod:4222");
        env.CredentialType.Should().Be(CredentialType.Token);
        env.CredentialReference.Should().Be("encrypted-token");
        env.IsProduction.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidName_ShouldThrow(string? name)
    {
        var act = () => Environment.Create(name!, "nats://localhost:4222");

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidServerUrl_ShouldThrow(string? serverUrl)
    {
        var act = () => Environment.Create("Test", serverUrl!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNameExceeding100Chars_ShouldThrow()
    {
        var longName = new string('a', 101);

        var act = () => Environment.Create(longName, "nats://localhost:4222");

        act.Should().Throw<ArgumentException>().WithMessage("*100 characters*");
    }

    [Fact]
    public void Create_ShouldTrimNameAndDescription()
    {
        var env = Environment.Create("  Trimmed  ", "  nats://localhost  ", description: "  desc  ");

        env.Name.Should().Be("Trimmed");
        env.ServerUrl.Should().Be("nats://localhost");
        env.Description.Should().Be("desc");
    }

    [Fact]
    public void Update_WithValidParameters_ShouldUpdateProperties()
    {
        var env = Environment.Create("Original", "nats://original:4222");
        var originalUpdatedAt = env.UpdatedAt;

        env.Update("Updated", "nats://updated:4222", description: "New desc", isProduction: true);

        env.Name.Should().Be("Updated");
        env.ServerUrl.Should().Be("nats://updated:4222");
        env.Description.Should().Be("New desc");
        env.IsProduction.Should().BeTrue();
        env.UpdatedAt.Should().BeOnOrAfter(originalUpdatedAt);
    }

    [Fact]
    public void Update_WithInvalidName_ShouldThrow()
    {
        var env = Environment.Create("Original", "nats://localhost:4222");

        var act = () => env.Update("", "nats://localhost:4222");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Update_WithNameExceeding100Chars_ShouldThrow()
    {
        var env = Environment.Create("Original", "nats://localhost:4222");

        var act = () => env.Update(new string('x', 101), "nats://localhost:4222");

        act.Should().Throw<ArgumentException>().WithMessage("*100 characters*");
    }

    [Fact]
    public void Update_WithNullCredentialReference_ShouldNotChangeExisting()
    {
        var env = Environment.Create("Env", "nats://localhost:4222",
            credentialType: CredentialType.Token, credentialReference: "original-ref");

        env.Update("Env", "nats://localhost:4222", credentialReference: null);

        env.CredentialReference.Should().Be("original-ref");
    }

    [Fact]
    public void UpdateConnectionStatus_ToAvailable_ShouldSetLastSuccessfulContact()
    {
        var env = Environment.Create("Test", "nats://localhost:4222");
        env.LastSuccessfulContact.Should().BeNull();

        env.UpdateConnectionStatus(ConnectionStatus.Available);

        env.ConnectionStatus.Should().Be(ConnectionStatus.Available);
        env.LastSuccessfulContact.Should().NotBeNull();
        env.LastSuccessfulContact.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void UpdateConnectionStatus_ToUnavailable_ShouldNotSetLastSuccessfulContact()
    {
        var env = Environment.Create("Test", "nats://localhost:4222");

        env.UpdateConnectionStatus(ConnectionStatus.Unavailable);

        env.ConnectionStatus.Should().Be(ConnectionStatus.Unavailable);
        env.LastSuccessfulContact.Should().BeNull();
    }

    [Fact]
    public void Enable_ShouldSetEnabledAndResetConnectionStatus()
    {
        var env = Environment.Create("Test", "nats://localhost:4222");
        env.Disable();
        env.UpdateConnectionStatus(ConnectionStatus.Unavailable);

        env.Enable();

        env.IsEnabled.Should().BeTrue();
        env.ConnectionStatus.Should().Be(ConnectionStatus.Unknown);
    }

    [Fact]
    public void Disable_ShouldSetDisabled()
    {
        var env = Environment.Create("Test", "nats://localhost:4222");

        env.Disable();

        env.IsEnabled.Should().BeFalse();
    }
}
