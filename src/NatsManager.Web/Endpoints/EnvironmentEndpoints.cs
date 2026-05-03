using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using NatsManager.Application.Common;
using NatsManager.Application.Modules.Environments.Commands;
using NatsManager.Application.Modules.Environments.Ports;
using NatsManager.Application.Modules.Environments.Queries;
using NatsManager.Domain.Modules.Auth;
using NatsManager.Web.Presenters;
using NatsManager.Web.Security;

namespace NatsManager.Web.Endpoints;

public static class EnvironmentEndpoints
{
    public static IEndpointRouteBuilder MapEnvironmentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/environments")
            .WithTags("Environments")
            .RequireAuthorization();

        group.MapGet("/", GetEnvironments);
        group.MapGet("/{id:guid}", GetEnvironmentDetail);
        group.MapPost("/", RegisterEnvironment).RequireAuthorization(AuthorizationPolicyNames.OperatorAccess);
        group.MapPut("/{id:guid}", UpdateEnvironment).RequireAuthorization(AuthorizationPolicyNames.OperatorAccess);
        group.MapDelete("/{id:guid}", DeleteEnvironment).RequireAuthorization(AuthorizationPolicyNames.OperatorAccess);
        group.MapPost("/{id:guid}/test", TestConnection).RequireAuthorization(AuthorizationPolicyNames.OperatorAccess);
        group.MapPost("/{id:guid}/enable", EnableDisableEnvironment).RequireAuthorization(AuthorizationPolicyNames.OperatorAccess);

        return app;
    }

    private static async Task<IResult> GetEnvironments(
        [AsParameters] GetEnvironmentsQueryParams queryParams,
        IUseCase<GetEnvironmentsQuery, PaginatedResult<EnvironmentListItem>> useCase,
        CancellationToken cancellationToken)
    {
        var query = new GetEnvironmentsQuery
        {
            Page = queryParams.Page ?? 1,
            PageSize = queryParams.PageSize ?? 25,
            SortBy = queryParams.SortBy,
            SortDescending = string.Equals(queryParams.SortOrder, "desc", StringComparison.OrdinalIgnoreCase),
            Search = queryParams.Search
        };

        var presenter = new Presenter<PaginatedResult<EnvironmentListItem>>();
        await useCase.ExecuteAsync(query, presenter, cancellationToken);
        return presenter.ToResult();
    }

    private static async Task<IResult> GetEnvironmentDetail(
        Guid id,
        IUseCase<GetEnvironmentDetailQuery, EnvironmentDetailResult> useCase,
        CancellationToken cancellationToken)
    {
        var presenter = new Presenter<EnvironmentDetailResult>();
        await useCase.ExecuteAsync(new GetEnvironmentDetailQuery(id), presenter, cancellationToken);
        return presenter.ToResult();
    }

    private static async Task<IResult> RegisterEnvironment(
        [FromBody] RegisterEnvironmentRequest request,
        IUseCase<RegisterEnvironmentCommand, RegisterEnvironmentResult> useCase,
        CancellationToken cancellationToken)
    {
        var command = new RegisterEnvironmentCommand
        {
            Name = request.Name,
            Description = request.Description,
            ServerUrl = request.ServerUrl,
            CredentialType = request.CredentialType,
            Credential = request.Credential,
            IsProduction = request.IsProduction
        };

        var presenter = new Presenter<RegisterEnvironmentResult>();
        await useCase.ExecuteAsync(command, presenter, cancellationToken);
        return presenter.ToCreatedResult($"/api/environments/{presenter.Value!.Id}");
    }

    private static async Task<IResult> UpdateEnvironment(
        Guid id,
        [FromBody] UpdateEnvironmentRequest request,
        ClaimsPrincipal user,
        IEnvironmentRepository environmentRepository,
        IUseCase<UpdateEnvironmentCommand, Unit> useCase,
        CancellationToken cancellationToken)
    {
        var environment = await environmentRepository.GetByIdAsync(id, cancellationToken);
        if (environment is not null
            && HasMonitoringUrlChanged(environment.MonitoringUrl, request.MonitoringUrl)
            && !user.IsInRoleForEnvironment(Role.PredefinedNames.Administrator, id))
        {
            return Results.Forbid();
        }

        var command = new UpdateEnvironmentCommand
        {
            Id = id,
            Name = request.Name,
            Description = request.Description,
            ServerUrl = request.ServerUrl,
            CredentialType = request.CredentialType,
            Credential = request.Credential,
            IsProduction = request.IsProduction,
            IsEnabled = request.IsEnabled,
            MonitoringUrl = request.MonitoringUrl,
            MonitoringPollingIntervalSeconds = request.MonitoringPollingIntervalSeconds
        };

        var presenter = new Presenter<Unit>();
        await useCase.ExecuteAsync(command, presenter, cancellationToken);
        return presenter.ToResult();
    }

    private static bool HasMonitoringUrlChanged(string? current, string? requested) =>
        !string.Equals(NormalizeMonitoringUrl(current), NormalizeMonitoringUrl(requested), StringComparison.Ordinal);

    private static string? NormalizeMonitoringUrl(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static async Task<IResult> DeleteEnvironment(
        Guid id,
        IUseCase<DeleteEnvironmentCommand, Unit> useCase,
        CancellationToken cancellationToken)
    {
        var presenter = new Presenter<Unit>();
        await useCase.ExecuteAsync(new DeleteEnvironmentCommand { Id = id }, presenter, cancellationToken);
        return presenter.ToNoContentResult();
    }

    private static async Task<IResult> TestConnection(
        Guid id,
        IUseCase<TestConnectionCommand, TestConnectionResult> useCase,
        CancellationToken cancellationToken)
    {
        var presenter = new Presenter<TestConnectionResult>();
        await useCase.ExecuteAsync(new TestConnectionCommand { Id = id }, presenter, cancellationToken);
        return presenter.ToResult();
    }

    private static async Task<IResult> EnableDisableEnvironment(
        Guid id,
        [FromBody] EnableDisableRequest request,
        IUseCase<EnableDisableEnvironmentCommand, Unit> useCase,
        CancellationToken cancellationToken)
    {
        var command = new EnableDisableEnvironmentCommand
        {
            Id = id,
            Enable = request.Enable
        };

        var presenter = new Presenter<Unit>();
        await useCase.ExecuteAsync(command, presenter, cancellationToken);
        return presenter.ToResult();
    }
}

public sealed record GetEnvironmentsQueryParams
{
    [FromQuery] public int? Page { get; init; }
    [FromQuery] public int? PageSize { get; init; }
    [FromQuery] public string? SortBy { get; init; }
    [FromQuery] public string? SortOrder { get; init; }
    [FromQuery] public string? Search { get; init; }
}

public sealed record RegisterEnvironmentRequest(
    string Name,
    string? Description,
    string ServerUrl,
    Domain.Modules.Common.CredentialType CredentialType,
    string? Credential,
    bool IsProduction);

public sealed record UpdateEnvironmentRequest(
    string Name,
    string? Description,
    string ServerUrl,
    Domain.Modules.Common.CredentialType CredentialType,
    string? Credential,
    bool IsProduction,
    bool IsEnabled,
    string? MonitoringUrl,
    int? MonitoringPollingIntervalSeconds);

public sealed record EnableDisableRequest(bool Enable);
