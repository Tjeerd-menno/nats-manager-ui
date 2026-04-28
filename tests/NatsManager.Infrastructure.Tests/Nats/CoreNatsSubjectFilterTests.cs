using NatsManager.Infrastructure.Nats;
using Shouldly;

namespace NatsManager.Infrastructure.Tests.Nats;

public sealed class CoreNatsSubjectFilterTests
{
    [Theory]
    [InlineData("")]
    [InlineData("$SYS.REQ.SERVER.PING")]
    [InlineData("_INBOX")]
    [InlineData("_INBOX.reply")]
    public void IsInternalSubject_WithReservedOrEmptySubject_ShouldReturnTrue(string subject)
    {
        // Arrange, Act
        var result = CoreNatsSubjectFilter.IsInternalSubject(subject);

        // Assert
        result.ShouldBeTrue();
    }

    [Theory]
    [InlineData("orders.created")]
    [InlineData("test.e2e.subject")]
    [InlineData("public._INBOX")]
    [InlineData("events.$SYS.visible")]
    public void IsInternalSubject_WithUserSubject_ShouldReturnFalse(string subject)
    {
        // Arrange, Act
        var result = CoreNatsSubjectFilter.IsInternalSubject(subject);

        // Assert
        result.ShouldBeFalse();
    }
}
