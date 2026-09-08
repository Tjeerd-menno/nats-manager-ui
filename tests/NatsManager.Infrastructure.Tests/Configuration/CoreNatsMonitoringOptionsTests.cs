using NatsManager.Infrastructure.Configuration;
using Shouldly;

namespace NatsManager.Infrastructure.Tests.Configuration;

public sealed class CoreNatsMonitoringOptionsTests
{
    [Fact]
    public void IsValid_WithDefaults_ShouldBeTrue() =>
        CoreNatsMonitoringOptions.IsValid(new CoreNatsMonitoringOptions()).ShouldBeTrue();

    [Theory]
    [InlineData("http://localhost:8222")]
    [InlineData("https://monitoring.example.com:8222")]
    public void IsValid_WithHttpOrHttpsBaseUrl_ShouldBeTrue(string baseUrl) =>
        CoreNatsMonitoringOptions.IsValid(new CoreNatsMonitoringOptions { BaseUrl = baseUrl }).ShouldBeTrue();

    [Theory]
    [InlineData("nats://localhost:4222")]
    [InlineData("file:///etc/passwd")]
    [InlineData("ftp://example.com")]
    [InlineData("not-a-url")]
    public void IsValid_WithNonHttpBaseUrl_ShouldBeFalse(string baseUrl) =>
        CoreNatsMonitoringOptions.IsValid(new CoreNatsMonitoringOptions { BaseUrl = baseUrl }).ShouldBeFalse();

    [Theory]
    [InlineData(0)]
    [InlineData(70000)]
    public void IsValid_WithOutOfRangePort_ShouldBeFalse(int port) =>
        CoreNatsMonitoringOptions.IsValid(new CoreNatsMonitoringOptions { DefaultPort = port }).ShouldBeFalse();

    [Fact]
    public void IsValid_WithNonPositiveTimeout_ShouldBeFalse() =>
        CoreNatsMonitoringOptions.IsValid(new CoreNatsMonitoringOptions { HttpTimeout = TimeSpan.Zero }).ShouldBeFalse();

    [Fact]
    public void IsValid_WithExcessiveTimeout_ShouldBeFalse() =>
        CoreNatsMonitoringOptions.IsValid(new CoreNatsMonitoringOptions { HttpTimeout = TimeSpan.FromSeconds(61) }).ShouldBeFalse();
}
