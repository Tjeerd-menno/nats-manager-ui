using FluentValidation;
using NatsManager.Application.Behaviors;
using NatsManager.Application.Common;
using NatsManager.Application.Modules.Environments.Ports;
using NatsManager.Domain.Modules.Common;

namespace NatsManager.Application.Modules.Environments.Commands;

public sealed record TestConnectionCommand : IAuditableCommand
{
    public required Guid Id { get; init; }
    internal string? ResolvedName { get; set; }

    ActionType IAuditableCommand.ActionType => ActionType.TestInvoke;
    ResourceType IAuditableCommand.ResourceType => ResourceType.Environment;
    string IAuditableCommand.ResourceId => Id.ToString();
    string IAuditableCommand.ResourceName => ResolvedName ?? Id.ToString();
    Guid? IAuditableCommand.EnvironmentId => Id;
}

public sealed record TestConnectionResult(
    bool Reachable,
    int? LatencyMs,
    string? ServerVersion,
    bool JetStreamAvailable);

public sealed class TestConnectionCommandValidator : AbstractValidator<TestConnectionCommand>
{
    public TestConnectionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class TestConnectionCommandHandler(
    IEnvironmentRepository environmentRepository,
    INatsHealthChecker healthChecker,
    IAuditTrail auditTrail) : IUseCase<TestConnectionCommand, TestConnectionResult>
{
    public async Task ExecuteAsync(TestConnectionCommand request, IOutputPort<TestConnectionResult> outputPort, CancellationToken cancellationToken)
    {
        var environment = await environmentRepository.GetByIdAsync(request.Id, cancellationToken);
        if (environment is null)
        {
            outputPort.NotFound("Environment", request.Id.ToString());
            return;
        }

        request.ResolvedName = environment.Name;

        var result = await healthChecker.CheckHealthAsync(environment, cancellationToken);

        environment.UpdateConnectionStatus(result.Reachable ? ConnectionStatus.Available : ConnectionStatus.Unavailable);
        await environmentRepository.UpdateAsync(environment, cancellationToken);

        await auditTrail.RecordAsync(request, cancellationToken);
        outputPort.Success(result);
    }
}
