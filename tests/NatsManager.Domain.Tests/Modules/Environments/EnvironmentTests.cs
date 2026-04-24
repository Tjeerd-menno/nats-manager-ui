using Shouldly;
using NatsManager.Domain.Modules.Common;
using Environment = NatsManager.Domain.Modules.Environments.Environment;

namespace NatsManager.Domain.Tests.Modules.Environments;

public sealed class EnvironmentTests
{
    [Fact]
    public void Create_WithValidParameters_ShouldCreateEnvironment()
    {
        var env = Environment.Create("Test Env", "nats://localhost:4222");

        env.Id.ShouldNotBe(Guid.Empty);
        env.Name.ShouldBe("Test Env");
        env.ServerUrl.ShouldBe("nats://localhost:4222");
        env.Description.ShouldBeEmpty();
        env.CredentialType.ShouldBe(CredentialType.None);
        env.CredentialReference.ShouldBeEmpty();
        env.IsEnabled.ShouldBeTrue();
        env.IsProduction.ShouldBeFalse();
        env.ConnectionStatus.ShouldBe(ConnectionStatus.Unknown);
        env.LastSuccessfulContact.ShouldBeNull();
        (env.CreatedAt - DateTimeOffset.UtcNow).Duration().ShouldBeLessThan(TimeSpan.FromSeconds(2));
        (env.UpdatedAt - DateTimeOffset.UtcNow).Duration().ShouldBeLessThan(TimeSpan.FromSeconds(2));
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

        env.Name.ShouldBe("Production");
        env.Description.ShouldBe("Prod env");
        env.ServerUrl.ShouldBe("nats://prod:4222");
        env.CredentialType.ShouldBe(CredentialType.Token);
        env.CredentialReference.ShouldBe("encrypted-token");
        env.IsProduction.ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidName_ShouldThrow(string? name)
    {
        var act = () => Environment.Create(name!, "nats://localhost:4222");

        Should.Throw<ArgumentException>(act);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidServerUrl_ShouldThrow(string? serverUrl)
    {
        var act = () => Environment.Create("Test", serverUrl!);

        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Create_WithNameExceeding100Chars_ShouldThrow()
    {
        var longName = new string('a', 101);

        var act = () => Environment.Create(longName, "nats://localhost:4222");

        Should.Throw<ArgumentException>(act).Message.ShouldContain("100 characters");
    }

    [Fact]
    public void Create_ShouldTrimNameAndDescription()
    {
        var env = Environment.Create("  Trimmed  ", "  nats://localhost  ", description: "  desc  ");

        env.Name.ShouldBe("Trimmed");
        env.ServerUrl.ShouldBe("nats://localhost");
        env.Description.ShouldBe("desc");
    }

    [Fact]
    public void Update_WithValidParameters_ShouldUpdateProperties()
    {
        var env = Environment.Create("Original", "nats://original:4222");
        var originalUpdatedAt = env.UpdatedAt;

        env.Update("Updated", "nats://updated:4222", description: "New desc", isProduction: true);

        env.Name.ShouldBe("Updated");
        env.ServerUrl.ShouldBe("nats://updated:4222");
        env.Description.ShouldBe("New desc");
        env.IsProduction.ShouldBeTrue();
        env.UpdatedAt.ShouldBeGreaterThanOrEqualTo(originalUpdatedAt);
    }

    [Fact]
    public void Update_WithInvalidName_ShouldThrow()
    {
        var env = Environment.Create("Original", "nats://localhost:4222");

        var act = () => env.Update("", "nats://localhost:4222");

        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Update_WithNameExceeding100Chars_ShouldThrow()
    {
        var env = Environment.Create("Original", "nats://localhost:4222");

        var act = () => env.Update(new string('x', 101), "nats://localhost:4222");

        Should.Throw<ArgumentException>(act).Message.ShouldContain("100 characters");
    }

    [Fact]
    public void Update_WithNullCredentialReference_ShouldNotChangeExisting()
    {
        var env = Environment.Create("Env", "nats://localhost:4222",
            credentialType: CredentialType.Token, credentialReference: "original-ref");

        env.Update("Env", "nats://localhost:4222", credentialReference: null);

        env.CredentialReference.ShouldBe("original-ref");
    }

    [Fact]
    public void UpdateConnectionStatus_ToAvailable_ShouldSetLastSuccessfulContact()
    {
        var env = Environment.Create("Test", "nats://localhost:4222");
        env.LastSuccessfulContact.ShouldBeNull();

        env.UpdateConnectionStatus(ConnectionStatus.Available);

        env.ConnectionStatus.ShouldBe(ConnectionStatus.Available);
        env.LastSuccessfulContact.ShouldNotBeNull();
        (env.LastSuccessfulContact!.Value - DateTimeOffset.UtcNow).Duration().ShouldBeLessThan(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void UpdateConnectionStatus_ToUnavailable_ShouldNotSetLastSuccessfulContact()
    {
        var env = Environment.Create("Test", "nats://localhost:4222");

        env.UpdateConnectionStatus(ConnectionStatus.Unavailable);

        env.ConnectionStatus.ShouldBe(ConnectionStatus.Unavailable);
        env.LastSuccessfulContact.ShouldBeNull();
    }

    [Fact]
    public void Enable_ShouldSetEnabledAndResetConnectionStatus()
    {
        var env = Environment.Create("Test", "nats://localhost:4222");
        env.Disable();
        env.UpdateConnectionStatus(ConnectionStatus.Unavailable);

        env.Enable();

        env.IsEnabled.ShouldBeTrue();
        env.ConnectionStatus.ShouldBe(ConnectionStatus.Unknown);
    }

    [Fact]
    public void Disable_ShouldSetDisabled()
    {
        var env = Environment.Create("Test", "nats://localhost:4222");

        env.Disable();

        env.IsEnabled.ShouldBeFalse();
    }
}
