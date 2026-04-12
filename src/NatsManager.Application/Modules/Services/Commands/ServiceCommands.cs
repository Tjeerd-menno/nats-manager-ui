using FluentValidation;
using NatsManager.Application.Common;
using NatsManager.Application.Modules.Services.Ports;

namespace NatsManager.Application.Modules.Services.Commands;

public sealed class TestServiceRequestCommand
{
    public Guid EnvironmentId { get; init; }
    public string Subject { get; init; } = string.Empty;
    public string? Payload { get; init; }
}

public sealed class TestServiceRequestCommandValidator : AbstractValidator<TestServiceRequestCommand>
{
    public TestServiceRequestCommandValidator()
    {
        RuleFor(x => x.Subject).NotEmpty();
    }
}

public sealed class TestServiceRequestCommandHandler(IServiceDiscoveryAdapter adapter) : IUseCase<TestServiceRequestCommand, string>
{
    public async Task ExecuteAsync(TestServiceRequestCommand request, IOutputPort<string> outputPort, CancellationToken cancellationToken)
    {
        var result = await adapter.TestServiceRequestAsync(request.EnvironmentId, request.Subject, request.Payload, cancellationToken);
        outputPort.Success(result);
    }
}
