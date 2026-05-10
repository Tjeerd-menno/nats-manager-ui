using NatsManager.Infrastructure.Nats;
using Shouldly;

namespace NatsManager.Infrastructure.Tests.Nats;

public sealed class NatsMonitoringUptimeParserTests
{
    [Theory]
    [InlineData(null, 0)]
    [InlineData("", 0)]
    [InlineData("7m16s", 436)]
    [InlineData("1h2m3s", 3723)]
    [InlineData("2d3h4m5s", 183845)]
    public void ParseSeconds_WithSupportedUptimeSegments_ShouldReturnTotalSeconds(string? uptime, long expected)
    {
        // Arrange, Act
        var result = NatsMonitoringUptimeParser.ParseSeconds(uptime);

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData("n/a", 0)]
    [InlineData("15", 0)]
    [InlineData("1hbad", 3600)]
    [InlineData("1w2d", 172800)]
    public void ParseSeconds_WithMalformedOrUnsupportedSegments_ShouldReturnParsedSegments(string uptime, long expected)
    {
        // Arrange, Act
        var result = NatsMonitoringUptimeParser.ParseSeconds(uptime);

        // Assert
        result.ShouldBe(expected);
    }
}
