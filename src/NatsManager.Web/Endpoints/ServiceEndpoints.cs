using System.Security.Claims;
using NatsManager.Application.Common;
using NatsManager.Application.Modules.Environments.Ports;
using NatsManager.Application.Modules.Services.Commands;
using NatsManager.Application.Modules.Services.Models;
using NatsManager.Application.Modules.Services.Queries;
using NatsManager.Web.Presenters;
using NatsManager.Web.Security;

namespace NatsManager.Web.Endpoints;

public static class ServiceEndpoints
{
    public static IEndpointRouteBuilder MapServiceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/environments/{envId:guid}/services")
            .WithTags("Services")
            .RequireAuthorization();

        group.MapGet("/", GetServices);
        group.MapGet("/{name}", GetServiceDetail);
        group.MapPost("/{name}/test", TestServiceRequest).RequireAuthorization(AuthorizationPolicyNames.OperatorAccess);

        return app;
    }

    private static async Task<IResult> GetServices(Guid envId, IUseCase<GetServicesQuery, IReadOnlyList<ServiceInfo>> useCase, CancellationToken cancellationToken)
    {
        var presenter = new Presenter<IReadOnlyList<ServiceInfo>>();
        await useCase.ExecuteAsync(new GetServicesQuery(envId), presenter, cancellationToken);
        return presenter.ToResult();
    }

    private static async Task<IResult> GetServiceDetail(Guid envId, string name, IUseCase<GetServiceDetailQuery, ServiceInfo> useCase, CancellationToken cancellationToken)
    {
        var presenter = new Presenter<ServiceInfo>();
        await useCase.ExecuteAsync(new GetServiceDetailQuery(envId, name), presenter, cancellationToken);
        return presenter.ToResult();
    }

    private static async Task<IResult> TestServiceRequest(
        Guid envId,
        string name,
        TestServiceRequestBody body,
        ClaimsPrincipal user,
        IEnvironmentRepository environmentRepository,
        IUseCase<TestServiceRequestCommand, string> useCase,
        CancellationToken cancellationToken)
    {
        var guardResult = await HighImpactActionGuard.RequireAllowedAsync(envId, user, environmentRepository, cancellationToken);
        if (guardResult is not null) return guardResult;

        var presenter = new Presenter<string>();
        await useCase.ExecuteAsync(new TestServiceRequestCommand
        {
            EnvironmentId = envId,
            Subject = body.Subject,
            Payload = body.Payload,
        }, presenter, cancellationToken);
        if (presenter.IsSuccess) return Results.Ok(new { response = presenter.Value });
        return presenter.ToResult();
    }
}

public sealed record TestServiceRequestBody(string Subject, string? Payload);
