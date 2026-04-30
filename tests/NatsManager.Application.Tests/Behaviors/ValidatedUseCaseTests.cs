using FluentValidation;
using NatsManager.Application.Behaviors;
using NatsManager.Application.Common;
using Shouldly;
using NSubstitute;

namespace NatsManager.Application.Tests.Behaviors;

public sealed class ValidatedUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenNoValidatorsAreRegistered_ShouldInvokeInnerUseCase()
    {
        var inner = Substitute.For<IUseCase<TestRequest, string>>();
        var outputPort = new TestOutputPort<string>();
        var request = new TestRequest("valid");
        var sut = new ValidatedUseCase<TestRequest, string>(
            inner,
            Array.Empty<IValidator<TestRequest>>());

        await sut.ExecuteAsync(request, outputPort, CancellationToken.None);

        await inner.Received(1).ExecuteAsync(request, outputPort, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenValidatorsPass_ShouldInvokeInnerUseCase()
    {
        var inner = Substitute.For<IUseCase<TestRequest, string>>();
        var outputPort = new TestOutputPort<string>();
        var request = new TestRequest("valid");
        var sut = new ValidatedUseCase<TestRequest, string>(
            inner,
            new[] { new TestRequestValidator() });

        await sut.ExecuteAsync(request, outputPort, CancellationToken.None);

        await inner.Received(1).ExecuteAsync(request, outputPort, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenValidationFails_ShouldThrowAndNotInvokeInnerUseCase()
    {
        var inner = Substitute.For<IUseCase<TestRequest, string>>();
        var outputPort = new TestOutputPort<string>();
        var request = new TestRequest(string.Empty);
        var sut = new ValidatedUseCase<TestRequest, string>(
            inner,
            new[] { new TestRequestValidator() });

        var act = async () => await sut.ExecuteAsync(request, outputPort, CancellationToken.None);

        var exception = await Should.ThrowAsync<ValidationException>(act);
        exception.Errors.Single().PropertyName.ShouldBe(nameof(TestRequest.Name));
        await inner.DidNotReceive().ExecuteAsync(Arg.Any<TestRequest>(), Arg.Any<IOutputPort<string>>(), Arg.Any<CancellationToken>());
    }

    private sealed record TestRequest(string Name);

    private sealed class TestRequestValidator : AbstractValidator<TestRequest>
    {
        public TestRequestValidator() => RuleFor(request => request.Name).NotEmpty();
    }
}
